using DSharpPlus.Entities;
using Eco.Gameplay.Civics;
using Eco.Gameplay.Civics.Elections;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    class ElectionDisplay : Display
    {
        protected override int TimerUpdateIntervalMS { get { return 60000; } }
        protected override string BaseTag { get { return "[Election]"; } }
        protected override int TimerStartDelayMS { get { return 15000; } }

        protected override TriggerType GetTriggers()
        {
            return TriggerType.Startup | TriggerType.Timer | TriggerType.Login
                | TriggerType.Vote | TriggerType.StartElection | TriggerType.StopElection;
        }

        protected override List<ChannelLink> GetChannelLinks()
        {
            return DLConfig.Data.ElectionChannels.Cast<ChannelLink>().ToList();
        }

        protected override void GetDisplayContent(ChannelLink link, out List<Tuple<string, DiscordEmbed>> tagAndContent)
        {
            tagAndContent = new List<Tuple<string, DiscordEmbed>>();
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.WithColor(MessageBuilder.EmbedColor);
            builder.WithFooter(MessageBuilder.GetStandardEmbedFooter());
            foreach (Election election in EcoUtil.ActiveElections)
            {
                string tag = BaseTag + " [" + election.Id + "]";
                builder.WithTitle(election.Name);

                // Proposer name
                builder.AddField("Proposer", election.Creator.Name);

                // Time left
                builder.AddField("Time Left", TimeFormatter.FormatSpan(election.TimeLeft));

                // Process
                builder.AddField("Process", election.Process.Name);

                // Choices
                if (!election.BooleanElection && election.Choices.Count > 0)
                {
                    string choiceDesc = string.Empty;
                    foreach (ElectionChoice choice in election.Choices)
                    {
                        choiceDesc += choice.Name + "\n";
                    }
                    builder.AddField("Choices", choiceDesc);
                }

                // Votes
                string voteDesc = string.Empty;
                foreach (RunoffVote vote in election.Votes)
                {
                    string topChoiceName = null;
                    int topChoiceID = vote.RankedVotes.FirstOrDefault();
                    foreach (ElectionChoice choice in election.Choices)
                    {
                        if(choice.ID == topChoiceID)
                        {
                            topChoiceName = choice.Name;
                            break;
                        }
                    }
                    voteDesc += vote.Voter.Name + ": " + topChoiceName + "\n";
                }
                if(string.IsNullOrEmpty(voteDesc))
                {
                    voteDesc = "--- No Votes Recorded ---";
                }
                builder.AddField("Votes (" + election.TotalVotes + ")", voteDesc);

                if (builder.Fields.Count > 0)
                {
                    tagAndContent.Add(new Tuple<string, DiscordEmbed>(tag, builder.Build()));
                }
                builder.ClearFields();
            }
        }
    }
}
