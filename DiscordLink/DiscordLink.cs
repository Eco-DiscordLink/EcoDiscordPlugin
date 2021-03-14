using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Eco.Core;
using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Players;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Modules;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using Eco.WorldGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    [Priority(PriorityAttribute.High)] // Need to start before WorldGenerator in order to listen for world generation finished event
    public class DiscordLink : IModKitPlugin, IInitializablePlugin, IShutdownablePlugin, IConfigurablePlugin, IDisplayablePlugin, IGameActionAware
    {
        public readonly Version PluginVersion = new Version(2, 2, 1);

        private string _status = "Not yet started";
        private CommandsNextExtension _commands = null;

        public event EventHandler OnClientStarted;
        public event EventHandler OnClientStopped;

        public static DiscordLink Obj { get { return PluginManager.GetPlugin<DiscordLink>(); } }
        public DiscordClient DiscordClient { get; private set; }
        public List<Module> Modules { get; private set; } = new List<Module>();
        public IPluginConfig PluginConfig { get { return DLConfig.Instance.PluginConfig; } }
        public ThreadSafeAction<object, string> ParamChanged { get; set; }
        public DateTime InitTime { get; private set; } = DateTime.MinValue;
        public DateTime LastConnectionTime { get; private set; } = DateTime.MinValue;
        public bool DiscordConnected { get; private set; } = false;
        public bool CanRestart { get; private set; } = false; // False to start with as we cannot restart while the initial startup is in progress

        public override string ToString()
        {
            return "DiscordLink";
        }

        public string GetStatus()
        {
            return _status;
        }

        public object GetEditObject()
        {
            return DLConfig.Data;
        }

        public void OnEditObjectChanged(object o, string param)
        {
            DLConfig.Instance.HandleConfigChanged();
        }

        public string GetDisplayText()
        {
#if DEBUG
            bool debug = true;
#else
            bool debug = false;
#endif
            return MessageBuilder.Shared.GetDisplayString(verbose: debug);
        }

        public void Initialize(TimedTask timer)
        {
            DLConfig.Instance.Initialize();
            EventConverter.Instance.Initialize();
            DLStorage.Instance.Initialize();
            Logger.Initialize();
            Logger.Debug("Plugin Initializing");
            Logger.Info($"Plugin version is {PluginVersion}");
            InitTime = DateTime.Now;

            _ = StartClient();

            WorldGeneratorPlugin.OnFinishGenerate.AddUnique(this.HandleWorldReset);
            PluginManager.Controller.RunIfOrWhenInited(PostServerInitialize); // Defer some initialization for when the server initialization is completed.
        }

        private void PostServerInitialize()
        {
            Logger.Debug("Plugin Post Eco Server Init");

            if (!DiscordConnected)
                return;

            DLConfig.Instance.VerifyConfig(DLConfig.VerificationFlags.ChannelLinks | DLConfig.VerificationFlags.BotData);

            _ = EcoUser; // Create the Eco User on startup

            PostClientConnected();

            // Set up callbacks
            UserManager.OnNewUserJoined.Add(user => HandleEvent(DLEventType.Join, user));
            UserManager.OnUserLoggedIn.Add(user => HandleEvent(DLEventType.Login, user));
            UserManager.OnUserLoggedOut.Add(user => HandleEvent(DLEventType.Logout, user));
            Election.OnElectionStarted.Add(election => HandleEvent(DLEventType.StartElection, election));
            Election.OnElectionFinished.Add(election => HandleEvent(DLEventType.StopElection, election));
            EventConverter.OnEventFired += (sender, args) => HandleEvent(args.EventType, args.Data);

            HandleEvent(DLEventType.ServerStarted, null);
        }

        private void PostClientConnected()
        {
            Logger.Debug("Plugin Post Client Conencted");

            // Start modules
            InitializeModules();
            BeginRelaying();
            HandleEvent(DLEventType.DiscordClientStarted, null);

            _status = "Connected and running";
            CanRestart = true;
        }

        public void Shutdown()
        {
            Logger.Debug("Plugin Shutting Down");

            HandleEvent(DLEventType.ServerStopped, null);

            ShutdownModules();
            EventConverter.Instance.Shutdown();
            DLStorage.Instance.Shutdown();
            Logger.Shutdown();
        }

        public void ActionPerformed(GameAction action)
        {
            switch (action)
            {
                case ChatSent chatSent:
                    OnMessageReceivedFromEco(chatSent);
                    break;
            
                case CurrencyTrade currencyTrade:
                    HandleEvent(DLEventType.Trade, currencyTrade);
                    break;

                case WorkOrderAction workOrderAction:
                    HandleEvent(DLEventType.WorkOrderCreated, workOrderAction);
                    break;
            
                case PostedWorkParty postedWorkParty:
                    HandleEvent(DLEventType.PostedWorkParty, postedWorkParty);
                    break;
            
                case CompletedWorkParty completedWorkParty:
                    HandleEvent(DLEventType.CompletedWorkParty, completedWorkParty);
                    break;
            
                case JoinedWorkParty joinedWorkParty:
                    HandleEvent(DLEventType.JoinedWorkParty, joinedWorkParty);
                    break;
            
                case LeftWorkParty leftWorkParty:
                    HandleEvent(DLEventType.LeftWorkParty, leftWorkParty);
                    break;
            
                case WorkedForWorkParty workedParty:
                    HandleEvent(DLEventType.WorkedWorkParty, workedParty);
                    break;

                case Vote vote:
                    HandleEvent(DLEventType.Vote, vote);
                    break;

                case CreateCurrency createCurrency:
                    HandleEvent(DLEventType.CurrencyCreated, createCurrency);
                    break;

                default:
                    break;
            }
        }

        public Result ShouldOverrideAuth(GameAction action)
        {
            return new Result(ResultType.None);
        }

        public void HandleEvent(DLEventType eventType, object data)
        {
            Logger.DebugVerbose($"Event of type {eventType} received");

            EventConverter.Instance.HandleEvent(eventType, data);
            DLStorage.Instance.HandleEvent(eventType, data);
            UpdateModules(eventType, data);
        }

        public void HandleWorldReset()
        {
            Logger.Info("New world generated - Removing storage data for previous world");
            DLStorage.Instance.Reset();
        }

        #region DiscordClient Management

        private async Task<bool> StartClient()
        {
            Logger.Debug("Plugin Starting Discord Client");
            _status = "Setting up Discord client";

            bool BotTokenIsNull = string.IsNullOrWhiteSpace(DLConfig.Data.BotToken);
            if (BotTokenIsNull)
            {
                DLConfig.Instance.VerifyConfig(DLConfig.VerificationFlags.Static); // Make the user aware of the empty bot token
                return false; // Do not attempt to initialize if the bot token is empty
            }    

            try
            {
                // Create the new client
                DiscordClient = new DiscordClient(new DiscordConfiguration
                {
                    AutoReconnect = true,
                    Token = DLConfig.Data.BotToken,
                    TokenType = TokenType.Bot,
                    MinimumLogLevel = DLConfig.Data.BackendLogLevel
                });

                DiscordClient.ClientErrored += async (client, args) => { Logger.Debug("A Discord client error occurred. Error messages was: " + args.EventName + " " + args.Exception.ToString()); };
                DiscordClient.SocketErrored += async (client, args) => { Logger.Debug("A socket error occurred. Error message was: " + args.Exception.ToString()); };
                DiscordClient.SocketClosed += async (client, args) => { Logger.DebugVerbose("Socket Closed: " + args.CloseMessage + " " + args.CloseCode); };
                DiscordClient.Resumed += async (client, args) => { Logger.Debug("Resumed connection"); };

                DiscordClient.MessageDeleted += async (client, args) =>
                {
                    Modules.ForEach(async module => await module.OnMessageDeleted(args.Message));
                };

                // Register Discord commands
                _commands = DiscordClient.UseCommandsNext(new CommandsNextConfiguration
                {
                    StringPrefixes = DLConfig.Data.DiscordCommandPrefix.SingleItemAsEnumerable()
                });
                _commands.RegisterCommands<DiscordCommands>();
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while creating the Discord client. Error message: " + e);
                _status = "Failed to create Discord client";
                return false;
            }

            try
            {
                await ConnectAsync();
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while connecting to Discord. Error message: " + e);
                _status = "Failed to connect to Discord";
                return false;
            }

            OnClientStarted?.Invoke(this, EventArgs.Empty);

            return true;
        }

        private void StopClient()
        {
            Logger.Debug("Plugin Stopping Discord Client");
            _status = "Shutting down";

            ShutdownModules();

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
            Logger.Debug("Plugin Restarting Discord Client");

            bool result = false;
            if (CanRestart)
            {
                CanRestart = false;
                StopClient();
                result = await StartClient();

                if (result)
                    PostClientConnected();
                else
                    CanRestart = true; // If the client setup failed, enable restarting, otherwise we should wait for the callbacks from Discord to fire
            }
            return result;
        }

        public async Task ConnectAsync()
        {
            try
            {
                _status = "Attempting Discord connection...";
                await DiscordClient.ConnectAsync();
                DiscordConnected = true;
                Logger.Info("Connected to Discord");
                _status = "Discord connection successful";
                LastConnectionTime = DateTime.Now;
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred when connecting to Discord: Error message: " + e.Message);
                _status = "Discord connection failed";
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                await DiscordClient.DisconnectAsync();
                DiscordConnected = false;
                _status = "Disconnected from Discord";
            }
            catch (Exception e)
            {
                Logger.Error("An Error occurred when disconnecting from Discord: Error message: " + e.Message);
                _status = "Discord connection failed";
            }
        }

        #endregion

        #region Module Management

        private void InitializeModules()
        {
            Logger.Debug("Initializing modules");

            Modules.Add(new DiscordChatFeed());   // Discord -> Eco
            Modules.Add(new EcoChatFeed());       // Eco -> Discord
            Modules.Add(new ChatlogFeed());
            Modules.Add(new TradeFeed());
            Modules.Add(new CraftingFeed());
            Modules.Add(new ServerStatusFeed());
            Modules.Add(new PlayerStatusFeed());
            Modules.Add(new ElectionFeed());
            Modules.Add(new ServerInfoDisplay());
            Modules.Add(new WorkPartyDisplay());
            Modules.Add(new PlayerDisplay());
            Modules.Add(new ElectionDisplay());
            Modules.Add(new CurrencyDisplay());
            Modules.Add(new TradeTrackerDisplay());
            Modules.Add(new SnippetInput());

            Modules.ForEach(module => module.Setup());
            Modules.ForEach(async module => await module.HandleStartOrStop());
        }

        private void ShutdownModules()
        {
            Logger.Debug("Shutting down modules");

            Modules.ForEach(async module => await module.Stop());
            Modules.ForEach(module => module.Destroy());
            Modules.Clear();
        }

        private void UpdateModules(DLEventType trigger, object data)
        {
            Modules.ForEach(async module => await module.Update(this, trigger, data));
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

        public async Task<string> SendDiscordMessageAsUser(string message, User user, DiscordChannel channel, bool allowGlobalMentions = false)
        {
            await DiscordUtil.SendAsync(channel, MessageUtil.FormatMessageForDiscord(message, channel, user.Name, allowGlobalMentions));
            return "Message sent";
        }

        #endregion

        #region Message Relaying

        private const string EcoUserSteamId = "DiscordLinkSteam";
        private const string EcoUserSlgId = "DiscordLinkSlg";

        private User _ecoUser;
        public User EcoUser => _ecoUser ??= UserManager.GetOrCreateUser(EcoUserSteamId, EcoUserSlgId, !string.IsNullOrWhiteSpace(DLConfig.Data.EcoBotName) ? DLConfig.Data.EcoBotName : DLConfig.DefaultValues.EcoBotName);

        private void BeginRelaying()
        {
            Logger.Debug("Relaying Started");

            ActionUtil.AddListener(this);
            DiscordClient.MessageCreated += OnDiscordMessageCreateEvent;
        }

        private void StopRelaying()
        {
            Logger.Debug("Relaying stopped");

            ActionUtil.RemoveListener(this);
            DiscordClient.MessageCreated -= OnDiscordMessageCreateEvent;
        }

        public ChatChannelLink GetLinkForEcoChannel(string discordChannelNameOrId)
        {
            return DLConfig.Data.ChatChannelLinks.FirstOrDefault(link => link.DiscordChannel == discordChannelNameOrId);
        }

        public ChatChannelLink GetLinkForDiscordChannel(string ecoChannelName)
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
            if (chatMessage.Citizen.Name == EcoUser.Name && !chatMessage.Message.StartsWith(DLConstants.ECHO_COMMAND_TOKEN))
                return;

            HandleEvent(DLEventType.EcoMessage, chatMessage);
        }

        public async Task OnDiscordMessageCreateEvent(DiscordClient client, MessageCreateEventArgs messageArgs)
        {
            OnMessageReceivedFromDiscord(messageArgs.Message);
        }

        public void OnMessageReceivedFromDiscord(DiscordMessage message)
        {
            LogDiscordMessage(message);

            // Ignore commands and messages sent by our bot
            if (message.Author == DiscordClient.CurrentUser) return;
            if (message.Content.StartsWith(DLConfig.Data.DiscordCommandPrefix)) return;

            UpdateModules(DLEventType.DiscordMessage, message);
        }
        #endregion
    }
}
