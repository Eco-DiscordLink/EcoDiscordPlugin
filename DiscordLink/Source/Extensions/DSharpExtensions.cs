using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using Eco.Moose.Tools.Logger;
using Eco.Shared.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Extensions
{
    public static class DSharpExtensions
    {
        #region InteractionContext

        public static ulong GetSenderId(this InteractionContext ctx)
        {
            DiscordUser user = ctx.Member ?? ctx.User;
            return user.Id;
        }

        public static string GetSenderName(this InteractionContext ctx)
        {
            if (ctx.Member != null)
                return ctx.Member.DisplayName;
            else
                return ctx.User.Username;
        }

        #endregion

        #region DiscordClient

        public static string[] GuildNames(this DSharpPlus.DiscordClient client) => client.Guilds.Values.Select(guild => guild.Name).ToArray();

        public static DiscordGuild DefaultGuild(this DSharpPlus.DiscordClient client) => client.Guilds.FirstOrDefault().Value;

        public static DiscordGuild GuildByName(this DSharpPlus.DiscordClient client, string name) => client.Guilds.Values.FirstOrDefault(guild => guild.Name == name);

        #endregion

        #region DiscordGuild

        public static DiscordRole GetRoleByName(this DiscordGuild guild, string roleName)
        {
            return guild.Roles.Values.FirstOrDefault(role => role.Name.EqualsCaseInsensitive(roleName));
        }

        public static DiscordRole GetRoleById(this DiscordGuild guild, ulong roleId)
        {
            return guild.Roles.Values.FirstOrDefault(role => role.Id == roleId);
        }

        #endregion

        #region DiscordChannel

        public static bool HasNameOrId(this DiscordChannel channel, string nameOrChannelId)
        {
            if (nameOrChannelId.TryParseSnowflakeId(out ulong channelId))
                return channel.Id == channelId;

            return channel.Name.EqualsCaseInsensitive(nameOrChannelId);
        }

        #endregion

        #region DiscordUser

        public static bool HasNameOrId(this DiscordUser user, string nameOrUserId)
        {
            if (nameOrUserId.TryParseSnowflakeId(out ulong userId))
                return user.Id == userId;

            return user.Username.EqualsCaseInsensitive(nameOrUserId);
        }

        public static async Task<DiscordMember> LookupMember(this DiscordUser user)
        {
            DiscordClient client = DiscordLink.Obj.Client;
            DiscordMember member = client.Guild.Members.FirstOrDefault(m => m.Key == user.Id).Value;
            if (member == null)
            {
                try
                {
                    member = await client.Guild.GetMemberAsync(user.Id);
                }
                catch (NotFoundException)
                {
                    // No action: It's normal to fail to look up members of users who have left the guild
                }
                catch (Exception e)
                {
                    Logger.Exception($"Failed to look up discord member for discord user {user.Username} ({user.Id})", e);
                }
                
            }

            return member;
        }

        #endregion

        #region DiscordMember

        public static bool HasNameOrMemberId(this DiscordMember member, string nameOrId)
        {
            if (nameOrId.TryParseSnowflakeId(out ulong Id))
                return member.Id == Id;

            return member.Username.EqualsCaseInsensitive(nameOrId) || member.Username.EqualsCaseInsensitive(nameOrId);
        }

        public static DiscordRole GetHighestHierarchyRole(this DiscordMember member)
        {
            return member.Roles.OrderByDescending(r => r.Position).FirstOrDefault();
        }

        public static string GetHighestHierarchyRoleName(this DiscordMember member)
        {
            string topRoleName = "Member";
            if (member.IsOwner)
            {
                topRoleName = "Owner";
            }
            else
            {
                DiscordRole topRole = member.GetHighestHierarchyRole();
                if (topRole != null)
                    topRoleName = topRole.Name;
            }
            return topRoleName;
        }

        public static bool HasRole(this DiscordMember member, DiscordRole role)
        {
            return member.Roles.Any(memberRole => memberRole == role);
        }

        public static bool HasRoleWithName(this DiscordMember member, string roleName)
        {
            return member.Roles.Any(memberRole => memberRole.Name.EqualsCaseInsensitive(roleName));
        }

        #endregion

        #region DiscordRole

        public static bool HasNameOrId(this DiscordRole role, string nameOrId)
        {
            if (nameOrId.TryParseSnowflakeId(out ulong roleId))
                return role.Id == roleId;

            return role.Name.EqualsCaseInsensitive(nameOrId);
        }

        #endregion

        #region DiscordMessage

        public static DiscordChannel GetChannel(this DiscordMessage message) => message.Channel ?? DiscordLink.Obj.Client.DSharpClient.GetChannelAsync(message.ChannelId).Result;
        public static string FormatForLog(this DiscordMessage message) => $"Channel: {message.GetChannel()}\nAuthor: {message.Author}\nMessage: {message.Content}\nAttachments ({message.Attachments.Count}): {string.Join(", ", message.Attachments.Select(a => $"{a.FileName} ({a.FileSize} bytes)"))}";

        #endregion
    }
}