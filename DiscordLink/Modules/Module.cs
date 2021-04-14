using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using Nito.AsyncEx;
using System;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public abstract class Module
    {
        public abstract override string ToString();

        public bool IsEnabled { get; private set; } = false;

        // These events may fire very frequently and may trigger rate limitations and therefore some special handling is done based on this field.
        public const DLEventType HighFrequencyTriggerFlags = DLEventType.EcoMessageSent | DLEventType.DiscordMessageSent | DLEventType.Trade | DLEventType.WorkedWorkParty | DLEventType.WorkOrderCreated;
        protected readonly AsyncLock _overlapLock = new AsyncLock();
        protected bool _isShuttingDown = false;
        protected string _status = "Off";
        protected DateTime _startTime = DateTime.MinValue;
        protected int _opsCount = 0;

        public virtual string GetDisplayText(string childInfo, bool verbose)
        {
            string info = $"\r\nStatus: {_status}";
            if (IsEnabled)
            {
                if (verbose)
                {
                    int operationsPerMinute = 0;
                    int elapsedMinutes = (int)(DateTime.Now - _startTime).TotalMinutes;
                    if (elapsedMinutes > 0)
                        operationsPerMinute = (_opsCount / elapsedMinutes);

                    info += $"\r\nStart Time: {_startTime:yyyy-MM-dd HH:mm}";
                    info += $"\r\nOperations Per Minute: {operationsPerMinute}";
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
        }

        public virtual void Destroy()
        {
            DLConfig.Instance.OnConfigChanged -= HandleConfigChanged;
        }

        public async Task<bool> HandleStartOrStop()
        {
            bool shouldRun = ShouldRun();
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

        protected abstract DLEventType GetTriggers();

        protected abstract bool ShouldRun();

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

            _isShuttingDown = true;
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
        protected abstract Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data);

        public virtual async Task Update(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            if (plugin == null) return;

            // Check if this module should execute on the supplied trigger
            if ((GetTriggers() & trigger) == 0) return;

            // Make sure that the Update function doesn't get overlapping executions
            using (await _overlapLock.LockAsync())
            {
                if (_isShuttingDown) return;
                await UpdateInternal(plugin, trigger, data);
            }
        }
    }
}
