using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    public class ServerInfoDisplay : Display
    {
        protected override int TimerUpdateIntervalMS { get { return 60000; } }
        protected override string BaseTag { get { return "[Server Info]"; } }
        protected override int TimerStartDelayMS { get { return 0; } }

        protected override TriggerType GetTriggers()
        {
            return TriggerType.Startup | TriggerType.Timer | TriggerType.Login;
        }

        protected override List<ChannelLink> GetChannelLinks()
        {
            return DLConfig.Data.ServerInfoChannels.Cast<ChannelLink>().ToList();
        }

        protected override void GetDisplayContent(ChannelLink link, out List<Tuple<string, DiscordEmbed>> tagAndContent)
        {
            DiscordEmbed content = MessageBuilder.GetServerInfo(GetServerInfoFlagForChannel(link as ServerInfoChannel));

            tagAndContent = new List<Tuple<string, DiscordEmbed>>();
            tagAndContent.Add(new Tuple<string, DiscordEmbed>(BaseTag, content));
        }

        private static MessageBuilder.ServerInfoComponentFlag GetServerInfoFlagForChannel(ServerInfoChannel statusChannel)
        {
            MessageBuilder.ServerInfoComponentFlag statusFlag = 0;
            if (statusChannel.UseName)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.Name;
            if (statusChannel.UseDescription)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.Description;
            if (statusChannel.UseLogo)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.Logo;
            if (statusChannel.UseAddress)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.ServerAddress;
            if (statusChannel.UsePlayerCount)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.PlayerCount;
            if (statusChannel.UseTimeSinceStart)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.TimeSinceStart;
            if (statusChannel.UseTimeRemaining)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.TimeRemaining;
            if (statusChannel.UseMeteorHasHit)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.MeteorHasHit;

            return statusFlag;
        }
    }
}
