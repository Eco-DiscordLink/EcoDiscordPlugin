using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
using System.Linq;
using System.Threading.Tasks;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Gameplay.GameActions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Gameplay.Skills;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class SpecialtiesRoleModule : Module
    {
        private static readonly DiscordColor SpecialtyColor = DiscordColor.Green;

        private static readonly string[] IgnoredSpecialtyNames = { "SelfImprovementSkill" };
        private static Skill[] IgnoredSpecialties = new Skill[IgnoredSpecialtyNames.Length];

        public override string ToString()
        {
            return "Specialties Role Module";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.DiscordClientConnected | DLEventType.GainedSpecialty;
        }

        public override void Setup()
        {
            foreach (Skill specialty in EcoUtils.Specialties)
            {
                if (IgnoredSpecialtyNames.Contains(specialty.Name))
                {
                    IgnoredSpecialties.Append(specialty);
                }
            }

            base.Setup();
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
                if (!client.BotHasIntent(DSharpPlus.DiscordIntents.GuildMembers))
                    return;

                ++_opsCount;
                foreach (DiscordMember member in await client.GetGuildMembersAsync())
                {
                    LinkedUser linkedUser = UserLinkManager.LinkedUserByDiscordUser(member);
                    foreach (Skill specialty in EcoUtils.Specialties)
                    {
                        if (IgnoredSpecialties.Contains(specialty))
                            continue;

                        if (linkedUser == null || !DLConfig.Data.UseSpecialtyRoles || !linkedUser.EcoUser.Skillset.Skills.Contains(specialty))
                        {
                            if (member.HasRoleWithName(specialty.DisplayName))
                            {
                                ++_opsCount;
                                await client.RemoveRoleAsync(member, specialty.DisplayName);
                            }
                        }
                        else if (!member.HasRoleWithName(specialty.DisplayName) && linkedUser.EcoUser.Skillset.Skills.Contains(specialty))
                        {
                            ++_opsCount;
                            await AddSpecialtyRole(client, linkedUser.DiscordMember, specialty.DisplayName);
                        }
                    }
                }
            }
            else
            {
                if (!DLConfig.Data.UseSpecialtyRoles)
                    return;

                if (!(data[0] is GainSpecialty gainSpecialty))
                    return;

                if (IgnoredSpecialties.Contains(gainSpecialty.Specialty))
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

        private async Task AddSpecialtyRole(DLDiscordClient client, DiscordMember member, string specialtyName)
        {
            await client.AddRoleAsync(member, new DiscordLinkRole(specialtyName, null, SpecialtyColor, false, true, $"User has the {specialtyName} specialty"));
        }
    }
}
