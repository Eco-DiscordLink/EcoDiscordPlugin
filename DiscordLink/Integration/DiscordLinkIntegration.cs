namespace Eco.Plugins.DiscordLink.IntegrationTypes
using Nito.AsyncEx;
using System.Threading.Tasks;
{
    public enum TriggerType
    {
        Timer           = 1 << 0,
        Startup         = 1 << 1,
        EcoMessage      = 1 << 2,
        DiscordMessage  = 1 << 3,
        Login           = 1 << 4,
        Trade           = 1 << 5,
    }

    public abstract class DiscordLinkIntegration
    {
        protected object _overlapLock = new object();
        protected readonly AsyncLock _overlapLock = new AsyncLock();
        public DiscordLinkIntegration()
        {
            Initialize();
        }

        ~DiscordLinkIntegration()
        {
            Shutdown();
        }

        public virtual void Initialize()
        { }

        public virtual void Shutdown()
        { }

        public virtual void OnConfigChanged()
        { }

        public virtual async Task Update(DiscordLink plugin, TriggerType trigger, object data)
        {
            if (plugin == null) return;
            if ((GetTriggers() & trigger) == 0) return;

            using (await _overlapLock.LockAsync()) // Make sure that the Update function doesn't get overlapping executions
            {
                await UpdateInternal(plugin, trigger, data);
            }
        }

        protected abstract TriggerType GetTriggers();

        protected abstract Task UpdateInternal(DiscordLink plugin, TriggerType trigger, object data);
    }
}
