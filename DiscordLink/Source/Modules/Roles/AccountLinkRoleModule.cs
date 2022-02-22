using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
using System.Threading.Tasks;
using Eco.Plugins.DiscordLink.Extensions;
using DSharpPlus;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class AccountLinkRoleModule : Module
    {
        private readonly Dictionary<ulong, DiscordRole> LinkedAccountRolePerGuildId = new();

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
            foreach (var guild in DiscordLink.Obj.Client.Guilds)
            {
                if (DiscordLink.Obj.Client.BotHasPermission(Permissions.ManageRoles, guild.Id))
                {
                    var linkedAccountRole = guild.RoleByName(DLConstants.ROLE_LINKED_ACCOUNT.Name);
                    if (linkedAccountRole == null)
                    {
                        linkedAccountRole = DiscordLink.Obj.Client.CreateRoleAsync(DLConstants.ROLE_LINKED_ACCOUNT, guild).Result;
                    }
                    LinkedAccountRolePerGuildId[guild.Id] = linkedAccountRole;
                }
            }

            base.Setup();
        }

        protected override async Task<bool> ShouldRun()
        {
            return DiscordLink.Obj.Client.Guilds.Select(g => g.Id).Any(guildId => DiscordLink.Obj.Client.BotHasPermission(Permissions.ManageRoles, guildId));
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            DLDiscordClient client = DiscordLink.Obj.Client;
            foreach (var linkedAccountRole in LinkedAccountRolePerGuildId)
            {
                if (!client.BotHasPermission(Permissions.ManageRoles, linkedAccountRole.Key))
                    return;

                if (trigger == DLEventType.DiscordClientConnected)
                {
                    if (!client.BotHasIntent(DiscordIntents.GuildMembers))
                        return;

                    ++_opsCount;
                    foreach (DiscordMember member in await client.GetGuildMembersAsync(linkedAccountRole.Key))
                    {
                        LinkedUser linkedUser = UserLinkManager.LinkedUserByDiscordUser(member);
                        if (linkedUser == null || !DLConfig.Data.UseLinkedAccountRole)
                        {
                            if (member.HasRole(linkedAccountRole.Value))
                            {
                                ++_opsCount;
                                await client.RemoveRoleAsync(member, linkedAccountRole.Value);
                            }
                        }
                        else if (linkedUser.Verified && !member.HasRole(linkedAccountRole.Value))
                        {
                            ++_opsCount;
                            await client.AddRoleAsync(member, linkedAccountRole.Value);
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
                        await client.AddRoleAsync(linkedUser.DiscordMember, linkedAccountRole.Value);
                    }
                    else if (trigger == DLEventType.AccountLinkRemoved)
                    {
                        ++_opsCount;
                        await client.RemoveRoleAsync(linkedUser.DiscordMember, linkedAccountRole.Value);
                    }
                }
            }
        }
    }
}
