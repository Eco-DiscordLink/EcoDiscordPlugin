using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    class PlayerDisplay : Display
    {
        protected override int TimerUpdateIntervalMS { get { return 60000; } }

        protected override TriggerType GetTriggers()
        {
            return TriggerType.Startup | TriggerType.Timer | TriggerType.Login;
        }

        protected override List<ChannelLink> GetChannelLinks()
        {
            return DLConfig.Data.PlayerListChannels.Cast<ChannelLink>().ToList();
        }

        protected override void GetDisplayContent(ChannelLink link, out List<Tuple<string, DiscordEmbed>> tagAndContent)
        {
            string tag = BaseDisplayTag + " [Player List]";
            string title = "Players";
            string content = "\n" + MessageBuilder.GetPlayerList();

            if ((link as PlayerListChannelLink).UsePlayerCount == true)
            {
                title = MessageBuilder.GetPlayerCount() + " players online";
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(MessageBuilder.EmbedColor)
                .WithTitle(title)
                .WithDescription(content);

            tagAndContent = new List<Tuple<string, DiscordEmbed>>();
            tagAndContent.Add(new Tuple<string, DiscordEmbed>(tag, embed));
        }
    }
}
