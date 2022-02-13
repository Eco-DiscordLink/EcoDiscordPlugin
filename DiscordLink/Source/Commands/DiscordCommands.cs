using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Gameplay.Systems.Messaging.Chat;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public class DiscordCommands : BaseCommandModule
    {
        #region Commands Base

        public enum PermissionType
        {
            User,
            Admin
        }

        public delegate Task DiscordCommand(CommandContext ctx, params string[] parameters);

        private static async Task ExecuteCommand<TRet>(PermissionType requiredPermission, DiscordCommand command, CommandContext ctx, params string[] parameters)
        {
            try
            {
                if (!IsCommandAllowedForUser(ctx, requiredPermission))
                {
                    string permittedRolesDesc = (DLConfig.Data.AdminRoles.Count > 0) ? string.Join("\n- ", DLConfig.Data.AdminRoles.ToArray()) : "No admin roles configured";
                    await RespondToCommand(ctx, $"You lack the `{requiredPermission}` level permission required to execute this command.\nThe permitted roles are:\n```- {permittedRolesDesc}```");
                    return;
                }

                if (!IsCommandAllowedInChannel(ctx))
                {
                    string commandChannels = string.Join("\n- ", DLConfig.Data.DiscordCommandChannels.Where(link => link.IsValid()).Select(channel => channel.Channel.Name));
                    await RespondToCommand(ctx, $"You aren't allowed to post commands in this channel.\nCommands are allowed in the following channels:\n```- {commandChannels}```");
                    return;
                }

                if (ctx.Channel.IsPrivate)
                    Logger.Debug($"{ctx.User.Username} invoked Discord command \"{ctx.Prefix}{command.Method.Name}\" in DM");
                else
                    Logger.Debug($"{ctx.User.Username} invoked Discord command \"{ctx.Prefix}{command.Method.Name}\" in channel {ctx.Channel.Name}");

                await command(ctx, parameters);
            }
            catch (Exception e)
            {
                Logger.Error($"An error occurred while attempting to execute a Discord command. Error message: {e}");
                await RespondToCommand(ctx, $"An error occurred while attempting to run that command. Error message: {e}");
            }
        }

        private static async Task RespondToCommand(CommandContext ctx, string fullTextContent, DiscordLinkEmbed embedContent = null)
        {
            async static Task Respond(CommandContext ctx, string textContent, DiscordLinkEmbed embedContent)
            {
                // If needed; split the message into multiple parts
                ICollection<string> stringParts = MessageUtils.SplitStringBySize(textContent, DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT);
                ICollection<DiscordEmbed> embedParts = MessageUtils.BuildDiscordEmbeds(embedContent);

                if (stringParts.Count <= 1 && embedParts.Count == 1)
                {
                    await ctx.RespondAsync(textContent, embedParts.First());
                }
                else
                {
                    foreach (string textMessagePart in stringParts)
                    {
                        await ctx.RespondAsync(textMessagePart);
                    }
                    foreach (DiscordEmbed embedPart in embedParts)
                    {
                        await ctx.RespondAsync(embedPart);
                    }
                }
            }

            try
            {
                DLDiscordClient client = DiscordLink.Obj.Client;
                if (!client.ChannelHasPermission(ctx.Channel, Permissions.SendMessages) || !client.ChannelHasPermission(ctx.Channel, Permissions.ReadMessageHistory))
                {
                    Logger.Error($"Failed to respond to command \"{ctx.Command.Name}\" in channel \"{ctx.Channel}\" as the bot lacks permissions for sending and/or reading messages in this channel.");
                    return;
                }

                if (embedContent == null)
                {
                    await Respond(ctx, fullTextContent, null);
                }
                else
                {
                    // Either make sure we have permission to use embeds or convert the embed to text
                    if (client.ChannelHasPermission(ctx.Channel, Permissions.EmbedLinks))
                    {
                        await Respond(ctx, fullTextContent, embedContent);
                    }
                    else
                    {
                        await Respond(ctx, $"{fullTextContent}\n{embedContent.AsText()}", null);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"An error occurred while attempting to respond to command. Error message: {e}");
            }
        }

        private static bool IsCommandAllowedForUser(CommandContext ctx, PermissionType requiredPermission)
        {
            return requiredPermission switch
            {
                PermissionType.User => true,
                PermissionType.Admin => DiscordLink.Obj.Client.MemberIsAdmin(ctx.Member),
                _ => false,
            };
        }

        private static bool IsCommandAllowedInChannel(CommandContext ctx)
        {
            var commandChannels = DLConfig.Data.DiscordCommandChannels;
            bool allowed =
                ctx.Channel.IsPrivate
                || DiscordLink.Obj.Client.MemberIsAdmin(ctx.Member) // Allow admins to override channel requirements
                || !(commandChannels.Any(link => link.IsValid())); // Always allow if there are no valid command channels or the command is sent via DM

            // Check if the discord channel used is listed as a command channel
            if (!allowed)
            {
                foreach (ChannelLink link in commandChannels)
                {
                    if (link.IsChannel(ctx.Channel))
                    {
                        allowed = true;
                        break;
                    }
                }
            }

            return allowed;
        }

        #endregion

        #region User Feedback

        public static async Task ReportCommandError(CommandContext ctx, string message)
        {
            await RespondToCommand(ctx, message);
        }

        public static async Task ReportCommandInfo(CommandContext ctx, string message)
        {
            await RespondToCommand(ctx, message);
        }

        public static async Task DisplayCommandData(CommandContext ctx, string title, DiscordLinkEmbed embed)
        {
            await RespondToCommand(ctx, title, embed);
        }
        public static async Task DisplayCommandData(CommandContext ctx, string title, string content)
        {
            await RespondToCommand(ctx,  $"**{title}**\n```{content}```");
        }

        #endregion

        #region Plugin Management

        [Command("Restart")]
        [Description("Restarts the plugin.")]
        [Aliases("DL-Restart")]
        public async Task Restart(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.Restart(SharedCommands.CommandInterface.Discord, ctx);
            }, ctx);
        }

        [Command("ResetPersistentData")]
        [Description("Removes all persistent storage data.")]
        [Aliases("DL-ResetPersistentdata")]
        public async Task ResetPersistentData(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.ResetPersistentData(SharedCommands.CommandInterface.Discord, ctx);
            }, ctx);
        }

        [Command("ResetWorldData")]
        [Description("Resets world data as if a new world had been created.")]
        [Aliases("DL-ResetWorldData")]
        public async Task ResetWorldData(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.ResetWorldData(SharedCommands.CommandInterface.Discord, ctx);
            }, ctx);
        }

        #endregion

        #region Meta

        [Command("DiscordLinkAbout")]
        [Description("Posts a message describing what the DiscordLink plugin is.")]
        [Aliases("DL-About")]
        public async Task About(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle("About DiscordLink")
                    .WithDescription(MessageBuilder.Shared.GetAboutMessage());

                await RespondToCommand(ctx, null, embed);
            }, ctx);
        }

        [Command("PluginStatus")]
        [Description("Shows the plugin status.")]
        [Aliases("DL-Status", "Status")]
        public async Task PluginStatus(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, await MessageBuilder.Shared.GetDisplayStringAsync(verbose: false));
            }, ctx);
        }

        [Command("PluginStatusVerbose")]
        [Description("Shows the plugin status including verbose debug level information.")]
        [Aliases("DL-StatusVerbose", "StatusVerbose")]
        public async Task PluginStatusVerbose(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, await MessageBuilder.Shared.GetDisplayStringAsync(verbose: true));
            }, ctx);
        }

        [Command("VerifyConfig")]
        [Description("Checks configuration setup and reports any errors.")]
        [Aliases("DL-VerifyConfig", "DL-ConfigReport", "ConfigReport")]
        public async Task VerifyConfig(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.VerifyConfig(SharedCommands.CommandInterface.Discord, ctx);
            }, ctx);
        }

        [Command("VerifyPermissions")]
        [Description("Checks all permissions and intents needed for the current configuration and reports any missing ones.")]
        [Aliases("DL-VerifyPermissions", "DL-PermissionReport", "PermissionReport")]
        public async Task VerifyPermissions(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.VerifyPermissions(SharedCommands.CommandInterface.Discord, ctx, MessageBuilder.PermissionReportComponentFlag.All);
            }, ctx);
        }

        [Command("VerifyIntents")]
        [Description("Checks all intents needed and reports any missing ones.")]
        [Aliases("DL-VerifyIntents")]
        public async Task CheckIntents(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.VerifyPermissions(SharedCommands.CommandInterface.Discord, ctx, MessageBuilder.PermissionReportComponentFlag.Intents);
            }, ctx);
        }

        [Command("VerifyServerPermissions")]
        [Description("Checks all server permissions needed and reports any missing ones.")]
        [Aliases("DL-VerifyServerPermissions", "ServerPermissions", "DL-ServerPermissions")]
        public async Task CheckServerPermissions(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.VerifyPermissions(SharedCommands.CommandInterface.Discord, ctx, MessageBuilder.PermissionReportComponentFlag.ServerPermissions);
            }, ctx);
        }

        [Command("VerifyChannelPermissions")]
        [Description("Checks all permissions needed for the given channel and reports any missing ones.")]
        [Aliases("DL-VerifyChannelPermissions", "ChannelPermissions", "DL-ChannelPermissions")]
        public async Task CheckChannelPermissions(CommandContext ctx, [Description("Name or ID of the channel to check permissions for. The current channel will be used if this parameter is omitted.")] string channelNameOrID = "")
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                if(string.IsNullOrWhiteSpace(channelNameOrID))
                    await SharedCommands.VerifyPermissionsForChannel(SharedCommands.CommandInterface.Discord, ctx, ctx.Channel);
                else
                    await SharedCommands.VerifyPermissionsForChannel(SharedCommands.CommandInterface.Discord, ctx, channelNameOrID);
            }, ctx);
        }

        [Command("ListLinkedChannels")]
        [Description("Presents a list of all channel links.")]
        [Aliases("DL-ListLinkedChannels", "ListChannels", "DL-ListChannels")]
        public async Task ListLinkedChannels(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.ListChannelLinks(SharedCommands.CommandInterface.Discord, ctx);
            }, ctx);
        }

        [Command("Print")]
        [Description("Reposts the inputted message. Can be used to create tags for ordering display tags within a channel.")]
        public async Task Print(CommandContext ctx, [Description("The message to print.")] string message)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, message);
            }, ctx);
        }

        [Command("Echo")]
        [Description("Sends the provided message to Eco and back to Discord again.")]
        public async Task Echo(CommandContext ctx, [Description("The message to send and then receive back again. A random message will be sent if this parameter is omitted.")] string message = "", [Description("The eco channel you want to test.")] string ecoChannel = "")
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    Random rnd = new Random();
                    switch (rnd.Next(1, 5))
                    {
                        case 1:
                            message = "One thing has suddenly ceased to lead to another.";
                            break;

                        case 2:
                            message = "Nothing travels faster than the speed of light with the possible exception of bad news, which obeys its own special laws.";
                            break;

                        case 3:
                            message = "Life... is like a grapefruit. It's orange and squishy, and has a few pips in it, and some folks have half a one for breakfast.";
                            break;

                        case 4:
                            message = "So long and thanks for all the fish.";
                            break;

                        case 5:
                            message = "Time is an illusion. Lunch-time doubly so.";
                            break;
                    }
                }

                if (string.IsNullOrWhiteSpace(ecoChannel))
                    ecoChannel = DLConstants.DEFAULT_CHAT_CHANNEL;

                EcoUtils.SendChatToChannel(ecoChannel, $"{DLConstants.ECHO_COMMAND_TOKEN} {message}");
            }, ctx);
        }

        [Command("Ping")]
        [Description("Checks if the bot is online.")]
        public async Task Ping(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, $"Pong {ctx.User.Mention}");
            }, ctx);
        }

        #endregion

        #region Account Linking

        [Command("LinkInformation")]
        [Description("Presents information about account linking.")]
        [Aliases("DL-LinkInfo")]
        public async Task LinkInformation(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle("Eco --> Discord Account Linking")
                    .WithDescription(MessageBuilder.Shared.GetLinkAccountInfoMessage(SharedCommands.CommandInterface.Discord));

                await RespondToCommand(ctx, null, embed);
            }, ctx);
        }

        #endregion

        #region Lookups

        [Command("ServerStatus")]
        [Description("Displays the Server Info status.")]
        [Aliases("DL-EcoStatus", "DL-ServerInfo", "EcoStatus")]
        public async Task ServerStatus(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await DisplayCommandData(ctx, string.Empty, MessageBuilder.Discord.GetServerInfo(MessageBuilder.ServerInfoComponentFlag.All));
            }, ctx);
        }

        [Command("PlayerList")]
        [Description("Lists the players currently online on the server.")]
        [Aliases("Players", "DL-Players")]
        public async Task PlayerList(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle("Players")
                    .WithDescription(MessageBuilder.Shared.GetOnlinePlayerList());
                await DisplayCommandData(ctx, string.Empty, embed);
            }, ctx);
        }

        [Command("PlayerReport")]
        [Description("Displays the Player Report for the given player.")]
        [Aliases("Player", "DL-PlayerReport")]
        public async Task PlayerReport(CommandContext ctx,
            [Description("Name or ID of the player for which to display the report.")] string playerNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.PlayerReport(SharedCommands.CommandInterface.Discord, ctx, playerNameOrID);
            }, ctx);
        }

        [Command("PlayerOnlineReport")]
        [Description("Displays the Player Online Status Report for the given player.")]
        [Aliases("PlayerOnline", "DL-PlayerOnline")]
        public async Task PlayerOnlineReport(CommandContext ctx,
            [Description("Name or ID of the player for which to display the report.")] string playerNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.PlayerOnlineReport(SharedCommands.CommandInterface.Discord, ctx, playerNameOrID);
            }, ctx);
        }

        [Command("PlayerTimeReport")]
        [Description("Displays the Player Time Report for the given player.")]
        [Aliases("PlayerTime", "DL-PlayerTime")]
        public async Task PlayerTimeReport(CommandContext ctx,
            [Description("Name or ID of the player for which to display the report.")] string playerNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.PlayerTimeReport(SharedCommands.CommandInterface.Discord, ctx, playerNameOrID);
            }, ctx);
        }

        [Command("PlayerPermissionsReport")]
        [Description("Displays the Player Permissions Report for the given player.")]
        [Aliases("PlayerPermissions", "DL-PlayerPermissions")]
        public async Task PlayerPermissionsReport(CommandContext ctx,
            [Description("Name or ID of the player for which to display the report.")] string playerNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.PlayerPermissionsReport(SharedCommands.CommandInterface.Discord, ctx, playerNameOrID);
            }, ctx);
        }

        [Command("PlayerAccessReport")]
        [Description("Displays the Player WhiteList/Ban/Mute Report for the given player.")]
        [Aliases("PlayerAccess", "DL-PlayerAccess", "PlayerWhiteListed", "PlayerBanned", "PlayerMuted")]
        public async Task PlayerAccessReport(CommandContext ctx,
            [Description("Name or ID of the player for which to display the report.")] string playerNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.PlayerAccessReport(SharedCommands.CommandInterface.Discord, ctx, playerNameOrID);
            }, ctx);
        }

        [Command("PlayerDiscordReport")]
        [Description("Displays the Discord Report for the given user.")]
        [Aliases("DiscordReport", "DL-PlayerDiscord")]
        public async Task DiscordReport(CommandContext ctx,
            [Description("Name or ID of the player or linked Discord user for which to display the report.")] string userNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                // Eco user
                User ecoUser = EcoUtils.UserByNameOrEcoID(userNameOrID);
                if (ecoUser != null)
                {
                    DiscordLinkEmbed ecoUserReport = MessageBuilder.Discord.GetPlayerReport(ecoUser, MessageBuilder.PlayerReportComponentFlag.DiscordInfo).Result;
                    await DisplayCommandData(ctx, string.Empty, ecoUserReport);
                    return;
                }

                // Discord member
                DiscordMember member = DiscordLink.Obj.Client.MemberByNameOrID(userNameOrID);
                if (ecoUser == null && member == null)
                {
                    await ReportCommandError(ctx, $"No Eco or Discord User with the name or ID {userNameOrID} could be found.");
                    return;
                }

                LinkedUser linkedUser = UserLinkManager.LinkedUserByDiscordUser(member, ctx.Member, "Player Discord Report Generation");
                if (linkedUser == null)
                    return;

                DiscordLinkEmbed linkedUserReport = MessageBuilder.Discord.GetPlayerReport(linkedUser.EcoUser, MessageBuilder.PlayerReportComponentFlag.DiscordInfo).Result;
                await DisplayCommandData(ctx, string.Empty, linkedUserReport);
            }, ctx);
        }

        [Command("PlayerReputationReport")]
        [Description("Displays the Player Reputation Report for the given player.")]
        [Aliases("PlayerReputation", "DL-PlayerReputation")]
        public async Task PlayerReputationReport(CommandContext ctx,
            [Description("Name or ID of the player for which to display the report.")] string playerNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.PlayerReputationReport(SharedCommands.CommandInterface.Discord, ctx, playerNameOrID);
            }, ctx);
        }

        [Command("PlayerXPReport")]
        [Description("Displays the Player XP Report for the given player.")]
        [Aliases("PlayerXP", "DL-PlayerXP")]
        public async Task PlayerXPReport(CommandContext ctx,
            [Description("Name or ID of the player for which to display the report.")] string playerNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.PlayerXPReport(SharedCommands.CommandInterface.Discord, ctx, playerNameOrID);
            }, ctx);
        }

        [Command("PlayerSkillsReport")]
        [Description("Displays the Player Skills Report for the given player.")]
        [Aliases("PlayerSkills", "DL-PlayerSkills")]
        public async Task PlayerSkillsReport(CommandContext ctx,
            [Description("Name or ID of the player for which to display the report.")] string playerNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.PlayerSkillsReport(SharedCommands.CommandInterface.Discord, ctx, playerNameOrID);
            }, ctx);
        }

        [Command("PlayerDemographicsReport")]
        [Description("Displays the Player Demographics Report for the given player.")]
        [Aliases("PlayerDemographics", "DL-PlayerDemographics")]
        public async Task PlayerDemographicsReport(CommandContext ctx,
            [Description("Name or ID of the player for which to display the report.")] string playerNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.PlayerDemographicsReport(SharedCommands.CommandInterface.Discord, ctx, playerNameOrID);
            }, ctx);
        }

        [Command("PlayerTitlesReport")]
        [Description("Displays the Player Titles Report for the given player.")]
        [Aliases("PlayerTitles", "DL-PlayerTitles")]
        public async Task PlayerTitlesReport(CommandContext ctx,
            [Description("Name or ID of the player for which to display the report.")] string playerNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.PlayerTitlesReport(SharedCommands.CommandInterface.Discord, ctx, playerNameOrID);
            }, ctx);
        }

        [Command("PlayerPropertyReport")]
        [Description("Displays the Player Property Report for the given player.")]
        [Aliases("PlayerProperty", "DL-PlayerProperty")]
        public async Task PlayerPropertyReport(CommandContext ctx,
            [Description("Name or ID of the player for which to display the report.")] string playerNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.PlayerPropertiesReport(SharedCommands.CommandInterface.Discord, ctx, playerNameOrID);
            }, ctx);
        }

        [Command("CurrencyReport")]
        [Description("Displays the Currency Report for the given currency.")]
        [Aliases("Currency", "DL-Currency")]
        public async Task CurrencyReport(CommandContext ctx,
            [Description("Name or ID of the currency for which to display a report.")] string currencyNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.CurrencyReport(SharedCommands.CommandInterface.Discord, ctx, currencyNameOrID);
            }, ctx);
        }

        [Command("CurrenciesReport")]
        [Description("Displays a report for the top used currencies.")]
        [Aliases("Currencies", "DL-Currencies")]
        public async Task CurrenciesReport(CommandContext ctx,
            [Description("The type of currencies to include in the report (all, minted or personal).")] string currencyType = "all",
            [Description("How many currencies per type to display reports for.")] string maxCurrenciesPerType = DLConstants.CURRENCY_REPORT_COMMAND_MAX_CURRENCIES_PER_TYPE_DEFAULT,
            [Description("How many top account holders per currency to include in the report.")] string holdersPerCurrency = DLConstants.CURRENCY_REPORT_COMMAND_MAX_TOP_HOLDERS_PER_CURRENCY_DEFAULT)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.CurrenciesReport(SharedCommands.CommandInterface.Discord, ctx, currencyType, maxCurrenciesPerType, holdersPerCurrency);
            }, ctx);
        }

        [Command("ElectionReport")]
        [Description("Displays the Election Report for the given election.")]
        [Aliases("Election", "DL-Election")]
        public async Task ElectionReport(CommandContext ctx,
            [Description("Name or ID of the election for which to display a report.")] string electionNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.ElectionReport(SharedCommands.CommandInterface.Discord, ctx, electionNameOrID);
            }, ctx);
        }

        [Command("ElectionsReport")]
        [Description("Displays a report for the currently active elections.")]
        [Aliases("Elections", "DL-Elections")]
        public async Task ElectionsReport(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.ElectionsReport(SharedCommands.CommandInterface.Discord, ctx);
            }, ctx);
        }

        [Command("WorkPartyReport")]
        [Description("Displays the Work Party Report for the given work party.")]
        [Aliases("WorkParty", "DL-WorkParty")]
        public async Task WorkPartyReport(CommandContext ctx,
            [Description("Name or ID of the work party for which to display a report.")] string workPartyNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.WorkPartyReport(SharedCommands.CommandInterface.Discord, ctx, workPartyNameOrID);
            }, ctx);
        }

        [Command("WorkPartiesReport")]
        [Description("Displays a report for the currently active work parties.")]
        [Aliases("WorkParties", "DL-WorkParties")]
        public async Task WorkPartiesReport(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.WorkPartiesReport(SharedCommands.CommandInterface.Discord, ctx);
            }, ctx);
        }

        #endregion

        #region Message Relaying

        [Command("ServerMessageToAll")]
        [Description("Sends an Eco server message to all online users.")]
        [Aliases("DL-ServerMessage")]
        public async Task ServerMessageToAll(CommandContext ctx, [Description("The message to send.")] string message)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendServerMessage(SharedCommands.CommandInterface.Discord, ctx, message, string.Empty);
            }, ctx);
        }

        [Command("ServerMessageToUser")]
        [Description("Sends an Eco server message to the specified user.")]
        [Aliases("DL-ServerMessageUser")]
        public async Task ServerMessageToUser(CommandContext ctx,
            [Description("The message to send.")] string message,
            [Description("Name or ID of the recipient Eco user.")] string recipientUserNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendServerMessage(SharedCommands.CommandInterface.Discord, ctx, message, recipientUserNameOrID);
            }, ctx);
        }

        [Command("AnnouncementToAll")]
        [Description("Sends an Eco info box message to all online users.")]
        [Aliases("DL-Announce")]
        public async Task AnnouncementToAll(CommandContext ctx, [Description("The message to send.")] string message)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Info, SharedCommands.CommandInterface.Discord, ctx, message, string.Empty);
            }, ctx);
        }

        [Command("AnnouncementToUser")]
        [Description("Sends an Eco info box message to the specified user.")]
        [Aliases("DL-AnnounceUser")]
        public async Task AnnounceToAll(CommandContext ctx, [Description("The message to send.")] string message,
            [Description("Name or ID of the recipient Eco user.")] string recipientUserNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Info, SharedCommands.CommandInterface.Discord, ctx, message, recipientUserNameOrID);
            }, ctx);
        }

        [Command("WarningToAll")]
        [Description("Sends an Eco warning box message to all online users.")]
        [Aliases("DL-Warning", "DL-Warn")]
        public async Task WarningToAll(CommandContext ctx, [Description("The message to send.")] string message)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Warning, SharedCommands.CommandInterface.Discord, ctx, message, string.Empty);
            }, ctx);
        }

        [Command("WarningToUser")]
        [Description("Sends an Eco warning box message to the specified user.")]
        [Aliases("DL-WarningUser", "DL-WarnUser")]
        public async Task WarningToUser(CommandContext ctx, [Description("The message to send.")] string message,
            [Description("Name or ID of the recipient Eco user.")] string recipientUserNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Warning, SharedCommands.CommandInterface.Discord, ctx, message, recipientUserNameOrID);
            }, ctx);
        }

        [Command("ErrorToAll")]
        [Description("Sends an Eco error box message to all online users.")]
        [Aliases("DL-Error")]
        public async Task ErrorToAll(CommandContext ctx, [Description("The message to send.")] string message)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Error, SharedCommands.CommandInterface.Discord, ctx, message, string.Empty);
            }, ctx);
        }

        [Command("ErrorToUser")]
        [Description("Sends an Eco error box message to the specified user.")]
        [Aliases("DL-ErrorUser")]
        public async Task ErrorToUser(CommandContext ctx, [Description("The message to send.")] string message,
            [Description("Name or ID of the recipient Eco user.")] string recipientUserNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Error, SharedCommands.CommandInterface.Discord, ctx, message, recipientUserNameOrID);
            }, ctx);
        }

        [Command("NotificationToAll")]
        [Description("Sends an Eco notification message to all online and conditionally offline users.")]
        [Aliases("DL-Notification", "DL-Notify")]
        public async Task NotificationToAll(CommandContext ctx, [Description("The message to send.")] string message,
            [Description("Whether or not to send the message to offline users as well.")] bool includeOfflineUsers = true)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendNotification(SharedCommands.CommandInterface.Discord, ctx, message, string.Empty, includeOfflineUsers);
            }, ctx);
        }

        [Command("NotificationToUser")]
        [Description("Sends an Eco notification message to the specified user.")]
        [Aliases("DL-NotificationUser", "DL-NotifyUser")]
        public async Task NotificationToUser(CommandContext ctx, [Description("The message to send.")] string message,
            [Description("Name or ID of the recipient Eco user.")] string recipientUserNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendNotification(SharedCommands.CommandInterface.Discord, ctx, message, recipientUserNameOrID, includeOfflineUsers: true);
            }, ctx);
        }

        [Command("PopupToAll")]
        [Description("Sends an Eco popup message to all online users.")]
        [Aliases("DL-Popup")]
        public async Task PopupToAll(CommandContext ctx, [Description("The message to send.")] string message)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendPopup(SharedCommands.CommandInterface.Discord, ctx, message, string.Empty);
            }, ctx);
        }

        [Command("PopupToUser")]
        [Description("Sends an Eco popup message to the specified user.")]
        [Aliases("DL-PopupUser")]
        public async Task PopupToUser(CommandContext ctx, [Description("The message to send.")] string message,
            [Description("Name or ID of the recipient Eco user.")] string recipientUserNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendPopup(SharedCommands.CommandInterface.Discord, ctx, message, recipientUserNameOrID);
            }, ctx);
        }

        [Command("InfoPanelToAll")]
        [Description("Displays an info panel to all online users.")]
        [Aliases("DL-InfoPanel")]
        public async Task InfoPanelToAll(CommandContext ctx, [Description("The title for the info panel.")] string title,
            [Description("The message to display in the info panel.")] string message)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendInfoPanel(SharedCommands.CommandInterface.Discord, ctx, DLConstants.ECO_PANEL_NOTIFICATION, title, message, string.Empty);
            }, ctx);
        }

        [Command("InfoPanelToUser")]
        [Description("Displays an info panel to the specified user.")]
        [Aliases("DL-InfoPanelUser")]
        public async Task InfoPanelToUser(CommandContext ctx, [Description("The title for the info panel.")] string title,
            [Description("The message to display in the info panel.")] string message,
            [Description("Name or ID of the recipient Eco user.")] string recipientUserNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendInfoPanel(SharedCommands.CommandInterface.Discord, ctx, DLConstants.ECO_PANEL_NOTIFICATION, title, message, recipientUserNameOrID);
            }, ctx);
        }

        #endregion

        #region Invites

        [Command("Invite")]
        [Description("Posts the Discord invite message to the target user. The invite will be broadcasted if no target user is specified.")]
        [Aliases("DL-Invite")]
        public async Task Invite(CommandContext ctx, [Description("The Eco username of the user receiving the invite")] string targetUserName)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.DiscordInvite(SharedCommands.CommandInterface.Discord, ctx, targetUserName);
            }, ctx);
        }

        [Command("BroadcastInvite")]
        [Description("Posts the Discord invite message to the Eco chat.")]
        [Aliases("DL-Broadcastinvite")]
        public async Task BroadcastInvite(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.DiscordInvite(SharedCommands.CommandInterface.Discord, ctx, string.Empty);
            }, ctx);
        }

        #endregion

        #region Trades

        [Command("Trades")]
        [Description("Displays available trades by player, tag, item or store")]
        [Aliases("DL-Trades", "DL-Trade", "Trade", "DLT")]
        public async Task Trades(CommandContext ctx, [Description("The player name or item name for which to display trades.")] string searchName = "")
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.Trades(SharedCommands.CommandInterface.Discord, ctx, searchName);
            }, ctx);
        }

        [Command("AddTradeWatcherDisplay")]
        [Description("Creates a live updated display of available trades by player, tag, item or store")]
        [Aliases("DL-WatchTradeDisplay")]
        public async Task AddTradeWatcherDisplay(CommandContext ctx, [Description("The player, tag, item or store name for which to display trades.")] string searchName = "")
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.AddTradeWatcher(SharedCommands.CommandInterface.Discord, ctx, searchName, Modules.ModuleType.Display);
            }, ctx);
        }

        [Command("RemoveTradeWatcherDisplay")]
        [Description("Removes the live updated display of available trades for a player, tag, item or store.")]
        [Aliases("DL-UnwatchTradeDisplay")]
        public async Task RemoveTradeWatcherDisplay(CommandContext ctx, [Description("The player, tag, item or store name for which to display trades.")] string searchName = "")
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.RemoveTradeWatcher(SharedCommands.CommandInterface.Discord, ctx, searchName, Modules.ModuleType.Display);
            }, ctx);
        }

        [Command("AddTradeWatcherFeed")]
        [Description("Creates a feed where the bot will post trades filtered by the search query, as they occur ingame. The search query can filter by player, tag, item or store.")]
        [Aliases("DL-WatchTradeFeed")]
        public async Task AddTradeWatcherFeed(CommandContext ctx, [Description("The player, tag item or store name for which to post trades.")] string searchName = "")
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.AddTradeWatcher(SharedCommands.CommandInterface.Discord, ctx, searchName, Modules.ModuleType.Feed);
            }, ctx);
        }

        [Command("RemoveTradeWatcherFeed")]
        [Description("Removes the trade watcher feed for a player, tag, item or store.")]
        [Aliases("DL-UnwatchTradeFeed")]
        public async Task RemoveTradeWatcherFeed(CommandContext ctx, [Description("The player, tag item or store name for which to post trades.")] string searchName = "")
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.RemoveTradeWatcher(SharedCommands.CommandInterface.Discord, ctx, searchName, Modules.ModuleType.Feed);
            }, ctx);
        }

        [Command("ListTradeWatchers")]
        [Description("Lists all trade watchers for the calling user.")]
        [Aliases("DL-TradeWatchers")]
        public async Task ListTradeWatchers(CommandContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.ListTradeWatchers(SharedCommands.CommandInterface.Discord, ctx);
            }, ctx);
        }

        #endregion

        #region Snippets

        [Command("DiscordSnippet")]
        [Description("Post a predefined snippet.")]
        [Aliases("DL-DiscordSnippet", "DL-SnippetToDiscord", "DL-Snippet")]
        public async Task DiscordSnippet(CommandContext ctx, string snippetKey = "")
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.Snippet(SharedCommands.CommandInterface.Discord, ctx, SharedCommands.CommandInterface.Discord, ctx.GetSenderName(), snippetKey);
            }, ctx);
        }

        [Command("EcoSnippet")]
        [Description("Post a predefined snippet.")]
        [Aliases("DL-EcoSnippet", "DL-SnippetToEco")]
        public async Task EcoSnippet(CommandContext ctx, string snippetKey = "")
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.Snippet(SharedCommands.CommandInterface.Discord, ctx, SharedCommands.CommandInterface.Eco, ctx.GetSenderName(), snippetKey);
            }, ctx);
        }

        #endregion
    }
}
