using DiscordLink.Extensions;
using DSharpPlus.Entities;
using Eco.Gameplay.Players;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    class PlayerStatusFeed : Feed
    {
        public override string ToString()
        {
            return "Player Status Feed";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.Join | DLEventType.Login | DLEventType.Logout;
        }

        protected override bool ShouldRun()
        {
            foreach (ChannelLink link in DLConfig.Data.PlayerStatusChannels)
            {
                if (link.IsValid())
                    return true;
            }
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, object data)
        {
            if (!(data is User user)) return;
            
            string message = string.Empty;
            switch (trigger)
            {
                case DLEventType.Join:
                    message = $":tada:  {MessageUtil.StripTags(user.Name)} Joined The Server!  :tada:";
                    break;

                case DLEventType.Login:
                    message = $":arrow_up:  {MessageUtil.StripTags(user.Name)} Logged In  :arrow_up:";
                    break;

                case DLEventType.Logout:
                    message = $":arrow_down:  {MessageUtil.StripTags(user.Name)} Logged Out  :arrow_down:";
                    break;

                default:
                    Logger.Debug("Player Status Feed received unexpected trigger type");
                    return;
            }

            DiscordLinkEmbed embed = new DiscordLinkEmbed();
            embed.WithTitle(message);
            embed.WithFooter(MessageBuilder.Discord.GetStandardEmbedFooter());

            foreach (ChannelLink playerStatusChannel in DLConfig.Data.PlayerStatusChannels)
            {
                if (!playerStatusChannel.IsValid()) continue;
                DiscordGuild discordGuild = plugin.GuildByNameOrId(playerStatusChannel.DiscordGuild);
                if (discordGuild == null) continue;
                DiscordChannel discordChannel = discordGuild.ChannelByNameOrId(playerStatusChannel.DiscordChannel);
                if (discordChannel == null) continue;
                await DiscordUtil.SendAsync(discordChannel, null, embed);
                ++_opsCount;
            }
        }
    }
}
