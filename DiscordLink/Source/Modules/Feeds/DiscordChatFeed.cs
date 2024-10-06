using DSharpPlus.Entities;
using Eco.Gameplay.Players;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Message;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class DiscordChatFeed : FeedModule
    {
        public override string ToString()
        {
            return "Discord Chat Feed";
        }

        protected override DlEventType GetTriggers()
        {
            return DlEventType.DiscordMessageSent;
        }

        protected override async Task<bool> ShouldRun()
        {
            foreach (ChatChannelLink link in DLConfig.Data.ChatChannelLinks)
            {
                if (link.IsValid() && (link.Direction == ChatSyncDirection.DiscordToEco || link.Direction == ChatSyncDirection.Duplex))
                    return true;
            }
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DlEventType trigger, params object[] data)
        {
            if (!(data[0] is DiscordMessage message))
                return;

            IEnumerable<ChatChannelLink> chatLinks = DLConfig.ChatLinksForDiscordChannel(message.GetChannel());
            foreach (ChatChannelLink chatLink in chatLinks
                .Where(link => link.Direction == ChatSyncDirection.EcoToDiscord || link.Direction == ChatSyncDirection.Duplex))
            {
                await ForwardMessageToEcoChannel(message, chatLink.EcoChannel);
            }
        }

        private async Task ForwardMessageToEcoChannel(DiscordMessage discordMessage, string ecoChannel)
        {
            Logger.Trace($"Sending Discord message to Eco channel: {ecoChannel}");
            DiscordMember author = await discordMessage.GetChannel().Guild.GetMemberAsync(discordMessage.Author.Id);

            User sender = null;
            LinkedUser linkedUser = UserLinkManager.LinkedUserByDiscordUser(author);
            if (linkedUser != null)
                sender = linkedUser.EcoUser;

            string messageContent = await GetReadableContent(discordMessage);
            if (sender == null)
            {
                DiscordMember memberAuthor = discordMessage.Author as DiscordMember ?? await discordMessage.Author.LookupMember();
                messageContent = $"{Text.Color(DLConstants.DISCORD_COLOR, memberAuthor.DisplayName)} {DLConstants.ECO_DISCORDLINK_ICON} {messageContent}";
            }
            else
            {
                messageContent = $"{DLConstants.ECO_DISCORDLINK_ICON} {messageContent}";
            }

            Message.SendChatRaw(sender, MessageUtils.FormatChatMessageForEcoChannel(messageContent, ecoChannel));
            ++_opsCount;
        }

        private async Task<string> GetReadableContent(DiscordMessage message)
        {
            // Substitute Discord standard emojis
            string content = DLConstants.DISCORD_EMOJI_SUBSTITUTION_MAP.Aggregate(message.Content, (current, emojiMapping) => current.Replace(emojiMapping.Key, $"<ecoicon name=\"{emojiMapping.Value}\">"));

            // Substitute custom emojis
            content = MessageUtils.DiscordCustomEmoteRegex.Replace(content, capture =>
            {
                string group1 = capture.Groups[1].Value;
                EmoteIconSubstitution sub = DLConfig.Data.EmoteIconSubstitutions.FirstOrDefault(sub => sub.DiscordEmoteKey.EqualsCaseInsensitive(group1));
                if (sub != null)
                {
                    return $"<ecoicon name=\"{sub.EcoIconKey}\">";
                }
                else
                {
                    return $":{group1}:";
                }
            });

            // Substitute mentioned user, roles and channels
            foreach (var user in message.MentionedUsers)
            {
                if (user == null)
                    continue;

                DiscordMember member = message.GetChannel().Guild.Members.FirstOrDefault(m => m.Value?.Id == user.Id).Value;
                if (member == null)
                    continue;

                string name = $"@{member.DisplayName}";
                content = content.Replace($"<@{user.Id}>", name).Replace($"<@!{user.Id}>", name);
            }
            foreach (var role in message.MentionedRoles)
            {
                if (role == null)
                    continue;

                content = content.Replace($"<@&{role.Id}>", $"@{role.Name}");
            }
            foreach (var channel in message.MentionedChannels)
            {
                if (channel == null)
                    continue;

                content = content.Replace($"<#{channel.Id}>", $"#{channel.Name}");
            }

            // Prefix message with referenced message preview
            if (message.ReferencedMessage != null)
            {
                DiscordMessage refMessage = message.ReferencedMessage;

                string authorName;
                string referenceContent;
                if (refMessage.Author == DiscordLink.Obj.Client.BotMember)
                {
                    const string subStringStartComparison = "**: ";
                    authorName = MessageUtils.DiscordBoldRegex.Match(refMessage.Content).Groups[0].Value.Strip('*'); // Pull the original sender name from the reference message
                    referenceContent = refMessage.Content.Substring(refMessage.Content.IndexOf(subStringStartComparison) + subStringStartComparison.Length); // Skip ahead to the actual message
                }
                else
                {
                    if (refMessage.Author is DiscordMember member)
                        authorName = member.DisplayName;
                    else
                        authorName = (await refMessage.Author.LookupMember())?.DisplayName ?? refMessage.Author.Username; // Fall back on User if member for have left the guild or is otherwise unavailable

                    referenceContent = refMessage.Content;
                }

                string referenceContentPreview = referenceContent.Length < DLConstants.DISCORD_REFERENCE_MESSAGE_CONTENT_PREVIEW_LENGTH
                        ? referenceContent
                        : $"{referenceContent.Substring(0, DLConstants.DISCORD_REFERENCE_MESSAGE_CONTENT_PREVIEW_LENGTH)}...";
                content = $"{Text.Color(DLConstants.DISCORD_COLOR, "[RE]")} \"{authorName}: {referenceContentPreview}\"\n{content}";
            }

            // Add attachment information
            if (message.Attachments.Count > 0)
            {
                content += "\nAttachments:";
                foreach (DiscordAttachment attachment in message.Attachments)
                {
                    content += $"\n{attachment.FileName}";
                }
            }

            return content;
        }
    }
}
