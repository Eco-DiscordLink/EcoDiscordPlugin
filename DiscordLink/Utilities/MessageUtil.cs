using DSharpPlus.Entities;
using Eco.Core.Utils;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class MessageUtil
    {
        // Snippet matching regex: Match the (case insensitive) [Snippet] header and capture the the content of the following bracket pair
        public static readonly Regex SnippetRegex = new Regex("(?i)\\[snippet\\]\\s*\\[([^\\]]+)\\].*\\s([^$]*)");

        // Eco tag matching regex: Match all characters that are used to create HTML style tags
        private static readonly Regex HTMLTagRegex = new Regex("<[^>]*>");

        // Discord mention matching regex: Match all characters followed by a mention character(@ or #) character (including that character) until encountering any type of whitespace, end of string or a new mention character
        private static readonly Regex DiscordMentionRegex = new Regex("([@#].+?)(?=\\s|$|@|#)");

        // Discord @everyone and @here matching regex: Match all instances that would trigger a Discord tag to @everyone or @here and capture the tag so the @ can easily removed.
        private static readonly Regex DiscordGlobalMentionRegex = new Regex("@(everyone|here)");

        private const string EcoNametagColor = "7289DAFF";

        #region General

        public static string FirstNonEmptyString(params string[] strings)
        {
            return strings.FirstOrDefault(str => !string.IsNullOrEmpty(str)) ?? "";
        }

        public static List<string> SplitStringBySize(string str, int chunkSize)
        {
            if (str == null)
                return new List<string>();

            return Enumerable.Range(0, str.Length / chunkSize).Select(i => str.Substring(i * chunkSize, chunkSize)).ToList();
        }

        public static List<DiscordEmbed> SplitEmbed(DiscordEmbed fullEmbed)
        {
            List<DiscordEmbed> resultEmbeds = new List<DiscordEmbed>();

            if (fullEmbed == null)
                return resultEmbeds;

            // Count chars needed for title and footer
            int titleFooterCharCount = 0;
            if (fullEmbed.Title != null)
                titleFooterCharCount += fullEmbed.Title.Length;
            if (fullEmbed.Footer != null)
                titleFooterCharCount += fullEmbed.Footer.Text.Length;

            int totalCharsCount = titleFooterCharCount;
            int maxEmbedCharCount = DLConstants.DISCORD_EMBED_CONTENT_CHARACTER_LIMIT + titleFooterCharCount;

            // Count chars needed for fields and track fields that are too long
            List<bool> needsSplitFields = Enumerable.Repeat(false, fullEmbed.Fields.Count).ToList();
            for(int i = 0; i < fullEmbed.Fields.Count; ++i)
            {
                DiscordEmbedField field = fullEmbed.Fields[i];
                int length = field.Name.Length + field.Value.Length;
                if ( length > DLConstants.DISCORD_EMBED_FIELD_CHARACTER_LIMIT)
                    needsSplitFields[i] = true;

                totalCharsCount += length;
            }

            // Early escape if no splitting is needed
            if (totalCharsCount <= maxEmbedCharCount && needsSplitFields.Count <= 0)
            {
                resultEmbeds.Add(fullEmbed);
                return resultEmbeds;
            }

            // Create a dummy embed and split too long fields
            DiscordEmbedBuilder splitFieldsCollector = new DiscordEmbedBuilder();
            for (int i = 0; i < fullEmbed.Fields.Count; ++i)
            {
                DiscordEmbedField field = fullEmbed.Fields[i];
                if (needsSplitFields[i] == true)
                {
                    IEnumerable<string> splits = SplitStringBySize(field.Value, DLConstants.DISCORD_EMBED_FIELD_CHARACTER_LIMIT);
                    int partCount = 1;
                    foreach(string fieldSplit in splits)
                    {
                        splitFieldsCollector.AddField($"{field.Name} ({partCount})", fieldSplit);
                        ++partCount;
                    }
                }
                else
                {
                    splitFieldsCollector.AddField(fullEmbed.Fields[i].Name, fullEmbed.Fields[i].Value);
                }
            }

            // Create new embeds that fit within the char limits
            List<DiscordEmbed> splitEmbeds = new List<DiscordEmbed>();
            DiscordEmbedBuilder splitEmbedBuilder = new DiscordEmbedBuilder();
            if (!string.IsNullOrWhiteSpace(fullEmbed.Title))
                splitEmbedBuilder.WithTitle(fullEmbed.Title);
            if (!string.IsNullOrWhiteSpace(fullEmbed.Footer?.Text))
                splitEmbedBuilder.WithFooter(fullEmbed.Footer.Text);

            int characterCount = 0;
            foreach (DiscordEmbedField field in splitFieldsCollector.Fields)
            {
                if(characterCount + field.Value.Length > maxEmbedCharCount)
                {
                    splitEmbeds.Add(splitEmbedBuilder.Build());
                    splitEmbedBuilder.ClearFields();
                    characterCount = 0;
                }

                splitEmbedBuilder.AddField(field.Name, field.Value);
                characterCount += field.Value.Length;
            }
            splitEmbeds.Add(splitEmbedBuilder.Build());

            return splitEmbeds;
        }

        #endregion

        #region EcoFormatting

        public static string MakeBold(string text)
        {
            return $"<b>{text}</b>";
        }

        public static string MakeItalic(string text)
        {
            return $"<i>{text}</i>";
        }

        public static string MakeColored(string text, string color)
        {
            return $"<color=\"{color}\">{text}</color>";
        }

        #endregion

        #region Eco --> Discord

        public static string StripTags(string toStrip)
        {
            if (toStrip == null) return null;
            return HTMLTagRegex.Replace(toStrip, string.Empty);
        }

        public static string StripGlobalMentions(string toStrip)
        {
            if (toStrip == null) return null;
            return DiscordGlobalMentionRegex.Replace(toStrip, "$1");
        }

        public static string FormatMessageForDiscord(string message, DiscordChannel channel, string username = "", bool allowGlobalMentions = false)
        {
            string formattedMessage = (username.IsEmpty() ? "" : $"**{username.Replace("@", "")}**:") + StripTags(message); // All @ characters are removed from the name in order to avoid unintended mentions of the sender
            if (!allowGlobalMentions)
            {
                formattedMessage = StripGlobalMentions(formattedMessage);
            }
            return FormatDiscordMentions(formattedMessage, channel);
        }

        private static string FormatDiscordMentions(string message, DiscordChannel channel)
        {
            return DiscordMentionRegex.Replace(message, capture =>
            {
                string match = capture.ToString().Substring(1).ToLower(); // Strip the mention character from the match
                string FormatMention(string name, string mention)
                {
                    if (match == name)
                    {
                        return mention;
                    }

                    string beforeMatch = "";
                    int matchStartIndex = match.IndexOf(name);
                    if (matchStartIndex > 0) // There are characters before @username
                    {
                        beforeMatch = match.Substring(0, matchStartIndex);
                    }

                    string afterMatch = "";
                    int matchStopIndex = matchStartIndex + name.Length - 1;
                    int numCharactersAfter = match.Length - 1 - matchStopIndex;
                    if (numCharactersAfter > 0) // There are characters after @username
                    {
                        afterMatch = match.Substring(matchStopIndex + 1, numCharactersAfter);
                    }

                    return beforeMatch + mention + afterMatch; // Add whatever characters came before or after the username when replacing the match in order to avoid changing the message context
                }

                ChatChannelLink link = DLConfig.Instance.GetChannelLinkFromDiscordChannel(channel.Guild.Name, channel.Name);
                bool allowRoleMentions = (link == null || link.AllowRoleMentions);
                bool allowMemberMentions = (link == null || link.AllowUserMentions);
                bool allowChannelMentions = (link == null || link.AllowChannelMentions);

                if (capture.ToString()[0] == '@')
                {
                    if (allowRoleMentions)
                    {
                        foreach (var role in channel.Guild.Roles.Values) // Checking roles first in case a user has a name identiacal to that of a role
                        {
                            if (!role.IsMentionable) continue;

                            string name = role.Name.ToLower();
                            if (match.Contains(name))
                            {
                                return FormatMention(name, role.Mention);
                            }
                        }
                    }

                    if (allowMemberMentions)
                    {
                        foreach (var member in channel.Guild.Members.Values)
                        {
                            string name = member.DisplayName.ToLower();
                            if (match.Contains(name))
                            {
                                return FormatMention(name, member.Mention);
                            }
                        }
                    }
                }
                else if (capture.ToString()[0] == '#' && allowChannelMentions)
                {
                    foreach (var listChannel in channel.Guild.Channels.Values)
                    {
                        string name = listChannel.Name.ToLower();
                        if (match.Contains(name))
                        {
                            return FormatMention(name, listChannel.Mention);
                        }
                    }
                }

                return capture.ToString(); // No match found, just return the original string
            });
        }

        #endregion

        #region Discord -> Eco

        public static async Task<string> FormatMessageForEco(DiscordMessage message, string ecoChannel)
        {
            DiscordMember author = await message.Channel.Guild.MaybeGetMemberAsync(message.Author.Id);
            string nametag = author != null
                ? Text.Bold(Text.Color(EcoNametagColor, author.DisplayName))
                : message.Author.Username;
            return $"#{ecoChannel} {nametag}: {GetReadableContent(message)}";
        }

        public static string GetReadableContent(DiscordMessage message)
        {
            var content = message.Content;
            foreach (var user in message.MentionedUsers)
            {
                if (user == null) { continue; }
                DiscordMember member = message.Channel.Guild.Members.FirstOrDefault(m => m.Value?.Id == user.Id).Value;
                if (member == null) { continue; }
                string name = "@" + member.DisplayName;
                content = content.Replace($"<@{user.Id}>", name).Replace($"<@!{user.Id}>", name);
            }
            foreach (var role in message.MentionedRoles)
            {
                if (role == null) continue;
                content = content.Replace($"<@&{role.Id}>", $"@{role.Name}");
            }
            foreach (var channel in message.MentionedChannels)
            {
                if (channel == null) continue;
                content = content.Replace($"<#{channel.Id}>", $"#{channel.Name}");
            }

            if(message.Attachments.Count > 0)
            {
                content += "\nAttachments:";
                foreach(DiscordAttachment attachment in message.Attachments)
                {
                    content += $"\n{attachment.FileName}";
                }
            }

            return content;
        }

        #endregion
    }
}
