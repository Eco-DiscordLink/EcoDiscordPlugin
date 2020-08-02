using DSharpPlus;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public static class DiscordUtil
    {
        public static bool ChannelHasPermission(DiscordChannel channel, Permissions permission)
        {
            return channel.PermissionsFor(channel.Guild.CurrentMember).HasPermission(permission);
        }

        public static async Task<DiscordMessage> SendAsync(DiscordChannel channel, string textContent, DiscordEmbed embedContent = null)
        {
            if (embedContent == null)
            {
                return await channel.SendMessageAsync(textContent, false);
            }
            else
            {
                // Either make sure we have permission to use embeds or convert the embed to text
                if (ChannelHasPermission(channel, Permissions.EmbedLinks))
                {
                    return await channel.SendMessageAsync(textContent, false, embedContent);
                }
                else
                {
                    return await channel.SendMessageAsync(MessageBuilder.EmbedToText(textContent, embedContent));
                }
            }
        }

        public static async Task ModifyAsync(DiscordMessage message, string textContent, DiscordEmbed embedContent = null)
        {
            if (embedContent == null)
            {
                await message.ModifyAsync(textContent);
            }
            else
            {
                // Either make sure we have permission to use embeds or convert the embed to text
                if (ChannelHasPermission(message.Channel, Permissions.EmbedLinks))
                {
                    await message.ModifyAsync(textContent, embedContent);
                }
                else
                {
                    await message.ModifyEmbedSuppressionAsync(true); // Remove existing embeds
                    await message.ModifyAsync(MessageBuilder.EmbedToText(textContent, embedContent));
                }
            }
        }
    }
}
