using Eco.Core;
using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.EM.Framework.VersioningTools;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Players;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Modules;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.WorldGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Module = Eco.Plugins.DiscordLink.Modules.Module;

namespace Eco.Plugins.DiscordLink
{
    [Priority(PriorityAttribute.High)] // Need to start before WorldGenerator in order to listen for world generation finished event
    public class DiscordLink : IModKitPlugin, IInitializablePlugin, IShutdownablePlugin, IConfigurablePlugin, IDisplayablePlugin, IGameActionAware
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

        private const string ModIOAppID = "";
        private const string ModIODeveloperToken = ""; // This will always be empty for all but actual release builds.

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

        #region Plugin Management

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
            ParamChanged?.Invoke(o, param);
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
            Status = "Initializing";
            Logger.Info($"Plugin version is {PluginVersion}");
            InitTime = DateTime.Now;

            WorldGeneratorPlugin.OnFinishGenerate.AddUnique(this.HandleWorldReset);
            PluginManager.Controller.RunIfOrWhenInited(PostServerInitialize); // Defer some initialization for when the server initialization is completed

            // Start the Discord client so that a connection has hopefully been established before the server is done initializing
            _ = Client.Start().Result;

            // Ensure that the bot Eco user exists
            EcoUser = UserManager.Users.FirstOrDefault(u => u.SlgId == DLConstants.ECO_USER_SLG_ID && u.SteamId == DLConstants.ECO_USER_STEAM_ID);
            if (EcoUser == null)
                EcoUser = UserManager.CreateNewUser(DLConstants.ECO_USER_STEAM_ID, DLConstants.ECO_USER_SLG_ID, !string.IsNullOrWhiteSpace(DLConfig.Data.EcoBotName) ? DLConfig.Data.EcoBotName : DLConfig.DefaultValues.EcoBotName);

            // Check mod versioning if the required data exists
            if (!string.IsNullOrWhiteSpace(ModIOAppID) && !string.IsNullOrWhiteSpace(ModIODeveloperToken))
                ModVersioning.GetModInit(ModIOAppID, ModIODeveloperToken, "DiscordLink", "DiscordLink", ConsoleColor.Cyan, "DiscordLink");
        }

        private void PostServerInitialize()
        {
            if (!Client.Connected)
            {
                Status = "Initialization aborted";
                Logger.Error("Discord client did not connect before server initialization was completed. Use restart commands to make a new connection attempt");
                Client.OnConnected.Add(HandleClientConnected);
                return;
            }

            Status = "Performing post server start initialization";

            DLConfig.Instance.VerifyConfig(DLConfig.VerificationFlags.ChannelLinks | DLConfig.VerificationFlags.BotData);

            HandleClientConnected();

            // Set up callbacks
            UserManager.OnNewUserJoined.Add(user => HandleEvent(DLEventType.Join, user));
            UserManager.OnUserLoggedIn.Add(user => HandleEvent(DLEventType.Login, user));
            UserManager.OnUserLoggedOut.Add(user => HandleEvent(DLEventType.Logout, user));
            Election.OnElectionStarted.Add(election => HandleEvent(DLEventType.StartElection, election));
            Election.OnElectionFinished.Add(election => HandleEvent(DLEventType.StopElection, election));
            EventConverter.OnEventFired += (sender, args) => HandleEvent(args.EventType, args.Data);

            HandleEvent(DLEventType.ServerStarted, null);
        }

        public void Shutdown()
        {
            Status = "Shutting down";

            HandleEvent(DLEventType.ServerStopped, null);

            ShutdownModules();
            EventConverter.Instance.Shutdown();
            DLStorage.Instance.Shutdown();
            Logger.Shutdown();
        }

        public async Task<bool> Restart()
        {
            Logger.Debug("Plugin restarting");

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
            InitializeModules();
            ActionUtil.AddListener(this);
            Client.OnDisconnecting.Add(HandleClientDisconnecting);

            Status = "Connected and running";
            CanRestart = true;
        }

        private void HandleClientDisconnecting()
        {
            Client.OnDisconnecting.Remove(HandleClientDisconnecting);

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

                default:
                    break;
            }
        }

        public Result ShouldOverrideAuth(GameAction action)
        {
            return new Result(ResultType.None);
        }

        public void HandleEvent(DLEventType eventType, params object[] data)
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
            Status = "Shutting down modules";

            Modules.ForEach(async module => await module.Stop());
            Modules.ForEach(module => module.Destroy());
            Modules.Clear();
        }

        private void UpdateModules(DLEventType trigger, params object[] data)
        {
            Modules.ForEach(async module => await module.Update(this, trigger, data));
        }

        #endregion
    }
}
