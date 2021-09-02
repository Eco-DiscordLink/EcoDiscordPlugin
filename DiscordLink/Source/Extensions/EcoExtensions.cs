using Eco.Gameplay.Components;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Gameplay.Property;
using Eco.Gameplay.Systems.Balance;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System.Linq;

namespace Eco.Plugins.DiscordLink.Extensions
{
    public static class EcoExtensions
    {
        #region User

        public static bool IsWhitelisted(this User user) => UserManager.Config.UserPermission.WhiteList.Contains(user);
        public static bool IsBanned(this User user) => UserManager.Config.UserPermission.BlackList.Contains(user);
        public static bool IsMuted(this User user) => UserManager.Config.UserPermission.MuteList.Contains(user);

        public static double GetSecondsSinceLogin(this User user) => user.IsOnline ? Simulation.Time.WorldTime.Seconds - user.LoginTime : 0.0;
        public static double GetSecondsSinceLogout(this User user) => user.IsOnline ? 0.0 : Simulation.Time.WorldTime.Seconds - user.LogoutTime;
        public static double GetSecondsLeftUntilExhaustion(this User user) => TimeUtil.HoursToSeconds(BalancePlugin.Obj.Config.ExhaustionAfterHours) - user.OnlineTimeLog.SecondsPlayedToday();

        public static float GetTotalXPMultiplier(this User user) => user.GetNutritionXP() + user.GetHousingXP();
        public static float GetNutritionXP(this User user) => user.Stomach.NutrientSkillRate();
        public static float GetHousingXP(this User user) => user.HomePropertyValue.TotalSkillPoints;

        #endregion

        #region ChatMessage

        public static string FormatForLog(this ChatSent message) => $"Author: {MessageUtils.StripTags(message.Citizen.Name)}\nChannel: {message.Tag}\nMessage: {message.Message}";

        #endregion

        #region Deed

        public static int GetTotalPlotSize(this Deed deed) => deed.Plots.Count() * DLConstants.ECO_PLOT_SIZE_M2;

        public static bool IsVehicle(this Deed deed) => deed.OwnedObjects.Select(handle => handle.OwnedObject).OfType<WorldObject>().Any(x => x?.HasComponent<VehicleComponent>() == true);

        public static VehicleComponent GetVehicle(this Deed deed) => deed.OwnedObjects.Select(handle => handle.OwnedObject).OfType<WorldObject>().Where(x => x?.HasComponent<VehicleComponent>() == true).FirstOrDefault().GetComponent<VehicleComponent>();

        #endregion
    }
}
