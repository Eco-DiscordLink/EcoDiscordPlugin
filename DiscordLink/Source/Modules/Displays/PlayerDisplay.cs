using DiscordLink.Extensions;
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
            return base.GetTriggers() | DLEventType.DiscordClientStarted | DLEventType.Timer | DLEventType.Join | DLEventType.Login | DLEventType.Logout;
        }

        protected override List<DiscordTarget> GetDiscordTargets()
        {
            return DLConfig.Data.PlayerListDisplayChannels.Cast<DiscordTarget>().ToList();
        }

        protected override void GetDisplayContent(DiscordTarget target, out List<Tuple<string, DiscordLinkEmbed>> tagAndContent)
        {
            tagAndContent = new List<Tuple<string, DiscordLinkEmbed>>();

            if (!(target is PlayerListChannelLink playerListLink))
                return;

            string tag = BaseTag;
            string title = "Players";

            if (playerListLink.UsePlayerCount == true)
            {
                title = MessageBuilder.Shared.GetPlayerCount() + " Players Online";
            }

            DiscordLinkEmbed embed = new DiscordLinkEmbed()
                .WithTitle(title)
                .WithFooter(MessageBuilder.Discord.GetStandardEmbedFooter());
            embed.AddField("Online Players", MessageBuilder.Shared.GetPlayerList(), inline: true);

            if(playerListLink.UseLoggedInTime)
            {
                embed.AddField("Session Time", MessageBuilder.Shared.GetPlayerSessionTimeList(), inline: true);
            }

            tagAndContent.Add(new Tuple<string, DiscordLinkEmbed>(tag, embed));
        }
    }
}
