using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using Eco.Core.Utils;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Messaging.Chat;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Lookups;
using Eco.Moose.Utils.Message;
using Eco.Moose.Utils.TextUtils;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Plugins.Networking;
using Eco.Shared.IoC;
using Eco.Shared.Utils;
using Eco.Simulation.WorldLayers.Layers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Eco.Plugins.DiscordLink.DiscordCommands;
using static Eco.Plugins.DiscordLink.Utilities.MessageBuilder;

namespace Eco.Plugins.DiscordLink
{
    public class DiscordCommandContext : CommandContext
    {
        public DiscordCommandContext(InteractionContext interaction, ResponseTiming timing)
        {
            base.Interface = ApplicationInterfaceType.Discord;
            Interaction = interaction;
            Timing = timing;
        }

        public InteractionContext Interaction { get; private set; }
        public ResponseTiming Timing { get; private set; }
    }

    public class DiscordCommands : ApplicationCommandModule
    {
        #region Commands Base

        public enum PermissionType
        {
            User,
            Admin
        }

        public enum ResponseTiming
        {
            Immediate,
            Delayed
        }

        public delegate Task DiscordCommand(DiscordCommandContext ctx, params string[] parameters);

        private static async Task ExecuteCommand<TRet>(PermissionType requiredPermission, DiscordCommandContext ctx, DiscordCommand command)
        {
            try
            {
                if (ctx.Timing == ResponseTiming.Delayed)
                {
                    await ctx.Interaction.DeferAsync();
                }

                if (!IsCommandAllowedForUser(ctx.Interaction, requiredPermission))
                {
                    string permittedRolesDesc = (DLConfig.Data.AdminRoles.Count > 0) ? string.Join("\n- ", DLConfig.Data.AdminRoles.ToArray()) : "No admin roles configured";
                    await RespondToCommand(ctx, $"You lack the `{requiredPermission}` level permission required to execute this command.\nThe permitted roles are:\n```- {permittedRolesDesc}```");
                    return;
                }

                if (ctx.Interaction.Channel.IsPrivate)
                    Logger.Debug($"{ctx.Interaction.User.Username} invoked Discord command \"/{command.Method.Name}\" in DM");
                else
                    Logger.Debug($"{ctx.Interaction.User.Username} invoked Discord command \"/{command.Method.Name}\" in channel {ctx.Interaction.Channel.Name}");

                await command(ctx);
            }
            catch (Exception e)
            {
                Logger.Exception($"An error occurred while attempting to execute a Discord command", e);
                await RespondToCommand(ctx, $"An error occurred while attempting to run that command. Error message: {e}");
            }
        }

        private static async Task RespondToCommand(DiscordCommandContext ctx, string fullTextContent, DiscordLinkEmbed embedContent) => await RespondToCommand(ctx, fullTextContent, embedContent.SingleItemAsEnumerable());

        private static async Task RespondToCommand(DiscordCommandContext ctx, string fullTextContent, IEnumerable<DiscordLinkEmbed> embedContent = null)
        {
            async static Task Respond(DiscordCommandContext ctx, string textContent, IEnumerable<DiscordLinkEmbed> embedContent)
            {
                string bulderText = string.Empty;
                if (!string.IsNullOrWhiteSpace(textContent))
                {
                    if (textContent.Length < DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT)
                        bulderText = textContent;
                    else
                        bulderText = $"{textContent.Substring(0, DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT - 4)}...";
                }

                List<DiscordEmbed> builderEmbeds = new List<DiscordEmbed>();
                if (embedContent != null)
                {
                    foreach (DiscordLinkEmbed embed in embedContent)
                    {
                        builderEmbeds = MessageUtils.BuildDiscordEmbeds(embed);
                    }
                }

                // Send initial response
                if (ctx.Timing == ResponseTiming.Immediate)
                {
                    DiscordInteractionResponseBuilder builder = new DiscordInteractionResponseBuilder();
                    builder.Content = bulderText;
                    if (builderEmbeds.Count > 0)
                        builder.AddEmbed(builderEmbeds.First());

                    await ctx.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                }
                else if (ctx.Timing == ResponseTiming.Delayed)
                {
                    DiscordWebhookBuilder builder = new DiscordWebhookBuilder();
                    builder.Content = bulderText;
                    if (builderEmbeds.Count > 0)
                        builder.AddEmbed(builderEmbeds.First());

                    await ctx.Interaction.EditResponseAsync(builder);
                }

                // Send any remaining embeds as follow up messages
                for (int i = 1; i < builderEmbeds.Count; ++i)
                {
                    DiscordFollowupMessageBuilder builder = new DiscordFollowupMessageBuilder();
                    builder.AddEmbed(builderEmbeds[i]);

                    await ctx.Interaction.FollowUpAsync(builder);
                }
            }

            string errorMessage = string.Empty;
            try
            {
                DiscordClient client = DiscordLink.Obj.Client;
                if (!client.ChannelHasPermission(ctx.Interaction.Channel, Permissions.SendMessages) || !client.ChannelHasPermission(ctx.Interaction.Channel, Permissions.ReadMessageHistory))
                {
                    Logger.Error($"Failed to respond to command \"{ctx.Interaction.CommandName}\" in channel \"{ctx.Interaction.Channel}\" as the bot lacks permissions for sending and/or reading messages in this channel.");
                    return;
                }

                if (embedContent == null)
                {
                    await Respond(ctx, fullTextContent, null);
                }
                else
                {
                    // Either make sure we have permission to use embeds or convert the embed to text
                    if (client.ChannelHasPermission(ctx.Interaction.Channel, Permissions.EmbedLinks))
                    {
                        await Respond(ctx, fullTextContent, embedContent);
                    }
                    else
                    {
                        await Respond(ctx, $"{fullTextContent}\n{string.Join("\n\n", embedContent.Select(embed => embed.AsDiscordText()))}", null);
                    }
                }
            }
            catch (NotFoundException e)
            {
                errorMessage = $"An error occurred while attempting to respond to command\\nException {e}\nMessage: {e.JsonMessage}";
            }
            catch (BadRequestException e)
            {
                errorMessage = $"An error occurred while attempting to respond to command\nException {e}\nRequest Error: {e.Errors}";
            }
            catch (Exception e)
            {
                errorMessage = $"An error occurred while attempting to respond to command\nException: {e}";
            }

            if (!errorMessage.IsEmpty())
            {
                Logger.Error(errorMessage);
                try
                {
                    await Respond(ctx, errorMessage, null);
                }
                catch { } // If we fail, it's probably for the same reason as above, so let's not spam the log
            }
        }

        private static bool IsCommandAllowedForUser(InteractionContext ctx, PermissionType requiredPermission)
        {
            return requiredPermission switch
            {
                PermissionType.User => true,
                PermissionType.Admin => DiscordLink.Obj.Client.MemberIsAdmin(ctx.Member),
                _ => false,
            };
        }

        #endregion

        #region User Feedback

        public static async Task ReportCommandError(DiscordCommandContext ctx, string message)
        {
            await RespondToCommand(ctx, message);
        }

        public static async Task ReportCommandInfo(DiscordCommandContext ctx, string message)
        {
            await RespondToCommand(ctx, message);
        }

        public static async Task DisplayCommandData(DiscordCommandContext ctx, string title, DiscordLinkEmbed embed) => await DisplayCommandData(ctx, title, embed.SingleItemAsEnumerable());

        public static async Task DisplayCommandData(DiscordCommandContext ctx, string title, IEnumerable<DiscordLinkEmbed> embeds)
        {
            await RespondToCommand(ctx, title, embeds);
        }

        public static async Task DisplayCommandData(DiscordCommandContext ctx, string title, string content)
        {
            await RespondToCommand(ctx, $"**{title}**\n```{content}```");
        }

        #endregion

        #region Eco Commands

        [SlashCommand("EcoCommand", "Executes an ingame command.")]
        public async Task EcoCommand(InteractionContext interaction,
            [Option("Command", "The Eco command to run.")] string command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                RemoteEcoCommandClient Client = new RemoteEcoCommandClient(ctx);
                await ServiceHolder<IChatManager>.Obj.ExecuteCommandAsync(Client, command);
            });
        }

        #endregion

        #region Plugin Management

        [SlashCommand("Update", "Forces an update of most internal systems.")]
        public async Task Update(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.Update(ctx);
            });
        }

        [SlashCommand("RestartPlugin", "Restarts the DiscordLink plugin.")]
        public async Task RestartPlugin(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.RestartPlugin(ctx);
            });
        }

        [SlashCommand("ReloadConfig", "Reloads the DiscordLink config.")]
        public async Task ReloadConfig(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ReloadConfig(ctx);
            });
        }

        [SlashCommand("ResetPersistentData", "Removes all persistent storage data.")]
        public async Task ResetPersistentData(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ResetPersistentData(ctx);
            });
        }

        [SlashCommand("ResetWorldData", "Resets world data as if a new world had been created.")]
        public async Task ResetWorldData(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ResetWorldData(ctx);
            });
        }

        [SlashCommand("PersistentStorageData", "Displays a description of the persistent storage data.")]
        public async Task PersistentStorageData(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.PersistentStorageData(ctx);
            });
        }

        [SlashCommand("WorldStorageData", "Displays a description of the world storage data.")]
        public async Task WorldStorageData(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.WorldStorageData(ctx);
            });
        }

        [SlashCommand("ClearRoles", "Deletes all Discord roles created and tracked by DiscordLink.")]
        public async Task ClearRoles(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ClearRoles(ctx);
            });
        }

        #endregion

        #region Server Management

        [SlashCommand("ServerShutdown", "Shuts down the Eco server.")]
        public async Task ServerShutdown(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ServerShutdown(ctx);
            });
        }

        #endregion

        #region Meta

        [SlashCommand("Version", "Displays the installed and latest available plugin version.")]
        public async Task Version(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle("Version")
                    .WithDescription(TextUtils.StripTags(MessageBuilder.Shared.GetVersionMessage()));

                await RespondToCommand(ctx, null, embed);
            });
        }

        [SlashCommand("About", "Displays information about the DiscordLink plugin.")]
        public async Task About(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle("About DiscordLink")
                    .WithDescription(MessageBuilder.Shared.GetAboutMessage());

                await RespondToCommand(ctx, null, embed);
            });
        }

        [SlashCommand("Documentation", "Opens the documentation web page.")]
        public async Task Documentation(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, "The documentation can be found here: <https://github.com/Eco-DiscordLink/EcoDiscordPlugin>");
            });
        }

        [SlashCommand("PluginStatus", "Displays the current plugin status.")]
        public async Task PluginStatus(InteractionContext interaction,
            [Option("Verbose", "Use verbose output with extra information.")] bool verbose = false)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, await MessageBuilder.Shared.GetDisplayStringAsync(verbose));
            });
        }

        [SlashCommand("VerifyConfig", "Checks configuration setup and reports any errors.")]
        public async Task VerifyConfig(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.VerifyConfig(ctx);
            });
        }

        [SlashCommand("VerifyPermissions", "Checks all permissions and intents needed and reports any missing ones.")]
        public async Task VerifyPermissions(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.VerifyPermissions(ctx, MessageBuilder.PermissionReportComponentFlag.All);
            });
        }

        [SlashCommand("VerifyIntents", "Checks all intents needed and reports any missing ones.")]
        public async Task CheckIntents(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.VerifyPermissions(ctx, MessageBuilder.PermissionReportComponentFlag.Intents);
            });
        }

        [SlashCommand("VerifyServerPermissions", "Checks all server permissions needed and reports any missing ones.")]
        public async Task VerifyServerPermissions(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.VerifyPermissions(ctx, MessageBuilder.PermissionReportComponentFlag.ServerPermissions);
            });
        }

        [SlashCommand("VerifyChannelPermissions", "Checks all permissions needed for the given channel and reports any missing ones.")]
        public async Task CheckChannelPermissions(InteractionContext interaction, [Option("Channel", "Name or ID of the channel to check permissions for. Defaults to the current channel.")] string channelNameOrId = "")
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                if (string.IsNullOrWhiteSpace(channelNameOrId))
                    await SharedCommands.VerifyPermissionsForChannel(ctx, ctx.Interaction.Channel);
                else
                    await SharedCommands.VerifyPermissionsForChannel(ctx, channelNameOrId);
            });
        }

        [SlashCommand("ListLinkedChannels", "Presents a list of all channel links.")]
        public async Task ListLinkedChannels(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ListChannelLinks(ctx);
            });
        }

        [SlashCommand("Echo", "Sends a message to Eco and back to Discord again if a chat link is configured for the channel.")]
        public async Task Echo(InteractionContext interaction,
            [Option("Message", "The message to send. Defaults to a random message.")] string message = "",
            [Option("EcoChannel", "The eco channel you want to test.")] string ecoChannel = "")
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
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

                List<string> targetEcoChannelNames = new List<string>();
                if (!string.IsNullOrWhiteSpace(ecoChannel))
                {
                    Message.SendChatToChannel(null, ecoChannel, $"{DLConstants.ECHO_COMMAND_TOKEN} {message}");
                    targetEcoChannelNames.Add(ecoChannel);
                }
                else
                {
                    bool linkFound = false;
                    foreach (ChatChannelLink chatLink in DLConfig.ChatLinksForDiscordChannel(ctx.Interaction.Channel))
                    {
                        Message.SendChatToChannel(null, chatLink.EcoChannel, $"{DLConstants.ECHO_COMMAND_TOKEN} {message}");
                        targetEcoChannelNames.Add(chatLink.EcoChannel);
                        linkFound = true;
                    }

                    if (!linkFound)
                    {
                        Message.SendChatToChannel(null, DLConstants.DEFAULT_CHAT_CHANNEL, $"{DLConstants.ECHO_COMMAND_TOKEN} {message}");
                        targetEcoChannelNames.Add(DLConstants.DEFAULT_CHAT_CHANNEL);
                    }
                }

                await RespondToCommand(ctx, $"Message sent to the following Eco channel(s): {string.Join(',', targetEcoChannelNames)}");
            });
        }

        #endregion

        #region Account Linking

        [SlashCommand("LinkInformation", "Presents information about account linking.")]
        public async Task LinkInformation(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle("Eco --> Discord Account Linking")
                    .WithDescription(MessageBuilder.Shared.GetLinkAccountInfoMessage());

                await RespondToCommand(ctx, null, embed);
            });
        }

        [SlashCommand("UnlinkAccount", "Unlinks the Discord account from a linked Eco account.")]
        public async Task UnlinkAccount(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                bool result = UserLinkManager.RemoveLinkedUser(interaction.Member);
                if (result)
                    await ReportCommandInfo(ctx, $"Eco account unlinked.");
                else
                    await ReportCommandError(ctx, $"No linked Eco account could be found.");
            });
        }

        #endregion

        #region Lookups

        [SlashCommand("ServerStatus", "Displays the Server Info status.")]
        public async Task ServerStatus(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await DisplayCommandData(ctx, string.Empty, MessageBuilder.Discord.GetServerInfo(MessageBuilder.ServerInfoComponentFlag.All));
            });
        }

        [SlashCommand("PlayerList", "Lists the players currently online on the server.")]
        public async Task PlayerList(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle("Players")
                    .WithDescription(MessageBuilder.Shared.GetOnlinePlayerList());
                await DisplayCommandData(ctx, string.Empty, embed);
            });
        }

        [SlashCommand("PlayerReport", "Displays the Player Report for the given player.")]
        public async Task PlayerReport(InteractionContext interaction,
            [Option("Player", "Name or ID of the player for which to display the report.")] string playerNameOrId,
            [Option("Report", "Which type of information the report should include.")] PlayerReportComponentFlag reportType = PlayerReportComponentFlag.All)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.PlayerReport(ctx, playerNameOrId, reportType);
            });
        }

        [SlashCommand("CurrencyReport", "Displays the Currency Report for the given currency.")]
        public async Task CurrencyReport(InteractionContext interaction,
            [Option("Currency", "Name or ID of the currency for which to display a report.")] string currencyNameOrId,
            [Option("TopHoldersCount", "How many top account holders to include in the report")] long maxTopHoldersCount = DLConfig.DefaultValues.MaxTopCurrencyHolderCount,
            [Option("ShowTradeCount", "Should the total trade count for the currency be displayed in the report?")] bool useTradeCount = true,
            [Option("ShowBacking", "Should information about the currency backing be displayed in the report?")] bool useBackingInfo = false)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.CurrencyReport(ctx, currencyNameOrId, (int)maxTopHoldersCount, useBackingInfo, useTradeCount);
            });
        }

        [SlashCommand("CurrenciesReport", "Displays a report for the top used currencies.")]
        public async Task CurrenciesReport(InteractionContext interaction,
            [Option("Type", "The type of currencies to include in the report.")] CurrencyType currencyType = CurrencyType.All,
            [Option("MaxPerType", "How many currencies per type to display reports for.")] long maxCurrenciesPerType = DLConstants.CURRENCY_REPORT_COMMAND_MAX_CURRENCIES_PER_TYPE_DEFAULT,
            [Option("HolderCount", "How many top account holders per currency to include in the report.")] long holdersPerCurrency = DLConstants.CURRENCY_REPORT_COMMAND_MAX_TOP_HOLDERS_PER_CURRENCY_DEFAULT)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.CurrenciesReport(ctx, currencyType, (int)maxCurrenciesPerType, (int)holdersPerCurrency);
            });
        }

        [SlashCommand("ElectionReport", "Displays the Election Report for the given election.")]
        public async Task ElectionReport(InteractionContext interaction,
            [Option("Election", "Name or ID of the election for which to display a report.")] string electionNameOrId)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ElectionReport(ctx, electionNameOrId);
            });
        }

        [SlashCommand("ElectionsReport", "Displays a report for the currently active elections.")]
        public async Task ElectionsReport(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ElectionsReport(ctx);
            });
        }

        [SlashCommand("WorkPartyReport", "Displays the Work Party Report for the given work party.")]
        public async Task WorkPartyReport(InteractionContext interaction,
            [Option("WorkParty", "Name or ID of the work party for which to display a report.")] string workPartyNameOrI)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.WorkPartyReport(ctx, workPartyNameOrI);
            });
        }

        [SlashCommand("WorkPartiesReport", "Displays a report for the currently active work parties.")]
        public async Task WorkPartiesReport(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.WorkPartiesReport(ctx);
            });
        }

        #endregion

        #region Images

        [SlashCommand("ShowLayer", "Posts a link to the requested layer image.")]
        public async Task ShowLayer(InteractionContext interaction,
            [Option("LayerName", "Name of the world layer to show. The layer must must be a visible layer.")] string layerName,
            [Option("ShowLayerHistory", "If true; will post an animated gif showing how the history of the layer has changed per hour.")] bool showLayerHistory = false,
            [Option("ShowTerrainComparison", "If true; will post a comparison gif showing the world terrain.")] bool showComparsionTerrain = false)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                string webServerUrl = NetworkManager.Config.WebServerUrl;
                if (webServerUrl.IsEmpty())
                {
                    await ReportCommandError(ctx, "Web server URL not configured - Ensure that network config parameter `WebServerUrl` is set.");
                    return;
                }

                IEnumerable<WorldLayer> layers = interaction.Member.IsAdmin() ? Lookups.Layers : Lookups.VisibleLayers;
                WorldLayer layer = layers.FirstOrDefault(layer => layer.Name.EqualsCaseInsensitive(layerName));
                if (layer == null)
                {
                    layer = Lookups.Layers.FirstOrDefault(layer => layer.Name.EqualsCaseInsensitive(layerName));
                    if (layer != null)
                        await ReportCommandError(ctx, $"{layer.Name} is not a visible layer.");
                    else
                        await ReportCommandError(ctx, $"No layer named \"{layerName}\" could be found.");
                    return;
                }

                string layerFileName = showLayerHistory ? layer.Name : $"{layer.Name}Latest";
                string terrainFileName = showLayerHistory ? "Terrain" : "TerrainLatest";
                string output = showComparsionTerrain
                ? $"{LayerUtils.GetLayerLink(layerFileName)}\n{LayerUtils.GetLayerLink(terrainFileName)}"
                : $"{LayerUtils.GetLayerLink(layerFileName)}";
                await ReportCommandInfo(ctx, output);
            });
        }

        [SlashCommand("ShowMap", "Posts a link to an image showing the world map.")]
        public async Task ShowMap(InteractionContext interaction,
            [Option("MapType", "The representation of the world map image.")] MapRepresentationType mapType = MapRepresentationType.Preview)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                string webServerUrl = NetworkManager.Config.WebServerUrl;
                if (webServerUrl.IsEmpty())
                {
                    await ReportCommandError(ctx, "Web server URL not configured - Ensure that network config parameter `WebServerUrl` is set.");
                    return;
                }

                string layerFileName = LayerUtils.GetLayerName(mapType);
                if (layerFileName.IsEmpty())
                {
                    await ReportCommandError(ctx, "Failed to resolve mapType parameter");
                    return;
                }
                    
                await ReportCommandInfo(ctx, $"{LayerUtils.GetLayerLink(layerFileName)}");
            });
        }

        [SlashCommand("ShowWorldHistory", "Posts a link to a gif showing the world history.")]
        public async Task ShowWorldHistory(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                string webServerUrl = NetworkManager.Config.WebServerUrl;
                if (webServerUrl.IsEmpty())
                {
                    await ReportCommandError(ctx, "Web server URL not configured - Ensure that network config parameter `WebServerUrl` is set.");
                    return;
                }

                string layerFileName = "Terrain";
                await ReportCommandInfo(ctx, $"{LayerUtils.GetLayerLink(layerFileName)}");
            });
        }

        #endregion

        #region Invites

        [SlashCommand("PostInviteMessage", "Posts a Discord invite message to the Eco chat.")]
        public async Task PostInviteMessage(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.PostInviteMessage(ctx);
            });
        }

        #endregion

        #region Trades

        [SlashCommand("Trades", "Displays available trades by player, tag, item or store.")]
        public async Task Trades(InteractionContext interaction,
            [Option("SearchName", "The player name or item name for which to display trades. Case insensitive and auto completed.")] string searchName)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.Trades(ctx, searchName);
            });
        }

        [SlashCommand("DLT", "Shorthand for the Trades command.")]
        public async Task DLT(InteractionContext interaction,
            [Option("SearchName", "The player name or item name for which to display trades. Case insensitive and auto completed.")] string searchName)
        {
            await Trades(interaction, searchName);
        }

        [SlashCommand("AddTradeWatcherDisplay", "Creates a live updated display of available trades by player, tag, item or store.")]
        public async Task AddTradeWatcherDisplay(InteractionContext interaction,
            [Option("SearchName", "The player name or item name for which to display trades.")] string searchName)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.AddTradeWatcher(ctx, searchName, Modules.ModuleArchetype.Display);
            });
        }

        [SlashCommand("RemoveTradeWatcherDisplay", "Removes the live updated display of available trades for a player, tag, item or store.")]
        public async Task RemoveTradeWatcherDisplay(InteractionContext interaction,
            [Option("SearchName", "The player, tag, item or store name for which to display trades.")] string searchName)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.RemoveTradeWatcher(ctx, searchName, Modules.ModuleArchetype.Display);
            });
        }

        [SlashCommand("AddTradeWatcherFeed", "Creates a trade feed filtered by a search query.")]
        public async Task AddTradeWatcherFeed(InteractionContext interaction,
            [Option("SearchName", "The player, tag, item or store name for which to post trades.")] string searchName)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.AddTradeWatcher(ctx, searchName, Modules.ModuleArchetype.Feed);
            });
        }

        [SlashCommand("RemoveTradeWatcherFeed", "Removes the trade watcher feed for a player, tag, item or store.")]
        public async Task RemoveTradeWatcherFeed(InteractionContext interaction,
            [Option("SearchName", "The player, tag item or store name for which to remove trades.")] string searchName)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.RemoveTradeWatcher(ctx, searchName, Modules.ModuleArchetype.Feed);
            });
        }

        [SlashCommand("ListTradeWatchers", "Lists all trade watchers for the calling user.")]
        public async Task ListTradeWatchers(InteractionContext interaction)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ListTradeWatchers(ctx);
            });
        }

        #endregion

        #region Snippets

        [SlashCommand("Snippet", "Posts a predefined snippet to Eco or Discord.")]
        public async Task Snippet(InteractionContext interaction,
            [Option("Key", "Key of the snippet to post. Displays the key list if omitted.")] string snippetKey = "")
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.Snippet(ctx, ApplicationInterfaceType.Discord, ctx.Interaction.GetSenderName(), snippetKey);
            });
        }

        [SlashCommand("EcoSnippet", "Posts a predefined snippet to Eco.")]
        public async Task EcoSnippet(InteractionContext interaction,
            [Option("Key", "Key of the snippet to post. Displays the key list if omitted.")] string snippetKey = "")
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.Snippet(ctx, ApplicationInterfaceType.Eco, ctx.Interaction.GetSenderName(), snippetKey);
            });
        }

        #endregion

        #region Message Relaying

        [SlashCommand("Announce", "Announces a message to everyone or a specified user.")]
        public async Task Announce(InteractionContext interaction,
            [Option("Message", "The message to send.")] string message,
            [Option("MessageType", "The type of message to send.")] Moose.Data.Enums.MessageType messageType = Moose.Data.Enums.MessageType.Notification,
            [Option("Player", "Name or ID of the player to send the message to. Sends to everyone if omitted.")] string recipientUserNameOrId = "")
        {
            DiscordCommandContext ctx = new DiscordCommandContext(interaction, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                if (message.IsEmpty())
                {
                    await ReportCommandError(ctx, $"Failed to send message - Message can not be empty");
                    return;
                }

                User recipient = null;
                if (!recipientUserNameOrId.IsEmpty())
                {
                    recipient = Lookups.Users.FirstOrDefault(user => user.Name.EqualsCaseInsensitive(recipientUserNameOrId) || user.Id.ToString().EqualsCaseInsensitive(recipientUserNameOrId));
                    if (recipient == null)
                    {
                        await ReportCommandError(ctx, $"No player with the name or ID \"{recipientUserNameOrId}\" could be found.");
                        return;
                    }
                }


                if (recipient != null && messageType != Moose.Data.Enums.MessageType.NotificationOffline && !recipient.IsOnline)
                {
                    await ReportCommandError(ctx, $"Failed to send message - {recipient.Name} is offline.");
                    return;
                }

                string formattedMessage = messageType switch
                {
                    Moose.Data.Enums.MessageType.Chat => $"{ctx.Interaction.Member.DisplayName}: {message}",
                    Moose.Data.Enums.MessageType.Info => $"{ctx.Interaction.Member.DisplayName}: {message}",
                    Moose.Data.Enums.MessageType.Warning => $"{ctx.Interaction.Member.DisplayName}: {message}",
                    Moose.Data.Enums.MessageType.Error => $"{ctx.Interaction.Member.DisplayName}: {message}",
                    Moose.Data.Enums.MessageType.Notification => $"[{ctx.Interaction.Member.DisplayName}]\n\n{message}",
                    Moose.Data.Enums.MessageType.NotificationOffline => $"[{ctx.Interaction.Member.DisplayName}]\n\n{message}",
                    Moose.Data.Enums.MessageType.Popup => $"[{ctx.Interaction.Member.DisplayName}]\n{message}",
                };

                bool result = true;
                switch (messageType)
                {
                    case Moose.Data.Enums.MessageType.Chat:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendChatToUser(null, recipient, formattedMessage);
                            }
                            else
                            {
                                result = Message.SendChatToDefaultChannel(null, formattedMessage);
                            }
                            break;
                        }

                    case Moose.Data.Enums.MessageType.Info:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendInfoBoxToUser(recipient, formattedMessage);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendInfoBoxToUser(onlineUser, formattedMessage) && result;
                                }
                            }
                            break;
                        }

                    case Moose.Data.Enums.MessageType.Warning:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendWarningBoxToUser(recipient, formattedMessage);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendWarningBoxToUser(onlineUser, formattedMessage) && result;
                                }
                            }
                            break;
                        }
                    case Moose.Data.Enums.MessageType.Error:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendErrorBoxToUser(recipient, formattedMessage);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendErrorBoxToUser(onlineUser, formattedMessage) && result;
                                }
                            }
                            break;
                        }
                    case Moose.Data.Enums.MessageType.Popup:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendPopupToUser(recipient, formattedMessage);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendPopupToUser(onlineUser, formattedMessage) && result;
                                }
                            }
                            break;
                        }
                    case Moose.Data.Enums.MessageType.Notification:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendNotificationToUser(recipient, message, sendOffline: false);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendNotificationToUser(onlineUser, formattedMessage, sendOffline: false) && result;
                                }
                            }
                            break;
                        }

                    case Moose.Data.Enums.MessageType.NotificationOffline:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendNotificationToUser(recipient, message, sendOffline: true);
                            }
                            else
                            {
                                foreach (User user in UserManager.Users)
                                {
                                    result = Message.SendNotificationToUser(user, formattedMessage, sendOffline: true) && result;
                                }
                            }
                            break;
                        }
                }

                string sendContext = recipient == null ? "all players" : recipient.Name;
                if (result)
                    await ReportCommandInfo(ctx, $"Message delivered to {sendContext}.");
                else
                    await ReportCommandError(ctx, $"Failed to send message to {sendContext}.");
            });
        }

        #endregion
    }
}