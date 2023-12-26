using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Eco.Moose.Tools;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Extensions
{
    public static class DSharpExtensions
    {
        #region InteractionContext

        public static ulong GetSenderID(this InteractionContext ctx)
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

        public static string[] GuildNames(this DiscordClient client) => client.Guilds.Values.Select(guild => guild.Name).ToArray();

        public static DiscordGuild DefaultGuild(this DiscordClient client) => client.Guilds.FirstOrDefault().Value;

        public static DiscordGuild GuildByName(this DiscordClient client, string name) => client.Guilds.Values.FirstOrDefault(guild => guild.Name == name);

        #endregion

        #region DiscordGuild

        public static IReadOnlyList<KeyValuePair<ulong, DiscordChannel>> TextChannels(this DiscordGuild guild) => guild.Channels.Where(channel => channel.Value.Type == ChannelType.Text).ToList();
        public static string[] TextChannelNames(this DiscordGuild guild) => guild.TextChannels().Select(channel => channel.Value.Name).ToArray();

        public static DiscordRole RoleByName(this DiscordGuild guild, string roleName)
        {
            return guild.Roles.Values.FirstOrDefault(role => role.Name.EqualsCaseInsensitive(roleName));
        }

        public static DiscordRole RoleByID(this DiscordGuild guild, ulong ID)
        {
            return guild.Roles.Values.FirstOrDefault(role => role.Id == ID);
        }

        #endregion

        #region DiscordChannel

        public static bool HasNameOrID(this DiscordChannel channel, string nameOrID)
        {
            if (nameOrID.TryParseSnowflakeID(out ulong ID))
                return channel.Id == ID;

            return channel.Name.EqualsCaseInsensitive(nameOrID);
        }

        #endregion

        #region DiscordUser

        public static string UniqueUsername(this DiscordUser user) => $"{user.Username}#{user.Discriminator}";

        public static bool HasNameOrID(this DiscordUser user, string nameOrID)
        {
            if (nameOrID.TryParseSnowflakeID(out ulong ID))
                return user.Id == ID;

            return user.Username.EqualsCaseInsensitive(nameOrID);
        }

        public static async Task<DiscordMember> LookupMember(this DiscordUser user)
        {
            DLDiscordClient client = DiscordLink.Obj.Client;
            DiscordMember member = client.Guild.Members.FirstOrDefault(m => m.Key == user.Id).Value;
            if (member == null)
            {
                member = await client.Guild.GetMemberAsync(user.Id);
            }

            return member;
        }

        #endregion

        #region DiscordMember

        public static bool HasNameOrID(this DiscordMember member, string nameOrID)
        {
            if (nameOrID.TryParseSnowflakeID(out ulong ID))
                return member.Id == ID;

            return member.UniqueUsername().EqualsCaseInsensitive(nameOrID) || member.Username.EqualsCaseInsensitive(nameOrID);
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

        public static bool HasNameOrID(this DiscordRole role, string nameOrID)
        {
            if (nameOrID.TryParseSnowflakeID(out ulong roleID))
                return role.Id == roleID;

            return role.Name.EqualsCaseInsensitive(nameOrID);
        }

        #endregion

        #region DiscordMessage

        public static DiscordChannel GetChannel(this DiscordMessage message) => message.Channel ?? DiscordLink.Obj.Client.DiscordClient.GetChannelAsync(message.ChannelId).Result;
        public static string FormatForLog(this DiscordMessage message) => $"Channel: {message.GetChannel()}\nAuthor: {message.Author}\nMessage: {message.Content}\nAttachments ({message.Attachments.Count}): {string.Join(", ", message.Attachments.Select(a => $"{a.FileName} ({a.FileSize} bytes)"))}";

        #endregion
    }
}