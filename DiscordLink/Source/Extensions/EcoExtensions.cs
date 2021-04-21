using Eco.Gameplay.Components;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Gameplay.Property;
using Eco.Plugins.DiscordLink.Utilities;
using System.Linq;

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

        #region Deed

        public static int GetTotalPlotSize(this Deed deed) => deed.Plots.Count() * DLConstants.ECO_PLOT_SIZE_M2;

        public static bool IsVehicle(this Deed deed) => deed.OwnedObjects.OfType<WorldObject>().Any(x => x?.HasComponent<VehicleComponent>() == true);

        public static VehicleComponent GetVehicle(this Deed deed) => deed.OwnedObjects.OfType<WorldObject>().Where(x => x?.HasComponent<VehicleComponent>() == true).FirstOrDefault().GetComponent<VehicleComponent>();

        #endregion
    }
}
