using DSharpPlus.Entities;
using Eco.Core.Utils;
using Eco.Shared.Utils;
using System;
using System.Linq;
using System.Text.RegularExpressions;


namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class MessageUtil
    {
        // Eco tag matching regex: Match all characters that are used to tag data inside the character name in Eco message formatting (color codes, badges, links etc)
        private static readonly Regex EcoNameTagRegex = new Regex("<[^>]*>");

        // Discord mention matching regex: Match all characters followed by a mention character(@ or #) character (including that character) until encountering any type of whitespace, end of string or a new mention character
        private static readonly Regex DiscordMentionRegex = new Regex("([@#].+?)(?=\\s|$|@|#)");

        private const string EcoNametagColor = "7289DAFF";

        #region Eco --> Discord

        public static string StripEcoTags(string toStrip)
        {
            return EcoNameTagRegex.Replace(toStrip, String.Empty);
        }

        public static string FormatMessageForDiscord(string message, DiscordChannel channel, string username = "")
        {
            string formattedMessage = (username.IsEmpty() ? "" : $"**{username.Replace("@", "")}**:") + StripEcoTags(message); // All @ characters are removed from the name in order to avoid unintended mentions of the sender
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

        public static string FormatMessageForEco(DiscordMessage message, string ecoChannel)
        {
            DiscordMember author = message.Channel.Guild.MaybeGetMemberAsync(message.Author.Id).Result;
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
            return content;
        }

        #endregion
    }
}
