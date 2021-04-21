using Eco.Gameplay.GameActions;
using Eco.Gameplay.Players;
using Eco.Plugins.DiscordLink.Utilities;

namespace Eco.Plugins.DiscordLink.Extensions
{
    public static class EcoExtensions
    {
        #region User

        public static bool IsWhitelisted(this User user) => UserManager.Config.WhiteList.Contains(user.SteamId) || UserManager.Config.WhiteList.Contains(user.SlgId);
        public static bool IsBanned(this User user) => UserManager.Config.BlackList.Contains(user.SteamId) || UserManager.Config.BlackList.Contains(user.SlgId);
        public static bool IsMuted(this User user) => UserManager.Config.MuteList.Contains(user.SteamId) || UserManager.Config.MuteList.Contains(user.SlgId);

        public static double GetSecondsSinceLogin(this User user) => user.IsOnline ? Simulation.Time.WorldTime.Seconds - user.LoginTime : 0.0;
        public static double GetSecondsSinceLogout(this User user) => user.IsOnline ? 0.0 : Simulation.Time.WorldTime.Seconds - user.LogoutTime;

        public static float GetTotalXPMultiplier(this User user) => user.GetNutritionXP() + user.GetHousingXP();
        public static float GetNutritionXP(this User user) => user.Stomach.NutrientSkillRate;
        public static float GetHousingXP(this User user) => user.CachedHouseValue.HousingSkillRate;

        #endregion

        #region ChatMessage

        public static string FormatForLog(this ChatSent message) => $"Author: {MessageUtils.StripTags(message.Citizen.Name)}\nChannel: {message.Tag}\nMessage: {message.Message}";

        #endregion
    }
}
