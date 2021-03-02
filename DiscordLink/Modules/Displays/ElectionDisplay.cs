using DiscordLink.Extensions;
using DSharpPlus.Entities;
using Eco.Gameplay.Civics.Elections;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Eco.Plugins.DiscordLink.Modules
{
    class ElectionDisplay : Display
    {
        protected override int TimerUpdateIntervalMS { get { return 60000; } }
        protected override string BaseTag { get { return "[Election]"; } }
        protected override int TimerStartDelayMS { get { return 15000; } }

        public override string ToString()
        {
            return "Election Display";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.DiscordClientStarted | DLEventType.Timer | DLEventType.Login
                | DLEventType.Vote | DLEventType.StartElection | DLEventType.StopElection;
        }

        protected override List<DiscordTarget> GetDiscordTargets()
        {
            return DLConfig.Data.ElectionChannels.Cast<DiscordTarget>().ToList();
        }

        protected override void GetDisplayContent(DiscordTarget target, out List<Tuple<string, DiscordLinkEmbed>> tagAndContent)
        {
            tagAndContent = new List<Tuple<string, DiscordLinkEmbed>>();
            DiscordLinkEmbed embed = new DiscordLinkEmbed();
            embed.WithFooter(MessageBuilder.Discord.GetStandardEmbedFooter());
            foreach (Election election in EcoUtil.ActiveElections)
            {
                string tag = $"{BaseTag} [{election.Id}]";
                embed.WithTitle(MessageUtil.StripTags(election.Name));

                // Proposer name
                embed.AddField("Proposer", election.Creator.Name);

                // Time left
                embed.AddField("Time Left", TimeFormatter.FormatSpan(election.TimeLeft));

                // Process
                embed.AddField("Process", MessageUtil.StripTags(election.Process.Name));

                // Choices
                if (!election.BooleanElection && election.Choices.Count > 0)
                {
                    string choiceDesc = string.Empty;
                    foreach (ElectionChoice choice in election.Choices)
                    {
                        choiceDesc += $"{choice.Name}\n";
                    }
                    embed.AddField("Choices", choiceDesc);
                }

                // Votes
                string voteDesc = string.Empty;
                if (!election.Process.AnonymousVoting)
                {
                    foreach (RunoffVote vote in election.Votes)
                    {
                        string topChoiceName = null;
                        int topChoiceID = vote.RankedVotes.FirstOrDefault();
                        foreach (ElectionChoice choice in election.Choices)
                        {
                            if (choice.ID == topChoiceID)
                            {
                                topChoiceName = choice.Name;
                                break;
                            }
                        }
                        voteDesc += $"{vote.Voter.Name} : {topChoiceName}\n";
                    }
                }
                else
                {
                    voteDesc = "--- Anonymous Voting ---";
                }

                if (string.IsNullOrEmpty(voteDesc))
                    voteDesc = "--- No Votes Recorded ---";

                embed.AddField($"Votes ({election.TotalVotes})", voteDesc);

                if (embed.Fields.Count > 0)
                    tagAndContent.Add(new Tuple<string, DiscordLinkEmbed>(tag, new DiscordLinkEmbed(embed)));

                embed.ClearFields();
            }
        }
    }
}
