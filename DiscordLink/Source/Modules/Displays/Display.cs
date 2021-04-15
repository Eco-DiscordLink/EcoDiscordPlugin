using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiscordLink.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;

namespace Eco.Plugins.DiscordLink.Modules
{
    abstract public class Display : Module
    {
        public DateTime LastUpdateTime { get; protected set; } = DateTime.MinValue;

        protected virtual string BaseTag { get; } = "[Unset Tag]";
        protected virtual int TimerUpdateIntervalMS { get; } = -1;
        protected virtual int TimerStartDelayMS { get; } = 0;
        protected virtual int HighFrequencyEventDelayMS { get; } = 2000;

        private bool _dirty = false;
        private Timer _updateTimer = null;
        private Timer _HighFrequencyEventTimer = null;
        private readonly List<TargetDisplayData> _targetDisplays = new List<TargetDisplayData>();

        public override string GetDisplayText(string childInfo, bool verbose)
        {
            string lastUpdateTime = (LastUpdateTime == DateTime.MinValue) ? "Never" : LastUpdateTime.ToString("yyyy-MM-dd HH:mm");
            int trackedMessageCount = 0;
            foreach(TargetDisplayData target in _targetDisplays)
            {
                trackedMessageCount += target.MessageIDs.Count;
            }
            string info = $"Last update time: {lastUpdateTime}";
                info += $"\r\nTracked Display Messages: {trackedMessageCount}";
                info += $"\r\n{childInfo}";

            return base.GetDisplayText(info, verbose);
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.DiscordMessageDeleted;
        }

        protected override async Task Initialize()
        {
            StartTimer();
            await base.Initialize();
        }

        protected override async Task Shutdown()
        {
            StopTimer();
            await base.Shutdown();
        }

        protected override async Task HandleConfigChanged(object sender, EventArgs e)
        {
            using (await _overlapLock.LockAsync()) // Avoid crashes caused by data being manipulated and used simultaneously
            {
                Clear(); // The channel links may have changed so we should find the messages again.
            }
            await base.HandleConfigChanged(sender, e);
        }

        protected override bool ShouldRun()
        {
            foreach(DiscordTarget target in GetDiscordTargets())
            {
                // If there is at least one valid target, we should run the display
                if (target.IsValid())
                    return true;
            }
            return false;
        }

        public void StartTimer()
        {
            if ((GetTriggers() & DLEventType.Timer) == 0) return;

            if (_updateTimer != null)
                StopTimer();

            _updateTimer = new Timer(this.TriggerTimedUpdate, null, TimerStartDelayMS, TimerUpdateIntervalMS == -1 ? Timeout.Infinite : TimerUpdateIntervalMS);
        }

        public void StopTimer()
        {
            if ((GetTriggers() & DLEventType.Timer) == 0) return;
            SystemUtil.StopAndDestroyTimer(ref _updateTimer);
        }

        protected abstract List<DiscordTarget> GetDiscordTargets();

        protected void Clear()
        {
            _targetDisplays.Clear();
        }

        private void TriggerTimedUpdate(object stateInfo)
        {
            _ = base.Update(DiscordLink.Obj, DLEventType.Timer, null);
            SystemUtil.StopAndDestroyTimer(ref _HighFrequencyEventTimer);
        }

        protected sealed override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            // Handle deleted messages first to avoid crashes
            if(trigger == DLEventType.DiscordMessageDeleted)
            {
                if (!(data[0] is DiscordMessage message))
                    return;

                foreach (TargetDisplayData display in _targetDisplays)
                {
                    bool found = false;
                    for (int i = 0; i < display.MessageIDs.Count; ++i)
                    {
                        if (message.Id == display.MessageIDs[i])
                        {
                            display.MessageIDs.RemoveAt(i);
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        break;
                }
            }

            // Block Display implementations from using edit and delete events
            if (trigger == DLEventType.DiscordMessageEdited || trigger == DLEventType.DiscordMessageDeleted)
                return;

            // Avoid hitting the rate limitation by not allowig events that can be fired often to pass straight through.
            if ((trigger & HighFrequencyTriggerFlags) == trigger)
            {
                if (_HighFrequencyEventTimer == null)
                    _HighFrequencyEventTimer = new Timer(this.TriggerTimedUpdate, null, HighFrequencyEventDelayMS, Timeout.Infinite);
                return;
            }

            if(_dirty || _targetDisplays.Count <= 0)
            {
                await FindMessages(plugin);
                if (_dirty || _targetDisplays.Count <= 0)
                    return; // If something went wrong, we should just retry later
            }

            bool createdOrDestroyedMessage = false;
            List<string> matchedTags = new List<string>();
            List<DiscordMessage> unmatchedMessages = new List<DiscordMessage>();
            foreach(TargetDisplayData channelDisplayData in _targetDisplays)
            {
                DiscordTarget target = channelDisplayData.Target;
                ChannelLink channelLink = target as ChannelLink;
                UserLink userLink = target as UserLink;
                if (channelLink == null && userLink == null)
                    continue;

                DiscordChannel targetChannel = null;
                if (channelLink != null)
                {
                    if (!DiscordUtil.ChannelHasPermission(channelLink.Channel, Permissions.ReadMessageHistory))
                        continue;
                }
                else if(userLink != null)
                {
                    targetChannel = await userLink.Member.CreateDmChannelAsync();
                }

                GetDisplayContent(target, out List<Tuple<string, DiscordLinkEmbed>> tagsAndContent);

                foreach (ulong messageID in channelDisplayData.MessageIDs)
                {
                    DiscordMessage message = await DiscordUtil.GetMessageAsync(targetChannel, messageID);
                    if (message == null)
                    {
                        _dirty = true;
                        return; // We cannot know which messages are wrong and duplicates may be created if we continue.
                    }
                    if (!message.Content.StartsWith(BaseTag)) continue; // The message belongs to a different display

                    bool found = false;
                    foreach(var tagAndContent in tagsAndContent)
                    {
                        if (message.Content.Contains(tagAndContent.Item1))
                        {
                            _ = DiscordUtil.ModifyAsync(message, tagAndContent.Item1, tagAndContent.Item2);
                            matchedTags.Add(tagAndContent.Item1);
                            found = true;
                            ++_opsCount;
                            break;
                        }
                    }

                    if (!found)
                        unmatchedMessages.Add(message);
                }

                // Delete the messages that are no longer relevant
                foreach(DiscordMessage message in unmatchedMessages)
                {
                    DiscordUtil.DeleteAsync(message).Wait();
                    createdOrDestroyedMessage = true;
                    ++_opsCount;
                }
                unmatchedMessages.Clear();

                // Send the messages that didn't already exist
                foreach(var tagAndContent in tagsAndContent)
                {
                    if(!matchedTags.Contains(tagAndContent.Item1))
                    {
                        DiscordUtil.SendAsync(targetChannel, tagAndContent.Item1, tagAndContent.Item2).Wait();
                        createdOrDestroyedMessage = true;
                        ++_opsCount;
                    }
                }
                matchedTags.Clear();
            }

            if(createdOrDestroyedMessage)
                await FindMessages(plugin);

            LastUpdateTime = DateTime.Now;
        }

        protected abstract void GetDisplayContent(DiscordTarget target, out List<Tuple<string, DiscordLinkEmbed>> tagAndContent);

        private async Task FindMessages(DiscordLink plugin)
        {
            _targetDisplays.Clear();

            foreach (DiscordTarget target in GetDiscordTargets())
            {
                IReadOnlyList<DiscordMessage> targetMessages = null;

                ChannelLink channelLink = target as ChannelLink;
                UserLink userLink = target as UserLink;
                if (channelLink == null && userLink == null)
                    continue;

                TargetDisplayData data = new TargetDisplayData(target);
                _targetDisplays.Add(data);
                if (channelLink != null)
                {
                    if (!channelLink.IsValid() || !DiscordUtil.ChannelHasPermission(channelLink.Channel, Permissions.ReadMessageHistory))
                        continue;

                    targetMessages = await DiscordUtil.GetMessagesAsync(channelLink.Channel);
                }
                else if(userLink != null)
                {
                    DiscordDmChannel dmChannel = await userLink.Member.CreateDmChannelAsync();
                    targetMessages = await dmChannel.GetMessagesAsync();
                }
                
                if (targetMessages == null)
                {
                    // There was an error or no messages exist - Clean up and return
                    _targetDisplays.Clear();
                    return;
                } 

                // Go through the messages and find any our tagged messages
                foreach(DiscordMessage message in targetMessages)
                {
                    if (!message.Content.StartsWith(BaseTag)) continue;
                    data.MessageIDs.Add(message.Id);
                }
            }
            _dirty = false;
        }

        private struct TargetDisplayData
        {
            public TargetDisplayData(DiscordTarget target)
            {
                this.Target = target;
                this.MessageIDs = new List<ulong>();
            }

            public DiscordTarget Target;
            public List<ulong> MessageIDs;
        }
    }
}
