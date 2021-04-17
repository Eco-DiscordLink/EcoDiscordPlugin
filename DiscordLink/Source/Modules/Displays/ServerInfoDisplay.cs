using DiscordLink.Extensions;
using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class ServerInfoDisplay : Display
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
            return base.GetTriggers() | DLEventType.DiscordClientStarted | DLEventType.Timer | DLEventType.Login| DLEventType.StartElection
                | DLEventType.StopElection | DLEventType.Vote;
        }

        protected override List<DiscordTarget> GetDiscordTargets()
        {
            return DLConfig.Data.ServerInfoDisplayChannels.Cast<DiscordTarget>().ToList();
        }

        protected override void GetDisplayContent(DiscordTarget target, out List<Tuple<string, DiscordLinkEmbed>> tagAndContent)
        {
            tagAndContent = new List<Tuple<string, DiscordLinkEmbed>>();
            if (!(target is ServerInfoChannel serverInfoChannel))
                return;

            DiscordLinkEmbed content = MessageBuilders.Discord.GetServerInfo(GetServerInfoFlagForChannel(serverInfoChannel));
            tagAndContent.Add(new Tuple<string, DiscordLinkEmbed>(BaseTag, content));
        }

        private static MessageBuilders.ServerInfoComponentFlag GetServerInfoFlagForChannel(ServerInfoChannel statusChannel)
        {
            MessageBuilders.ServerInfoComponentFlag statusFlag = 0;
            if (statusChannel.UseName)
                statusFlag |= MessageBuilders.ServerInfoComponentFlag.Name;
            if (statusChannel.UseDescription)
                statusFlag |= MessageBuilders.ServerInfoComponentFlag.Description;
            if (statusChannel.UseLogo)
                statusFlag |= MessageBuilders.ServerInfoComponentFlag.Logo;
            if (statusChannel.UseConnectionInfo)
                statusFlag |= MessageBuilders.ServerInfoComponentFlag.ConnectionInfo;
            if (statusChannel.UsePlayerCount)
                statusFlag |= MessageBuilders.ServerInfoComponentFlag.PlayerCount;
            if (statusChannel.UsePlayerList)
                statusFlag |= MessageBuilders.ServerInfoComponentFlag.PlayerList;
            if (statusChannel.UsePlayerListLoggedInTime)
                statusFlag |= MessageBuilders.ServerInfoComponentFlag.PlayerListLoginTime;
            if (statusChannel.UseCurrentTime)
                statusFlag |= MessageBuilders.ServerInfoComponentFlag.CurrentTime;
            if (statusChannel.UseTimeRemaining)
                statusFlag |= MessageBuilders.ServerInfoComponentFlag.TimeRemaining;
            if (statusChannel.UseMeteorHasHit)
                statusFlag |= MessageBuilders.ServerInfoComponentFlag.MeteorHasHit;
            if (statusChannel.UseElectionCount)
                statusFlag |= MessageBuilders.ServerInfoComponentFlag.ActiveElectionCount;
            if (statusChannel.UseElectionList)
                statusFlag |= MessageBuilders.ServerInfoComponentFlag.ActiveElectionList;
            if (statusChannel.UseLawCount)
                statusFlag |= MessageBuilders.ServerInfoComponentFlag.LawCount;
            if (statusChannel.UseLawList)
                statusFlag |= MessageBuilders.ServerInfoComponentFlag.LawList;

            return statusFlag;
        }
    }
}
