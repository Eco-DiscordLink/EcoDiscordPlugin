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
        protected TriggerType _triggerTypeField = 0;
        protected object _updateLock = new object();
        public DiscordLinkIntegration()
        {
            Initialize();
        }

        ~DiscordLinkIntegration()
        {
            Shutdown();
        }

        public virtual void Initialize()
        {
            _triggerTypeField = GetTriggers();
        }

        public virtual void Shutdown()
        { }

        public virtual void OnConfigChanged()
        { }

        public virtual void Update(DiscordLink plugin, TriggerType trigger, object data)
        {
            if ((GetTriggers() & trigger) == 0) return;
            if (plugin == null) return;

            lock (_updateLock) // Make sure that the Update function doesn't get overlapping executions
            {
                UpdateInternal(plugin, trigger, data);
            }
        }

        protected abstract TriggerType GetTriggers();

        protected abstract void UpdateInternal(DiscordLink plugin, TriggerType trigger, object data);
    }
}
