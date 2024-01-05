using Eco.Moose.Tools.Logger;
using Eco.Gameplay.Civics.Demographics;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems;
using Eco.Gameplay.Systems.Messaging.Chat;
using Eco.Gameplay.Systems.Messaging.Chat.Channels;
using Eco.Gameplay.Systems.Messaging.Mail;
using Eco.Gameplay.Utils;
using Eco.Shared.Localization;
using Eco.Shared.Networking;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using User = Eco.Gameplay.Players.User;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class EcoUtils
    {
        public enum BoxMessageType
        {
            Info,
            Warning,
            Error
        }

        #region Message Sending

        public static readonly string DefaultChatChannelName = "general";

        public static void EnsureChatChannelExists(string channelName)
        {
            if (!ChatChannelExists(channelName))
            {
                CreateChannel(channelName);
            }
        }

        public static bool ChatChannelExists(string channelName)
        {
            return ChannelManager.Obj.Registrar.GetByName(channelName) != null;
        }

        public static Channel CreateChannel(string channelName)
        {
            Channel newChannel = new Channel();
            newChannel.Managers.Add(DemographicManager.Obj.Get(SpecialDemographics.Admins));
            newChannel.Users.Add(DemographicManager.Obj.Get(SpecialDemographics.Everyone));
            newChannel.Name = channelName;
            ChannelManager.Obj.Registrar.Insert(newChannel);

            var channelUsers = newChannel.ChatRecipients;
            foreach (User user in channelUsers)
            {
                var tabSettings = GlobalData.Obj.ChatSettings(user).ChatTabSettings;
                var generalChannel = ChannelManager.Obj.Get(SpecialChannel.General);
                var chatTab = tabSettings.OfType<ChatTabSettingsCommon>().FirstOrDefault(tabSetting => tabSetting.Channels.Contains(generalChannel));
                if(chatTab != null)
                {
                    chatTab.Channels.Add(newChannel);
                }
                else
                {
                    Logger.Warning($"Failed to find chat tab when creating channel \"{channelName}\" for user \"{user.Name}\"");
                }
            }

            return newChannel;
        }

        public static bool SendChatRaw(User sender, string targetAndMessage) // NOTE: Does not trigger ChatMessageSent GameAction
        {
            var to = ChatParsingUtils.ResolveReceiver(targetAndMessage, out var messageContent);
            if (to.Failed || to.Val == null)
            {
                Logger.Error($"Failed to resolve receiver of message: \"{targetAndMessage}\"");
                return false;
            }
            IChatReceiver receiver = to.Val;

            // Clean the message
            messageContent = messageContent.Replace("<br>", "");
            ProfanityUtils.ReplaceIfNotClear(ref messageContent, "<Message blocked - Contained profanity>", null);

            if (string.IsNullOrEmpty(messageContent))
            {
                Logger.Warning($"Attempted to send empty message: \"{targetAndMessage}\"");
                return false;
            }

            // TODO: Handle muted users
            // TODO: Handle access to channels
            // TODO: Handle tab opening for DMs
            // TODO: Handle tab opening for channels

            ChatMessage chatMessage = new ChatMessage(sender, receiver, messageContent);
            IEnumerable<User> receivers = (sender != null ? chatMessage.Receiver.ChatRecipients.Append(sender).Distinct() : chatMessage.Receiver.ChatRecipients);
            foreach (INetClient client in receivers.Select(u => u.Player?.Client).NonNull())
                ChatManager.Obj.RPC("DisplayChatMessage", client, chatMessage.ToBson(client));

            // Add to chatlog so that offline users can see the message when they come online
            ChatManager.Obj.AddToChatLog(chatMessage);

            ChatManager.MessageSent.Invoke(chatMessage);
            return true;
        }

        public static bool SendChatToChannel(User sender, string channel, string message)
        {
            return SendChatRaw(sender, $"#{channel} {message}");
        }

        public static bool SendChatToDefaultChannel(User sender, string message)
        {
            return SendChatRaw(sender, $"#{DefaultChatChannelName} {message}");
        }

        public static bool SendChatToUser(User sender, User receiver, string message)
        {
            return SendChatRaw(sender, $"@{receiver.Name} {message}");
        }

        public static bool SendOKBoxToUser(User receiver, string message)
        {
            try
            {
                receiver?.Player?.OkBoxLoc($"{message}");
                return true;
            }
            catch(Exception e)
            {
                Logger.Exception($"Failed to send OKBox to user \"{receiver.Name}\"", e);
                return false;
            }
        }

        public static bool SendOKBoxToAll(string message)
        {
            try
            {
                foreach (User receiver in UserManager.OnlineUsers)
                {
                    receiver?.Player?.OkBoxLoc($"{message}");
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to send OKBox to all users", e);
                return false;
            }
        }

        public static bool SendInfoBoxToUser(User receiver, string message)
        {
            try
            {
                receiver.Msg(Localizer.DoStr(Text.InfoLight(message)));
                return true;
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to send InfoBox to user \"{receiver.Name}\"", e);
                return false;
            }
        }

        public static bool SendInfoBoxToAll(string message)
        {
            try
            {
                foreach (User receiver in UserManager.OnlineUsers)
                {
                    receiver.Msg(Localizer.DoStr(Text.InfoLight(message)));
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to send InfoBox to all users", e);
                return false;
            }
        }

        public static bool SendWarningBoxToUser(User receiver, string message)
        {
            try
            {
                receiver.Msg(Localizer.DoStr(Text.Warning(message)));
                return true;
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to send WarningBox to user \"{receiver.Name}\"", e);
                return false;
            }
        }

        public static bool SendWarningBoxToAll(string message)
        {
            try
            {
                foreach (User receiver in UserManager.OnlineUsers)
                {
                    receiver.Msg(Localizer.DoStr(Text.Warning(message)));
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to send WarningBox to all users", e);
                return false;
            }
        }

        public static bool SendErrorBoxToUser(User receiver, string message)
        {
            try
            {
                receiver.Msg(Localizer.DoStr(Text.Error(message)));
                return true;
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to send ErrorBox to user \"{receiver.Name}\"", e);
                return false;
            }
        }

        public static bool SendErrorBoxToAll(string message)
        {
            try
            {
                foreach (User receiver in UserManager.OnlineUsers)
                {
                    receiver.Msg(Localizer.DoStr(Text.Error(message)));
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to send ErrorBox to all users", e);
                return false;
            }
        }

        public static bool SendNotificationToUser(User receiver, string message)
        {
            try
            {
                receiver.Mailbox.Add(new MailMessage(message, "Notifications"), false);
                return true;
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to send notification to user \"{receiver.Name}\"", e);
                return false;
            }
        }

        public static bool SendNotificationToAll(string message)
        {
            try
            {
                foreach (User receiver in UserManager.OnlineUsers)
                {
                    receiver.Mailbox.Add(new MailMessage(message, "Notifications"), false);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to send notification to all users", e);
                return false;
            }
        }

        public static bool SendInfoPanelToUser(User receiver, string instance, string title, string message)
        {
            try
            {
                receiver?.Player.OpenInfoPanel(title, message, instance);
                return true;
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to send InfoPanel to user \"{receiver.Name}\"", e);
                return false;
            }
        }

        public static bool SendInfoPanelToAll(string instance, string title, string message)
        {
            try
            {
                foreach (User receiver in UserManager.OnlineUsers)
                {
                    receiver?.Player.OpenInfoPanel(title, message, instance);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to send InfoPanel to all users", e);
                return false;
            }
        }

        #endregion
    }
}
