using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Utilities;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    public class SnippetInput : Input
    {
        public override void Initialize()
        {
            ReloadSnippets();
            base.Initialize();
        }

        public override void OnConfigChanged()
        {
            ReloadSnippets();
        }

        protected override TriggerType GetTriggers()
        {
            return TriggerType.DiscordMessage;
        }

        protected override void UpdateInternal(DiscordLink plugin, TriggerType trigger, object data)
        {
            if (!(data is DiscordMessage message)) return;

            bool isSnippetChannel = false;
            foreach( ChannelLink link in DLConfig.Data.SnippetChannels)
            {
                if (link.DiscordGuild.ToLower() == message.Channel.Guild.Name.ToLower()
                    && link.DiscordChannel == message.Channel.Name )
                {
                    isSnippetChannel = true;
                    break;
                }
            }
            if (isSnippetChannel)
            {
                ReloadSnippets();
            }
        }

        private void ReloadSnippets()
        {
            DiscordLink plugin = DiscordLink.Obj;
            if (plugin == null) return;
            foreach (ChannelLink snippetChannel in DLConfig.Data.SnippetChannels)
            {
                if (!snippetChannel.IsValid()) continue;
                DiscordGuild discordGuild = plugin.GuildByName(snippetChannel.DiscordGuild);
                if (discordGuild == null) continue;
                DiscordChannel discordChannel = discordGuild.ChannelByName(snippetChannel.DiscordChannel);
                if (discordChannel == null) continue;
                if (!DiscordUtil.ChannelHasPermission(discordChannel, Permissions.ReadMessageHistory)) continue;

                IReadOnlyList<DiscordMessage> snippetChannelMessages = DiscordUtil.GetMessagesAsync(discordChannel).Result;
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
