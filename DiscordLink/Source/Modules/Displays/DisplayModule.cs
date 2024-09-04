using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Moose.Utils.SystemUtils;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Eco.Plugins.DiscordLink.Enums;

namespace Eco.Plugins.DiscordLink.Modules
{
    abstract public class DisplayModule : Module
    {
        public DateTime LastUpdateTime { get; protected set; } = DateTime.MinValue;

        protected virtual string BaseTag { get; } = "[Unset Tag]";
        protected virtual int TimerUpdateIntervalMs { get; } = -1;
        protected virtual int TimerStartDelayMs { get; } = 0;
        protected virtual int HighFrequencyEventDelayMs { get; } = 2000;
        protected List<TargetDisplayData> TargetDisplays { get; } = new List<TargetDisplayData>();

        private bool _dirty = false;
        private Timer _updateTimer = null;
        private Timer _HighFrequencyEventTimer = null;

        public override string GetDisplayText(string childInfo, bool verbose)
        {
            string lastUpdateTime = (LastUpdateTime == DateTime.MinValue) ? "Never" : LastUpdateTime.ToString("yyyy-MM-dd HH:mm");
            int trackedMessageCount = 0;
            foreach (TargetDisplayData target in TargetDisplays)
            {
                trackedMessageCount += target.DisplayMessages.Count;
            }
            string info = $"Last update time: {lastUpdateTime}";
            info += $"\r\nTracked Display Messages: {trackedMessageCount}";
            info += $"\r\n{childInfo}";

            return base.GetDisplayText(info, verbose);
        }

        protected override DlEventType GetTriggers()
        {
            return DlEventType.ForceUpdate | DlEventType.DiscordMessageDeleted | DlEventType.DiscordReactionAdded | DlEventType.DiscordReactionRemoved;
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
                ClearTargetDisplays(); // The channel links may have changed so we should find the messages again.
            }
            await base.HandleConfigChanged(sender, e);
        }

        protected override async Task<bool> ShouldRun()
        {
            foreach (DiscordTarget target in await GetDiscordTargets())
            {
                // If there is at least one valid target, we should run the display
                if (target.IsValid())
                    return true;
            }
            return false;
        }

        public void StartTimer()
        {
            if ((GetTriggers() & DlEventType.Timer) == 0) return;

            if (_updateTimer != null)
                StopTimer();

            _updateTimer = new Timer(this.TriggerTimedUpdate, null, TimerStartDelayMs, TimerUpdateIntervalMs == -1 ? Timeout.Infinite : TimerUpdateIntervalMs);
        }

        public void StopTimer()
        {
            if ((GetTriggers() & DlEventType.Timer) == 0)
                return;

            SystemUtils.StopAndDestroyTimer(ref _updateTimer);
        }

        protected virtual async Task<List<DiscordTarget>> GetDiscordTargets() { throw new NotImplementedException(); }

        protected void ClearTargetDisplays()
        {
            TargetDisplays.Clear();
        }

        private void TriggerTimedUpdate(object stateInfo)
        {
            _ = base.Update(DiscordLink.Obj, DlEventType.Timer, null);
            SystemUtils.StopAndDestroyTimer(ref _HighFrequencyEventTimer);
        }

        protected sealed override async Task UpdateInternal(DiscordLink plugin, DlEventType trigger, params object[] data)
        {
            // Handle deleted messages first to avoid exceptions
            if (trigger == DlEventType.DiscordMessageDeleted)
            {
                if (!(data[0] is DiscordMessage message))
                    return;

                foreach (TargetDisplayData display in TargetDisplays)
                {
                    bool found = false;
                    for (int i = 0; i < display.DisplayMessages.Count; ++i)
                    {
                        if (display.DisplayMessages.ContainsKey(message.Id))
                        {
                            display.DisplayMessages.Remove(message.Id);
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        break;
                }
            }
            else if (trigger == DlEventType.DiscordReactionAdded || trigger == DlEventType.DiscordReactionRemoved)
            {
                DiscordReactionChange changeType = (trigger == DlEventType.DiscordReactionAdded ? DiscordReactionChange.Added : DiscordReactionChange.Removed);
                await HandleReactionChange(data[0] as DiscordUser, data[1] as DiscordMessage, data[2] as DiscordEmoji, changeType);
                return;
            }

            // Block Display implementations from using edit and delete events
            if (trigger == DlEventType.DiscordMessageEdited || trigger == DlEventType.DiscordMessageDeleted)
                return;

            // Avoid hitting the rate limitation by not allowig events that can be fired often to pass straight through.
            if ((trigger & HighFrequencyTriggerFlags) == trigger)
            {
                if (_HighFrequencyEventTimer == null)
                    _HighFrequencyEventTimer = new Timer(this.TriggerTimedUpdate, null, HighFrequencyEventDelayMs, Timeout.Infinite);
                return;
            }

            if (_dirty || TargetDisplays.Count <= 0)
            {
                await FindMessages(plugin);
                if (_dirty || TargetDisplays.Count <= 0)
                    return; // If something went wrong, we should just retry later
            }

            List<DiscordMessage> createdMessages = new List<DiscordMessage>();
            List<string> matchedTags = new List<string>();
            List<DiscordMessage> unmatchedMessages = new List<DiscordMessage>();
            foreach (TargetDisplayData channelDisplayData in TargetDisplays)
            {
                DiscordTarget target = channelDisplayData.Target;
                ChannelLink channelLink = target as ChannelLink;
                UserLink userLink = target as UserLink;
                if (channelLink == null && userLink == null)
                    continue;

                DiscordChannel targetChannel;
                if (channelLink != null && channelLink.IsValid())
                    targetChannel = channelLink.Channel;
                else if (userLink != null)
                    targetChannel = await userLink.Member.CreateDmChannelAsync();
                else
                    continue;

                if (!plugin.Client.ChannelHasPermission(targetChannel, Permissions.ReadMessageHistory))
                    continue;

                GetDisplayContent(target, out List<Tuple<string, DiscordLinkEmbed>> tagsAndContent);

                foreach (ulong messageId in channelDisplayData.DisplayMessages.Keys)
                {
                    DiscordMessage message = await plugin.Client.GetMessageAsync(targetChannel, messageId);
                    if (message == null)
                    {
                        _dirty = true;
                        return; // We cannot know which messages are wrong and duplicates may be created if we continue.
                    }
                    if (!message.Content.StartsWith(BaseTag))
                        continue; // The message belongs to a different display

                    bool found = false;
                    foreach (var tagAndContent in tagsAndContent)
                    {
                        if (message.Content.Contains(tagAndContent.Item1))
                        {
                            found = true;
                            matchedTags.Add(tagAndContent.Item1);

                            ++_opsCount;
                            DiscordMessage editedMessage = await plugin.Client.ModifyMessageAsync(message, tagAndContent.Item1, tagAndContent.Item2);
                            if (editedMessage != null)
                                await PostDisplayEdited(editedMessage);

                            break;
                        }
                    }

                    if (!found)
                        unmatchedMessages.Add(message);
                }

                // Delete the messages that are no longer relevant
                foreach (DiscordMessage message in unmatchedMessages)
                {
                    channelDisplayData.DisplayMessages.Remove(message.Id);
                    await plugin.Client.DeleteMessageAsync(message);
                    ++_opsCount;
                }
                unmatchedMessages.Clear();

                // Send the messages that didn't already exist
                foreach (var tagAndContent in tagsAndContent)
                {
                    if (!matchedTags.Contains(tagAndContent.Item1))
                    {
                        DiscordMessage createdMessage = await plugin.Client.SendMessageAsync(targetChannel, tagAndContent.Item1, tagAndContent.Item2);
                        if (createdMessage == null)
                            continue;

                        createdMessages.Add(createdMessage);
                        ++_opsCount;
                    }
                }
                matchedTags.Clear();
            }

            if (unmatchedMessages.Count > 0 || createdMessages.Count > 0)
            {
                await FindMessages(plugin);

                foreach (DiscordMessage message in createdMessages)
                {
                    await PostDisplayCreated(message);
                }
            }

            LastUpdateTime = DateTime.Now;
        }

        protected abstract void GetDisplayContent(DiscordTarget target, out List<Tuple<string, DiscordLinkEmbed>> tagAndContent);

        protected async virtual Task PostDisplayCreated(DiscordMessage message) { }

        protected async virtual Task PostDisplayEdited(DiscordMessage message) { }

        protected async virtual Task HandleReactionChange(DiscordUser user, DiscordMessage message, DiscordEmoji reaction, DiscordReactionChange changeType) { }

        private async Task FindMessages(DiscordLink plugin)
        {
            ClearTargetDisplays();

            foreach (DiscordTarget target in await GetDiscordTargets())
            {
                IReadOnlyList<DiscordMessage> targetMessages = null;

                ChannelLink channelLink = target as ChannelLink;
                UserLink userLink = target as UserLink;
                if (channelLink == null && userLink == null)
                    continue;

                TargetDisplayData data = new TargetDisplayData(target);
                TargetDisplays.Add(data);
                if (channelLink != null)
                {
                    if (!channelLink.IsValid())
                        continue;

                    targetMessages = await plugin.Client.GetMessagesAsync(channelLink.Channel);
                }
                else if (userLink != null)
                {
                    DiscordDmChannel dmChannel = await userLink.Member.CreateDmChannelAsync();
                    targetMessages = await plugin.Client.GetMessagesAsync(dmChannel);
                }

                if (targetMessages == null)
                {
                    // There was an error or no messages exist - Clean up and return
                    ClearTargetDisplays();
                    return;
                }

                // Go through the messages and find any our tagged messages
                foreach (DiscordMessage message in targetMessages)
                {
                    Match match = MessageUtils.DisplayTagRegex.Match(message.Content);
                    if (match.Groups.Count <= 1)
                        continue;

                    if (match.Groups[1].Value != BaseTag)
                        continue;

                    string tag = null;
                    if (match.Groups.Count > 2)
                        tag = match.Groups[2].Value;

                    data.DisplayMessages.Add(message.Id, tag);
                }
            }
            _dirty = false;
        }

        protected struct TargetDisplayData
        {
            public TargetDisplayData(DiscordTarget target)
            {
                this.Target = target;
                this.DisplayMessages = new Dictionary<ulong, string>();
            }

            public DiscordTarget Target;
            public Dictionary<ulong, string> DisplayMessages;
        }
    }
}
