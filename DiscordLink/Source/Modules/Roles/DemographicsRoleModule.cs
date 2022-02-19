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
using System.Collections.Generic;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class DemographicsRoleModule : Module
    {
        private static readonly DiscordColor DemographicColor = DiscordColor.Wheat;
        private IEnumerable<DiscordGuild> Guilds = Enumerable.Empty<DiscordGuild>();

        public override string ToString()
        {
            return "Demographics Role Module";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.DiscordClientConnected | DLEventType.AccountLinkVerified | DLEventType.AccountLinkRemoved | DLEventType.EnteredDemographic | DLEventType.LeftDemographic;
        }

        public override void Setup()
        {
            Guilds = DiscordLink.Obj.Client.Guilds.Where(guild => DiscordLink.Obj.Client.BotHasPermission(Permissions.ManageRoles, guild));

            base.Setup();
        }

        protected override async Task<bool> ShouldRun()
        {
            return DiscordLink.Obj.Client.Guilds.Any(guild => DiscordLink.Obj.Client.BotHasPermission(Permissions.ManageRoles, guild));
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            DLDiscordClient client = DiscordLink.Obj.Client;
            foreach (var guild in Guilds)
            {
                if (!client.BotHasPermission(Permissions.ManageRoles, guild))
                    return;

                if (trigger == DLEventType.DiscordClientConnected)
                {
                    if (!client.BotHasIntent(DiscordIntents.GuildMembers))
                        return;

                    ++_opsCount;
                    foreach (DiscordMember member in await client.GetGuildMembersAsync(guild))
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
        }

        private async Task AddDemographicRole(DLDiscordClient client, DiscordMember member, string demographicName)
        {
            await client.AddRoleAsync(member, new DiscordLinkRole(demographicName, null, DemographicColor, false, true, $"User is in the {demographicName} demographic"), member.Guild);
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
