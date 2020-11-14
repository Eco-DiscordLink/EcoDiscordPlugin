using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Utilities;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    public class SnippetInput : Input
    {
        public override async Task Initialize()
        {
            _ = ReloadSnippets();
            await base.Initialize();
        }

        public override async Task OnConfigChanged()
        {
            using (await _overlapLock.LockAsync()) // Avoid crashes caused by data being manipulated and used simultaneously
            {
                await ReloadSnippets();
            }
            await base.OnConfigChanged();
        }

        public override async Task OnMessageDeleted(DiscordMessage message)
        {
            using (await _overlapLock.LockAsync()) // Avoid crashes caused by data being manipulated and used simultaneously
            {
                for (int i = 0; i < DLConfig.Data.SnippetChannels.Count; ++i)
                {
                    ChannelLink link = DLConfig.Data.SnippetChannels[i];
                    if (!link.IsValid()) continue;

                    string channel = link.DiscordChannel.ToLower();
                    if(channel == message.Channel.Name.ToLower() || channel == message.ChannelId.ToString())
                    {
                        await ReloadSnippets();
                        break;
                    }
                }
            }
            await base.OnMessageDeleted(message);
        }

        protected override TriggerType GetTriggers()
        {
            return TriggerType.DiscordMessage;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, TriggerType trigger, object data)
        {
            if (!(data is DiscordMessage message)) return;
            if (message.IsDm()) return;

            bool isSnippetChannel = false;
            foreach(ChannelLink link in DLConfig.Data.SnippetChannels)
            {
                if (!link.IsValid()) continue;

                if (link.DiscordGuild.ToLower() == message.Channel.Guild.Name.ToLower()
                    && link.DiscordChannel == message.Channel.Name )
                {
                    isSnippetChannel = true;
                    break;
                }
            }
            if (isSnippetChannel)
            {
                await ReloadSnippets();
            }
        }

        private async Task ReloadSnippets()
        {
            DiscordLink plugin = DiscordLink.Obj;
            if (plugin == null) return;
            foreach (ChannelLink snippetChannel in DLConfig.Data.SnippetChannels)
            {
                if (!snippetChannel.IsValid()) continue;
                DiscordGuild discordGuild = plugin.GuildByNameOrId(snippetChannel.DiscordGuild);
                if (discordGuild == null) continue;
                DiscordChannel discordChannel = discordGuild.ChannelByNameOrId(snippetChannel.DiscordChannel);
                if (discordChannel == null) continue;
                if (!DiscordUtil.ChannelHasPermission(discordChannel, Permissions.ReadMessageHistory)) continue;

                IReadOnlyList<DiscordMessage> snippetChannelMessages = await DiscordUtil.GetMessagesAsync(discordChannel);
                if (snippetChannelMessages == null) continue;

                DLStorage.Instance.Snippets.Clear();
                foreach (DiscordMessage channelMessage in snippetChannelMessages)
                {
                    Match match = MessageUtil.SnippetRegex.Match(channelMessage.Content);
                    if (match.Groups.Count == 3)
                    {
                        DLStorage.Instance.Snippets.Add(match.Groups[1].Value.ToLower(), match.Groups[2].Value);
                    }
                }
            }
        }
    }
}
