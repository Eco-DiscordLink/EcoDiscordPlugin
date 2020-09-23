using System;
using System.Linq;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Localization;
using System.Text.RegularExpressions;
using Eco.Plugins.DiscordLink.Utilities;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    /**
     * Handles commands coming from the Eco server.
     */
    public class EcoCommands : IChatCommandHandler
    {
        private delegate void EcoCommandFunction(User user, params string[] args);

        private static void CallWithErrorHandling<TRet>(EcoCommandFunction toCall, User user, params string[] args)
        {
            try
            {
                toCall(user, args);
            }
            catch (Exception e)
            {
                ChatManager.ServerMessageToPlayer(new LocString("Error occurred while attempting to run that command. Error message: " + e), user);
                Logger.Error("Error occurred while attempting to run that command. Error message: " + e);
            }
        }

        [ChatCommand("Commands for the Discord integration plugin.", "DL", ChatAuthorizationLevel.User)]
        public static void DiscordLink(User user) { }

        [ChatSubCommand("DiscordLink", "Lists Discord servers the bot is in.", ChatAuthorizationLevel.Admin)]
        public static void ListGuilds(User user)
        {
            CallWithErrorHandling<object>((lUser, args) =>
                {
                    var plugin = Plugins.DiscordLink.DiscordLink.Obj;
                    if (plugin == null) return;

                    var joinedNames = string.Join(", ", plugin.GuildNames);

                    ChatManager.ServerMessageToPlayer(new LocString("Servers: " + joinedNames), user);
                },
                user);
        }

        [ChatSubCommand("DiscordLink", "Lists channels available to the bot in a specific server.", ChatAuthorizationLevel.Admin)]
        public static void ListChannels(User user, string guildName)
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                var plugin = Plugins.DiscordLink.DiscordLink.Obj;
                if (plugin == null) return;

                var guild = string.IsNullOrEmpty(guildName)
                    ? plugin.DefaultGuild
                    : plugin.GuildByName(guildName);

                // Can happen if DefaultGuild is not configured.
                if (guild == null)
                {
                    ChatManager.ServerMessageToPlayer(new LocString("Unable to find that guild, perhaps the name was misspelled?"), user);
                }

                var joinedGames = string.Join(", ", guild.TextChannelNames());
                ChatManager.ServerMessageToAll(new LocString("Channels: " + joinedGames));
            },
                user);
        }

        [ChatSubCommand("DiscordLink", "Sends a message to a specific server and channel.", ChatAuthorizationLevel.Admin)]
        public static void SendMessageToChannel(User user, string guild, string channel, string outerMessage)
        {
            CallWithErrorHandling<object>((lUser, args) =>
                {
                    var plugin = Plugins.DiscordLink.DiscordLink.Obj;
                    if (plugin == null) return;

                    var guildName = args[0];
                    var channelName = args[1];
                    var message = args[2];

                    plugin.SendDiscordMessageAsUser(message, user, channelName, guildName).ContinueWith(result =>
                    {
                        ChatManager.ServerMessageToPlayer(new LocString(result.Result), user);
                    });
                },
                user, guild, channel, outerMessage);
        }

        [ChatSubCommand("DiscordLink", "Sends a message to the default server and channel.", ChatAuthorizationLevel.Admin)]
        public static async Task SendMessage(User user, string message)
        {
            CallWithErrorHandling<object>((lUser, args) =>
                {
                    var plugin = Plugins.DiscordLink.DiscordLink.Obj;
                    if (plugin == null) return;

                    var defaultChannel = plugin.GetDefaultChannelForPlayer(user.Name);

                    plugin.SendDiscordMessageAsUser(message, user, defaultChannel).ContinueWith(async result =>
                    {
                        ChatManager.ServerMessageToPlayer(new LocString(await result), user);
                    });
                },
                user, message);
        }

        [ChatSubCommand("DiscordLink", "Sets the default Discord channel to use.", ChatAuthorizationLevel.Admin)]
        public static void SetDefaultChannel(User user, string guildName, string channelName)
        {
            CallWithErrorHandling<object>((lUser, args) =>
                {
                    var plugin = Plugins.DiscordLink.DiscordLink.Obj;
                    if (plugin == null) return;

                    plugin.SetDefaultChannelForPlayer(user.Name, guildName, channelName);
                    ChatManager.ServerMessageToPlayer(new LocString("Default channel set to " + channelName), user);
                },
                user);
        }

        [ChatSubCommand("Restart", "Restarts the plugin.", "dl-restart", ChatAuthorizationLevel.Admin)]
        public static void Restart(User user)
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                DiscordLink plugin = Plugins.DiscordLink.DiscordLink.Obj;
                if (plugin == null) return;

                _ = plugin.RestartClient();
            },
            user);
        }

        [ChatSubCommand("DiscordLink", "Displays information about the DiscordLink plugin.", "dl-about", ChatAuthorizationLevel.User)]
        public static void About(User user)
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                ChatManager.ServerMessageToPlayer(new LocString(MessageBuilder.GetAboutMessage()), user);
            },
            user);
        }

        [ChatSubCommand("DiscordLink", "Displays Discord invite message.", "dl-invite", ChatAuthorizationLevel.User)]
        public static void Invite(User user, string ecoChannel = "")
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                var plugin = Plugins.DiscordLink.DiscordLink.Obj;
                if (plugin == null) return;

                var config = DLConfig.Data;
                var serverInfo = Networking.NetworkManager.GetServerInfo();

                string inviteMessage = config.InviteMessage;
                if (!inviteMessage.Contains(DLConfig.InviteCommandLinkToken) || string.IsNullOrEmpty(serverInfo.DiscordAddress))
                {
                    ChatManager.ServerMessageToPlayer(new LocString("This server is not configured for using the /DiscordInvite command."), user);
                    return;
                }

                inviteMessage = Regex.Replace(inviteMessage, Regex.Escape(DLConfig.InviteCommandLinkToken), serverInfo.DiscordAddress);
                string formattedInviteMessage = $"#{(string.IsNullOrEmpty(ecoChannel) ? config.EcoCommandChannel : ecoChannel) } {inviteMessage}";
                ChatManager.SendChat(formattedInviteMessage, plugin.EcoUser);
            },
            user);
        }

        [ChatSubCommand("DiscordLink", "Post a predefined snippet from Discord.", "dl-snippet", ChatAuthorizationLevel.User)]
        public static void Snippet(User user, string snippetKey = "", string ecoChannel = "")
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                var plugin = Plugins.DiscordLink.DiscordLink.Obj;
                if (plugin == null) return;

                var snippets = DLStorage.Instance.Snippets;
                string response;
                if (string.IsNullOrWhiteSpace(snippetKey)) // List all snippets if no key is given
                {
                    response = (snippets.Count > 0 ? "Available snippets:\n" : "There are no registered snippets.");
                    foreach (var snippetKeyVal in snippets)
                    {
                        response += snippetKeyVal.Key + "\n";
                    }
                    ChatManager.ServerMessageToPlayer(new LocString(response), user);
                }
                else
                {
                    string snippetKeyLower = snippetKey.ToLower();
                    if (snippets.TryGetValue(snippetKeyLower, out string sippetText))
                    {
                        response = user.Name + " invoked snippet \"" + snippetKey + "\"\n- - -\n" + sippetText + "\n- - -";
                        string formattedSnippetMessage = $"#{(string.IsNullOrEmpty(ecoChannel) ? DLConfig.Data.EcoCommandChannel : ecoChannel) } {response}";
                        ChatManager.SendChat(formattedSnippetMessage, plugin.EcoUser);
                    }
                    else
                    {
                        response = "No snippet with key \"" + snippetKey + "\" could be found.";
                        ChatManager.ServerMessageToPlayer(new LocString(response), user);
                    }
                }
            },
            user);
        }

        [ChatSubCommand("DiscordLink", "Sends an Eco server message according to parameters", "dl-servermessage", ChatAuthorizationLevel.Admin)]
        public static void SendServerMessage(User user, string message, string persistanceType = "temporary", string recipientUserName = "")
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                bool permanent;
                string persistanceTypeLower = persistanceType.ToLower();
                if (persistanceTypeLower == "temporary")
                {
                    permanent = true;
                }
                else if (persistanceTypeLower == "permanent")
                {
                    permanent = false;
                }
                else
                {
                    ChatManager.ServerMessageToPlayer(new LocString("Persistance type must either be \"Temporary\" or \"Permanent\"."), user);
                    return;
                }

                User recipient = null;
                if (!string.IsNullOrWhiteSpace(recipientUserName))
                {
                    user = UserManager.OnlineUsers.FirstOrDefault(x => x.Name.ToLower() == recipientUserName);
                    if (user == null)
                    {
                        ChatManager.ServerMessageToPlayer(new LocString("No online user with the name \"" + recipientUserName + "\" could be found."), user);
                        return;
                    }
                }

                EcoUtil.SendServerMessage("[" + user.Name + "] " + message, permanent, recipient);
                ChatManager.ServerMessageToPlayer(new LocString("Message delivered."), user);
            },
            user);
        }

        [ChatSubCommand("DiscordLink", "Sends an Eco popup message", "dl-popup", ChatAuthorizationLevel.Admin)]
        public static void SendPopup(User user, string message, string recipientUserName = "")
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                User recipient = null;
                if (!string.IsNullOrWhiteSpace(recipientUserName))
                {
                    recipient = UserManager.OnlineUsers.FirstOrDefault(x => x.Name.ToLower() == recipientUserName);
                    if (recipient == null)
                    {
                        ChatManager.ServerMessageToPlayer(new LocString("No online user with the name \"" + recipientUserName + "\" could be found."), user);
                        return;
                    }
                }

                EcoUtil.SendPopupMessage("[" + user.Name + "]\n\n" + message, recipient);
                ChatManager.ServerMessageToPlayer(new LocString("Message delivered."), user);
            },
            user);
        }

        // Announcements do not pop. May be broken in em-framework.
        //[ChatSubCommand("DiscordLink", "Sends an Eco announcement message", "dl-announcement", ChatAuthorizationLevel.Admin)]
        //public static void SendAnnouncement(User user, string title, string message, string recipientUserName = "")
        //{
        //    CallWithErrorHandling<object>((lUser, args) =>
        //    {
        //        User recipient = null;
        //        if (!string.IsNullOrWhiteSpace(recipientUserName))
        //        {
        //            recipient = UserManager.OnlineUsers.FirstOrDefault(x => x.Name.ToLower() == recipientUserName);
        //            if (recipient == null)
        //            {
        //                ChatManager.ServerMessageToPlayer(new LocString("No online user with the name \"" + recipientUserName + "\" could be found."), user);
        //                return;
        //            }
        //        }
        //
        //        EcoUtil.SendAnnouncementMessage(title, message + "\n\n[" + user.Name + "]", recipient);
        //        ChatManager.ServerMessageToPlayer(new LocString("Message delivered."), user);
        //    },
        //    user);
        //}
    }
}
