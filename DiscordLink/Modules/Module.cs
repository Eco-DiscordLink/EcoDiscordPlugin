using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
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
        public const DLEventType HighFrequencyTriggerFlags = DLEventType.EcoMessage | DLEventType.DiscordMessage | DLEventType.Trade | DLEventType.WorkedWorkParty;
        protected readonly AsyncLock _overlapLock = new AsyncLock();
        protected bool _isShuttingDown = false;

        public virtual void Setup()
        { }

        public virtual void Destroy()
        { }

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
            DLConfig.Instance.OnConfigChanged += OnConfigChanged; // Always listen for config changes as those may enable/disable the module
            IsEnabled = true;
        }

        protected virtual async Task Shutdown()
        {
            _isShuttingDown = true;
            DLConfig.Instance.OnConfigChanged -= OnConfigChanged;
            IsEnabled = false;
            using (await _overlapLock.LockAsync()) // Make sure that anything queued completes before we shut down
            { }
        }

        protected virtual async Task OnConfigChanged(object sender, EventArgs e)
        {
            await HandleStartOrStop();
        }

        protected abstract Task UpdateInternal(DiscordLink plugin, DLEventType trigger, object data);

        public virtual async Task OnMessageDeleted(DiscordMessage message)
        { }

        public virtual async Task Update(DiscordLink plugin, DLEventType trigger, object data)
        {
            if (plugin == null) return;

            // Check if this module should execute on the supplied trigger
            if ((GetTriggers() & trigger) == 0) return;

            using (await _overlapLock.LockAsync()) // Make sure that the Update function doesn't get overlapping executions
            {
                if (_isShuttingDown) return;
                await UpdateInternal(plugin, trigger, data);
            }
        }
    }
}
