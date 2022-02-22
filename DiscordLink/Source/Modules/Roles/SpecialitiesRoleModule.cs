using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Skills;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class SpecialtiesRoleModule : Module
    {
        private static readonly DiscordColor SpecialtyColor = DiscordColor.Green;
        private static readonly string[] IgnoredSpecialtyNames = { "SelfImprovementSkill" };
        private IEnumerable<ulong> GuildIds = Enumerable.Empty<ulong>();

        public override string ToString()
        {
            return "Specialties Role Module";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.DiscordClientConnected | DLEventType.AccountLinkVerified | DLEventType.AccountLinkRemoved | DLEventType.GainedSpecialty;
        }

        public override void Setup()
        {
            GuildIds = DiscordLink.Obj.Client.Guilds
                .Select(guild => guild.Id)
                .Where(guildId => DiscordLink.Obj.Client.BotHasPermission(Permissions.ManageRoles, guildId));

            base.Setup();
        }

        protected override async Task<bool> ShouldRun()
        {
            return DiscordLink.Obj.Client.Guilds.Select(g => g.Id).Any(guildId => DiscordLink.Obj.Client.BotHasPermission(Permissions.ManageRoles, guildId));
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            DLDiscordClient client = DiscordLink.Obj.Client;
            foreach (var guildId in GuildIds)
            {

                if (!client.BotHasPermission(Permissions.ManageRoles, guildId))
                    return;

                if (trigger == DLEventType.DiscordClientConnected)
                {
                    if (!client.BotHasIntent(DiscordIntents.GuildMembers))
                        return;

                    ++_opsCount;
                    foreach (DiscordMember member in await client.GetGuildMembersAsync(guildId))
                    {
                        LinkedUser linkedUser = UserLinkManager.LinkedUserByDiscordUser(member);
                        foreach (Skill specialty in EcoUtils.Specialties)
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
                else if (trigger == DLEventType.AccountLinkVerified || trigger == DLEventType.AccountLinkRemoved)
                {
                    if (!(data[0] is LinkedUser linkedUser))
                        return;

                    DiscordMember member = linkedUser.DiscordMember;
                    foreach (Skill specialty in EcoUtils.Specialties)
                    {
                        if (IgnoredSpecialtyNames.Contains(specialty.Name))
                            continue;

                        if (trigger == DLEventType.AccountLinkRemoved || !DLConfig.Data.UseSpecialtyRoles || !linkedUser.EcoUser.HasSpecialization(specialty.Type))
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
                else
                {
                    if (!DLConfig.Data.UseSpecialtyRoles)
                        return;

                    if (!(data[0] is GainSpecialty gainSpecialty))
                        return;

                    if (IgnoredSpecialtyNames.Contains(gainSpecialty.Specialty.Name))
                        return;

                    LinkedUser linkedUser = UserLinkManager.LinkedUserByEcoUser(gainSpecialty.Citizen);
                    if (linkedUser == null)
                        return;

                    if (trigger == DLEventType.GainedSpecialty)
                    {
                        ++_opsCount;
                        await AddSpecialtyRole(client, linkedUser.DiscordMember, gainSpecialty.Specialty.DisplayName);
                    }
                }
            }
        }

        private async Task AddSpecialtyRole(DLDiscordClient client, DiscordMember member, string specialtyName)
        {
            await client.AddRoleAsync(member, new DiscordLinkRole(specialtyName, null, SpecialtyColor, false, true, $"User has the {specialtyName} specialty"), member.Guild);
        }
    }
}
