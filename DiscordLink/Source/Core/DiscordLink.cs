using DSharpPlus.Entities;
using Eco.Core;
using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Core.Utils.Logging;
using Eco.EM.Framework.VersioningTools;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Players;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Modules;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.WorldGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Module = Eco.Plugins.DiscordLink.Modules.Module;

namespace Eco.Plugins.DiscordLink
{
    [Priority(PriorityAttribute.High)] // Need to start before WorldGenerator in order to listen for world generation finished event
    public class DiscordLink : IModKitPlugin, IInitializablePlugin, IShutdownablePlugin, IConfigurablePlugin, IDisplayablePlugin, IGameActionAware, ICommandablePlugin
    {
        public readonly Version PluginVersion = Assembly.GetExecutingAssembly().GetName().Version;

        public static DiscordLink Obj { get { return PluginManager.GetPlugin<DiscordLink>(); } }
        public DLDiscordClient Client { get; private set; } = new DLDiscordClient();
        public List<Module> Modules { get; private set; } = new List<Module>();
        public User EcoUser { get; private set; } = null;
        public IPluginConfig PluginConfig { get { return DLConfig.Instance.PluginConfig; } }
        public ThreadSafeAction<object, string> ParamChanged { get; set; }
        public DateTime InitTime { get; private set; } = DateTime.MinValue;
        public bool CanRestart { get; private set; } = false; // False to start with as we cannot restart while the initial startup is in progress

        private const string ModIOAppID = "77";
        private const string ModIODeveloperToken = ""; // This will always be empty for all but actual release builds.

        private bool _triggerWorldResetEvent = false;

        public string Status
        {
            get { return _status; }
            private set
            {
                Logger.Debug($"Plugin status changed from \"{_status}\" to \"{value}\"");
                _status = value;
            }
        }
        private string _status = "Uninitialized";
        private Timer _activityUpdateTimer = null;

        #region Plugin Management

        public override string ToString()
        {
            return "DiscordLink";
        }

        public string GetStatus()
        {
            return Status;
        }

        public LazyResult ShouldOverrideAuth(GameAction action)
        {
            return LazyResult.FailedNoMessage;
        }

        public object GetEditObject()
        {
            return DLConfig.Data;
        }

        public void OnEditObjectChanged(object o, string param)
        {
            _ = DLConfig.Instance.HandleConfigChanged();
        }

        public string GetDisplayText()
        {
            return MessageBuilder.Shared.GetDisplayStringAsync(DLConfig.Data.UseVerboseDisplay).Result;
        }

        public void Initialize(TimedTask timer)
        {
            DLConfig.Instance.Initialize();
            EventConverter.Instance.Initialize();
            DLStorage.Instance.Initialize();
            Status = "Initializing";
            InitTime = DateTime.Now;

            WorldGeneratorPlugin.OnFinishGenerate.AddUnique(this.HandleWorldReset);
            PluginManager.Controller.RunIfOrWhenInited(PostServerInitialize); // Defer some initialization for when the server initialization is completed

            // Start the Discord client so that a connection has hopefully been established before the server is done initializing
            _ = Client.Start();

            // Check mod versioning if the required data exists
            if (!string.IsNullOrWhiteSpace(ModIOAppID) && !string.IsNullOrWhiteSpace(ModIODeveloperToken))
                ModVersioning.GetModInit(ModIOAppID, ModIODeveloperToken, "DiscordLink", "DiscordLink", ConsoleColor.Cyan, "DiscordLink");
            else
                Logger.Info($"Plugin version is {PluginVersion}");
        }

        private void PostServerInitialize()
        {
            Status = "Performing post server start initialization";

            // Ensure that the bot Eco user exists (Needs to be done after Initialize() as it runs before the UserManager is initialized)
            EcoUser = UserManager.Users.FirstOrDefault(u => u.SlgId == DLConstants.ECO_USER_SLG_ID && u.SteamId == DLConstants.ECO_USER_STEAM_ID);
            if (EcoUser == null)
                EcoUser = UserManager.GetOrCreateUser(DLConstants.ECO_USER_STEAM_ID, DLConstants.ECO_USER_SLG_ID, !string.IsNullOrWhiteSpace(DLConfig.Data.EcoBotName) ? DLConfig.Data.EcoBotName : DLConfig.DefaultValues.EcoBotName);

            if (string.IsNullOrEmpty(DLConfig.Data.BotToken) || Client.ConnectionStatus != DLDiscordClient.ConnectionState.Connected)
            {
                Status = "Initialization aborted";
                Client.OnConnected.Add(HandleClientConnected);
                if (!string.IsNullOrEmpty(DLConfig.Data.BotToken))
                    Logger.Error("Discord client did not connect before server initialization was completed. Use restart commands to make a new connection attempt");

                CanRestart = true;
                return;
            }

            HandleClientConnected();

            // Set up callbacks
            UserManager.OnNewUserJoined.Add(user => HandleEvent(DLEventType.Join, user));
            UserManager.OnUserLoggedIn.Add(user => HandleEvent(DLEventType.Login, user));
            UserManager.OnUserLoggedOut.Add(user => HandleEvent(DLEventType.Logout, user));
            Election.OnElectionStarted.Add(election => HandleEvent(DLEventType.StartElection, election));
            Election.OnElectionFinished.Add(election => HandleEvent(DLEventType.StopElection, election));
            EventConverter.OnEventFired += (sender, args) => HandleEvent(args.EventType, args.Data);
            UserLinkManager.OnLinkedUserVerified += (sender, args) => HandleEvent(DLEventType.AccountLinkVerified, args);
            UserLinkManager.OnLinkedUserRemoved += (sender, args) => HandleEvent(DLEventType.AccountLinkRemoved, args);
            ClientLogEventTrigger.OnLogWritten += (message) => EventConverter.Instance.ConvertServerLogEvent(message);

            if(_triggerWorldResetEvent)
            {
                HandleEvent(DLEventType.WorldReset, null);
                _triggerWorldResetEvent = false;
            }

            HandleEvent(DLEventType.ServerStarted, null);
        }

        public Task ShutdownAsync()
        {
            Status = "Shutting down";

            HandleEvent(DLEventType.ServerStopped, null);

            ShutdownModules();
            EventConverter.Instance.Shutdown();
            DLStorage.Instance.Shutdown();
            return Task.CompletedTask;
        }

        public void GetCommands(Dictionary<string, Action> nameToFunction)
        {
            nameToFunction.Add("Verify Config", () => { Logger.Info($"Config Verification Report:\n{MessageBuilder.Shared.GetConfigVerificationReport()}"); });
            nameToFunction.Add("Verify Permissions", () =>
            {
                if (Client.ConnectionStatus == DLDiscordClient.ConnectionState.Connected)
                    Logger.Info($"Permission Verification Report:\n{MessageBuilder.Shared.GetPermissionsReport(MessageBuilder.PermissionReportComponentFlag.All)}");
                else
                    Logger.Error("Failed to verify permissions - Discord client not connected");
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

            DLConfig.Instance.PostConnectionInitialize();
            if (Client.Guild == null)
            {
                Status = "Discord Server connection failed";
                CanRestart = true;
                return;
            }

            InitializeModules();
            ActionUtil.AddListener(this);
            _activityUpdateTimer = new Timer(TriggerActivityStringUpdate, null, DLConstants.DISCORD_ACTIVITY_STRING_UPDATE_INTERVAL_MS, DLConstants.DISCORD_ACTIVITY_STRING_UPDATE_INTERVAL_MS);
            Client.OnDisconnecting.Add(HandleClientDisconnecting);
            HandleEvent(DLEventType.DiscordClientConnected);

            Status = "Connected and running";
            Logger.Info("Connection Successful - DiscordLink Running");
            CanRestart = true;
        }

        private void HandleClientDisconnecting()
        {
            Client.OnDisconnecting.Remove(HandleClientDisconnecting);

            SystemUtils.StopAndDestroyTimer(ref _activityUpdateTimer);
            ActionUtil.RemoveListener(this);
            ShutdownModules();
            Client.OnConnected.Add(HandleClientConnected);

            Status = "Disconnected";
        }

        public void ActionPerformed(GameAction action)
        {
            switch (action)
            {
                case ChatSent chatSent:
                    Logger.DebugVerbose($"Eco Message Received\n{chatSent.FormatForLog()}");

                    // Ignore commands and messages sent by our bot
                    if (chatSent.Citizen.Name == EcoUser.Name && !chatSent.Message.StartsWith(DLConstants.ECHO_COMMAND_TOKEN))
                        return;

                    HandleEvent(DLEventType.EcoMessageSent, chatSent);
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

                case DemographicChange demographicChange:
                    DLEventType type = demographicChange.Entered == Shared.Items.EnteredOrLeftDemographic.EnteringDemographic
                        ? DLEventType.EnteredDemographic
                        : DLEventType.LeftDemographic;
                    HandleEvent(type, demographicChange);
                    break;

                case GainSpecialty gainSpecialty:
                    HandleEvent(DLEventType.GainedSpecialty, gainSpecialty);
                    break;

                default:
                    break;
            }
        }

        public void HandleEvent(DLEventType eventType, params object[] data)
        {
            Logger.DebugVerbose($"Event of type {eventType} received");

            EventConverter.Instance.HandleEvent(eventType, data);
            DLStorage.Instance.HandleEvent(eventType, data);
            UserLinkManager.HandleEvent(eventType, data);
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

        private void InitializeModules()
        {
            Status = "Initializing modules";

            Modules.Add(new DiscordChatFeed());   // Discord -> Eco
            Modules.Add(new EcoChatFeed());       // Eco -> Discord
            Modules.Add(new TradeFeed());
            Modules.Add(new CraftingFeed());
            Modules.Add(new ServerStatusFeed());
            Modules.Add(new PlayerStatusFeed());
            Modules.Add(new ElectionFeed());
            Modules.Add(new ServerInfoDisplay());
            Modules.Add(new WorkPartyDisplay());
            Modules.Add(new ElectionDisplay());
            Modules.Add(new CurrencyDisplay());
            Modules.Add(new TradeWatcherDisplay());
            Modules.Add(new TradeWatcherFeed());
            Modules.Add(new SnippetInput());
            Modules.Add(new RoleCleanupModule());
            Modules.Add(new AccountLinkRoleModule());
            Modules.Add(new DemographicsRoleModule());
            Modules.Add(new SpecialtiesRoleModule());
            Modules.Add(new ServerLogFeed());

            Modules.ForEach(module => module.Setup());
            Modules.ForEach(async module => await module.HandleStartOrStop());
        }

        private void ShutdownModules()
        {
            Status = "Shutting down modules";

            Modules.ForEach(async module => await module.Stop());
            Modules.ForEach(module => module.Destroy());
            Modules.Clear();
        }

        private void UpdateModules(DLEventType trigger, params object[] data)
        {
            Modules.ForEach(async module => await module.Update(this, trigger, data));
        }

        private void TriggerActivityStringUpdate(object stateInfo)
        {
            UpdateActivityString(DLEventType.Timer);
        }

        private void UpdateActivityString(DLEventType trigger)
        {
            try
            {
                if (Client.ConnectionStatus != DLDiscordClient.ConnectionState.Connected
                    || (trigger & (DLEventType.Join | DLEventType.Login | DLEventType.Logout | DLEventType.Timer)) == 0)
                    return;

                Client.DiscordClient.UpdateStatusAsync(new DiscordActivity(MessageBuilder.Discord.GetActivityString(), ActivityType.Watching));
            }
            catch(Exception e)
            {
                Logger.Error($"An error occured while attempting to update the activity string. Error: {e}");
            }
        }

        #endregion
    }
}
