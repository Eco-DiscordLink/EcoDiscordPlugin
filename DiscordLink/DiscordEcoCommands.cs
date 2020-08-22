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
    public class DiscordEcoCommands : IChatCommandHandler
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
        
        [ChatCommand("Verifies that the Discord plugin is loaded", ChatAuthorizationLevel.Admin)]
        public static void VerifyDiscord(User user)
        {
            CallWithErrorHandling<object>((lUser, args) =>
                {
                    ChatManager.ServerMessageToPlayer(new LocString("Discord Plugin is loaded."), lUser);
                },
                user);
        }
        
        [ChatCommand("Lists Discord servers the bot is in.", ChatAuthorizationLevel.Admin)]
        public static void DiscordGuilds(User user)
        {
            CallWithErrorHandling<object>((lUser, args) =>
                {
                    var plugin = DiscordLink.Obj;
                    if (plugin == null) return;

                    var joinedNames = String.Join(", ", plugin.GuildNames);
            
                    ChatManager.ServerMessageToPlayer(new LocString("Servers: " + joinedNames), user);
                },
                user);
        }
        
        [ChatCommand("Sends a message to a specific server and channel.", ChatAuthorizationLevel.Admin)]
        public static void DiscordSendToChannel(User user, string guild, string channel, string outerMessage)
        {
            CallWithErrorHandling<object>((lUser, args) =>
                {
                    var plugin = DiscordLink.Obj;
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
        
        [ChatCommand("Sends a message to the default server and channel.", ChatAuthorizationLevel.Admin)]
        public static void DiscordMessage(User user, string message)
        {
            CallWithErrorHandling<object>((lUser, args) =>
                {
                    var plugin = DiscordLink.Obj;
                    if (plugin == null) return;

                    var defaultChannel = plugin.GetDefaultChannelForPlayer(user.Name);

                    plugin.SendDiscordMessageAsUser(message, user, defaultChannel).ContinueWith(result =>
                    {
                        ChatManager.ServerMessageToPlayer(new LocString(result.Result), user);   
                    });
                },
                user, message);
        }
        
        [ChatCommand("Lists channels available to the bot in a specific server.", ChatAuthorizationLevel.Admin)]
        public static void DiscordChannels(User user, string guildName)
        {   
            CallWithErrorHandling<object>((lUser, args) =>
                {
                    var plugin = DiscordLink.Obj;
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
                    ChatManager.ServerMessageToAll( new LocString("Channels: " + joinedGames) );
                },
                user);
        }
        
        [ChatCommand("Sets default channel to use.", ChatAuthorizationLevel.Admin)]
        public static void DiscordDefaultChannel(User user, string guildName, string channelName)
        {
            CallWithErrorHandling<object>((lUser, args) =>
                {
                    var plugin = DiscordLink.Obj;
                    if (plugin == null) return;

                    plugin.SetDefaultChannelForPlayer(user.Name, guildName, channelName);
                    ChatManager.ServerMessageToPlayer(new LocString("Default channel set to " + channelName), user);
                },
                user);
        }

        [ChatCommand("Displays Discord invite message.", ChatAuthorizationLevel.User)]
        public static void DiscordInvite(User user)
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                var plugin = DiscordLink.Obj;
                if (plugin == null) return;

                var config = DLConfig.Data;
                var serverInfo = Networking.NetworkManager.GetServerInfo();

                string inviteMessage = config.InviteMessage;
                if (!inviteMessage.Contains(DLConfig.InviteCommandLinkToken) || string.IsNullOrEmpty(serverInfo.DiscordAddress))
                {
                    ChatManager.ServerMessageToPlayer( new LocString("This server is not configured for using the /DiscordInvite command."), user);
                    return;
                }
                
                inviteMessage = Regex.Replace(inviteMessage, Regex.Escape(DLConfig.InviteCommandLinkToken), serverInfo.DiscordAddress);
                string formattedInviteMessage = $"#{config.EcoCommandChannel} {inviteMessage}";
                ChatManager.SendChat(formattedInviteMessage, plugin.EcoUser);
            },
            user);
        }
    }
}
