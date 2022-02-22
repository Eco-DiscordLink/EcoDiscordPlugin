﻿using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
using System.Linq;
using System.Threading.Tasks;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Gameplay.GameActions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Gameplay.Skills;
using DSharpPlus;
using System;
using System.Collections.Generic;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class SpecialtiesRoleModule : Module
    {
        private static readonly DiscordColor SpecialtyColor = DiscordColor.Green;
        private static readonly string[] IgnoredSpecialtyNames = { "SelfImprovementSkill" };
        // TODO Ienumerable<ulong> GuildIds reicht
        private IEnumerable<DiscordGuild> Guilds = Enumerable.Empty<DiscordGuild>();

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
            Guilds = DiscordLink.Obj.Client.Guilds.Where(guild => DiscordLink.Obj.Client.BotHasPermission(Permissions.ManageRoles, guild.Id));

            base.Setup();
        }

        protected override async Task<bool> ShouldRun()
        {
            return DiscordLink.Obj.Client.Guilds.Select(g => g.Id).Any(guildId => DiscordLink.Obj.Client.BotHasPermission(Permissions.ManageRoles, guildId));
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            DLDiscordClient client = DiscordLink.Obj.Client;
            foreach (var guild in Guilds)
            {

                if (!client.BotHasPermission(Permissions.ManageRoles, guild.Id))
                    return;

                if (trigger == DLEventType.DiscordClientConnected)
                {
                    if (!client.BotHasIntent(DiscordIntents.GuildMembers))
                        return;

                    ++_opsCount;
                    foreach (DiscordMember member in await client.GetGuildMembersAsync(guild))
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
