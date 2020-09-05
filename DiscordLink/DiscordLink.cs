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
        public readonly Version PluginVersion = new Version(2, 0, 1);

        private const int FIRST_DISPLAY_UPDATE_DELAY_MS = 20000;

        public event EventHandler OnClientStarted;
        public event EventHandler OnClientStopped;
        public event EventHandler OnDiscordMaybeReady;

        public static DiscordLink Obj { get { return PluginManager.GetPlugin<DiscordLink>(); } }
        public ThreadSafeAction<object, string> ParamChanged { get; set; }
        public DiscordClient DiscordClient { get; private set; }
        public const string EchoCommandToken = "[ECHO]";

        private string _status = "No Connection Attempt Made";
        private readonly ChatLogger _chatLogger = new ChatLogger();
        private CommandsNextExtension _commands;
        private Timer _discordDataMaybeAvailable = null;
        private Timer _tradePostingTimer = null;
        private List<Display> _displays = new List<Display>();

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

            // Triggered on a timer that starts when the Discord client connects.
            // It is likely that the client object has fetched all the relevant data, but there are not guarantees.
            OnDiscordMaybeReady += (obj, args) =>
            {
                InitializeDisplays();
                UpdateDisplays(DisplayTriggerType.Startup);
                _ = UpdateSnippets();

                _tradePostingTimer = new Timer(InnerArgs =>
                {
                    _ = PostAccumulatedTrades();
                }, null, 0, TRADE_POSTING_INTERVAL_MS);
            };
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
                _displays.ForEach(display => display.OnConfigChanged());
                _ = UpdateSnippets();
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
                    UpdateDisplays(DisplayTriggerType.Login);
                    break;

                case CurrencyTrade CurrencyTrade:
                    StoreTradeEvent(CurrencyTrade);
                    break;

                default:
                    break;
            }
        }

        public Result ShouldOverrideAuth(GameAction action)
        {
            return new Result(ResultType.None);
        }

        void InitializeDisplays()
        {
            _displays.Add(new EcoStatusDisplay());

            _displays.ForEach(display => display.StartTimer());
        }

        void DestroyDisplays()
        {
            _displays.Clear();
        }
        
        void UpdateDisplays(DisplayTriggerType trigger)
        {
            _displays.ForEach(display => display.Update(this, trigger));
        }

        

        #region DiscordClient Management

        private bool SetUpClient()
        {
            _status = "Setting up client";

            bool BotTokenIsNull = string.IsNullOrWhiteSpace(DLConfig.Data.BotToken);
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

                    _discordDataMaybeAvailable = new Timer(innerArgs =>
                    {
                        OnDiscordMaybeReady?.Invoke(this, EventArgs.Empty);
                        SystemUtil.StopAndDestroyTimer(ref _discordDataMaybeAvailable);
                    }, null, FIRST_DISPLAY_UPDATE_DELAY_MS, Timeout.Infinite);
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
            SystemUtil.StopAndDestroyTimer(ref _discordDataMaybeAvailable);
            SystemUtil.StopAndDestroyTimer(ref _tradePostingTimer);

            DestroyDisplays();

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

        public async Task<string> SendDiscordMessageAsUser(string message, User user, string channelNameOrId, string guildNameOrId, bool allowGlobalMentions = false)
        {
            var guild = GuildByNameOrId(guildNameOrId);
            if (guild == null) return "No guild of that name found";

            var channel = guild.ChannelByNameOrId(channelNameOrId);
            if (channel == null) return "No channel of that name or ID found in that guild";
            await DiscordUtil.SendAsync(channel, MessageUtil.FormatMessageForDiscord(message, channel, user.Name, allowGlobalMentions));
            return "Message sent";
        }

        public async Task<String> SendDiscordMessageAsUser(string message, User user, DiscordChannel channel, bool allowGlobalMentions = false)
        {
            await DiscordUtil.SendAsync(channel, MessageUtil.FormatMessageForDiscord(message, channel, user.Name, allowGlobalMentions));
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

            // Ignore commands and messages sent by our bot
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
                ForwardMessageToDiscordChannel(chatMessage, channel, guild, channelLink.HereAndEveryoneMentionPermission);
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
            if (message.Content.StartsWith(DLConfig.Data.DiscordCommandPrefix)) { return; }

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

        private void ForwardMessageToDiscordChannel(ChatSent chatMessage, string channelNameOrId, string guildNameOrId, GlobalMentionPermission globalMentionPermission)
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

            bool allowGlobalMention = (globalMentionPermission == GlobalMentionPermission.AnyUser
                || globalMentionPermission == GlobalMentionPermission.Admin && chatMessage.Citizen.IsAdmin);

            _ = DiscordUtil.SendAsync(channel, MessageUtil.FormatMessageForDiscord(chatMessage.Message, channel, chatMessage.Citizen.Name, allowGlobalMention));

            if (DLConfig.Data.LogChat)
            {
                _chatLogger.Write(chatMessage);
            }
        }

        #endregion

        #region Snippets

        public Dictionary<string, string> Snippets { get; private set; } = new Dictionary<string, string>();

        private async Task UpdateSnippets()
        {
            foreach (ChannelLink snippetChannel in DLConfig.Data.SnippetChannels)
            {
                if (DiscordClient == null) return;
                DiscordGuild discordGuild = DiscordClient.GuildByName(snippetChannel.DiscordGuild);
                if (discordGuild == null) continue;
                DiscordChannel discordChannel = discordGuild.ChannelByName(snippetChannel.DiscordChannel);
                if (discordChannel == null) continue;
                if (!DiscordUtil.ChannelHasPermission(discordChannel, Permissions.ReadMessageHistory)) continue;

                IReadOnlyList<DiscordMessage> snippetChannelMessages = DiscordUtil.GetMessagesAsync(discordChannel).Result;
                if (snippetChannelMessages == null) continue;

                Snippets.Clear();
                foreach (DiscordMessage message in snippetChannelMessages)
                {
                    Match match = MessageUtil.SnippetRegex.Match(message.Content);
                    if (match.Groups.Count == 3)
                    {
                        Snippets.Add(match.Groups[1].Value.ToLower(), match.Groups[2].Value);
                    }
                }
            }
        }

        #endregion

        #region Trade Channels

        private const int TRADE_POSTING_INTERVAL_MS = 1000;
        private readonly Dictionary<Tuple<int, int>, List<CurrencyTrade>> _accumulatedTrades = new Dictionary<Tuple<int, int>, List<CurrencyTrade>>();

        private void StoreTradeEvent(CurrencyTrade tradeEvent)
        {
            bool citizenIsBuyer = (tradeEvent.Citizen.Id == tradeEvent.Buyer.Id);
            Tuple<int, int> iDTuple = new Tuple<int, int>(tradeEvent.Citizen.Id, citizenIsBuyer ? tradeEvent.Seller.Id : tradeEvent.Buyer.Id);
            _accumulatedTrades.TryGetValue(iDTuple, out List<CurrencyTrade> trades);
            if (trades == null)
            {
                trades = new List<CurrencyTrade>();
                _accumulatedTrades.Add(iDTuple, trades);
            }

            trades.Add(tradeEvent);
        }

        private async Task PostAccumulatedTrades()
        {
            foreach (List<CurrencyTrade> accumulatedTrades in _accumulatedTrades.Values)
            {
                if (accumulatedTrades.Count <= 0) continue;

                CurrencyTrade firstTrade = accumulatedTrades[0];

                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                string leftName = firstTrade.Citizen.Name;
                string rightName = firstTrade.Citizen.Id == firstTrade.Buyer.Id ? firstTrade.Seller.Name : firstTrade.Buyer.Name;
                builder.Title = leftName + " traded with " + rightName;

                string boughtItemsDesc = string.Empty;
                float boughtTotal = 0;
                string soldItemsDesc = string.Empty;
                float soldTotal = 0;
                foreach (CurrencyTrade trade in accumulatedTrades)
                {
                    if (trade.BoughtOrSold == Shared.Items.BoughtOrSold.Buying)
                    {
                        boughtItemsDesc += trade.NumberOfItems + " X " + trade.ItemUsed.DisplayName + " * " + trade.CurrencyAmount / trade.NumberOfItems + " = " + trade.CurrencyAmount + "\n";
                        boughtTotal += trade.CurrencyAmount;
                    }
                    else if (trade.BoughtOrSold == Shared.Items.BoughtOrSold.Selling)
                    {
                        soldItemsDesc += trade.NumberOfItems + " X " + trade.ItemUsed.DisplayName + " * " + trade.CurrencyAmount / trade.NumberOfItems + " = " + trade.CurrencyAmount + "\n";
                        soldTotal += trade.CurrencyAmount;
                    }
                }

                if (!boughtItemsDesc.IsEmpty())
                {
                    boughtItemsDesc += "\nTotal = " + boughtTotal.ToString("n2");
                    builder.AddField("Bought", boughtItemsDesc);
                }

                if (!soldItemsDesc.IsEmpty())
                {
                    soldItemsDesc += "\nTotal = " + soldTotal.ToString("n2");
                    builder.AddField("Sold", soldItemsDesc);
                }

                float subTotal = soldTotal - boughtTotal;
                char sign = (subTotal > 0.0f ? '+' : '-');
                builder.AddField("Total", sign + Math.Abs(subTotal).ToString("n2") + " " + firstTrade.Currency.Name);

                foreach (ChannelLink tradeChannel in DLConfig.Data.TradeChannels)
                {
                    if (DiscordClient == null) return;
                    DiscordGuild discordGuild = DiscordClient.GuildByName(tradeChannel.DiscordGuild);
                    if (discordGuild == null) continue;
                    DiscordChannel discordChannel = discordGuild.ChannelByName(tradeChannel.DiscordChannel);
                    if (discordChannel == null) continue;

                    _ = DiscordUtil.SendAsync(discordChannel, "", builder.Build());
                }
            }
            _accumulatedTrades.Clear();
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
