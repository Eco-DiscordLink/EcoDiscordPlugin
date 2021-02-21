using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordLink.Extensions
{
    // DSharp DiscordEmbed throws exceptions when it goes over the character limits.
    // This class is used to get around that by deferring the embed building to a place where the character limits can be handled.
    public sealed class DiscordLinkEmbed
    {
        public DiscordLinkEmbed()
        { }

        public DiscordLinkEmbed(DiscordLinkEmbed RHS)
        {
            this.Title = RHS.Title;
            this.Description = RHS.Description;
            this.Footer = RHS.Footer;
            this.Thumbnail = RHS.Thumbnail;
            foreach(DiscordLinkEmbedField field in RHS.Fields)
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

        public DiscordLinkEmbed AddField(string name, string text)
        {
            Fields.Add(new DiscordLinkEmbedField(name, text));
            return this;
        }

        public DiscordLinkEmbed ClearFields()
        {
            Fields.Clear();
            return this;
        }

        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Footer { get; private set; }
        public string Thumbnail { get; private set; }
        public List<DiscordLinkEmbedField> Fields { get; private set; } = new List<DiscordLinkEmbedField>();
    }

    public sealed class DiscordLinkEmbedField
    {
        public DiscordLinkEmbedField(string title, string text)
        {
            Title = title;
            Text = text;
        }

        public DiscordLinkEmbedField(DiscordLinkEmbedField RHS)
        {
            Title = RHS.Title;
            Text = RHS.Text;
        }

        public string Title { get; private set; }
        public string Text { get; private set; }
    }
}
