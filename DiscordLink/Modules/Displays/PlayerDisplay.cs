using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class PlayerDisplay : Display
    {
        protected override string BaseTag { get { return "[Player List]"; } }
        protected override int TimerUpdateIntervalMS { get { return 60000; } }
        protected override int TimerStartDelayMS { get { return 5000; } }

        public override string ToString()
        {
            return "Player Display";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.Startup | DLEventType.Timer | DLEventType.Join | DLEventType.Login | DLEventType.Logout;
        }

        protected override List<ChannelLink> GetChannelLinks()
        {
            return DLConfig.Data.PlayerListChannels.Cast<ChannelLink>().ToList();
        }

        protected override void GetDisplayContent(ChannelLink link, out List<Tuple<string, DiscordEmbed>> tagAndContent)
        {
            PlayerListChannelLink playerListLink = link as PlayerListChannelLink;

            string tag = BaseTag;
            string title = "Players";
            string content = "\n" + MessageBuilder.Shared.GetPlayerList(playerListLink.UseLoggedInTime);

            if (playerListLink.UsePlayerCount == true)
            {
                title = MessageBuilder.Shared.GetPlayerCount() + " Players Online";
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(MessageBuilder.Discord.EmbedColor)
                .WithTitle(title)
                .WithDescription(content)
                .WithFooter(MessageBuilder.Discord.GetStandardEmbedFooter());

            tagAndContent = new List<Tuple<string, DiscordEmbed>>();
            tagAndContent.Add(new Tuple<string, DiscordEmbed>(tag, embed));
        }
    }
}
