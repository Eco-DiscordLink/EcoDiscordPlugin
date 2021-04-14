using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;

namespace Eco.Plugins.DiscordLink
{
    public static class DSharpExtensions
    {
        #region CommandContext

        public static ulong GetSenderId(this CommandContext ctx)
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

        #region DiscordGuild

        public static bool HasNameOrID(this DiscordGuild guild, string nameOrID)
        {
            if (DiscordUtil.TryParseSnowflakeID(nameOrID, out ulong ID))
                return guild.Id == ID;

            return guild.Name.EqualsCaseInsensitive(nameOrID);
        }

        public static string[] TextChannelNames(this DiscordGuild guild)
        {
            return guild != null ? guild.TextChannels().Select(channel => channel.Value.Name).ToArray() : new string[0];
        }
        
        public static IReadOnlyList<KeyValuePair<ulong, DiscordChannel>> TextChannels(this DiscordGuild guild)
        {
            return guild != null
                ? guild.Channels.Where(channel => channel.Value.Type == ChannelType.Text).ToList()
                : new List<KeyValuePair<ulong, DiscordChannel>>();
        }
        
        public static DiscordChannel ChannelByName(this DiscordGuild guild, string channelName)
        {
            return guild?.TextChannels().FirstOrDefault(channel => channel.Value.Name == channelName).Value;
        }

        public static DiscordChannel ChannelByNameOrId(this DiscordGuild guild, string channelNameOrId)
        {
            if (guild == null) { return null; }

            var maybeChannelId = TryParseSnowflakeId(channelNameOrId);
            return maybeChannelId != null ? guild.GetChannel(maybeChannelId.Value) : guild.ChannelByName(channelNameOrId);
        }

        public async static Task<DiscordMember> MaybeGetMemberAsync(this DiscordGuild guild, ulong userId)
        {
            try { return await guild.GetMemberAsync(userId); }
            catch (NotFoundException) { return null; }
        }
        
        #region DiscordChannel

        public static bool HasNameOrID(this DiscordChannel channel, string nameOrID)
        {
            if (DiscordUtil.TryParseSnowflakeID(nameOrID, out ulong ID))
                return channel.Id == ID;

            return channel.Name.EqualsCaseInsensitive(nameOrID);
        }

        #endregion

        #region DiscordClient

        public static string[] GuildNames(this DiscordClient client)
        {
            return client?.Guilds.Values.Select((guild => guild.Name)).ToArray();
        }

        public static DiscordGuild DefaultGuild(this DiscordClient client)
        {
            return client?.Guilds.FirstOrDefault().Value;
        }
     
        public static DiscordGuild GuildByName(this DiscordClient client, string name)
        {
            return client?.Guilds.Values.FirstOrDefault(guild => guild.Name == name);
        }
        
        #endregion

        #region DiscordUser
 
        public static string UniqueUsername(this DiscordUser user)
        {
            return user.Username + user.Discriminator;
        }

        public static bool HasNameOrID(this DiscordUser user, string nameOrID)
        {
            if (DiscordUtil.TryParseSnowflakeID(nameOrID, out ulong ID))
                return user.Id == ID;

            return user.Username.EqualsCaseInsensitive(nameOrID);
        }

        #endregion

        #region DiscordMember

        public static bool HasNameOrID(this DiscordMember member, string nameOrID)
        {
            if (DiscordUtil.TryParseSnowflakeID(nameOrID, out ulong ID))
                return member.Id == ID;

            return member.Username.EqualsCaseInsensitive(nameOrID);
        }

        #endregion

        #region DiscordRole

        public static bool HasNameOrID(this DiscordRole role, string nameOrID)
        {
            if (DiscordUtil.TryParseSnowflakeID(nameOrID, out ulong roleID))
                return role.Id == roleID;

            return role.Name.EqualsCaseInsensitive(nameOrID);
        }

        #endregion

        #region DiscordMessage

        public static bool IsDm (this DiscordMessage message)
        {
            return message.Channel.Guild == null;
        }

        #endregion
    }
}