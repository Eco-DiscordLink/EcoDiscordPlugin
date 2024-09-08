using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Gameplay.Civics.Demographics;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Settlements;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Lookups;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Shared.Utils;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class DemographicsRoleModule : RoleModule
    {
        private static readonly DiscordColor DemographicColor = DiscordColor.Wheat;

        public override string ToString()
        {
            return "Demographics Role Module";
        }

        protected override DlEventType GetTriggers()
        {
            return base.GetTriggers() | DlEventType.DiscordClientConnected | DlEventType.AccountLinkVerified | DlEventType.AccountLinkRemoved | DlEventType.EnteredDemographic | DlEventType.LeftDemographic;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DlEventType trigger, params object[] data)
        {
            DiscordClient client = DiscordLink.Obj.Client;
            if (!client.BotHasPermission(Permissions.ManageRoles))
                return;

            if (trigger == DlEventType.DiscordClientConnected || trigger == DlEventType.ForceUpdate)
            {
                if (!client.BotHasIntent(DiscordIntents.GuildMembers))
                    return;

                ++_opsCount;
                foreach (DiscordMember member in await client.GetMembersAsync())
                {
                    LinkedUser linkedUser = UserLinkManager.LinkedUserByDiscordUser(member);
                    foreach (Demographic demographic in Lookups.ActiveDemographics)
                    {
                        string demographicName = GetDemographicRoleName(demographic);
                        if (linkedUser == null || !DLConfig.Data.UseDemographicRoles || !demographic.ContainsUser(linkedUser.EcoUser))
                        {
                            if (member.HasRoleWithName(demographicName))
                            {
                                ++_opsCount;
                                await client.RemoveRoleAsync(member, demographicName);
                            }
                        }
                        else if (!member.HasRoleWithName(demographicName) && demographic.ContainsUser(linkedUser.EcoUser))
                        {
                            ++_opsCount;
                            await AddDemographicRole(client, member, demographicName);
                        }
                    }
                }
            }
            else if (trigger == DlEventType.AccountLinkVerified || trigger == DlEventType.AccountLinkRemoved)
            {
                if (!DLConfig.Data.UseDemographicRoles)
                    return;

                if (!(data[0] is LinkedUser linkedUser))
                    return;

                DiscordMember member = linkedUser.DiscordMember;
                if (member == null)
                {
                    Logger.Error($"Failed to handle role change for Eco user \"{linkedUser.EcoUser.Name}\". Linked Discord member was not loaded.");
                    return;
                }

                foreach (Demographic demographic in Lookups.ActiveDemographics)
                {
                    string demographicName = GetDemographicRoleName(demographic);
                    if (trigger == DlEventType.AccountLinkRemoved)
                    {
                        if (member.HasRoleWithName(demographicName))
                        {
                            ++_opsCount;
                            await client.RemoveRoleAsync(member, demographicName);
                        }
                    }
                    else if (trigger == DlEventType.AccountLinkVerified)
                    {
                        if (!member.HasRoleWithName(demographicName) && demographic.ContainsUser(linkedUser.EcoUser))
                        {
                            ++_opsCount;
                            await AddDemographicRole(client, linkedUser.DiscordMember, demographicName);
                        }
                    }
                }
            }
            else if (trigger == DlEventType.EnteredDemographic || trigger == DlEventType.LeftDemographic)
            {
                if (!DLConfig.Data.UseDemographicRoles)
                    return;

                if (!(data[0] is DemographicChange demographicChange))
                    return;

                LinkedUser linkedUser = UserLinkManager.LinkedUserByEcoUser(demographicChange.Citizen);
                if (linkedUser == null)
                    return;

                Demographic demographic = demographicChange.Demographic;
                string demographicName = GetDemographicRoleName(demographic);
                if (trigger == DlEventType.EnteredDemographic)
                {
                    Settlement settlement = demographicChange.Demographic.Settlement;
                    if (!demographic.IsSpecial && settlement == null && !settlement.Founded) // Settlement is null for special demographics
                        return;

                    ++_opsCount;
                    await AddDemographicRole(client, linkedUser.DiscordMember, demographicName);
                }
                else if (trigger == DlEventType.LeftDemographic)
                {
                    ++_opsCount;
                    await client.RemoveRoleAsync(linkedUser.DiscordMember, demographicName);
                }
            }
        }

        public static string GetDemographicRoleName(Demographic demographic)
        {
            DemographicRoleSubstitution replacement = DLConfig.Data.DemographicReplacementRoles.FirstOrDefault(s => !string.IsNullOrEmpty(s.DemographicName)
                && !string.IsNullOrEmpty(s.RoleName) && s.DemographicName.EqualsCaseInsensitive(demographic.Name));
            return replacement != null
                ? replacement.RoleName
                : demographic.Name;
        }

        private async Task AddDemographicRole(DiscordClient client, DiscordMember member, string demographicName)
        {
            await client.AddRoleAsync(member, new DiscordLinkRole(demographicName, null, DemographicColor, false, true, $"User is in the {demographicName} demographic"));
        }
    }
}
