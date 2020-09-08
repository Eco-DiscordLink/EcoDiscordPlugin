using System;
using System.Threading;
using Eco.Plugins.DiscordLink.Utilities;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    abstract public class Display : DiscordLinkIntegration
    {
        protected int _timerUpdateIntervalMS = -1;
        protected int _timerStartDelayMS = 0;
        private Timer _updateTimer = null;

        public override void Initialize()
        {
            StartTimer();
            base.Initialize();
        }

        public override void Shutdown()
        {
            StopTimer();
            base.Shutdown();
        }

        public void StartTimer()
        {
            if (_timerStartDelayMS == 0 && _timerUpdateIntervalMS == -1)
                return; // This Display does not use timers

            if (_updateTimer != null)
                StopTimer();

            _triggerTypeField |= TriggerType.Timer;
            _updateTimer = new Timer(this.TriggerTimedUpdate, null, _timerStartDelayMS, _timerUpdateIntervalMS);
        }

        public void StopTimer()
        {
            if (_timerStartDelayMS == 0 && _timerUpdateIntervalMS == -1)
                return; // This Display does not use timers

            SystemUtil.StopAndDestroyTimer(ref _updateTimer);
            _triggerTypeField &= ~TriggerType.Timer;
        }

        private void TriggerTimedUpdate(Object stateInfo)
        {
            Update(DiscordLink.Obj, TriggerType.Timer, null);
        }
    }
}
