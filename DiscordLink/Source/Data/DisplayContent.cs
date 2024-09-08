using Eco.Core.Utils;
using Eco.Plugins.DiscordLink.Extensions;

namespace Eco.Plugins.DiscordLink
{
    public class DisplayContent
    {
        public DisplayContent(string tag, string textContent = "", DiscordLinkEmbed embedContent = null)
        {
            Tag = tag;
            TextContent = textContent;
            EmbedContent = embedContent;
        }

        public string TagAndText => TextContent.IsEmpty() ? Tag : $"{Tag}\n{TextContent}";

        public string Tag { get; private set; }
        public string TextContent { get; private set; }
        public DiscordLinkEmbed EmbedContent { get; private set; }
    }
}
