using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Gameplay.Civics.Demographics;
using Eco.Gameplay.Skills;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Lookups;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class RoleCleanupModule : RoleModule
    {
        private bool _hasRun = false;

        public override string ToString()
        {
            return "Role Cleanup Module";
        }

        protected override DlEventType GetTriggers()
        {
            return base.GetTriggers() | DlEventType.WorldReset | DlEventType.ServerStarted;
        }

        protected override async Task<bool> ShouldRun()
        {
            return DiscordLink.Obj.Client.BotHasPermission(Permissions.ManageRoles) && !_hasRun;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DlEventType trigger, params object[] data)
        {
            DiscordClient client = DiscordLink.Obj.Client;
            if (!client.BotHasPermission(Permissions.ManageRoles))
                return;

            if (trigger == DlEventType.WorldReset)
            {
                DiscordRole AccountLinkRole = client.Guild.GetRoleByName(DLConstants.ROLE_LINKED_ACCOUNT.Name);

                List<DiscordRole> removeRoles = new List<DiscordRole>();
                foreach (ulong roleId in DLStorage.PersistentData.RoleIds)
                {
                    DiscordRole role = client.Guild.GetRoleById(roleId);
                    if (role == null)
                    {
                        // Unregister roles that have been removed by others
                        DLStorage.PersistentData.RoleIds.Remove(roleId);
                        Logger.Debug($"{this} Failed to find role with ID {roleId}. Removing tracking for role ID.");
                        continue;
                    }

                    // Special
                    if (role == AccountLinkRole && DLConfig.Data.UseLinkedAccountRole)
                        continue;

                    // Demographics
                    bool foundDemographic = false;
                    foreach (Demographic demographic in Lookups.ActiveDemographics)
                    {
                        string roleName = DemographicsRoleModule.GetDemographicRoleName(demographic);
                        if (role.Name.EqualsCaseInsensitive(roleName))
                        {
                            foundDemographic = true;
                            break;
                        }
                    }
                    if (foundDemographic)
                        continue;

                    // Specialties
                    bool foundSpecialty = false;
                    foreach (Skill specialty in Lookups.Specialties)
                    {
                        if (role.Name.EqualsCaseInsensitive(specialty.DisplayName))
                        {
                            foundSpecialty = true;
                            break;
                        }
                    }
                    if (foundSpecialty)
                        continue;

                    removeRoles.Add(role);
                }

                // Remove the obsolete roles
                foreach (DiscordRole role in removeRoles)
                {
                    ++_opsCount;
                    await client.DeleteRoleAsync(role);
                    DLStorage.PersistentData.RoleIds.Remove(role.Id);
                }

                // Turn ourselves off
                _hasRun = true;
                await HandleStartOrStop();
            }
            else if (trigger == DlEventType.ServerStarted)
            {
                // We are past the point where we should run, so we turn ourselves off
                _hasRun = true;
                await HandleStartOrStop();
            }
        }
    }
}
