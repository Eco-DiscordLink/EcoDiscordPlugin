using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.Civics.Titles;
using Eco.Gameplay.Players;
using Eco.Gameplay.Settlements;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Lookups;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class ElectedTitleRoleModule : RoleModule
    {

        private static readonly DiscordColor DemographicColor = DiscordColor.Blurple;

        public override string ToString()
        {
            return "Elected Title Role Module";
        }

        protected override DLEventType GetTriggers()
        {
            return base.GetTriggers() | DLEventType.DiscordClientConnected | DLEventType.AccountLinkVerified | DLEventType.AccountLinkRemoved | DLEventType.ElectionStopped | DLEventType.SettlementFounded;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            DiscordClient client = DiscordLink.Obj.Client;
            if (!client.BotHasPermission(Permissions.ManageRoles))
                return;

            if (trigger == DLEventType.DiscordClientConnected || trigger == DLEventType.ForceUpdate)
            {
                ++_opsCount;
                foreach (DiscordMember member in await client.GetGuildMembersAsync())
                {
                    LinkedUser linkedUser = UserLinkManager.LinkedUserByDiscordUser(member);
                    foreach (ElectedTitle title in Lookups.ActiveElectedTitles)
                    {
                        string titleName = title.Name;
                        if (linkedUser == null || !DLConfig.Data.UseElectedTitleRoles || !title.ContainsUser(linkedUser.EcoUser))
                        {
                            if (member.HasRoleWithName(titleName))
                            {
                                ++_opsCount;
                                await client.RemoveRoleAsync(member, titleName);
                            }
                        }
                        else if (!member.HasRoleWithName(titleName) && title.ContainsUser(linkedUser.EcoUser))
                        {
                            ++_opsCount;
                            await AddElectedTitleRole(client, linkedUser.DiscordMember, titleName);
                        }
                    }
                }
            }
            else if (trigger == DLEventType.AccountLinkVerified || trigger == DLEventType.AccountLinkRemoved)
            {
                if (!DLConfig.Data.UseDemographicRoles)
                    return;

                if (!(data[0] is LinkedUser linkedUser))
                    return;

                DiscordMember member = linkedUser.DiscordMember;
                if (member == null)
                {
                    Logger.Error($"Failed to handle account link role change for Eco user \"{linkedUser.EcoUser.Name}\". Linked Discord member could not be fetched.");
                    return;
                }

                foreach (ElectedTitle title in Lookups.ActiveElectedTitles)
                {
                    string titleName = title.Name;
                    if (trigger == DLEventType.AccountLinkRemoved)
                    {
                        if (member.HasRoleWithName(titleName))
                        {
                            ++_opsCount;
                            await client.RemoveRoleAsync(member, titleName);
                        }
                    }
                    else if (trigger == DLEventType.AccountLinkVerified)
                    {
                        if (!member.HasRoleWithName(titleName) && title.ContainsUser(linkedUser.EcoUser))
                        {
                            ++_opsCount;
                            await AddElectedTitleRole(client, member, titleName);
                        }
                    }
                }
            }
            else if (trigger == DLEventType.ElectionStopped)
            {
                if (!DLConfig.Data.UseDemographicRoles)
                    return;

                if (!(data[0] is Election election))
                    return;

                ElectedTitle title = election.PositionForWinner;
                if (title == null)
                    return;

                ElectionResult results = election.CurrentResults;
                if (!results.Passed)
                    return;

                foreach(User winner in results.WinningUsers)
                {
                    LinkedUser linkedUser = UserLinkManager.LinkedUserByEcoUser(winner);
                    if(linkedUser == null)
                        continue;

                    ++_opsCount;
                    await AddElectedTitleRole(client, linkedUser.DiscordMember, title.Name);
                }
                
            }
            else if(trigger == DLEventType.SettlementFounded)
            {
                if (!DLConfig.Data.UseDemographicRoles)
                    return;

                if (!(data[0] is Settlement settlement))
                    return;

                if (settlement.Leader == null)
                    return;

                foreach(User leader in settlement.Leader.DirectOccupants)
                {
                    LinkedUser linkedUser = UserLinkManager.LinkedUserByEcoUser(leader);
                    if (linkedUser == null)
                        continue;

                    ++_opsCount;
                    await AddElectedTitleRole(client, linkedUser.DiscordMember, settlement.Leader.Name);
                }
            }
        }

        private async Task AddElectedTitleRole(DiscordClient client, DiscordMember member, string titleName)
        {
            await client.AddRoleAsync(member, new DiscordLinkRole(titleName, null, DemographicColor, false, true, $"User has been elected as {titleName}"));
        }
    }
}
