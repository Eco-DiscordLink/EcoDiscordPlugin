using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
using System.Linq;
using System.Threading.Tasks;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Civics.Demographics;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class DemographicsRoleModule : Module
    {
        private static readonly DiscordColor DemographicColor = DiscordColor.Wheat;

        public override string ToString()
        {
            return "Demographics Role Module";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.DiscordClientConnected | DLEventType.EnteredDemographic | DLEventType.LeftDemographic;
        }

        protected override bool ShouldRun()
        {
            return true;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            DLDiscordClient client = DiscordLink.Obj.Client;
            if (trigger == DLEventType.DiscordClientConnected)
            {
                if (client.BotHasIntent(DSharpPlus.DiscordIntents.GuildMembers))
                {
                    ++_opsCount;
                    foreach (DiscordMember member in await client.GetGuildMembersAsync(DLConfig.Data.Guild))
                    {
                        LinkedUser linkedUser = UserLinkManager.LinkedUserByDiscordUser(member);
                        foreach (Demographic demographic in EcoUtils.ActiveDemographics)
                        {
                            string demographicName = GetDemographicRoleName(demographic);
                            if (linkedUser == null || !DLConfig.Data.UseDemographicRoles || !demographic.Contains(linkedUser.EcoUser))
                            {
                                if (member.HasRoleWithName(demographicName))
                                {
                                    ++_opsCount;
                                    await client.RemoveRoleAsync(member, demographicName);
                                }
                            }
                            else if (!member.HasRoleWithName(demographicName) && demographic.Contains(linkedUser.EcoUser))
                            {
                                ++_opsCount;
                                await AddDemographicRole(client, linkedUser.DiscordMember, demographicName);
                            }
                        }
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

        private async Task AddDemographicRole(DLDiscordClient client, DiscordMember member, string demographicName)
        {
            await client.AddRoleAsync(member, new DiscordLinkRole(demographicName, null, DemographicColor, false, true, $"User is in the {demographicName} demographic"));
        }

        private string GetDemographicRoleName(Demographic demographic)
        {
            DemographicRoleReplacement replacement = DLConfig.Data.DemographicReplacementRoles.FirstOrDefault(s => s.DemographicName.EqualsCaseInsensitive(demographic.Name));
            return replacement != null
                ? replacement.RoleName
                : demographic.Name;
        }
    }
}
