using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace Eco.Plugins.DiscordLink
{
    public static class DSharpExtensions
    {
        public static ulong? TryParseSnowflakeId(string nameOrId)
        {
            return ulong.TryParse(nameOrId, out ulong id) && id > 0xFFFFFFFFFFFFFUL ? new ulong?(id) : null;
        }

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

        public static string[] ChannelNames(this DiscordGuild guild, ChannelType type = ChannelType.Text)
        {
            return guild != null ? guild.ChannelsOfType(type).Select(channel => channel.Value.Name).ToArray() : new string[0];
        }
        
        public static IReadOnlyList<KeyValuePair<ulong, DiscordChannel>> ChannelsOfType(this DiscordGuild guild, ChannelType type)
        {
            return guild != null
                ? guild.Channels.Where(channel => channel.Value.Type == type).ToList()
                : new List<KeyValuePair<ulong, DiscordChannel>>();
        }
        
        public static DiscordChannel ChannelByName(this DiscordGuild guild, string channelName, ChannelType type = ChannelType.Text)
        {
            return guild?.ChannelsOfType(type).FirstOrDefault(channel => channel.Value.Name == channelName).Value;
        }

        public static DiscordChannel ChannelByNameOrId(this DiscordGuild guild, string channelNameOrId, ChannelType type = ChannelType.Text)
        {
            if (guild == null) { return null; }

            var maybeChannelId = TryParseSnowflakeId(channelNameOrId);
            return maybeChannelId != null ? guild.GetChannel(maybeChannelId.Value) : guild.ChannelByName(channelNameOrId, type);
        }

        public async static Task<DiscordMember> MaybeGetMemberAsync(this DiscordGuild guild, ulong userId)
        {
            try { return await guild.GetMemberAsync(userId); }
            catch (NotFoundException) { return null; }
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

        #endregion

        #region DiscordMessage

        public static bool IsDm (this DiscordMessage message)
        {
            return message.Channel.Guild == null;
        }

        #endregion
    }
}