using System.Collections.Generic;

namespace DiscordLink.Source.WebApi.V1.Models
{
    public class ChannelLinkStatusV1
    {
        public string Id;
        public bool Valid;
    }
    public class DiscordLinkConfigStatusV1
    {
        public bool ClientConnected;
        public bool ServerId;
        public bool BotToken;
        public bool InviteMessage;
        public IEnumerable<ChannelLinkStatusV1> ChannelLinks;
    }
}
