using System.Collections.Generic;
using System.Text;

namespace Eco.Plugins.DiscordLink.Extensions
{
    // DSharp DiscordEmbed throws exceptions when it goes over the character limits.
    // This class is used to get around that by deferring the embed building to a place where the character limits can be handled.
    public sealed class DiscordLinkEmbed
    {
        public enum EmbedSize
        {
            Large,
            Medium,
            Small,
            None // For config option to not have any footers
        }

        public static readonly string INVISIBLE_EMBED_CHAR = "\u200e";

        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Footer { get; private set; }
        public string Thumbnail { get; private set; }
        public List<DiscordLinkEmbedField> Fields { get; private set; } = new List<DiscordLinkEmbedField>();

        public DiscordLinkEmbed()
        { }

        public DiscordLinkEmbed(DiscordLinkEmbed RHS)
        {
            this.Title = RHS.Title;
            this.Description = RHS.Description;
            this.Footer = RHS.Footer;
            this.Thumbnail = RHS.Thumbnail;
            foreach (DiscordLinkEmbedField field in RHS.Fields)
            {
                Fields.Add(new DiscordLinkEmbedField(field));
            }
        }

        public DiscordLinkEmbed WithTitle(string title)
        {
            Title = title;
            return this;
        }

        public DiscordLinkEmbed WithDescription(string description)
        {
            Description = description;
            return this;
        }

        public DiscordLinkEmbed WithFooter(string text)
        {
            Footer = text;
            return this;
        }

        public DiscordLinkEmbed WithThumbnail(string thumbnailAddress)
        {
            Thumbnail = thumbnailAddress;
            return this;
        }

        public DiscordLinkEmbed AddField(string name, string text, bool? allowAutoLineBreak = null, bool inline = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = INVISIBLE_EMBED_CHAR;

            if (string.IsNullOrWhiteSpace(text))
                text = INVISIBLE_EMBED_CHAR;

            // Default auto line break allowing to opposite value of inline
            if (!allowAutoLineBreak == null)
                allowAutoLineBreak = !inline;

            // Shorten lines to avoid alignment being broken by automatic line breaks
            if (!(bool)allowAutoLineBreak && text.Length > DLConstants.DISCORD_EMBED_FIELD_CHARACTER_PER_LINE_LIMIT)
            {
                StringBuilder builder = new StringBuilder();
                text = text.Replace("\r", null); // Remove all carrige returns so they don't cause extra newlines
                foreach (string line in text.Split('\n'))
                {
                    string shortLine = line;
                    if (line.Length > DLConstants.DISCORD_EMBED_FIELD_CHARACTER_PER_LINE_LIMIT)
                        shortLine = $"{line.Substring(0, DLConstants.DISCORD_EMBED_FIELD_CHARACTER_PER_LINE_LIMIT - 3)}...";
                    builder.AppendLine(shortLine);
                }
                text = builder.ToString();
            }

            Fields.Add(new DiscordLinkEmbedField(name, text, (bool)allowAutoLineBreak, inline));
            return this;
        }

        public DiscordLinkEmbed AddAlignmentField()
        {
            AddField(INVISIBLE_EMBED_CHAR, INVISIBLE_EMBED_CHAR, inline: true); // Left to right mark character will appear as an empty field
            return this;
        }

        public DiscordLinkEmbed ClearFields()
        {
            Fields.Clear();
            return this;
        }

        public string AsText()
        {
            string text = string.Empty;
            if (!string.IsNullOrWhiteSpace(Title))
                text += $"{Title}\n\n";

            if (!string.IsNullOrWhiteSpace(Description))
                text += $"{Description}\n\n";

            foreach (DiscordLinkEmbedField field in Fields)
            {
                text += $"**{field.Title}**\n[{field.Text}\n\n";
            }

            if (!string.IsNullOrWhiteSpace(Footer))
                text += Footer;

            return text.Trim();
        }

        public EmbedSize GetSize()
        {
            if (Fields.Count <= DLConstants.DISCORD_EMBED_SIZE_SMALL_FIELD_LIMIT)
                return EmbedSize.Small;
            else if (Fields.Count <= DLConstants.DISCORD_EMBED_SIZE_MEDIUM_FIELD_LIMIT)
                return EmbedSize.Medium;
            else
                return EmbedSize.Large;
        }
    }

    public sealed class DiscordLinkEmbedField
    {
        public string Title { get; private set; }
        public string Text { get; private set; }
        public bool AllowAutoLineBreak { get; private set; }
        public bool Inline { get; private set; }

        public DiscordLinkEmbedField(string title, string text, bool allowAutoLineBreak = true, bool inline = false)
        {
            Title = title;
            Text = text;
            AllowAutoLineBreak = allowAutoLineBreak;
            Inline = inline;
        }

        public DiscordLinkEmbedField(DiscordLinkEmbedField RHS)
        {
            Title = RHS.Title;
            Text = RHS.Text;
            AllowAutoLineBreak = RHS.AllowAutoLineBreak;
            Inline = RHS.Inline;
        }
    }
}
