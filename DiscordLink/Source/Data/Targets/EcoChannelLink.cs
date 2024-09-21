using Eco.Gameplay.Systems.Messaging.Chat.Channels;
using Eco.Moose.Tools.Logger;
using System.ComponentModel;

namespace Eco.Plugins.DiscordLink
{
    public class EcoChannelLink : ChannelLink
    {
        [Description("Eco channel to use (omit # prefix).")]
        public string EcoChannel { get; set; } = ChannelManager.Obj?.Get(SpecialChannel.General)?.Name ?? string.Empty;
        public override string ToString()
        {
            string discordChannelName = IsValid() ? Channel.Name : "<Invalid Channel>";
            return $"#{discordChannelName} <--> {EcoChannel}";
        }

        public override bool IsValid() => base.IsValid() && !string.IsNullOrWhiteSpace(EcoChannel);

        public override bool MakeCorrections()
        {
            bool correctionMade = base.MakeCorrections();
            string original = EcoChannel;
            EcoChannel = EcoChannel.Trim('#');
            if (EcoChannel != original)
            {
                correctionMade = true;
                Logger.Info($"Corrected Eco channel name linked with Discord Channel name/ID \"{Channel?.Name ?? DiscordChannelId.ToString()}\" from \"{original}\" to \"{EcoChannel}\"");
            }
            return correctionMade;
        }
    }
}
