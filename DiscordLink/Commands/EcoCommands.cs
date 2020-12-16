using DSharpPlus.Entities;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Localization;
using Eco.Shared.Networking;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    /**
     * Handles commands coming from Eco.
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
                Logger.Error("An error occurred while attempting to execute an Eco command. Error message: " + e);
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

                Logger.Info("Eco Restart command executed - Restarting client");
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

                DLConfigData config = DLConfig.Data;
                ServerInfo serverInfo = Networking.NetworkManager.GetServerInfo();

                string inviteMessage = config.InviteMessage;
                if (!inviteMessage.Contains(DLConfig.InviteCommandLinkToken) || string.IsNullOrEmpty(serverInfo.DiscordAddress))
                {
                    ChatManager.ServerMessageToPlayer(new LocString("This server is not configured for using the /DiscordInvite command."), user);
                    return;
                }

                inviteMessage = Regex.Replace(inviteMessage, Regex.Escape(DLConfig.InviteCommandLinkToken), serverInfo.DiscordAddress);
                string formattedInviteMessage = $"#{(string.IsNullOrEmpty(ecoChannel) ? config.EcoCommandOutputChannel : ecoChannel) } {inviteMessage}";
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
                    // Find and post the snippet requested by the user
                    string snippetKeyLower = snippetKey.ToLower();
                    if (snippets.TryGetValue(snippetKeyLower, out string sippetText))
                    {
                        response = user.Name + " invoked snippet \"" + snippetKey + "\"\n- - -\n" + sippetText + "\n- - -";
                        string formattedSnippetMessage = $"#{(string.IsNullOrEmpty(ecoChannel) ? DLConfig.Data.EcoCommandOutputChannel : ecoChannel) } {response}";
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

        [ChatSubCommand("DiscordLink", "Sends an Eco announcement message", "dl-announcement", ChatAuthorizationLevel.Admin)]
        public static void SendAnnouncement(User user, string title, string message, string recipientUserName = "")
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

                EcoUtil.SendAnnouncementMessage(title, message + "\n\n[" + user.Name + "]", recipient);
                ChatManager.ServerMessageToPlayer(new LocString("Message delivered."), user);
            },
            user);
        }

        [ChatSubCommand("DiscordLink", "Links the calling user account to a Discord account", "dl-link", ChatAuthorizationLevel.User)]
        public static void LinkDiscordAccount(User user, string DiscordName)
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                var plugin = Plugins.DiscordLink.DiscordLink.Obj;
                if (plugin == null) return;

                // Find the Discord user
                DiscordMember matchingMember = null;
                foreach(DiscordGuild guild in plugin.DiscordClient.Guilds.Values)
                {
                    IReadOnlyCollection<DiscordMember> guildMembers = DiscordUtil.GetGuildMembersAsync(guild).Result;
                    if (guildMembers == null) continue;

                    foreach (DiscordMember member in guildMembers)
                    {
                        if(member.Id.ToString() == DiscordName || member.Username.ToLower() == DiscordName.ToLower())
                        {
                            matchingMember = member;
                            break;
                        }
                    }
                }

                if(matchingMember == null)
                {
                    ChatManager.ServerMessageToPlayer(new LocString($"No Discord account with the ID or name \"{DiscordName}\" could be found."), user);
                    return;
                }

                // Make sure that the accounts aren't already linked to any account
                foreach (LinkedUser linkedUser in DLStorage.PersistantData.LinkedUsers)
                {
                    
                    if (user.SlgId == linkedUser.SlgId || user.SteamId == linkedUser.SteamId)
                    {
                        if (linkedUser.DiscordId == matchingMember.Id.ToString())
                            ChatManager.ServerMessageToPlayer(new LocString("Eco account is already linked to this Discord account.\nUse /dl-unlink to remove the existing link."), user);
                        else
                            ChatManager.ServerMessageToPlayer(new LocString("Eco account is already linked to a different Discord account.\nUse /dl-unlink to remove the existing link."), user);
                        return;
                    }
                    else if (linkedUser.DiscordId == matchingMember.Id.ToString())
                    {
                        ChatManager.ServerMessageToPlayer(new LocString("Discord account is already linked to a different Eco account."), user);
                        return;
                    }
                }

                // Create a linked user from the combined Eco and Discord info
                LinkedUser DlUser = LinkedUserManager.AddLinkedUser(user, matchingMember.Id.ToString());

                // Notify the Discord account that a link has been made and ask for verification
                _ = DiscordUtil.SendDmAsync(matchingMember, null, MessageBuilder.GetVerificationDM(user));

                // Notify the Eco user that the link has been created and that verification is required
                ChatManager.ServerMessageToPlayer(new LocString($"Your account has been linked.\nThe link requires verification before becoming active.\nInstructions have been sent to the linked Discord account."), user);
            },
            user);
        }

        [ChatSubCommand("DiscordLink", "Unlinks the calling user account from a linked Discord account", "dl-unlink", ChatAuthorizationLevel.User)]
        public static void UnlinkDiscordAccount(User user)
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                bool result = LinkedUserManager.RemoveLinkedUser(user);
                if(result)
                    ChatManager.ServerMessageToPlayer(new LocString($"Discord account unlinked."), user);
                else
                    ChatManager.ServerMessageToPlayer(new LocString($"No linked Discord account could be found."), user);
            }, user);
        }
    }
}
