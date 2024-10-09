using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Gameplay.Systems.Messaging.Chat.Commands;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Localization;
using Eco.Shared.Services;
using System;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public class RemoteEcoCommandClient : IChatClient
    {
        readonly DiscordCommandContext DiscordCtx;
        readonly User EcoUser;

        public RemoteEcoCommandClient(DiscordCommandContext ctx)
        {
            DiscordCtx = ctx;
            LinkedUser userLink = UserLinkManager.LinkedUserByDiscordUser(DiscordCtx.Interaction.User);
            EcoUser = userLink != null ? userLink.EcoUser : null;
        }

        public string Name => EcoUser != null ? EcoUser.Name : $"DiscordLinkClient_{DiscordCtx.Interaction.User.Username}";
        public LocString MarkedUpName => Localizer.NotLocalizedStr(EcoUser != null ? EcoUser.MarkedUpName : $"DiscordLinkClient_{DiscordCtx.Interaction.User.Username}");
        public string ImplementationName => "DiscordLink Eco Command Client";

        public string ReportUserId => EcoUser != null ? EcoUser.StrangeId : DiscordCtx.Interaction.User.Id.ToString();
        public string ReportUsername => Name;
        public string ReportIpAddress => EcoUser != null ? EcoUser.ReportIpAddress : null;

        public ChatAuthorizationLevel GetChatAuthLevel()
        {
            if (EcoUser != null)
            {
                if (EcoUser.IsDevTier)
                    return ChatAuthorizationLevel.DevTier;
                else if (EcoUser.IsAdmin)
                    return ChatAuthorizationLevel.Admin;
            }
            else if (DiscordLink.Obj.Client.MemberIsAdmin(DiscordCtx.Interaction.Member))
            {
                return ChatAuthorizationLevel.Admin;
            }

            return ChatAuthorizationLevel.User;
        }

        public async void Msg(LocString msg, NotificationStyle style = NotificationStyle.Chat) => await SendMessage(msg, style);

        public async void MsgLoc(FormattableString msg, NotificationStyle style = NotificationStyle.Chat) => await SendMessage(Localizer.Do(msg).ToString(), style);

        public async void MsgLocStr(string msg, NotificationStyle style = NotificationStyle.Chat) => await SendMessage(Localizer.DoStr(msg).ToString(), style);

        public async void Error(LocString msg) => await SendMessage(msg, NotificationStyle.Error);

        public async void ErrorLoc(FormattableString msg) => await SendMessage(Localizer.Do(msg).ToString(), NotificationStyle.Error);

        public async void ErrorLocStr(string msg) => await SendMessage(Localizer.DoStr(msg).ToString(), NotificationStyle.Error);

        public async void OkBox(LocString msg) => await SendMessage(msg.ToString(), NotificationStyle.Info);

        public async void OkBoxLoc(FormattableString msg) => await SendMessage(msg.ToString(), NotificationStyle.Info);

        public async void OkBoxLocStr(string msg) => await SendMessage(Localizer.DoStr(msg).ToString(), NotificationStyle.Info);

        public void TempServerMessage(LocString message, NotificationCategory category = NotificationCategory.Notifications, NotificationStyle style = NotificationStyle.Chat) => Msg(message, style);

        private async Task SendMessage(string msg, NotificationStyle style)
        {
            if (style == NotificationStyle.Error)
                await DiscordCommands.ReportCommandError(DiscordCtx, MessageUtils.StripTags(msg));
            else
                await DiscordCommands.ReportCommandInfo(DiscordCtx, MessageUtils.StripTags(msg));
        }
    }
}
