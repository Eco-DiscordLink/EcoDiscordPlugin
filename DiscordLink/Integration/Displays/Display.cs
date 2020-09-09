using System;
using System.Collections.Generic;
using System.Threading;
using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Utilities;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    abstract public class Display : DiscordLinkIntegration
    {
        protected const string BaseDisplayTag = "[Display]";

        protected virtual int TimerUpdateIntervalMS { get; } = -1;
        protected virtual int TimerStartDelayMS { get; } = 0;

        private Timer _updateTimer = null;
        private readonly List<ChannelDisplayData> _channelDisplays = new List<ChannelDisplayData>();

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

        public override void OnConfigChanged()
        {
            Clear(); // The channel links may have changed so we should find the messages again.
            base.OnConfigChanged();
        }

        public void StartTimer()
        {
            if ((GetTriggers() & TriggerType.Timer) == 0) return;

            if (_updateTimer != null)
                StopTimer();

            _updateTimer = new Timer(this.TriggerTimedUpdate, null, TimerStartDelayMS, TimerUpdateIntervalMS);
        }

        public void StopTimer()
        {
            if ((GetTriggers() & TriggerType.Timer) == 0) return;
            SystemUtil.StopAndDestroyTimer(ref _updateTimer);
        }

        protected abstract List<ChannelLink> GetChannelLinks();

        protected void Clear()
        {
            _channelDisplays.Clear();
        }

        private void TriggerTimedUpdate(Object stateInfo)
        {
            base.Update(DiscordLink.Obj, TriggerType.Timer, null);
        }

        protected sealed override void UpdateInternal(DiscordLink plugin, TriggerType trigger, object data)
        {
            if(_channelDisplays.Count <= 0)
            {
                FindMessages(plugin);
            }

            bool createdOrDestroyedMessage = false;
            List<string> matchedTags = new List<string>();
            List<DiscordMessage> unmatchedMessages = new List<DiscordMessage>();
            foreach(ChannelDisplayData channelDisplayData in _channelDisplays)
            {
                ChannelLink link = channelDisplayData.Link;
                DiscordGuild discordGuild = plugin.GuildByName(link.DiscordGuild);
                if (discordGuild == null) continue;
                DiscordChannel discordChannel = discordGuild.ChannelByName(link.DiscordChannel);
                if (discordChannel == null) continue;
                if (!DiscordUtil.ChannelHasPermission(discordChannel, Permissions.ReadMessageHistory)) continue;

                GetDisplayContent(link, out List<Tuple<string, DiscordEmbed>> tagsAndContent);

                foreach (ulong messageID in channelDisplayData.MessageIDs)
                {
                    DiscordMessage message = DiscordUtil.GetMessageAsync(discordChannel, messageID).Result;
                    if (message == null) continue;

                    bool found = false;
                    foreach(var tagAndContent in tagsAndContent)
                    {
                        if (message.Content.Contains(tagAndContent.Item1))
                        {
                            _ = DiscordUtil.ModifyAsync(message, tagAndContent.Item1, tagAndContent.Item2);
                            matchedTags.Add(tagAndContent.Item1);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        unmatchedMessages.Add(message);
                    }
                }

                // Delete the messages that are no longer relevant
                foreach(DiscordMessage message in unmatchedMessages)
                {
                    DiscordUtil.DeleteAsync(message).Wait();
                    createdOrDestroyedMessage = true;
                }
                unmatchedMessages.Clear();

                // Send the messages that didn't already exist
                foreach(var tagAndContent in tagsAndContent)
                {
                    if(!matchedTags.Contains(tagAndContent.Item1))
                    {
                        DiscordUtil.SendAsync(discordChannel, tagAndContent.Item1, tagAndContent.Item2).Wait();
                        createdOrDestroyedMessage = true;
                    }
                }
                matchedTags.Clear();
            }

            if(createdOrDestroyedMessage)
            {
                FindMessages(plugin);
            }
        }

        protected abstract void GetDisplayContent(ChannelLink link, out List<Tuple<string, DiscordEmbed>> tagAndContent);

        private void FindMessages(DiscordLink plugin)
        {
            _channelDisplays.Clear();

            foreach (ChannelLink channelLink in GetChannelLinks())
            {
                DiscordGuild discordGuild = plugin.GuildByName(channelLink.DiscordGuild);
                if (discordGuild == null) continue;
                DiscordChannel discordChannel = discordGuild.ChannelByName(channelLink.DiscordChannel);
                if (discordChannel == null) continue;
                if (!DiscordUtil.ChannelHasPermission(discordChannel, Permissions.ReadMessageHistory)) continue;

                ChannelDisplayData data = new ChannelDisplayData(channelLink);
                _channelDisplays.Add(data);

                IReadOnlyList<DiscordMessage> channelMessages = DiscordUtil.GetMessagesAsync(discordChannel).Result;
                if (channelMessages == null) continue;

                foreach(DiscordMessage message in channelMessages)
                {
                    if (!message.Content.StartsWith(BaseDisplayTag)) continue;
                    data.MessageIDs.Add(message.Id);
                }
            }
        }

        private struct ChannelDisplayData
        {
            public ChannelDisplayData(ChannelLink link)
            {
                Link = link;
                MessageIDs = new List<ulong>();
            }

            public ChannelLink Link;
            public List<ulong> MessageIDs;
        }
    }
}
