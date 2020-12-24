using DSharpPlus.Entities;
using Nito.AsyncEx;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    public enum TriggerType
    {
        Timer               = 1 << 0,
        Startup             = 1 << 1,
        EcoMessage          = 1 << 2,
        DiscordMessage      = 1 << 3,
        Join                = 1 << 4,
        Login               = 1 << 5,
        Logout              = 1 << 6,
        Trade               = 1 << 7,
        PostedWorkParty     = 1 << 8,
        CompletedWorkParty  = 1 << 9,
        JoinedWorkParty     = 1 << 10,
        LeftWorkParty       = 1 << 11,
        WorkedWorkParty     = 1 << 12,
        Vote                = 1 << 13,
        StartElection       = 1 << 14,
        StopElection        = 1 << 15,
    }

    public abstract class DiscordLinkIntegration
    {
        public bool IsEnabled { get; private set; } = false;

        // These events may fire very frequently and may trigger rate limitations and therefore some special handling is done based on this field.
        public const TriggerType HighFrequencyTriggerFlags = TriggerType.EcoMessage | TriggerType.DiscordMessage | TriggerType.Trade | TriggerType.WorkedWorkParty;
        protected readonly AsyncLock _overlapLock = new AsyncLock();
        protected bool _isShuttingDown = false;

        public async Task StartIfRelevant()
        {
            IsEnabled = ShouldRun();
            if (IsEnabled)
                await Initialize();

            // Always listen for config changes as those may enable/disable
            DLConfig.Instance.OnConfigChanged += (obj, args) =>
            {
                _ = OnConfigChanged();
            };
        }

        public async Task Stop()
        {
            await Shutdown();
        }

        protected abstract TriggerType GetTriggers();

        protected abstract bool ShouldRun();

        protected virtual async Task Initialize()
        { }

        protected virtual async Task Shutdown()
        {
            _isShuttingDown = true;
            IsEnabled = false;
            using (await _overlapLock.LockAsync()) // Make sure that anything queued completes before we shut down
            { }
        }

        protected virtual async Task OnConfigChanged()
        {
            bool shouldRun = ShouldRun();
            if (!IsEnabled && shouldRun)
                await Initialize();
            else if (IsEnabled && !shouldRun)
                await Shutdown();

            IsEnabled = shouldRun;
        }

        protected abstract Task UpdateInternal(DiscordLink plugin, TriggerType trigger, object data);

        public virtual async Task OnMessageDeleted(DiscordMessage message)
        { }

        public virtual async Task Update(DiscordLink plugin, TriggerType trigger, object data)
        {
            if (plugin == null) return;

            // Check if this integration should execute on the supplied trigger
            if ((GetTriggers() & trigger) == 0) return; 

            using (await _overlapLock.LockAsync()) // Make sure that the Update function doesn't get overlapping executions
            {
                if (_isShuttingDown) return;
                await UpdateInternal(plugin, trigger, data);
            }
        }
    }
}
