using Eco.Gameplay.GameActions;
using Eco.Gameplay.Players;
using Eco.Plugins.DiscordLink.Utilities;

namespace Eco.Plugins.DiscordLink.Extensions
{
    public static class EcoExtensions
    {
        public static bool IsWhitelisted(this User user) => UserManager.Config.WhiteList.Contains(user.SteamId) || UserManager.Config.WhiteList.Contains(user.SlgId);
        public static bool IsBanned(this User user) => UserManager.Config.BlackList.Contains(user.SteamId) || UserManager.Config.BlackList.Contains(user.SlgId);
        public static bool IsMuted(this User user) => UserManager.Config.MuteList.Contains(user.SteamId) || UserManager.Config.MuteList.Contains(user.SlgId);
        #region ChatMessage

        public static string FormatForLog(this ChatSent message) => $"Author: {MessageUtils.StripTags(message.Citizen.Name)}\nChannel: {message.Tag}\nMessage: {message.Message}";

        #endregion
    }
}
