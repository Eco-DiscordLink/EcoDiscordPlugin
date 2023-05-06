using System.Collections.Generic;

namespace DiscordLink.Source.WebApi.V1.Models
{
    public class DiscordLinkConfigStatusV1
    {
        public bool ClientConnected;
        public bool ServerId;
        public bool BotToken;
        public bool InviteMessage;
        public IEnumerable<DiscordLinkChannelStatusV1> ChannelLinks;
    }
}
