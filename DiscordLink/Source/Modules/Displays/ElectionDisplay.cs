using DSharpPlus.Entities;
using Eco.Core.Utils;
using Eco.Gameplay.Civics.Elections;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Lookups;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Eco.Plugins.DiscordLink.Enums;

namespace Eco.Plugins.DiscordLink.Modules
{
    class ElectionDisplay : DisplayModule
    {
        protected override int TimerUpdateIntervalMs { get { return 60000; } }
        protected override string BaseTag { get { return "[Election]"; } }
        protected override int TimerStartDelayMs { get { return 15000; } }

        public override string ToString()
        {
            return "Election Display";
        }

        protected override DlEventType GetTriggers()
        {
            return base.GetTriggers() | DlEventType.DiscordClientConnected | DlEventType.Timer | DlEventType.Login
                | DlEventType.Vote | DlEventType.ElectionStarted | DlEventType.ElectionStopped;
        }

        protected override async Task<List<DiscordTarget>> GetDiscordTargets()
        {
            return DLConfig.Data.ElectionDisplayChannels.Cast<DiscordTarget>().ToList();
        }

        protected override void GetDisplayContent(DiscordTarget target, out List<Tuple<string, DiscordLinkEmbed>> tagAndContent)
        {
            tagAndContent = new List<Tuple<string, DiscordLinkEmbed>>();

            foreach (Election election in Lookups.ActiveElections)
            {
                string tag = $"{BaseTag} [{election.Id}]";
                DiscordLinkEmbed report = MessageBuilder.Discord.GetElectionReport(election);
                if (report.Fields.Count > 0)
                    tagAndContent.Add(new Tuple<string, DiscordLinkEmbed>(tag, report));
            }
        }

        protected async override Task PostDisplayCreated(DiscordMessage message)
        {
            Election election = GetElectionFromMessage(message);
            if (election != null && election.BooleanElection)
                await CreateVoteReactions(message);
        }

        protected async override Task HandleReactionChange(DiscordUser user, DiscordMessage message, DiscordEmoji emoji, DiscordReactionChange changeType)
        {
            if (emoji != DLConstants.ACCEPT_EMOJI && emoji != DLConstants.DENY_EMOJI)
                return;

            if (changeType != DiscordReactionChange.Added)
                return;

            Election election = GetElectionFromMessage(message);
            if (election == null || !election.BooleanElection)
                return;

            message.GetChannel().Guild.Members.TryGetValue(user.Id, out DiscordMember member);
            LinkedUser linkedUser = UserLinkManager.LinkedUserByDiscordUser(user, member, "Reaction Voting");
            if (linkedUser == null)
                return;

            if(!election.CanVote(linkedUser.EcoUser))
            {
                await linkedUser.DiscordMember.SendMessageAsync($"Your vote in election \"{election.Name}\" has not been registered as you are not an eligable voter for this election.");
                return;
            }

            string choice = emoji == DLConstants.ACCEPT_EMOJI ? "Yes" : "No";
            Result result = election.Vote(new UserRunoffVote(linkedUser.EcoUser, election.GetChoiceByName(choice).ID));
            if (result.Failed)
                Logger.Debug($"Failed to cast rection vote of type \"{choice}\" for Discord user \"{user.Username}\" in election {election.Id}. Message: {result.Message}");

            if (election.Process.AnonymousVoting)
            {
                await message.DeleteAllReactionsAsync("DiscordLink - Anonymous Election");
                await CreateVoteReactions(message);
            }
        }

        private Election GetElectionFromMessage(DiscordMessage message)
        {
            Election election = null;
            foreach (TargetDisplayData displayData in TargetDisplays)
            {
                if (!(displayData.Target is ChannelLink channelLink))
                    continue;

                string tag = displayData.DisplayMessages.GetValueOrDefault(message.Id);
                if (string.IsNullOrWhiteSpace(tag))
                    continue;

                if (!int.TryParse(tag, out int electionId))
                    continue;

                Election foundElection = Lookups.ActiveElections.FirstOrDefault(e => e.Id == electionId);
                if (foundElection != null)
                {
                    election = foundElection;
                    break;
                }
            }
            return election;
        }

        private async Task CreateVoteReactions(DiscordMessage message)
        {
            if (DiscordLink.Obj.Client.ChannelHasPermission(message.GetChannel(), DSharpPlus.Permissions.AddReactions))
            {
                await message.CreateReactionAsync(DLConstants.ACCEPT_EMOJI);
                await message.CreateReactionAsync(DLConstants.DENY_EMOJI);
            }
        }
    }
}
