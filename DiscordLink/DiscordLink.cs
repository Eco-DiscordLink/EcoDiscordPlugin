using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Eco.Core;
using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;

namespace Eco.Plugins.DiscordLink
{
    public class DiscordLink : IModKitPlugin, IInitializablePlugin, IShutdownablePlugin, IConfigurablePlugin, IGameActionAware
    {
        public readonly Version PluginVersion = new Version(2, 0);

        public event EventHandler OnClientStarted;
        public event EventHandler OnClientStopped;

        public static DiscordLink Obj { get { return PluginManager.GetPlugin<DiscordLink>(); } }
        public ThreadSafeAction<object, string> ParamChanged { get; set; }
        public DiscordClient DiscordClient { get; private set; }
        public const string EchoCommandToken = "[ECHO]";

        private string _status = "No Connection Attempt Made";
        private readonly ChatLogger _chatLogger = new ChatLogger();
        private CommandsNextExtension _commands;
        private Timer _ecoStatusStartupTimer = null;

        public override string ToString()
        {
            return "DiscordLink";
        }

        public string GetStatus()
        {
            return _status;
        }

        public IPluginConfig PluginConfig
        {
            get { return DLConfig.Instance.PluginConfig; }
        }

        public object GetEditObject()
        {
            return DLConfig.Data;
        }

        public void OnEditObjectChanged(object o, string param)
        {
            DLConfig.Instance.HandleConfigChanged();
        }

        public void Initialize(TimedTask timer)
        {
            Logger.Info("Plugin version is " + PluginVersion);

            SetupConfig();
            if (!SetUpClient())
            {
                return;
            }

            ConnectAsync().Wait();

            if (DLConfig.Data.LogChat)
            {
                _chatLogger.Start();
            }
        }

        public void Shutdown()
        {
            if (DLConfig.Data.LogChat)
            {
                _chatLogger.Stop();
            }
        }

        private void SetupConfig()
        {
            DLConfig config = DLConfig.Instance;
            config.Initialize();
            config.OnChatlogEnabled += (obj, args) => { _chatLogger.Start(); };
            config.OnChatlogDisabled += (obj, args) => { _chatLogger.Stop(); };
            config.OnChatlogPathChanged += (obj, args) => { _chatLogger.Restart(); };
            config.OnTokenChanged += (obj, args) =>
            {
                Logger.Info("Discord Bot Token changed - Reinitialising client");
                _ = RestartClient();
            };
            config.OnConfigChanged += (obj, args) =>
            {
                _ecoStatusMessages.Clear(); // The status channels may have changed so we should find the messages again;
            };
        }

        public void ActionPerformed(GameAction action)
        {
            switch (action)
            {
                case ChatSent chatSent:
                    OnMessageReceivedFromEco(chatSent);
                    break;

                case FirstLogin _:
                case Play _:
                    UpdateEcoStatus();
                    break;

                default:
                    break;
            }
        }

        public Result ShouldOverrideAuth(GameAction action)
        {
            return new Result(ResultType.None);
        }

        #region DiscordClient Management

        private bool SetUpClient()
        {
            _status = "Setting up client";

            bool BotTokenIsNull = String.IsNullOrWhiteSpace(DLConfig.Data.BotToken);
            if (BotTokenIsNull)
            {
                DLConfig.Instance.VerifyConfig(DLConfig.VerificationFlags.Static); // Make the user aware of the empty bot token
            }

            if (BotTokenIsNull) return false; // Do not attempt to initialize if the bot token is empty

            try
            {
                // Create the new client
                DiscordClient = new DiscordClient(new DiscordConfiguration
                {
                    AutoReconnect = true,
                    Token = DLConfig.Data.BotToken,
                    TokenType = TokenType.Bot
                });

                DiscordClient.ClientErrored += async args => { Logger.Error("A Discord client error occurred. Error messages was: " + args.EventName + " " + args.Exception.ToString()); };
                DiscordClient.SocketErrored += async args => { Logger.Error("A socket error occurred. Error message was: " + args.Exception.ToString()); };
                DiscordClient.SocketClosed += async args => { Logger.DebugVerbose("Socket Closed: " + args.CloseMessage + " " + args.CloseCode); };
                DiscordClient.Resumed += async args => { Logger.Info("Resumed connection"); };
                DiscordClient.Ready += async args =>
                {
                    DLConfig.Instance.EnqueueFullVerification();

                    // Run EcoStatus once when the server has started
                    _ecoStatusStartupTimer = new Timer(innerArgs =>
                    {
                        _ecoStatusStartupTimer = null;
                        UpdateEcoStatus();
                    }, null, ECO_STATUS_FIRST_UPDATE_DELAY_MS, Timeout.Infinite);
                };

                DiscordClient.GuildAvailable += async args =>
                {
                    DLConfig.Instance.EnqueueGuildVerification();
                };

                // Set up the client to use CommandsNext
                _commands = DiscordClient.UseCommandsNext(new CommandsNextConfiguration
                {
                    StringPrefixes = DLConfig.Data.DiscordCommandPrefix.SingleItemAsEnumerable()
                });
                _commands.RegisterCommands<DiscordDiscordCommands>();

                _ecoStatusUpdateTimer = new Timer(this.UpdateEcoStatusOnTimer, null, 0, ECO_STATUS_TIMER_INTERAVAL_MS);

                OnClientStarted?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while creating the Discord client. Error message: " + e);
            }

            return false;
        }

        void StopClient()
        {
            // Stop various timers that may have been set up so they do not trigger while the reset is ongoing
            DLConfig.Instance.DequeueAllVerification();
            SystemUtil.StopAndDestroyTimer(ref _ecoStatusStartupTimer);
            SystemUtil.StopAndDestroyTimer(ref _ecoStatusUpdateTimer);
            
            // Clear all the stored message references as they may become invalid if the token has changed
            _ecoStatusMessages.Clear();

            if (DiscordClient != null)
            {
                StopRelaying();

                // If DisconnectAsync() is called in the GUI thread, it will cause a deadlock
                SystemUtil.SynchronousThreadExecute(() =>
                {
                   DiscordClient.DisconnectAsync().Wait();
                });
                DiscordClient.Dispose();
                DiscordClient = null;

                OnClientStopped?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task<bool> RestartClient()
        {
            StopClient();
            bool result = SetUpClient();
            if (result)
            {
                await ConnectAsync();
            }
            return result;
        }

        public async Task<object> ConnectAsync()
        {
            try
            {
                _status = "Attempting connection...";
                await DiscordClient.ConnectAsync();
                BeginRelaying();
                Logger.Info("Connected to Discord");
                _status = "Connection successful";
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred when connecting to Discord: Error message: " + e.Message);
                _status = "Connection failed";
            }

            return null;
        }

        public async Task<object> DisconnectAsync()
        {
            try
            {
                StopRelaying();
                await DiscordClient.DisconnectAsync();
            }
            catch (Exception e)
            {
                Logger.Error("An Error occurred when disconnecting from Discord: Error message: " + e.Message);
                _status = "Connection failed";
            }

            return null;
        }

        #endregion

        #region Discord Guild Access

        public string[] GuildNames => DiscordClient.GuildNames();
        public DiscordGuild DefaultGuild => DiscordClient.DefaultGuild();

        public DiscordGuild GuildByName(string name)
        {
            return DiscordClient?.Guilds.Values.FirstOrDefault(guild => guild.Name?.ToLower() == name.ToLower());
        }

        public DiscordGuild GuildByNameOrId(string nameOrId)
        {
            var maybeGuildId = DSharpExtensions.TryParseSnowflakeId(nameOrId);
            return maybeGuildId != null ? DiscordClient.Guilds[maybeGuildId.Value] : GuildByName(nameOrId);
        }

        #endregion

        #region Message Sending

        public async Task<string> SendDiscordMessage(string message, string channelNameOrId, string guildNameOrId)
        {
            if (DiscordClient == null) return "No discord client";

            var guild = GuildByNameOrId(guildNameOrId);
            if (guild == null) return "No guild of that name found";

            var channel = guild.ChannelByNameOrId(channelNameOrId);
            await DiscordUtil.SendAsync(channel, message);
            return "Message sent";
        }

        public async Task<string> SendDiscordMessageAsUser(string message, User user, string channelNameOrId, string guildNameOrId)
        {
            var guild = GuildByNameOrId(guildNameOrId);
            if (guild == null) return "No guild of that name found";

            var channel = guild.ChannelByNameOrId(channelNameOrId);
            if (channel == null) return "No channel of that name or ID found in that guild";
            await DiscordUtil.SendAsync(channel, MessageUtil.FormatMessageForDiscord(message, channel, user.Name));
            return "Message sent";
        }

        public async Task<String> SendDiscordMessageAsUser(string message, User user, DiscordChannel channel)
        {
            await DiscordUtil.SendAsync(channel, MessageUtil.FormatMessageForDiscord(message, channel, user.Name));
            return "Message sent";
        }

        #endregion

        #region Message Relaying

        private const string EcoUserSteamId = "DiscordLinkSteam";
        private const string EcoUserSlgId = "DiscordLinkSlg";
        private const string EcoUserName = "Discord";

        private User _ecoUser;
        public User EcoUser => _ecoUser ??= UserManager.GetOrCreateUser(EcoUserSteamId, EcoUserSlgId, EcoUserName);

        private void BeginRelaying()
        {
            ActionUtil.AddListener(this);
            DiscordClient.MessageCreated += OnDiscordMessageCreateEvent;
        }

        private void StopRelaying()
        {
            ActionUtil.RemoveListener(this);
            DiscordClient.MessageCreated -= OnDiscordMessageCreateEvent;
        }

        private ChatChannelLink GetLinkForEcoChannel(string discordChannelNameOrId)
        {
            return DLConfig.Data.ChatChannelLinks.FirstOrDefault(link => link.DiscordChannel == discordChannelNameOrId);
        }

        private ChatChannelLink GetLinkForDiscordChannel(string ecoChannelName)
        {
            var lowercaseEcoChannelName = ecoChannelName.ToLower();
            return DLConfig.Data.ChatChannelLinks.FirstOrDefault(link => link.EcoChannel.ToLower() == lowercaseEcoChannelName);
        }

        public void LogEcoMessage(ChatSent chatMessage)
        {
            Logger.DebugVerbose("Eco Message Processed:");
            Logger.DebugVerbose("Message: " + chatMessage.Message);
            Logger.DebugVerbose("Tag: " + chatMessage.Tag);
            Logger.DebugVerbose("Sender: " + chatMessage.Citizen);
        }

        public void LogDiscordMessage(DiscordMessage message)
        {
            Logger.DebugVerbose("Discord Message Processed");
            Logger.DebugVerbose("Message: " + message.Content);
            Logger.DebugVerbose("Channel: " + message.Channel.Name);
            Logger.DebugVerbose("Sender: " + message.Author);
        }

        public void OnMessageReceivedFromEco(ChatSent chatMessage)
        {
            LogEcoMessage(chatMessage);

            // Ignore messages sent by our bot
            if (chatMessage.Citizen.Name == EcoUser.Name && !chatMessage.Message.StartsWith(EchoCommandToken))
            {
                return;
            }

            // Remove the # character from the start.
            var channelLink = GetLinkForDiscordChannel(chatMessage.Tag.Substring(1));
            var channel = channelLink?.DiscordChannel;
            var guild = channelLink?.DiscordGuild;

            if (!String.IsNullOrWhiteSpace(channel) && !String.IsNullOrWhiteSpace(guild))
            {
                ForwardMessageToDiscordChannel(chatMessage, channel, guild);
            }
        }

        public async Task OnDiscordMessageCreateEvent(MessageCreateEventArgs messageArgs)
        {
            OnMessageReceivedFromDiscord(messageArgs.Message);
        }

        public void OnMessageReceivedFromDiscord(DiscordMessage message)
        {
            LogDiscordMessage(message);
            if (message.Author == DiscordClient.CurrentUser) { return; }
            if (message.Content.StartsWith( DLConfig.Data.DiscordCommandPrefix)) { return; }

            var channelLink = GetLinkForEcoChannel(message.Channel.Name) ?? GetLinkForEcoChannel(message.Channel.Id.ToString());
            var channel = channelLink?.EcoChannel;
            if (!String.IsNullOrWhiteSpace(channel))
            {
                ForwardMessageToEcoChannel(message, channel);
            }
        }

        private async void ForwardMessageToEcoChannel(DiscordMessage message, string ecoChannel)
        {
            Logger.DebugVerbose("Sending Discord message to Eco channel: " + ecoChannel);
            ChatManager.SendChat(MessageUtil.FormatMessageForEco(message, ecoChannel), EcoUser);

            if (DLConfig.Data.LogChat)
            {
                _chatLogger.Write(message);
            }
        }

        private void ForwardMessageToDiscordChannel(ChatSent chatMessage, string channelNameOrId, string guildNameOrId)
        {
            Logger.DebugVerbose("Sending Eco message to Discord channel " + channelNameOrId + " in guild " + guildNameOrId);
            var guild = GuildByNameOrId(guildNameOrId);
            if (guild == null)
            {
                Logger.Error("Failed to forward Eco message from user " + MessageUtil.StripEcoTags(chatMessage.Citizen.Name) + " as no guild with the name or ID " + guildNameOrId + " exists");
                return;
            }
            var channel = guild.ChannelByNameOrId(channelNameOrId);
            if (channel == null)
            {
                Logger.Error("Failed to forward Eco message from user " + MessageUtil.StripEcoTags(chatMessage.Citizen.Name) + " as no channel with the name or ID " + channelNameOrId + " exists in the guild " + guild.Name);
                return;
            }

            _ = DiscordUtil.SendAsync(channel, MessageUtil.FormatMessageForDiscord(chatMessage.Message, channel, chatMessage.Citizen.Name));

            if (DLConfig.Data.LogChat)
            {
                _chatLogger.Write(chatMessage);
            }
        }

        #endregion

        #region EcoStatus
        private Timer _ecoStatusUpdateTimer = null;
        private const int ECO_STATUS_TIMER_INTERAVAL_MS = 60000;
        private const int ECO_STATUS_FIRST_UPDATE_DELAY_MS = 20000;
        private readonly Dictionary<EcoStatusChannel, ulong> _ecoStatusMessages = new Dictionary<EcoStatusChannel, ulong>();

        private void UpdateEcoStatusOnTimer(Object stateInfo)
        {
            UpdateEcoStatus();
        }

        private void UpdateEcoStatus()
        {
            if (DiscordClient == null) return;
            foreach (EcoStatusChannel statusChannel in DLConfig.Data.EcoStatusDiscordChannels)
            {
                DiscordGuild discordGuild = DiscordClient.GuildByName(statusChannel.DiscordGuild);
                if (discordGuild == null) continue;
                DiscordChannel discordChannel = discordGuild.ChannelByName(statusChannel.DiscordChannel);
                if (discordChannel == null) continue;

                if (!DiscordUtil.ChannelHasPermission(discordChannel, Permissions.ReadMessageHistory)) continue;
                bool HasEmbedPermission = DiscordUtil.ChannelHasPermission(discordChannel, Permissions.EmbedLinks);

                DiscordMessage ecoStatusMessage = null;
                bool created = false;
                if (_ecoStatusMessages.TryGetValue(statusChannel, out ulong statusMessageID))
                {
                    try
                    {
                        ecoStatusMessage = discordChannel.GetMessageAsync(statusMessageID).Result;
                    }
                    catch (System.AggregateException)
                    {
                        _ecoStatusMessages.Remove(statusChannel); // The message has been removed, take it out of the list
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error occurred when attempting to read message with ID " + statusMessageID + " from channel \"" + discordChannel.Name + "\". Error message: " + e);
                        continue;
                    }
                }
                else
                {
                    IReadOnlyList<DiscordMessage> ecoStatusChannelMessages = DiscordUtil.GetMessagesAsync(discordChannel).Result;
                    if (ecoStatusChannelMessages == null) continue;

                    foreach (DiscordMessage message in ecoStatusChannelMessages)
                    {
                        // We assume that it's our status message if it has parts of our string in it
                        if (message.Author == DiscordClient.CurrentUser
                            && (HasEmbedPermission ? (message.Embeds.Count == 1 && message.Embeds[0].Title.Contains("Live Server Status**")) : message.Content.Contains("Live Server Status**")))
                        {
                            ecoStatusMessage = message;
                            break;
                        }
                    }

                    // If we couldn't find a status message, create a new one
                    if (ecoStatusMessage == null)
                    {
                        ecoStatusMessage = DiscordUtil.SendAsync(discordChannel, null, MessageBuilder.GetEcoStatus(GetEcoStatusFlagForChannel(statusChannel), isLiveMessage: true)).Result;
                        created = true;
                    }

                    if (ecoStatusMessage != null) // SendAsync may return null in case an exception is raised
                    {
                        _ecoStatusMessages.Add(statusChannel, ecoStatusMessage.Id);
                    }
                }

                if (ecoStatusMessage != null && !created) // It is pointless to update the message if it was just created
                {
                    _ = DiscordUtil.ModifyAsync(ecoStatusMessage, "", MessageBuilder.GetEcoStatus(GetEcoStatusFlagForChannel(statusChannel), isLiveMessage: true));
                }
            }
        }

        private static MessageBuilder.EcoStatusComponentFlag GetEcoStatusFlagForChannel(EcoStatusChannel statusChannel)
        {
            MessageBuilder.EcoStatusComponentFlag statusFlag = 0;
            if (statusChannel.UseName)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.Name;
            if (statusChannel.UseDescription)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.Description;
            if (statusChannel.UseLogo)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.Logo;
            if (statusChannel.UseAddress)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.ServerAddress;
            if (statusChannel.UsePlayerCount)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.PlayerCount;
            if (statusChannel.UsePlayerList)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.PlayerList;
            if (statusChannel.UseTimeSinceStart)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.TimeSinceStart;
            if (statusChannel.UseTimeRemaining)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.TimeRemaining;
            if (statusChannel.UseMeteorHasHit)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.MeteorHasHit;

            return statusFlag;
        }

        #endregion

        #region Player Configs

        public DiscordPlayerConfig GetOrCreatePlayerConfig(string identifier)
        {
            var config = DLConfig.Data.PlayerConfigs.FirstOrDefault(user => user.Username == identifier);
            if (config == null)
            {
                config = new DiscordPlayerConfig
                {
                    Username = identifier
                };
                AddOrReplacePlayerConfig(config);
            }

            return config;
        }

        public bool AddOrReplacePlayerConfig(DiscordPlayerConfig config)
        {
            var playerConfigs = DLConfig.Data.PlayerConfigs;
            var removed = playerConfigs.Remove(config);
            playerConfigs.Add(config);
            DLConfig.Instance.Save();
            return removed;
        }

        public DiscordChannel GetDefaultChannelForPlayer(string identifier)
        {
            var playerConfig = GetOrCreatePlayerConfig(identifier);
            if (playerConfig.DefaultChannel == null
                || String.IsNullOrEmpty(playerConfig.DefaultChannel.Guild)
                || String.IsNullOrEmpty(playerConfig.DefaultChannel.Channel))
            {
                return null;
            }

            return GuildByName(playerConfig.DefaultChannel.Guild).ChannelByName(playerConfig.DefaultChannel.Channel);
        }

        public void SetDefaultChannelForPlayer(string identifier, string guildName, string channelName)
        {
            var playerConfig = GetOrCreatePlayerConfig(identifier);
            playerConfig.DefaultChannel.Guild = guildName;
            playerConfig.DefaultChannel.Channel = channelName;
            DLConfig.Instance.Save();
        }

        #endregion
    }
}