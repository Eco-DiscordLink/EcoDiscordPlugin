﻿using DiscordLink.Source.Utilities;
using Eco.Moose.Data;
using Eco.Moose.Tools.Logger;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.Networking;
using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public enum ModuleType
    {
        CurrencyDisplay,
        ElectionDisplay,
        ServerInfoDisplay,
        TradeWatcherDisplay,
        WorkPartyDisplay,
        MapDisplay,
        LayerDisplay,
        CraftingFeed,
        DiscordChatFeed,
        EcoChatFeed,
        ElectionFeed,
        PlayerStatusFeed,
        ServerLogFeed,
        ServerStatusFeed,
        TradeFeed,
        TradeWatcherFeed,
        AccountLinkRoleModule,
        DemographicRoleModule,
        ElectedTitleRoleModule,
        SpecialitiesRoleModule,
        RoleCleanupModule,
        SnippetInput
    }

    public enum ModuleArchetype
    {
        Display,
        Feed,
        Input
    }

    public abstract class Module
    {
        public abstract override string ToString();

        public bool IsEnabled { get; private set; } = false;

        // These events may fire very frequently and may trigger rate limitations and therefore some special handling is done based on this field.
        public const DlEventType HighFrequencyTriggerFlags = DlEventType.EcoMessageSent | DlEventType.DiscordMessageSent | DlEventType.Trade | DlEventType.WorkedWorkParty | DlEventType.WorkOrderCreated;
        protected readonly AsyncLock _overlapLock = new AsyncLock();
        protected string _status = "Off";
        protected DateTime _startTime = DateTime.MinValue;
        protected int _opsCount = 0;
        private RollingAverage _opsCountAverage = new RollingAverage(Constants.SECONDS_PER_MINUTE);
        private Timer _OpsCountTimer = null;

        public Module()
        {
            _OpsCountTimer = new Timer(UpdateRollingAverage, null, Constants.MILLISECONDS_PER_MINUTE, Constants.MILLISECONDS_PER_MINUTE);
        }

        public virtual string GetDisplayText(string childInfo, bool verbose)
        {
            string info = $"\r\nStatus: {_status}";
            if (IsEnabled)
            {
                if (verbose)
                {
                    info += $"\r\nStart Time: {_startTime:yyyy-MM-dd HH:mm}";
                    info += $"\r\nOperations Per Minute: {_opsCountAverage.Average.ToString("0.##")}";
                }
                info += $"\r\n{childInfo}";
            }
            else
            {
                info += "\r\n";
            }

            return $"[{ToString()}]{info}\r\n";
        }

        public virtual void Setup()
        {
            DLConfig.Instance.OnConfigChanged += HandleConfigChanged; // Always listen for config changes as those may enable/disable the module
            NetworkManager.Obj.ParamChanged.Add(async (s, e) => await HandleConfigChanged(s, new EventArgs()));
        }

        public virtual void Destroy()
        {
            DLConfig.Instance.OnConfigChanged -= HandleConfigChanged;
        }

        public async Task<bool> HandleStartOrStop()
        {
            bool shouldRun = await ShouldRun();
            if (!IsEnabled && shouldRun)
            {
                await Initialize();
                return true;
            }
            else if (IsEnabled && !shouldRun)
            {
                await Shutdown();
                return true;
            }
            return false;
        }

        public async Task Stop()
        {
            await Shutdown();
        }

        protected abstract DlEventType GetTriggers();

        protected virtual async Task<bool> ShouldRun() { throw new NotImplementedException(); }

        protected virtual async Task Initialize()
        {
            Logger.Debug($"Starting {this}");

            IsEnabled = true;
            _status = "Running";
            _startTime = DateTime.Now;
        }

        protected virtual async Task Shutdown()
        {
            Logger.Debug($"Stopping {this}");

            IsEnabled = false;
            _status = "Off";
            _startTime = DateTime.MinValue;
            using (await _overlapLock.LockAsync()) // Make sure that anything queued completes before we shut down
            { }
        }

        protected virtual async Task HandleConfigChanged(object sender, EventArgs e)
        {
            await HandleStartOrStop();
        }

        // NOTE: Do NOT acquire the overlap lock in this function or there will be deadlocks
        protected abstract Task UpdateInternal(DiscordLink plugin, DlEventType trigger, params object[] data);

        public virtual async Task Update(DiscordLink plugin, DlEventType trigger, params object[] data)
        {
            // Check if this module should execute on the supplied trigger
            if ((GetTriggers() & trigger) == 0)
                return;

            // Make sure that the Update function doesn't get overlapping executions
            using (await _overlapLock.LockAsync())
            {
                if (!IsEnabled)
                    return;

                try
                {
                    await UpdateInternal(plugin, trigger, data);
                }
                catch (Exception e)
                {
                    Logger.Exception($"An error occured while updating the {ToString()} module", e);
                }
            }
        }

        private void UpdateRollingAverage(object stateInfo)
        {
            _opsCountAverage.Add(_opsCount);
            _opsCount = 0;
        }
    }
}
