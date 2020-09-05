using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Eco.Plugins.DiscordLink.Utilities;

namespace Eco.Plugins.DiscordLink
{
    enum DisplayTriggerType
    {
        Timer   = 1 << 0,
        Startup = 1 << 1,
        Login   = 2 << 2,
    }

    abstract class Display
    {
        protected DisplayTriggerType _triggerTypeField = 0;
        protected int _timerUpdateIntervalMS = -1;
        protected int _timerStartDelayMS = 0;
        private Timer _updateTimer = null;
        private object _updateLock = new object();

        ~Display()
        {
            StopTimer();
        }

        public void StartTimer()
        {
            if (_timerStartDelayMS == 0 && _timerUpdateIntervalMS == -1)
                return; // This Display does not use timers

            if (_updateTimer != null)
                StopTimer();

            _triggerTypeField |= DisplayTriggerType.Timer;
            _updateTimer = new Timer(this.TriggerTimedUpdate, null, _timerStartDelayMS, _timerUpdateIntervalMS);
        }

        public void StopTimer()
        {
            if (_timerStartDelayMS == 0 && _timerUpdateIntervalMS == -1)
                return; // This Display does not use timers

            SystemUtil.StopAndDestroyTimer(ref _updateTimer);
            _triggerTypeField &= ~DisplayTriggerType.Timer;
        }

        public void Update(DiscordLink plugin, DisplayTriggerType trigger)
        {
            if ((_triggerTypeField & trigger) == 0)
                return;

            if(plugin == null) return;

            lock (_updateLock) // Make sure that the Update function doesn't get overlapping executions
            {
                UpdateDisplay(plugin, trigger);
            }
        }

        public virtual void OnConfigChanged()
        { }

        protected abstract void UpdateDisplay(DiscordLink plugin, DisplayTriggerType trigger);

        private void TriggerTimedUpdate(Object stateInfo)
        {
            Update(DiscordLink.Obj, DisplayTriggerType.Timer);
        }
    }
}
