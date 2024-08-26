using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Moose.Tools.Logger;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class AccountLinkRoleModule : RoleModule
    {
        private DiscordRole _linkedAccountRole = null;

        public override string ToString()
        {
            return "Account Link Role Module";
        }

        protected override DlEventType GetTriggers()
        {
            return base.GetTriggers() | DlEventType.DiscordClientConnected | DlEventType.AccountLinkVerified | DlEventType.AccountLinkRemoved;
        }

        public override void Setup()
        {
            _linkedAccountRole = DiscordLink.Obj.Client.Guild.GetRoleByName(DLConstants.ROLE_LINKED_ACCOUNT.Name);
            if (_linkedAccountRole == null)
                SetupLinkRole();

            base.Setup();
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DlEventType trigger, params object[] data)
        {
            DiscordClient client = DiscordLink.Obj.Client;
            if (!client.BotHasPermission(Permissions.ManageRoles))
                return;

            if (_linkedAccountRole == null || client.Guild.GetRoleById(_linkedAccountRole.Id) == null)
                SetupLinkRole();

            if (_linkedAccountRole == null)
                return;

            if (trigger == DlEventType.DiscordClientConnected || trigger == DlEventType.ForceUpdate)
            {
                if (!client.BotHasIntent(DiscordIntents.GuildMembers))
                    return;

                ++_opsCount;
                foreach (DiscordMember member in await client.GetMembersAsync())
                {
                    LinkedUser linkedUser = UserLinkManager.LinkedUserByDiscordUser(member);
                    if (linkedUser == null || !DLConfig.Data.UseLinkedAccountRole)
                    {
                        if (member.HasRole(_linkedAccountRole))
                        {
                            ++_opsCount;
                            await client.RemoveRoleAsync(member, _linkedAccountRole);
                        }
                    }
                    else if (linkedUser.Valid && !member.HasRole(_linkedAccountRole))
                    {
                        ++_opsCount;
                        await client.AddRoleAsync(member, _linkedAccountRole);
                    }
                }
            }
            else
            {
                if (!DLConfig.Data.UseLinkedAccountRole)
                    return;

                if (!(data[0] is LinkedUser linkedUser))
                    return;

                DiscordMember member = linkedUser.DiscordMember;
                if (member == null)
                {
                    Logger.Error($"Failed to handle role change for Eco user \"{linkedUser.EcoUser.Name}\". Linked Discord member was not loaded.");
                    return;
                }

                if (trigger == DlEventType.AccountLinkRemoved)
                {
                    ++_opsCount;
                    await client.RemoveRoleAsync(member, _linkedAccountRole);
                }
                else if (trigger == DlEventType.AccountLinkVerified)
                {
                    ++_opsCount;
                    await client.AddRoleAsync(member, _linkedAccountRole);
                }
                
            }
        }

        private void SetupLinkRole()
        {
            if (!DLConfig.Data.UseLinkedAccountRole)
                return;

            ++_opsCount;
            _linkedAccountRole = DiscordLink.Obj.Client.CreateRoleAsync(DLConstants.ROLE_LINKED_ACCOUNT).Result;
        }
    }
}
