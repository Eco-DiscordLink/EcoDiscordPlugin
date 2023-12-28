using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
using System.Linq;
using System.Threading.Tasks;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Civics.Demographics;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using DSharpPlus;
using Eco.Moose.Tools;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class DemographicsRoleModule : RoleModule
    {
        private static readonly DiscordColor DemographicColor = DiscordColor.Wheat;

        public override string ToString()
        {
            return "Demographics Role Module";
        }

        protected override DLEventType GetTriggers()
        {
            return base.GetTriggers() | DLEventType.DiscordClientConnected | DLEventType.AccountLinkVerified | DLEventType.AccountLinkRemoved | DLEventType.EnteredDemographic | DLEventType.LeftDemographic;
        }

        protected override async Task UpdateInternal(Plugin plugin, DLEventType trigger, params object[] data)
        {
            DLDiscordClient client = Plugin.Obj.Client;
            if (!client.BotHasPermission(Permissions.ManageRoles))
                return;

            if (trigger == DLEventType.DiscordClientConnected || trigger == DLEventType.ForceUpdate)
            {
                if (!client.BotHasIntent(DiscordIntents.GuildMembers))
                    return;

                ++_opsCount;
                foreach (DiscordMember member in await client.GetGuildMembersAsync())
                {
                    LinkedUser linkedUser = UserLinkManager.LinkedUserByDiscordUser(member);
                    foreach (Demographic demographic in EcoUtils.ActiveDemographics)
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
                            await AddDemographicRole(client, linkedUser.DiscordMember, demographicName);
                        }
                    }
                }
            }
            else if (trigger == DLEventType.AccountLinkVerified || trigger == DLEventType.AccountLinkRemoved)
            {
                if (!(data[0] is LinkedUser linkedUser))
                    return;

                DiscordMember member = linkedUser.DiscordMember;
                if (member == null)
                {
                    Logger.Error($"Failed to handle account link role change for Eco user \"{linkedUser.EcoUser.Name}\". Linked Discord member could not be fetched.");
                    return;
                }

                foreach (Demographic demographic in EcoUtils.ActiveDemographics)
                {
                    string demographicName = GetDemographicRoleName(demographic);
                    if (trigger == DLEventType.AccountLinkRemoved || !DLConfig.Data.UseDemographicRoles || !demographic.ContainsUser(linkedUser.EcoUser))
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
                        await AddDemographicRole(client, linkedUser.DiscordMember, demographicName);
                    }
                }
            }
            else
            {
                if (!DLConfig.Data.UseDemographicRoles)
                    return;

                if (!(data[0] is DemographicChange demographicChange))
                    return;

                LinkedUser linkedUser = UserLinkManager.LinkedUserByEcoUser(demographicChange.Citizen);
                if (linkedUser == null)
                    return;

                string demographicName = GetDemographicRoleName(demographicChange.Demographic);
                if (trigger == DLEventType.EnteredDemographic)
                {
                    ++_opsCount;
                    await AddDemographicRole(client, linkedUser.DiscordMember, demographicName);
                }
                else if (trigger == DLEventType.LeftDemographic)
                {
                    ++_opsCount;
                    await client.RemoveRoleAsync(linkedUser.DiscordMember, demographicName);
                }
            }
        }

        public static string GetDemographicRoleName(Demographic demographic)
        {
            DemographicRoleReplacement replacement = DLConfig.Data.DemographicReplacementRoles.FirstOrDefault(s => !string.IsNullOrEmpty(s.DemographicName)
                && !string.IsNullOrEmpty(s.RoleName) && s.DemographicName.EqualsCaseInsensitive(demographic.Name));
            return replacement != null
                ? replacement.RoleName
                : demographic.Name;
        }

        private async Task AddDemographicRole(DLDiscordClient client, DiscordMember member, string demographicName)
        {
            await client.AddRoleAsync(member, new DiscordLinkRole(demographicName, null, DemographicColor, false, true, $"User is in the {demographicName} demographic"));
        }
    }
}
