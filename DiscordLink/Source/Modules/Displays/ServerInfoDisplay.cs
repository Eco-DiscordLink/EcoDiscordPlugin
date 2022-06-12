using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class ServerInfoDisplay : DisplayModule
    {
        protected override int TimerUpdateIntervalMS { get { return 60000; } }
        protected override string BaseTag { get { return "[Server Info]"; } }
        protected override int TimerStartDelayMS { get { return 0; } }

        public override string ToString()
        {
            return "Server Info Display";
        }

        protected override DLEventType GetTriggers()
        {
            return base.GetTriggers() | DLEventType.DiscordClientConnected | DLEventType.Timer | DLEventType.Login | DLEventType.StartElection
                | DLEventType.StopElection | DLEventType.Vote;
        }

        protected override async Task<List<DiscordTarget>> GetDiscordTargets()
        {
            return DLConfig.Data.ServerInfoDisplayChannels.Cast<DiscordTarget>().ToList();
        }

        protected override void GetDisplayContent(DiscordTarget target, out List<Tuple<string, DiscordLinkEmbed>> tagAndContent)
        {
            tagAndContent = new List<Tuple<string, DiscordLinkEmbed>>();
            if (!(target is ServerInfoChannel serverInfoChannel))
                return;

            DiscordLinkEmbed content = MessageBuilder.Discord.GetServerInfo(GetServerInfoFlagForChannel(serverInfoChannel));
            tagAndContent.Add(new Tuple<string, DiscordLinkEmbed>(BaseTag, content));
        }

        private static MessageBuilder.ServerInfoComponentFlag GetServerInfoFlagForChannel(ServerInfoChannel infoChannel)
        {
            MessageBuilder.ServerInfoComponentFlag statusFlag = 0;
            if (infoChannel.UseName)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.Name;
            if (infoChannel.UseDescription)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.Description;
            if (infoChannel.UseLogo)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.Logo;
            if (infoChannel.UseConnectionInfo)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.ConnectionInfo;
            if (infoChannel.UseWebServerAddress)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.WebServerAddress;
            if (infoChannel.UsePlayerCount)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.PlayerCount;
            if (infoChannel.UsePlayerList)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.PlayerList;
            if (infoChannel.UsePlayerListLoggedInTime)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.PlayerListLoginTime;
            if (infoChannel.UsePlayerListExhaustionTime)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.PlayerListExhaustionTime;
            if (infoChannel.UseIngameTime)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.IngameTime;
            if (infoChannel.UseTimeRemaining)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.MeteorTimeRemaining;
            if (infoChannel.UseServerTime)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.ServerTime;
            if (infoChannel.UseExhaustionResetTimeLeft)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.ExhaustionResetTimeLeft;
            if (infoChannel.UseExhaustedPlayerCount)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.ExhaustedPlayerCount;
            if (infoChannel.UseElectionCount)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.ActiveElectionCount;
            if (infoChannel.UseElectionList)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.ActiveElectionList;
            if (infoChannel.UseLawCount)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.LawCount;
            if (infoChannel.UseLawList)
                statusFlag |= MessageBuilder.ServerInfoComponentFlag.LawList;

            return statusFlag;
        }
    }
}
