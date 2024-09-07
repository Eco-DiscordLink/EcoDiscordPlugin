using System.ComponentModel;

namespace Eco.Plugins.DiscordLink
{
    public class ChatLinkMentionPermissions
    {
        public bool AllowChannelMentions { get; set; }
        public bool AllowRoleMentions { get; set; }
        public bool AllowMemberMentions { get; set; }
    }
    public class ChatChannelLink : EcoChannelLink
    {
        [Browsable(false)]
        public ChatLinkMentionPermissions MentionPermissions => new()
        {
            AllowRoleMentions = AllowRoleMentions,
            AllowMemberMentions = AllowUserMentions,
            AllowChannelMentions = AllowChannelMentions,
        };

        [Description("Allow mentions of usernames to be forwarded from Eco to the Discord channel.")]
        public bool AllowUserMentions { get; set; } = true;

        [Description("Allow mentions of roles to be forwarded from Eco to the Discord channel.")]
        public bool AllowRoleMentions { get; set; } = true;

        [Description("Allow mentions of channels to be forwarded from Eco to the Discord channel.")]
        public bool AllowChannelMentions { get; set; } = true;

        [Description("Sets which direction chat should synchronize in.")]
        public ChatSyncDirection Direction { get; set; } = ChatSyncDirection.Duplex;

        [Description("Permissions for who is allowed to forward mentions of @here or @everyone from Eco to the Discord channel.")]
        public GlobalMentionPermission HereAndEveryoneMentionPermission { get; set; } = GlobalMentionPermission.Forbidden;

        [Description("Determines if timestamps should be added to each message forwarded to Discord.")]
        public bool UseTimestamp { get; set; } = false;
    }
}
