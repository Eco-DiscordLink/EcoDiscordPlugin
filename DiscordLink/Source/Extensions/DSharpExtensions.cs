using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public static class DSharpExtensions
    {
        #region CommandContext

        public static ulong GetSenderID(this CommandContext ctx)
        {
            DiscordUser user = ctx.Member ?? ctx.User;
            return user.Id;
        }

        public static string GetSenderName(this CommandContext ctx)
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

        public static bool HasNameOrID(this DiscordGuild guild, string nameOrID)
        {
            if (Utilities.Utils.TryParseSnowflakeID(nameOrID, out ulong ID))
                return guild.Id == ID;

            return guild.Name.EqualsCaseInsensitive(nameOrID);
        }
        public static DiscordChannel ChannelByName(this DiscordGuild guild, string channelName)
        {
            return guild.TextChannels().FirstOrDefault(channel => channel.Value.Name == channelName).Value;
        }

        public static DiscordChannel ChannelByNameOrID(this DiscordGuild guild, string channelNameOrID)
        {
            return Utilities.Utils.TryParseSnowflakeID(channelNameOrID, out ulong ID)
                ? guild.GetChannel(ID)
                : guild.ChannelByName(channelNameOrID);
        }

        #endregion

        #region DiscordChannel

        public static bool HasNameOrID(this DiscordChannel channel, string nameOrID)
        {
            if (Utilities.Utils.TryParseSnowflakeID(nameOrID, out ulong ID))
                return channel.Id == ID;

            return channel.Name.EqualsCaseInsensitive(nameOrID);
        }

        #endregion

        #region DiscordUser

        public static string UniqueUsername(this DiscordUser user) => user.Username + user.Discriminator;

        public static bool HasNameOrID(this DiscordUser user, string nameOrID)
        {
            if (Utilities.Utils.TryParseSnowflakeID(nameOrID, out ulong ID))
                return user.Id == ID;

            return user.Username.EqualsCaseInsensitive(nameOrID);
        }

        #endregion

        #region DiscordMember

        public static bool HasNameOrID(this DiscordMember member, string nameOrID)
        {
            if (Utilities.Utils.TryParseSnowflakeID(nameOrID, out ulong ID))
                return member.Id == ID;

            return member.Username.EqualsCaseInsensitive(nameOrID);
        }

        #endregion

        #region DiscordRole

        public static bool HasNameOrID(this DiscordRole role, string nameOrID)
        {
            if (Utilities.Utils.TryParseSnowflakeID(nameOrID, out ulong roleID))
                return role.Id == roleID;

            return role.Name.EqualsCaseInsensitive(nameOrID);
        }

        #endregion

        #region DiscordMessage

        public static bool IsDm(this DiscordMessage message) => message.Channel.Guild == null;

        public static string FormatForLog(this DiscordMessage message) => $"Channel: {message.Channel}\nAuthor: {message.Author}\nMessage: {message.Content}\nAttachments ({message.Attachments.Count}): {string.Join(", ", message.Attachments.Select(a => $"{a.FileName} ({a.FileSize} bytes)"))}";

        #endregion
    }
}