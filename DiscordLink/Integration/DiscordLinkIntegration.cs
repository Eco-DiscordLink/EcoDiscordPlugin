namespace Eco.Plugins.DiscordLink.IntegrationTypes
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

        public virtual void Update(DiscordLink plugin, TriggerType trigger, object data)
        {
            if (plugin == null) return;
            if ((GetTriggers() & trigger) == 0) return;

            lock (_overlapLock) // Make sure that the Update function doesn't get overlapping executions
            {
                UpdateInternal(plugin, trigger, data);
            }
        }

        protected abstract TriggerType GetTriggers();

        protected abstract void UpdateInternal(DiscordLink plugin, TriggerType trigger, object data);
    }
}
