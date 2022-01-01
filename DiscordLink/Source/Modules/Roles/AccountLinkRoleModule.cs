using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
using System.Threading.Tasks;
using Eco.Plugins.DiscordLink.Extensions;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class AccountLinkRoleModule : Module
    {
        private DiscordRole LinkedAccountRole = null;

        public override string ToString()
        {
            return "Account Link Role Module";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.DiscordClientConnected | DLEventType.AccountLinkVerified | DLEventType.AccountLinkRemoved;
        }

        public override void Setup()
        {
            LinkedAccountRole = DLConfig.Data.Guild.RoleByName(DLConstants.ROLE_LINKED_ACCOUNT.Name);
            if (LinkedAccountRole == null)
                LinkedAccountRole = DiscordLink.Obj.Client.CreateRoleAsync(DLConstants.ROLE_LINKED_ACCOUNT).Result;

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
                if (client.BotHasIntent(DSharpPlus.DiscordIntents.GuildMembers))
                {
                    ++_opsCount;
                    foreach (DiscordMember member in await client.GetGuildMembersAsync(DLConfig.Data.Guild))
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
