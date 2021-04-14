using DiscordLink.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class DiscordUtil
    {
        public static bool ChannelHasPermission(DiscordChannel channel, Permissions permission)
        {
            if (channel as DiscordDmChannel != null) return true; // Assume permission is given for DMs

            DiscordMember member = channel.Guild.CurrentMember;
            if (member == null)
            {
                Logger.Debug("CurrentMember was null when evaluating channel permissions for channel " + channel.Name);
                return false;
            }

            return channel.PermissionsFor(member).HasPermission(permission);
        }

        public static bool BotHasIntent(DiscordIntents intent)
        {
            return (DiscordLink.Obj.DiscordClient.Intents & intent) != 0;
        }

        public static bool MemberIsAdmin(DiscordMember member)
        {
            foreach (string adminRole in DLConfig.Data.AdminRoles)
            {
                if (ulong.TryParse(adminRole, out ulong adminRoleID) && member.Roles.Any(role => role.Id == adminRoleID))
                    return true;

                if (member.Roles.Any(role => role.Name.EqualsCaseInsensitive(adminRole)))
                    return true;
            }

            return false;
        }

        public static async Task SendAsync(DiscordChannel channel, string textContent, DiscordLinkEmbed embedContent = null)
        {
            try
            {
                if (!ChannelHasPermission(channel, Permissions.SendMessages)) return;

                // Either make sure we have permission to use embeds or convert the embed to text
                string fullTextContent = ChannelHasPermission(channel, Permissions.EmbedLinks) ? textContent : MessageBuilder.Discord.EmbedToText(textContent, embedContent);

                // If needed; split the message into multiple parts
                ICollection<string> stringParts = MessageUtil.SplitStringBySize(fullTextContent, DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT);
                ICollection<DiscordEmbed> embedParts = MessageUtil.BuildDiscordEmbeds(embedContent);

                if(stringParts.Count <= 1 && embedParts.Count <= 1)
                {
                    DiscordEmbed embed = (embedParts.Count >= 1) ? embedParts.First() : null;
                    await channel.SendMessageAsync(fullTextContent, tts: false, embed);
                }
                else
                {
                    foreach (string textMessagePart in stringParts)
                    {
                        await channel.SendMessageAsync(textMessagePart, tts: false, null);
                    }
                    foreach(DiscordEmbed embedPart in embedParts)
                    {
                        await channel.SendMessageAsync(null, tts: false, embedPart);
                    }
                }
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                Logger.Debug(e.ToString());
            }
            catch (Exception e)
            {
                Logger.Error($"Error occurred while attempting to send Discord message to channel \"{channel.Name}\". Error message: " + e);
            }
        }

        public static async Task SendDMAsync(DiscordMember targetMember, string textContent, DiscordLinkEmbed embedContent = null)
        {
            try
            {
                // If needed; split the message into multiple parts
                ICollection<string> stringParts = MessageUtil.SplitStringBySize(textContent, DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT);
                ICollection<DiscordEmbed> embedParts = MessageUtil.BuildDiscordEmbeds(embedContent);

                if (stringParts.Count <= 1 && embedParts.Count <= 1)
                {
                    DiscordEmbed embed = (embedParts.Count >= 1) ? embedParts.First() : null;
                    await targetMember.SendMessageAsync(textContent, is_tts: false, embed);
                }
                else
                {
                    foreach (string textMessagePart in stringParts)
                    {
                        await targetMember.SendMessageAsync(textMessagePart, is_tts: false, null);
                    }
                    foreach (DiscordEmbed embedPart in embedParts)
                    {
                        await targetMember.SendMessageAsync(null, is_tts: false, embedPart);
                    }
                }
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                Logger.Debug(e.ToString());
            }
            catch (Exception e)
            {
                Logger.Error($"Error occurred while attempting to send Discord message to user \"{targetMember.DisplayName}\". Error message: " + e);
            }
        }

        public static async Task ModifyAsync(DiscordMessage message, string textContent, DiscordLinkEmbed embedContent = null)
        {
            try
            {
                if (!ChannelHasPermission(message.Channel, Permissions.ManageMessages)) return;

                if (embedContent == null)
                {
                    await message.ModifyAsync(textContent);
                }
                else
                {
                    try
                    {
                        // Either make sure we have permission to use embeds or convert the embed to text
                        if (ChannelHasPermission(message.Channel, Permissions.EmbedLinks))
                        {
                            await message.ModifyAsync(textContent, MessageUtil.BuildDiscordEmbed(embedContent)); // TODO: Not safe! May require splitting!
                        }
                        else
                        {
                            await message.ModifyEmbedSuppressionAsync(true); // Remove existing embeds
                            await message.ModifyAsync(MessageBuilder.Discord.EmbedToText(textContent, embedContent));
                        }
                    }
                    catch(Exception e)
                    {
                        Logger.Warning("Failed to modify message. The message may be too long. Error message: " + e);
                    }
                }
            }
            catch (DSharpPlus.Exceptions.ServerErrorException e)
            {
                Logger.Debug(e.ToString());
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                Logger.Debug(e.ToString());
            }
            catch (DSharpPlus.Exceptions.NotFoundException e)
            {
                Logger.Debug(e.ToString());
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while attempting to modify Discord message. Error message: " + e);
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

        public static async Task<IReadOnlyCollection<DiscordMember>> GetGuildMembersAsync(DiscordGuild guild)
        {
            if(!BotHasIntent(DiscordIntents.GuildMembers))
            {
                Logger.Error("Attempted to get full guild member list without the bot having the privileged GuildMembers intent");
                return null;
            }

            try
            {
                return await guild.GetAllMembersAsync();
            }
            catch(Exception e)
            {
                Logger.Error("Error occured when attempting to fetch all guild members. Error message: " + e);
                return null;
            }
        }
    }
}
