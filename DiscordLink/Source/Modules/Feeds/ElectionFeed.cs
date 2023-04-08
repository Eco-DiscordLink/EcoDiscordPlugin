using Eco.EW.Tools;
using Eco.Gameplay.Civics.Elections;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    class ElectionFeed : FeedModule
    {
        public override string ToString()
        {
            return "Election Feed";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.StartElection | DLEventType.StopElection;
        }

        protected override async Task<bool> ShouldRun()
        {
            foreach (ChannelLink link in DLConfig.Data.ElectionFeedChannels)
            {
                if (link.IsValid())
                    return true;
            }
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            if (!(data[0] is Election election))
                return;

            DiscordLinkEmbed embed = new DiscordLinkEmbed();
            switch (trigger)
            {
                case DLEventType.StartElection:
                    embed.WithTitle($":ballot_box:  {MessageUtils.StripTags(election.Creator.Name)} Started An Election :ballot_box: ");
                    embed.AddField("Title", MessageUtils.StripTags(election.Name), inline: true);
                    embed.AddField("Process", MessageUtils.StripTags(election.Process.Name), inline: true);
                    embed.AddField("Time", TimeFormatter.FormatSpan(election.TimeLeft), inline: true);
                    break;

                case DLEventType.StopElection:
                    ElectionResult results = election.CurrentResults;
                    embed.WithTitle($":ballot_box:  Election Has Ended  :ballot_box: ");
                    embed.AddField("Title", MessageUtils.StripTags(election.Name));
                    if (results.Vetoed)
                    {
                        embed.AddField("Result", "Vetoed", inline: true);
                        embed.AddField("Vetoer", MessageUtils.StripTags(results.Vetoer.Name), inline: true);
                        embed.AddField("Time left when vetoed", TimeFormatter.FormatSpan(election.TimeLeft), inline: true);
                    }
                    else
                    {
                        if (results.Tied)
                        {
                            embed.AddField("Result", "Tie - No action taken", inline: true);
                        }
                        else if (election.BooleanElection)
                        {
                            embed.AddField("Result", results.Passed ? "Passed" : "Failed", inline: true);
                            embed.AddField("Votes", $"For - **{results.YesVotes}**\nAgainst - **{results.NoVotes}**", inline: true);
                        }
                        else
                        {
                            bool singleWinner = results.WinningUsers.Length == 1;
                            string title;
                            string winningUsers;
                            if (singleWinner)
                            {
                                title = "Winner";
                                winningUsers = MessageUtils.StripTags(results.WinningUsers[0].Name);
                            }
                            else
                            {
                                title = "Winners";
                                winningUsers = MessageUtils.StripTags(string.Join("\n", (object[])results.WinningUsers));
                            }
                            embed.AddField(title, winningUsers, inline: true);
                        }
                    }
                    break;

                default:
                    Logger.Debug("Election Feed received unexpected trigger type");
                    return;
            }

            foreach (ChannelLink electionLink in DLConfig.Data.ElectionFeedChannels)
            {
                if (!electionLink.IsValid())
                    continue;

                await DiscordLink.Obj.Client.SendMessageAsync(electionLink.Channel, null, embed);
                ++_opsCount;
            }
        }
    }
}
