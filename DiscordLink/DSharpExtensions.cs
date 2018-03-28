using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Eco.Plugins.DiscordLink
{
    public static class DSharpExtensions
    {
        public static string[] TextChannelNames(this DiscordGuild guild)
        {
            return guild != null ? guild.TextChannels().Select(channel => channel.Name).ToArray() : new string[0];
        }
        
        public static IReadOnlyList<DiscordChannel> TextChannels(this DiscordGuild guild)
        {
            return guild != null
                ? guild.Channels.Where(channel => channel.Type == ChannelType.Text).ToList()
                : new List<DiscordChannel>();
        }
        
        public static DiscordChannel ChannelByName(this DiscordGuild guild, string channelName)
        {
            return guild != null
                ? guild.TextChannels().FirstOrDefault(channel => channel.Name == channelName)
                : null;
        }


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
    }
}