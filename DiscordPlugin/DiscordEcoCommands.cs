using System;
using System.Threading.Tasks;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Utils;

namespace Eco.Spoffy
{
    /**
     * Handles commands coming from the Eco server.
     */
    public class DiscordEcoCommands : IChatCommandHandler
    {
        private static void Log(string message)
        {
            Eco.Shared.Utils.Log.Write("DiscordLink: " + message);
        }
        
        private delegate void EcoCommandFunction(User user, params string[] args);

        private static void CallWithErrorHandling<TRet>(EcoCommandFunction toCall, User user, params string[] args)
        {
            try
            {
                toCall(user, args);
            }
            catch (Exception e)
            {
                ChatManager.ServerMessageToPlayer("Error occurred while attempting to run that command. Error message: " + e, user, false);
                Log("Error occurred while attempting to run that command. Error message: " + e);
                Log(e.StackTrace);
            }
        }
        
        [ChatCommand("Verifies that the Discord plugin is loaded", ChatAuthorizationLevel.Admin)]
        public static void VerifyDiscord(User user)
        {
            CallWithErrorHandling<object>((lUser, args) =>
                {
                    ChatManager.ServerMessageToPlayer("Discord Plugin is loaded.", lUser);
                },
                user);
        }
        
        [ChatCommand("Lists Discord Servers the bot is in. ", ChatAuthorizationLevel.Admin)]
        public static void DiscordGuilds(User user)
        {
            CallWithErrorHandling<object>((lUser, args) =>
                {
                    var plugin = DiscordPlugin.Obj;
                    if (plugin == null) return;

                    var joinedNames = String.Join(", ", plugin.GuildNames);
            
                    ChatManager.ServerMessageToPlayer("Servers: " + joinedNames, user, false);
                },
                user);

        }
        
        [ChatCommand("Lists Discord Servers the bot is in. ", ChatAuthorizationLevel.Admin)]
        public static void DiscordSendMessage(User user, string guild, string channel, string server)
        {
            CallWithErrorHandling<object>((lUser, args) =>
                {
                    var plugin = DiscordPlugin.Obj;
                    if (plugin == null) return;

                    var guildName = args[0];
                    var channelName = args[1];
                    var message = args[2];

                    plugin.SendMessageAsUser(message, channelName, guildName, user).ContinueWith(result =>
                    {
                        ChatManager.ServerMessageToPlayer(result.Result, user);   
                    });
                },
                user, guild, channel, server);
        }
        
        [ChatCommand("Lists Discord Servers the bot is in. ", ChatAuthorizationLevel.Admin)]
        public static void DiscordChannels(User user, string guildName)
        {   
            CallWithErrorHandling<object>((lUser, args) =>
                {
                    var plugin = DiscordPlugin.Obj;
                    if (plugin == null) return;

                    var guild = string.IsNullOrEmpty(guildName)
                        ? plugin.DefaultGuild 
                        : plugin.GuildByName(guildName);

                    //Can happen in DefaultGuild is not configured.
                    if (guild == null)
                    {
                        ChatManager.ServerMessageToPlayer("Unable to find that guild, perhaps the name was misspelled?", user);
                    }

                    var joinedGames = String.Join(", ", plugin.ChannelsInGuild(guild));
                    ChatManager.ServerMessageToAll("Channels: " + joinedGames, false);
                },
                user);

        }
    }
}