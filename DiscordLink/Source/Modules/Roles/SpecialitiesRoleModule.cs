using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
using Eco.Moose.Extensions;
using Eco.Moose.Utils.Lookups;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Skills;
using System;
using System.Linq;
using System.Threading.Tasks;
using Eco.Moose.Tools.Logger;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class SpecialtiesRoleModule : RoleModule
    {
        private static readonly DiscordColor SpecialtyColor = DiscordColor.Green;

        private static readonly string[] IgnoredSpecialtyNames = { "SelfImprovementSkill" };

        public override string ToString()
        {
            return "Specialties Role Module";
        }

        protected override DlEventType GetTriggers()
        {
            return base.GetTriggers() | DlEventType.DiscordClientConnected | DlEventType.AccountLinkVerified | DlEventType.AccountLinkRemoved | DlEventType.GainedSpecialty | DlEventType.LostSpecialty;
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
                    foreach (Skill specialty in Lookups.Specialties)
                    {
                        if (IgnoredSpecialtyNames.Contains(specialty.Name))
                            continue;

                        if (linkedUser == null || !DLConfig.Data.UseSpecialtyRoles || !linkedUser.EcoUser.HasSpecialization(specialty.Type))
                        {
                            if (member.HasRoleWithName(specialty.DisplayName))
                            {
                                ++_opsCount;
                                await client.RemoveRoleAsync(member, specialty.DisplayName);
                            }
                        }
                        else if (!member.HasRoleWithName(specialty.DisplayName) && linkedUser.EcoUser.HasSpecialization(specialty.Type))
                        {
                            ++_opsCount;
                            await AddSpecialtyRole(client, linkedUser.DiscordMember, specialty.DisplayName);
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

                foreach (Skill specialty in Lookups.Specialties)
                {
                    if (IgnoredSpecialtyNames.Contains(specialty.Name))
                        continue;

                    if (trigger == DlEventType.AccountLinkRemoved)
                    {
                        if (member.HasRoleWithName(specialty.DisplayName))
                        {
                            ++_opsCount;
                            await client.RemoveRoleAsync(member, specialty.DisplayName);
                        }
                    }
                    else if (trigger == DlEventType.AccountLinkVerified)
                    {
                        if(!member.HasRoleWithName(specialty.DisplayName) && linkedUser.EcoUser.HasSpecialization(specialty.Type))
                        {
                            ++_opsCount;
                            await AddSpecialtyRole(client, member, specialty.DisplayName);
                        }
                    }
                }
            }
            else if (trigger == DlEventType.GainedSpecialty || trigger == DlEventType.LostSpecialty)
            {
                if (!DLConfig.Data.UseSpecialtyRoles)
                    return;

                SkillAction action = data[0] is GainSpecialty gainSpecialty ? gainSpecialty : data[0] is LoseSpecialty loseSpecialty ? loseSpecialty : null;
                if (action == null)
                    return;

                if (IgnoredSpecialtyNames.Contains(action.Specialty.Name))
                    return;

                LinkedUser linkedUser = UserLinkManager.LinkedUserByEcoUser(action.Citizen);
                if (linkedUser == null)
                    return;

                if (trigger == DlEventType.GainedSpecialty)
                {
                    ++_opsCount;
                    await AddSpecialtyRole(client, linkedUser.DiscordMember, action.Specialty.DisplayName);
                }
                else if(trigger == DlEventType.LostSpecialty)
                {
                    ++_opsCount;
                    await client.RemoveRoleAsync(linkedUser.DiscordMember, action.Specialty.DisplayName);
                }
            }
        }

        private async Task AddSpecialtyRole(DiscordClient client, DiscordMember member, string specialtyName)
        {
            await client.AddRoleAsync(member, new DiscordLinkRole(specialtyName, null, SpecialtyColor, false, true, $"User has the {specialtyName} specialty"));
        }
    }
}
