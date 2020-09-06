using System;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Localization;
using System.Text.RegularExpressions;
using Eco.Plugins.DiscordLink.Utilities;

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

                var joinedGames = String.Join(", ", guild.TextChannelNames());
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
        public static void SendMessage(User user, string message)
        {
            CallWithErrorHandling<object>((lUser, args) =>
                {
                    var plugin = Plugins.DiscordLink.DiscordLink.Obj;
                    if (plugin == null) return;

                    var defaultChannel = plugin.GetDefaultChannelForPlayer(user.Name);

                    plugin.SendDiscordMessageAsUser(message, user, defaultChannel).ContinueWith(result =>
                    {
                        ChatManager.ServerMessageToPlayer(new LocString(result.Result), user);
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

        [ChatSubCommand("DiscordLink", "Displays information about the DiscordLink plugin.", ChatAuthorizationLevel.User)]
        public static void About(User user)
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                string message = "DiscordLink is a plugin mod that runs on this server." +
                "\nIt connects the game server to a Discord bot in order to perform seamless communication between Eco and Discord." + 
                "\nThis enables you to chat with players who are currently not online in Eco, but are available on Discord." +
                "\nDiscordLink can also be used to display information about the Eco server in Discord, such as who is online and what items are available on the market." +
                "\n\nFor more information, visit \"www.github.com/Spoffy/EcoDiscordPlugin\".";
                ChatManager.ServerMessageToPlayer(new LocString(message), user);
            },
            user);
        }

        [ChatSubCommand("DiscordLink", "Displays Discord invite message.", ChatAuthorizationLevel.User)]
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

        [ChatSubCommand("DiscordLink", "Post a predefined snippet from Discord.", "snippet", ChatAuthorizationLevel.User)]
        public static void PostSnippet(User user, string snippetKey = "", string ecoChannel = "")
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
    }
}
