using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Eco.Core;
using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Moose.Tools;
using Eco.Gameplay.Aliases;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Players;
using Eco.Gameplay.Property;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Modules;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using Eco.WorldGenerator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Module = Eco.Plugins.DiscordLink.Modules.Module;

namespace Eco.Plugins.DiscordLink
{
    [Priority(PriorityAttribute.High)] // Need to start before WorldGenerator in order to listen for world generation finished event
    public class DiscordLink : IModKitPlugin, IInitializablePlugin, IShutdownablePlugin, IConfigurablePlugin, IDisplayablePlugin, IGameActionAware, ICommandablePlugin
    {
        public readonly Version InstalledVersion = Assembly.GetExecutingAssembly().GetName().Version;
        public Version? ModIOVersion = null;

        public static DiscordLink Obj { get { return PluginManager.GetPlugin<DiscordLink>(); } }
        public DiscordClient Client { get; private set; } = new DiscordClient();
        public Module[] Modules { get; private set; } = new Module[Enum.GetNames(typeof(ModuleType)).Length];
        public IPluginConfig PluginConfig { get { return DLConfig.Instance.PluginConfig; } }
        public ThreadSafeAction<object, string> ParamChanged { get; set; }
        public DateTime InitTime { get; private set; } = DateTime.MinValue;
        public bool CanRestart { get; private set; } = false; // False to start with as we cannot restart while the initial startup is in progress

        private const string ModIOAppID = "77";
        private const string ModIODeveloperToken = ""; // This will always be empty for all but actual release builds.

        private bool _triggerWorldResetEvent = false;

        private Action<User> OnNewUserJoined;
        private Action<User> OnNewUserLoggedIn;
        private Action<User> OnUserLoggedOut;
        private Action<Election> OnElectionStarted;
        private Action<Election> OnElectionFinished;
        private Action<DLEventArgs> OnEventConverted;
        private EventHandler<LinkedUser> OnLinkedUserVerified;
        private EventHandler<LinkedUser> OnLinkedUserRemoved;

        public override string ToString() => "DiscordLink";
        public string GetCategory() => "DiscordLink";
        public string GetStatus() => _statusDescription;
        public object GetEditObject() => DLConfig.Data;
        public void OnEditObjectChanged(object o, string param) => _ = DLConfig.Instance.HandleConfigChanged();
        public LazyResult ShouldOverrideAuth(IAlias alias, IOwned property, GameAction action) => LazyResult.FailedNoMessage;

        public StatusState Status
        {
            get { return _status; }
            private set
            {
                Logger.Debug($"Plugin status changed from \"{_status}\" to \"{value}\"");
                _status = value;
                _statusDescription = Moose.Utils.Text.GetEnumDescription(value);
            }
        }
        private StatusState _status = StatusState.Uninitialized;
        private string _statusDescription = Moose.Utils.Text.GetEnumDescription(StatusState.Uninitialized);
        public enum StatusState
        {
            [Description("Uninitialized")]
            Uninitialized,

            [Description("Initializing plugin")]
            InitializingPlugin,

            [Description("Initializing modules")]
            InitializingModules,

            [Description("Initialization aborted")]
            InitializationAborted,

            [Description("Awaiting guild download")]
            AwaitingGuildDownload,

            [Description("Performing post server init")]
            PostServerInit,

            [Description("Shutting down plugin")]
            ShuttingDownPlugin,

            [Description("Shutting down modules")]
            ShuttingDownModules,

            [Description("Connected and running")]
            Connected,

            [Description("Discord server connection failed")]
            ServerConnectionFailed,

            [Description("Disconnected")]
            Disconnected,
        }

        private Timer _activityUpdateTimer = null;

        #region Plugin Management

        public string GetDisplayText()
        {
            try
            {
                return MessageBuilder.Shared.GetDisplayStringAsync(DLConfig.Data.UseVerboseDisplay).Result;
            }
            catch (ServerErrorException e)
            {
                Logger.Exception($"Failed to get status display string", e);
                return "Failed to generate status string";
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to get status display string", e);
                return "Failed to generate status string";
            }
        }

        public async void Initialize(TimedTask timer)
        {
            InitCallbacks();
            DLConfig.Instance.Initialize();
            Logger.RegisterLogger("DiscordLink", ConsoleColor.Cyan, DLConfig.Data.LogLevel);
            Status = StatusState.InitializingPlugin;
            InitTime = DateTime.Now;

            EventConverter.Instance.Initialize();
            DLStorage.Instance.Initialize();

            WorldGeneratorPlugin.OnFinishGenerate.AddUnique(this.HandleWorldReset);
            
            // Start the Discord client so that a connection has hopefully been established before the server is done initializing
            _ = Client.Start();
            
            PluginManager.Controller.RunIfOrWhenInited(PostServerInitialize); // Defer some initialization for when the server initialization is completed

            // Check mod versioning if the required data exists
            if (!string.IsNullOrWhiteSpace(ModIOAppID) && !string.IsNullOrWhiteSpace(ModIODeveloperToken))
                ModIOVersion = await VersionChecker.CheckVersion("DiscordLink", ModIOAppID, ModIODeveloperToken);
            else
                Logger.Info($"Plugin version is {InstalledVersion.ToString(3)}");
        }

        private async void PostServerInitialize()
        {
            Status = StatusState.AwaitingGuildDownload;

            if (string.IsNullOrEmpty(DLConfig.Data.BotToken))
            {
                HandleDiscordConnectionFailed("Failed to start DiscordLink: Missing BotToken.");
                return;
            }

            // The Server is Started at this point, but the guild download might not have been completed yet, so we wait for it.
            await DelayPostInitUntilConnectionAttemptIsCompleted();
            
            if (Client.ConnectionStatus == DiscordClient.ConnectionState.Disconnected)
            {
                HandleDiscordConnectionFailed("Failed to start DiscordLink. See previous errors for more Information.");
                return;
            }
            CanRestart = true;

            Status = StatusState.PostServerInit;
            HandleClientConnected();

            if (_triggerWorldResetEvent)
            {
                await HandleEvent(DLEventType.WorldReset, null);
                _triggerWorldResetEvent = false;
            }

            await HandleEvent(DLEventType.ServerStarted, null);
        }

        private void HandleDiscordConnectionFailed(string message)
        {
            Status = StatusState.InitializationAborted;
            Client.OnConnected.Add(HandleClientConnected);
            Logger.Error(message);
            CanRestart = true;
        }

        private async Task DelayPostInitUntilConnectionAttemptIsCompleted()
        {
            int connectingTimePassed = 0;
            while (Client.ConnectionStatus == DiscordClient.ConnectionState.Connecting)
            {
                connectingTimePassed++;
                await Task.Delay(1000);

                // Warn after 5 seconds, then every minute.
                if (connectingTimePassed == 5 || connectingTimePassed % 60 == 0)
                {
                    Logger.Info("DiscordLink is still trying to connect to Discord.. If this message keeps appearing, make sure your BotToken is correct and discord.com is reachable from your Server.");
                }
            }
        }


        public async Task ShutdownAsync()
        {
            Status = StatusState.ShuttingDownPlugin;

            await HandleEvent(DLEventType.ServerStopped, null);
            ShutdownModules();
            EventConverter.Instance.Shutdown();
            DLStorage.Instance.Shutdown();
        }

        public void GetCommands(Dictionary<string, Action> nameToFunction)
        {
            nameToFunction.Add("Verify Config", () => { Logger.Info($"Config Verification Report:\n{MessageBuilder.Shared.GetConfigVerificationReport()}"); });
            nameToFunction.Add("Verify Permissions", () =>
            {
                if (Client.ConnectionStatus == DiscordClient.ConnectionState.Connected)
                    Logger.Info($"Permission Verification Report:\n{MessageBuilder.Shared.GetPermissionsReport(MessageBuilder.PermissionReportComponentFlag.All)}");
                else
                    Logger.Error("Failed to verify permissions - Discord client not connected");
            });
            nameToFunction.Add("Force Update", async () =>
            {
                if (Client.ConnectionStatus != DiscordClient.ConnectionState.Connected)
                {
                    Logger.Info("Failed to force update - Disoord client not connected");
                    return;
                }

                Modules.ForEach(async module => await module.HandleStartOrStop());
                await HandleEvent(DLEventType.ForceUpdate);
                Logger.Info("Forced update");
            });
            nameToFunction.Add("Restart Plugin", () =>
            {
                if (CanRestart)
                {
                    Logger.Info("DiscordLink Restarting...");
                    _ = Restart();
                }
                else
                {
                    Logger.Info("Could not restart - The plugin is not in a ready state.");
                }
            });
        }

        public async Task<bool> Restart()
        {
            Logger.Debug("Attempting plugin restart");

            bool result = false;
            if (CanRestart)
            {
                CanRestart = false;
                result = await Client.Restart();
                if (!result)
                    CanRestart = true; // If the client setup failed, enable restarting, otherwise we should wait for the callbacks from Discord to fire
            }
            return result;
        }

        private void HandleClientConnected()
        {
            Client.OnConnected.Remove(HandleClientConnected);

            DLConstants.PostConnectionInit();

            DLConfig.Instance.PostConnectionInit();
            if (Client.Guild == null)
            {
                Status = StatusState.ServerConnectionFailed;
                CanRestart = true;
                return;
            }

            UserLinkManager.Initialize();
            InitializeModules();

            RegisterCallbacks();
            ActionUtil.AddListener(this);
            _activityUpdateTimer = new Timer(TriggerActivityStringUpdate, null, DLConstants.DISCORD_ACTIVITY_STRING_UPDATE_INTERVAL_MS, DLConstants.DISCORD_ACTIVITY_STRING_UPDATE_INTERVAL_MS);
            Client.OnDisconnecting.Add(HandleClientDisconnecting);
            _ = HandleEvent(DLEventType.DiscordClientConnected);

            Status = StatusState.Connected;
            Logger.Info("Connection Successful - DiscordLink Running");
            CanRestart = true;
        }

        private void HandleClientDisconnecting()
        {
            Client.OnDisconnecting.Remove(HandleClientDisconnecting);
            DeregisterCallbacks();

            SystemUtils.StopAndDestroyTimer(ref _activityUpdateTimer);
            ActionUtil.RemoveListener(this);
            ShutdownModules();
            Client.OnConnected.Add(HandleClientConnected);

            Status = StatusState.Disconnected;
        }

        public void ActionPerformed(GameAction action)
        {
            switch (action)
            {
                case ChatSent chatSent:
                    Logger.DebugVerbose($"Eco Message Received\n{chatSent.FormatForLog()}");
                    _ = HandleEvent(DLEventType.EcoMessageSent, chatSent);
                    break;

                case CurrencyTrade currencyTrade:
                    _ = HandleEvent(DLEventType.Trade, currencyTrade);
                    break;

                case CreateWorkOrder createWorkOrderAction:
                    _ = HandleEvent(DLEventType.WorkOrderCreated, createWorkOrderAction);
                    break;

                case PostedWorkParty postedWorkParty:
                    _ = HandleEvent(DLEventType.PostedWorkParty, postedWorkParty);
                    break;

                case CompletedWorkParty completedWorkParty:
                    _ = HandleEvent(DLEventType.CompletedWorkParty, completedWorkParty);
                    break;

                case JoinedWorkParty joinedWorkParty:
                    _ = HandleEvent(DLEventType.JoinedWorkParty, joinedWorkParty);
                    break;

                case LeftWorkParty leftWorkParty:
                    _ = HandleEvent(DLEventType.LeftWorkParty, leftWorkParty);
                    break;

                case WorkedForWorkParty workedParty:
                    _ = HandleEvent(DLEventType.WorkedWorkParty, workedParty);
                    break;

                case Vote vote:
                    _ = HandleEvent(DLEventType.Vote, vote);
                    break;

                case CreateCurrency createCurrency:
                    _ = HandleEvent(DLEventType.CurrencyCreated, createCurrency);
                    break;

                case DemographicChange demographicChange:
                    DLEventType type = demographicChange.Entered == Shared.Items.EnteredOrLeftDemographic.EnteringDemographic
                        ? DLEventType.EnteredDemographic
                        : DLEventType.LeftDemographic;
                    _ = HandleEvent(type, demographicChange);
                    break;

                case GainSpecialty gainSpecialty:
                    _ = HandleEvent(DLEventType.GainedSpecialty, gainSpecialty);
                    break;

                default:
                    break;
            }
        }

        public async Task HandleEvent(DLEventType eventType, params object[] data)
        {
            Logger.DebugVerbose($"Event of type {eventType} received");

            EventConverter.Instance.HandleEvent(eventType, data);
            DLStorage.Instance.HandleEvent(eventType, data);
            await UserLinkManager.HandleEvent(eventType, data);
            UpdateModules(eventType, data);
            UpdateActivityString(eventType);
        }

        public void HandleWorldReset()
        {
            Logger.Info("New world generated - Removing storage data for previous world");
            DLStorage.Instance.ResetWorldData();
            _triggerWorldResetEvent = true;
        }

        #endregion

        #region Module Management

        private async void InitializeModules()
        {
            Status = StatusState.InitializingModules;

            Modules[(int)ModuleType.CurrencyDisplay] = new CurrencyDisplay();
            Modules[(int)ModuleType.ElectionDisplay] = new ElectionDisplay();
            Modules[(int)ModuleType.ServerInfoDisplay] = new ServerInfoDisplay();
            Modules[(int)ModuleType.TradeWatcherDisplay] = new TradeWatcherDisplay();
            Modules[(int)ModuleType.WorkPartyDisplay] = new WorkPartyDisplay();
            Modules[(int)ModuleType.CraftingFeed] = new CraftingFeed();
            Modules[(int)ModuleType.DiscordChatFeed] = new DiscordChatFeed();
            Modules[(int)ModuleType.EcoChatFeed] = new EcoChatFeed();
            Modules[(int)ModuleType.ElectionFeed] = new ElectionFeed();
            Modules[(int)ModuleType.PlayerStatusFeed] = new PlayerStatusFeed();
            Modules[(int)ModuleType.ServerLogFeed] = new ServerLogFeed();
            Modules[(int)ModuleType.ServerStatusFeed] = new ServerStatusFeed();
            Modules[(int)ModuleType.TradeFeed] = new TradeFeed();
            Modules[(int)ModuleType.TradeWatcherFeed] = new TradeWatcherFeed();
            Modules[(int)ModuleType.AccountLinkRoleModule] = new AccountLinkRoleModule();
            Modules[(int)ModuleType.DemographicRoleModule] = new DemographicsRoleModule();
            Modules[(int)ModuleType.RoleCleanupModule] = new RoleCleanupModule();
            Modules[(int)ModuleType.SpecialitiesRoleModule] = new SpecialtiesRoleModule();
            Modules[(int)ModuleType.SnippetInput] = new SnippetInput();

            foreach (Module module in Modules)
            {
                module.Setup();
            }
            foreach (Module module in Modules)
            {
                await module.HandleStartOrStop();
            }
        }

        private async void ShutdownModules()
        {
            Status = StatusState.ShuttingDownModules;

            foreach (Module module in Modules.NonNull())
            {
                await module.Stop();
            }
            foreach (Module module in Modules.NonNull())
            {
                module.Destroy();
            }
            Modules = new Module[Enum.GetNames(typeof(ModuleType)).Length];
        }

        private async void UpdateModules(DLEventType trigger, params object[] data)
        {
            foreach (Module module in Modules.NonNull())
            {
                try
                {
                    await module.Update(this, trigger, data);
                }
                catch (Exception e)
                {
                    Logger.Exception($"An error occurred while updating module: {module}", e);
                }
            }
        }

        private void TriggerActivityStringUpdate(object stateInfo)
        {
            UpdateActivityString(DLEventType.Timer);
        }

        private void UpdateActivityString(DLEventType trigger)
        {
            try
            {
                if (Client.ConnectionStatus != DiscordClient.ConnectionState.Connected
                    || (trigger & (DLEventType.Join | DLEventType.Login | DLEventType.Logout | DLEventType.Timer)) == 0)
                    return;

                Client.DSharpClient.UpdateStatusAsync(new DiscordActivity(MessageBuilder.Discord.GetActivityString(), ActivityType.Watching));
            }
            catch (Exception e)
            {
                Logger.Exception($"An error occured while attempting to update the activity string", e);
            }
        }

        #endregion

        private void InitCallbacks()
        {
            OnNewUserJoined = async user => await HandleEvent(DLEventType.Join, user);
            OnNewUserLoggedIn = async user => await HandleEvent(DLEventType.Login, user);
            OnUserLoggedOut = async user => await HandleEvent(DLEventType.Logout, user);
            OnElectionStarted = async election => await HandleEvent(DLEventType.StartElection, election);
            OnElectionFinished = async election => await HandleEvent(DLEventType.StopElection, election);
            OnEventConverted = async args => await HandleEvent(args.EventType, args.Data);
            OnLinkedUserVerified = async (sender, args) => await HandleEvent(DLEventType.AccountLinkVerified, args);
            OnLinkedUserRemoved = async (sender, args) => await HandleEvent(DLEventType.AccountLinkRemoved, args);
        }

        private void RegisterCallbacks()
        {
            UserManager.NewUserJoinedEvent.Add(OnNewUserJoined);
            UserManager.OnUserLoggedIn.Add(OnNewUserLoggedIn);
            UserManager.OnUserLoggedOut.Add(OnUserLoggedOut);
            Election.ElectionStartedEvent.Add(OnElectionStarted);
            Election.ElectionFinishedEvent.Add(OnElectionFinished);
            EventConverter.OnEventConverted.Add(OnEventConverted);
            UserLinkManager.OnLinkedUserVerified += OnLinkedUserVerified;
            UserLinkManager.OnLinkedUserRemoved += OnLinkedUserRemoved;
        }

        private void DeregisterCallbacks()
        {
            UserManager.NewUserJoinedEvent.Remove(OnNewUserJoined);
            UserManager.OnUserLoggedIn.Remove(OnNewUserLoggedIn);
            UserManager.OnUserLoggedOut.Remove(OnUserLoggedOut);
            Election.ElectionStartedEvent.Remove(OnElectionStarted);
            Election.ElectionFinishedEvent.Remove(OnElectionFinished);
            EventConverter.OnEventConverted.Remove(OnEventConverted);
            UserLinkManager.OnLinkedUserVerified -= OnLinkedUserVerified;
            UserLinkManager.OnLinkedUserRemoved -= OnLinkedUserRemoved;
        }
    }
}