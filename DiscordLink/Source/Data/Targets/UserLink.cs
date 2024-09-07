using DSharpPlus.Entities;

namespace Eco.Plugins.DiscordLink
{
    public class UserLink : DiscordTarget
    {
        public UserLink(DiscordMember member)
        {
            this.Member = member;
        }

        public DiscordMember Member { get; set; }

        public override bool IsValid() => Member != null;
    }
}
