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
        Login               = 1 << 4,
        Trade               = 1 << 5,
        PostedWorkParty     = 1 << 6,
        CompletedWorkParty  = 1 << 7,
        JoinedWorkParty     = 1 << 8,
        LeftWorkParty       = 1 << 9,
        WorkedWorkParty     = 1 << 10,
    }

    public abstract class DiscordLinkIntegration
    {
        // These events may fire very frequently and may trigger rate limitations and therefore some special handling is done based on this field.
        public const TriggerType HighFrequencyTriggerFlags = TriggerType.EcoMessage | TriggerType.DiscordMessage | TriggerType.Trade | TriggerType.WorkedWorkParty;
        protected readonly AsyncLock _overlapLock = new AsyncLock();
        protected bool IsShuttingDown = false;

        public virtual async Task Initialize()
        { }

        public virtual async Task Shutdown()
        {
            IsShuttingDown = true;
            using (await _overlapLock.LockAsync()) // Make sure that anything queued completes before we shut down
            { }
        }

        public virtual async Task OnConfigChanged()
        { }

        public virtual async Task OnMessageDeleted(DiscordMessage message)
        { }

        public virtual async Task Update(DiscordLink plugin, TriggerType trigger, object data)
        {
            if (plugin == null) return;
            if ((GetTriggers() & trigger) == 0) return;

            using (await _overlapLock.LockAsync()) // Make sure that the Update function doesn't get overlapping executions
            {
                if (IsShuttingDown) return;
                await UpdateInternal(plugin, trigger, data);
            }
        }

        protected abstract TriggerType GetTriggers();

        protected abstract Task UpdateInternal(DiscordLink plugin, TriggerType trigger, object data);
    }
}
