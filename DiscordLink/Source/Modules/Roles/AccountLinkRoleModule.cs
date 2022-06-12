using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
using System.Threading.Tasks;
using Eco.Plugins.DiscordLink.Extensions;
using DSharpPlus;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class AccountLinkRoleModule : RoleModule
    {
        private DiscordRole LinkedAccountRole = null;

        public override string ToString()
        {
            return "Account Link Role Module";
        }

        protected override DLEventType GetTriggers()
        {
            return base.GetTriggers() | DLEventType.DiscordClientConnected | DLEventType.AccountLinkVerified | DLEventType.AccountLinkRemoved;
        }

        public override void Setup()
        {
            LinkedAccountRole = DiscordLink.Obj.Client.Guild.RoleByName(DLConstants.ROLE_LINKED_ACCOUNT.Name);
            if (LinkedAccountRole == null)
                LinkedAccountRole = DiscordLink.Obj.Client.CreateRoleAsync(DLConstants.ROLE_LINKED_ACCOUNT).Result;

            base.Setup();
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            DLDiscordClient client = DiscordLink.Obj.Client;
            if (!client.BotHasPermission(Permissions.ManageRoles))
                return;

            if (trigger == DLEventType.DiscordClientConnected)
            if (trigger == DLEventType.DiscordClientConnected || trigger == DLEventType.ForceUpdate)
            {
                if (!client.BotHasIntent(DiscordIntents.GuildMembers))
                    return;

                ++_opsCount;
                foreach (DiscordMember member in await client.GetGuildMembersAsync())
                {
                    LinkedUser linkedUser = UserLinkManager.LinkedUserByDiscordUser(member);
                    if (linkedUser == null || !DLConfig.Data.UseLinkedAccountRole)
                    {
                        if (member.HasRole(LinkedAccountRole))
                        {
                            ++_opsCount;
                            await client.RemoveRoleAsync(member, LinkedAccountRole);
                        }
                    }
                    else if (linkedUser.Verified && !member.HasRole(LinkedAccountRole))
                    {
                        ++_opsCount;
                        await client.AddRoleAsync(member, LinkedAccountRole);
                    }
                }
            }
            else
            {
                if (!DLConfig.Data.UseLinkedAccountRole)
                    return;

                if (!(data[0] is LinkedUser linkedUser))
                    return;

                if (trigger == DLEventType.AccountLinkVerified)
                {
                    ++_opsCount;
                    await client.AddRoleAsync(linkedUser.DiscordMember, LinkedAccountRole);
                }
                else if (trigger == DLEventType.AccountLinkRemoved)
                {
                    ++_opsCount;
                    await client.RemoveRoleAsync(linkedUser.DiscordMember, LinkedAccountRole);
                }
            }
        }
    }
}
