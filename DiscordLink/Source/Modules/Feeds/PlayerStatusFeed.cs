using Eco.Gameplay.Players;
using Eco.Moose.Tools.Logger;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    class PlayerStatusFeed : FeedModule
    {
        public override string ToString()
        {
            return "Player Status Feed";
        }

        protected override DlEventType GetTriggers()
        {
            return DlEventType.Join | DlEventType.Login | DlEventType.Logout;
        }

        protected override async Task<bool> ShouldRun()
        {
            foreach (ChannelLink link in DLConfig.Data.PlayerStatusFeedChannels)
            {
                if (link.IsValid())
                    return true;
            }
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DlEventType trigger, params object[] data)
        {
            if (!(data[0] is User user))
                return;

            string message;
            switch (trigger)
            {
                case DlEventType.Join:
                    message = $":tada:  {MessageUtils.StripTags(user.Name)} Joined The Server!  :tada:";
                    break;

                case DlEventType.Login:
                    message = $":arrow_up:  {MessageUtils.StripTags(user.Name)} Logged In  :arrow_up:";
                    break;

                case DlEventType.Logout:
                    message = $":arrow_down:  {MessageUtils.StripTags(user.Name)} Logged Out  :arrow_down:";
                    break;

                default:
                    Logger.Debug("Player Status Feed received unexpected trigger type");
                    return;
            }

            DiscordLinkEmbed embed = new DiscordLinkEmbed();
            embed.WithTitle(message);

            foreach (ChannelLink playerStatusLink in DLConfig.Data.PlayerStatusFeedChannels)
            {
                if (!playerStatusLink.IsValid())
                    continue;

                await DiscordLink.Obj.Client.SendMessageAsync(playerStatusLink.Channel, null, embed);
                ++_opsCount;
            }
        }
    }
}
