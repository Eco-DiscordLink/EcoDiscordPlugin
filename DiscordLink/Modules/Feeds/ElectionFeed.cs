using DiscordLink.Extensions;
using DSharpPlus.Entities;
using Eco.Gameplay.Civics.Elections;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    class ElectionFeed : Feed
    {
        public override string ToString()
        {
            return "Election Feed";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.StartElection | DLEventType.StopElection;
        }

        protected override bool ShouldRun()
        {
            foreach (ChannelLink link in DLConfig.Data.ElectionFeedChannels)
            {
                if (link.IsValid())
                    return true;
            }
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, object data)
        {
            if (!(data is Election election))
                return;

            DiscordLinkEmbed embed = new DiscordLinkEmbed();
            switch (trigger)
            {
                case DLEventType.StartElection:
                    embed.WithTitle($":ballot_box:  {MessageUtil.StripTags(election.Creator.Name)} Started An Election :ballot_box: ");
                    embed.AddField("Title", MessageUtil.StripTags(election.Name), inline: true);
                    embed.AddField("Process", MessageUtil.StripTags(election.Process.Name), inline: true);
                    embed.AddField("Time", TimeFormatter.FormatSpan(election.TimeLeft), inline: true);
                    break;

                case DLEventType.StopElection:
                    ElectionResults results = election.Results;
                    embed.WithTitle($":ballot_box:  Election Has Ended  :ballot_box: ");
                    embed.AddField("Title", MessageUtil.StripTags(election.Name));
                    if (results.Vetoed)
                    {
                        embed.AddField("Result", "Vetoed", inline: true);
                        embed.AddField("Vetoer", MessageUtil.StripTags(results.Vetoer.Name), inline: true);
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
                            bool singleWinner = results.WinningUsers.Count == 1;
                            string title;
                            string winningUsers;
                            if (singleWinner)
                            {
                                title = "Winner";
                                winningUsers = MessageUtil.StripTags(results.WinningUsers.GetAt(0).Name);
                            }
                            else
                            {
                                title = "Winners";
                                winningUsers = MessageUtil.StripTags(string.Join("\n", results.WinningUsers));
                            }
                            embed.AddField(title, winningUsers, inline: true);
                        }
                    }
                    break;

                default:
                    Logger.Debug("Election Feed received unexpected trigger type");
                    return;
            }

            embed.WithFooter(MessageBuilder.Discord.GetStandardEmbedFooter());

            foreach (ChannelLink electionChannel in DLConfig.Data.ElectionFeedChannels)
            {
                if (!electionChannel.IsValid()) continue;
                DiscordGuild discordGuild = plugin.GuildByNameOrId(electionChannel.DiscordGuild);
                if (discordGuild == null) continue;
                DiscordChannel discordChannel = discordGuild.ChannelByNameOrId(electionChannel.DiscordChannel);
                if (discordChannel == null) continue;
                await DiscordUtil.SendAsync(discordChannel, null, embed);
                ++_opsCount;
            }
        }
    }
}
