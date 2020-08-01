using DSharpPlus;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public static class DiscordUtil
    {
        public static bool HasEmbedPermission(DiscordChannel channel)
        {
            return channel.PermissionsFor(channel.Guild.CurrentMember).HasPermission(Permissions.EmbedLinks);
        }

        public static async Task SendAsync(DiscordChannel channel, string textContent, DiscordEmbed embedContent = null)
        {
            if (embedContent == null)
            {
                await channel.SendMessageAsync(textContent, false);
            }
            else
            {
                // Either make sure we have permission to use embeds or convert the embed to text
                if (DiscordUtil.HasEmbedPermission(channel))
                {
                    await channel.SendMessageAsync(textContent, false, embedContent);
                }
                else
                {
                    await channel.SendMessageAsync(MessageBuilder.EmbedToText(textContent, embedContent));
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
                if (DiscordUtil.HasEmbedPermission(message.Channel))
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
