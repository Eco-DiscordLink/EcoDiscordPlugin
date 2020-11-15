using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class DiscordUtil
    {
        public const int EMBED_CONTENT_CHARACTER_LIMIT = 5000;
        public const int EMBED_FIELD_CHARACTER_LIMIT = 900;
        public static bool ChannelHasPermission(DiscordChannel channel, Permissions permission)
        {
            DiscordMember member = channel.Guild.CurrentMember;
            if (member == null)
            {
                Logger.Debug("CurrentMember was false when evaluating channel permissions for channel " + channel.Name);
                return false;
            }

            return channel.PermissionsFor(member).HasPermission(permission);
        }

        public static async Task<DiscordMessage> SendAsync(DiscordChannel channel, string textContent, DiscordEmbed embedContent = null)
        {
            try
            {
                if (!ChannelHasPermission(channel, Permissions.SendMessages)) return null;

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
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                Logger.Debug(e.ToString());
                return null;
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while attempting to send Discord message. Error message: " + e);

        public static async Task<DiscordMessage> SendDmAsync(DiscordMember targetMember, string textContent, DiscordEmbed embedContent = null)
        {
            try
            {
                return await targetMember.SendMessageAsync(textContent, is_tts: false, embedContent);
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                Logger.Debug(e.ToString());
                return null;
            }
            catch (Exception e)
            {
                Logger.Error($"Error occurred while attempting to send Discord message to user \"{targetMember.DisplayName}\". Error message: " + e);
                return null;
            }
        }

        public static async Task<DiscordMessage> ModifyAsync(DiscordMessage message, string textContent, DiscordEmbed embedContent = null)
        {
            try
            {
                if (!ChannelHasPermission(message.Channel, Permissions.ManageMessages)) return null;

                if (embedContent == null)
                {
                    return await message.ModifyAsync(textContent);
                }
                else
                {
                    // Either make sure we have permission to use embeds or convert the embed to text
                    if (ChannelHasPermission(message.Channel, Permissions.EmbedLinks))
                    {
                        return await message.ModifyAsync(textContent, embedContent);
                    }
                    else
                    {
                        await message.ModifyEmbedSuppressionAsync(true); // Remove existing embeds
                        return await message.ModifyAsync(MessageBuilder.EmbedToText(textContent, embedContent));
                    }
                }
            }
            catch (DSharpPlus.Exceptions.ServerErrorException e)
            {
                Logger.Debug(e.ToString());
                return null;
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                Logger.Debug(e.ToString());
                return null;
            }
            catch (DSharpPlus.Exceptions.NotFoundException e)
            {
                Logger.Debug(e.ToString());
                return null;
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while attempting to modify Discord message. Error message: " + e);
                return null;
            }
        }

        public static async Task DeleteAsync(DiscordMessage message)
        {
            if (!ChannelHasPermission(message.Channel, Permissions.ManageMessages)) return;

            try
            {
                await message.DeleteAsync("Deleted by DiscordLink");
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while attempting to delete Discord message. Error message: " + e);
            }
        }

        public static async Task<DiscordMessage> GetMessageAsync(DiscordChannel channel, ulong messageID)
        {
            if (!ChannelHasPermission(channel, Permissions.ReadMessageHistory)) return null;

            try
            {
                return await channel.GetMessageAsync(messageID);
            }
            catch (DSharpPlus.Exceptions.ServerErrorException e)
            {
                Logger.Debug(e.ToString());
                return null;
            }
            catch (DSharpPlus.Exceptions.NotFoundException e)
            {
                Logger.Debug(e.ToString());
                return null;
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred when attempting to read message with ID " + messageID + " from channel \"" + channel.Name + "\". Error message: " + e);
                return null;
            }
        }

        public static async Task<IReadOnlyList<DiscordMessage>> GetMessagesAsync(DiscordChannel channel)
        {
            if (!ChannelHasPermission(channel, Permissions.ReadMessageHistory)) return null;

            try
            {
                return await channel.GetMessagesAsync();
            }
            catch (DSharpPlus.Exceptions.ServerErrorException e)
            {
                Logger.Debug(e.ToString());
                return null;
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred when attempting to read message history from channel \"" + channel.Name + "\". Error message: " + e);
                return null;
            }
        }
    }
}
