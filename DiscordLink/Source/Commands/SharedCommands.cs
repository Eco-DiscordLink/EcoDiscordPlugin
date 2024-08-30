using DSharpPlus.Entities;
using Eco.Core;
using Eco.Core.Plugins;
using Eco.Core.Utils;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.Economy;
using Eco.Gameplay.Economy.WorkParties;
using Eco.Gameplay.Players;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Lookups;
using Eco.Moose.Utils.Message;
using Eco.Moose.Utils.Plugin;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Plugins.Networking;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Eco.Moose.Features.Trade;
using static Eco.Plugins.DiscordLink.Enums;
using static Eco.Plugins.DiscordLink.Utilities.MessageBuilder;
using StoreOfferList = System.Collections.Generic.IEnumerable<System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.Store.StoreComponent, Eco.Gameplay.Components.TradeOffer>>>;

namespace Eco.Plugins.DiscordLink
{
    public class CommandContext { }

    /**
     * Platform independent implementations of commands used in both Discord and Eco.
     */
    public static class SharedCommands
    {
        #region Commands Base

        private static async Task ReportCommandError(ApplicationInterfaceType source, CommandContext ctx, string message)
        {
            if (source == ApplicationInterfaceType.Eco)
                EcoCommands.ReportCommandError(ctx as EcoCommandContext, message);
            else
                await DiscordCommands.ReportCommandError(ctx as DiscordCommandContext, message);
        }

        private static async Task ReportCommandInfo(ApplicationInterfaceType source, CommandContext ctx, string message)
        {
            if (source == ApplicationInterfaceType.Eco)
                EcoCommands.ReportCommandInfo(ctx as EcoCommandContext, message);
            else
                await DiscordCommands.ReportCommandInfo(ctx as DiscordCommandContext, message);
        }

        private static async Task DisplayCommandData(ApplicationInterfaceType source, CommandContext commandContext, string title, object data, string panelInstance = "")
        {
            if (source == ApplicationInterfaceType.Eco)
            {
                EcoCommandContext EcoCtx = commandContext as EcoCommandContext;
                if (data is string message)
                {
                    EcoCommands.DisplayCommandData(EcoCtx, panelInstance, MessageUtils.FormatMessageForEco(title), MessageUtils.FormatMessageForEco(message));
                }
                else if (data is DiscordLinkEmbed embed)
                {
                    string titleToUse = string.Empty;
                    if (!title.IsEmpty())
                        titleToUse = title;
                    else if (!embed.Title.IsEmpty())
                        titleToUse = embed.Title;
                    else if (!embed.Fields.First().Title.IsEmpty())
                        titleToUse = embed.Fields.First().Title;
                    EcoCommands.DisplayCommandData(EcoCtx, panelInstance, MessageUtils.FormatMessageForEco(titleToUse), embed.AsEcoText());
                }
            }
            else if (source == ApplicationInterfaceType.Discord)
            {
                DiscordCommandContext discordCtx = commandContext as DiscordCommandContext;
                if (data is DiscordLinkEmbed embed)
                    await DiscordCommands.DisplayCommandData(discordCtx, title, embed);
                else if (data is IEnumerable<DiscordLinkEmbed> embeds)
                    await DiscordCommands.DisplayCommandData(discordCtx, title, embeds);
                else if (data is string content)
                    await DiscordCommands.DisplayCommandData(discordCtx, title, content);
                else
                    Logger.Error($"Attempted to display command data of unhandled type \"{data.GetType()}\"");
            }
        }

        #endregion

        #region Plugin Management

        public static async Task<bool> Update(ApplicationInterfaceType source, CommandContext ctx)
        {
            if (DiscordLink.Obj.Client.ConnectionStatus != DiscordClient.ConnectionState.Connected)
            {
                await ReportCommandError(source, ctx, "Failed to force update - Discord client not connected");
                return false;
            }

            DiscordLink plugin = DiscordLink.Obj;
            plugin.Modules.ForEach(async module => await module.HandleStartOrStop());
            await plugin.HandleEvent(Events.DlEventType.ForceUpdate);
            await ReportCommandInfo(source, ctx, "Forced update");
            return true;
        }

        public static async Task<bool> RestartPlugin(ApplicationInterfaceType source, CommandContext ctx)
        {
            DiscordLink plugin = DiscordLink.Obj;
            Logger.Info("Restart command executed - Restarting");
            await ReportCommandInfo(source, ctx, "Attempting Restart!");
            bool restarted = plugin.Restart().Result;

            string result;
            if (restarted)
            {
                result = "Restart Successful!";
                if (source == ApplicationInterfaceType.Eco)
                {
                    await ReportCommandInfo(source, ctx, result);
                }
                else if (source == ApplicationInterfaceType.Discord)
                {
                    // Special handling since the call context is broken by the restart and can't be used to respond to the command
                    DiscordChannel channel = plugin.Client.ChannelByNameOrId(((DiscordCommandContext)ctx).Interaction.Channel.Id.ToString());
                    _ = plugin.Client.SendMessageAsync(channel, result);
                }
            }
            else
            {
                result = "Restart failed or a restart was already in progress.";
                await ReportCommandError(source, ctx, result);
            }

            Logger.Info(result);
            return restarted;
        }

        public static async Task<bool> ReloadConfig(ApplicationInterfaceType source, CommandContext ctx)
        {
            var resultAndMessage = await PluginUtils.ReloadConfig(DiscordLink.Obj);
            if (resultAndMessage.Item1)
            {
                await ReportCommandInfo(source, ctx, resultAndMessage.Item2);
            }
            else
            {
                await ReportCommandError(source, ctx, resultAndMessage.Item2);
            }
            return resultAndMessage.Item1;
        }

        public static async Task<bool> ResetPersistentData(ApplicationInterfaceType source, CommandContext ctx)
        {
            Logger.Info("ResetPersistentData command invoked - Resetting persistent storage data");
            DLStorage.Instance.ResetPersistentData();
            await ReportCommandInfo(source, ctx, "Persistent storage data has been reset.");
            return true;
        }

        public static async Task<bool> ResetWorldData(ApplicationInterfaceType source, CommandContext ctx)
        {
            Logger.Info("ResetWorldData command invoked - Resetting world storage data");
            DLStorage.Instance.ResetWorldData();
            await ReportCommandInfo(source, ctx, "World storage data has been reset.");
            return true;
        }

        public static async Task<bool> PersistentStorageData(ApplicationInterfaceType source, CommandContext ctx)
        {
            DiscordLinkEmbed embed = await DLStorage.PersistentData.GetDataDescription();
            await DisplayCommandData(source, ctx, "Persistent Storage Data", embed, DLConstants.ECO_PANEL_COMPLEX_LIST);
            return true;
        }

        public static async Task<bool> WorldStorageData(ApplicationInterfaceType source, CommandContext ctx)
        {
            DiscordLinkEmbed embed = await DLStorage.WorldData.GetDataDescription();
            await DisplayCommandData(source, ctx, "World Storage Data", embed, DLConstants.ECO_PANEL_COMPLEX_LIST);
            return true;
        }

        public static async Task<bool> ClearRoles(ApplicationInterfaceType source, CommandContext ctx)
        {
            Logger.Info("ClearRoles command invoked - Deleting all created roles");
            DiscordLink plugin = DiscordLink.Obj;
            DiscordClient client = plugin.Client;
            foreach (ulong roleId in DLStorage.PersistentData.RoleIds)
            {
                DiscordRole role = client.Guild.GetRoleById(roleId);
                await client.DeleteRoleAsync(role);
            }
            DLStorage.PersistentData.RoleIds.Clear();
            DLStorage.Instance.Write();
            await ReportCommandInfo(source, ctx, "Deleted all tracked roles.");
            return true;
        }

        #endregion

        #region Server Management

        public static async Task<bool> ServerShutdown(ApplicationInterfaceType source, CommandContext ctx)
        {
            Logger.Info("Server shutdown command issued");
            await ReportCommandInfo(source, ctx, "Shutdown command issued");
            PluginManager.Controller.FireShutdown(ApplicationExitCodes.NormalShutdown);
            return true;
        }

        #endregion

        #region Troubleshooting

        public static async Task<bool> ListChannelLinks(ApplicationInterfaceType source, CommandContext ctx)
        {
            await DisplayCommandData(source, ctx, "List of Linked Channels", MessageBuilder.Shared.GetChannelLinkList());
            return true;
        }

        public static async Task<bool> VerifyConfig(ApplicationInterfaceType source, CommandContext ctx)
        {
            await DisplayCommandData(source, ctx, "Config Verification Report", MessageBuilder.Shared.GetConfigVerificationReport());
            return true;
        }

        public static async Task<bool> VerifyPermissions(ApplicationInterfaceType source, CommandContext ctx, MessageBuilder.PermissionReportComponentFlag flag)
        {
            await DisplayCommandData(source, ctx, "Permission Verification Report", MessageBuilder.Shared.GetPermissionsReport(flag));
            return true;
        }

        public static async Task<bool> VerifyPermissionsForChannel(ApplicationInterfaceType source, CommandContext ctx, string channelNameOrId)
        {
            DiscordChannel channel = DiscordLink.Obj.Client.ChannelByNameOrId(channelNameOrId);
            if (channel == null)
            {
                await ReportCommandError(source, ctx, $"No channel with the named \"{channelNameOrId}\" could be found.");
                return false;
            }

            await VerifyPermissionsForChannel(source, ctx, channel);
            return true;
        }

        public static async Task<bool> VerifyPermissionsForChannel(ApplicationInterfaceType source, CommandContext ctx, DiscordChannel channel)
        {
            await DisplayCommandData(source, ctx, $"Permission Verification Report for {channel.Name}", MessageBuilder.Shared.GetPermissionsReportForChannel(channel));
            return true;
        }

        #endregion

        #region Lookups

        public static async Task<bool> PlayerReport(ApplicationInterfaceType source, CommandContext ctx, string playerNameOrId, PlayerReportComponentFlag ReportType)
        {
            User user = Lookups.UserByNameOrId(playerNameOrId);
            if (user == null)
            {
                await ReportCommandError(source, ctx, $"No player with the name or ID \"{playerNameOrId}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, ReportType);
            if (source == ApplicationInterfaceType.Eco)
                await DisplayCommandData(source, ctx, $"Player report for {user.MarkedUpName}", report, DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, ctx, $"Player report for {MessageUtils.StripTags(user.Name)}", report);
            return true;
        }

        public static async Task<bool> CurrencyReport(ApplicationInterfaceType source, CommandContext ctx, string currencyNameOrId)
        {
            Currency currency = Lookups.CurrencyByNameOrId(currencyNameOrId);
            if (currency == null)
            {
                await ReportCommandError(source, ctx, $"No currency with the name or ID \"{currencyNameOrId}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = MessageBuilder.Discord.GetCurrencyReport(currency, DLConfig.DefaultValues.MaxTopCurrencyHolderCount, useBackingInfo: true, useTradeCount: true);
            if (report == null)
            {
                await ReportCommandError(source, ctx, $"Could not create a report for {currency} as no one holds this currency and no trades has been made with it.");
                return false;
            }

            if (source == ApplicationInterfaceType.Eco)
                await DisplayCommandData(source, ctx, $"Currency report for {currency.MarkedUpName}", report, DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, ctx, $"Currency report for {currency}", report);
            return true;
        }

        public static async Task<bool> CurrenciesReport(ApplicationInterfaceType source, CommandContext ctx, CurrencyType currencyType, int maxCurrenciesPerType, int holdersPerCurrency)
        {
            if (maxCurrenciesPerType <= 0)
            {
                await ReportCommandError(source, ctx, "The MaxCurrenciesPerType parameter must be a positive number.");
                return false;
            }
            if (holdersPerCurrency <= 0)
            {
                await ReportCommandError(source, ctx, "The HoldersPerCurrency parameter must be a positive number.");
                return false;
            }

            bool useMinted = currencyType == CurrencyType.All || currencyType == CurrencyType.Minted;
            bool usePersonal = currencyType == CurrencyType.All || currencyType == CurrencyType.Personal;

            IEnumerable<Currency> currencies = Lookups.Currencies;
            var currencyTradesMap = Moose.Plugin.MooseStorage.WorldData.CurrencyToTradeCountMap;
            List<DiscordLinkEmbed> reports = new List<DiscordLinkEmbed>();

            if (useMinted)
            {
                IEnumerable<Currency> mintedCurrencies = currencies.Where(c => c.Backed).OrderByDescending(c => currencyTradesMap.Keys.Contains(c.Id) ? currencyTradesMap[c.Id] : 0);
                var currencyEnumerator = mintedCurrencies.GetEnumerator();
                for (int i = 0; i < maxCurrenciesPerType && currencyEnumerator.MoveNext(); ++i)
                {
                    DiscordLinkEmbed currencyReport = MessageBuilder.Discord.GetCurrencyReport(currencyEnumerator.Current, holdersPerCurrency, useBackingInfo: true, useTradeCount: true);
                    if (currencyReport != null)
                        reports.Add(currencyReport);
                }
            }

            if (usePersonal)
            {
                IEnumerable<Currency> personalCurrencies = currencies.Where(c => !c.Backed).OrderByDescending(c => currencyTradesMap.Keys.Contains(c.Id) ? currencyTradesMap[c.Id] : 0);
                var currencyEnumerator = personalCurrencies.GetEnumerator();
                for (int i = 0; i < maxCurrenciesPerType && currencyEnumerator.MoveNext(); ++i)
                {
                    DiscordLinkEmbed currencyReport = MessageBuilder.Discord.GetCurrencyReport(currencyEnumerator.Current, holdersPerCurrency, useBackingInfo: true, useTradeCount: true);
                    if (currencyReport != null)
                        reports.Add(currencyReport);
                }
            }

            if (source == ApplicationInterfaceType.Eco)
            {
                string fullReport = string.Join("\n\n", reports.Select(r => r.AsEcoText()));
                await DisplayCommandData(source, ctx, $"Currencies Report", fullReport, DLConstants.ECO_PANEL_REPORT);
            }
            else
            {
                if (reports.Count > 0)
                    await DisplayCommandData(source, ctx, $"Currencies Report", reports);
                else
                    await ReportCommandError(source, ctx, "No matching currencies found.");
            }

            return true;
        }

        public static async Task<bool> ElectionReport(ApplicationInterfaceType source, CommandContext ctx, string electionNameOrId)
        {
            Election election = Lookups.ActiveElectionByNameOrId(electionNameOrId);
            if (election == null)
            {
                await ReportCommandError(source, ctx, $"No election with the name or ID \"{electionNameOrId}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = MessageBuilder.Discord.GetElectionReport(election);
            if (source == ApplicationInterfaceType.Eco)
                await DisplayCommandData(source, ctx, $"Election report for {election.MarkedUpName}", report, DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, ctx, $"Election report for {election}", report);
            return true;
        }

        public static async Task<bool> ElectionsReport(ApplicationInterfaceType source, CommandContext ctx)
        {
            IEnumerable<Election> elections = Lookups.ActiveElections;
            if (elections.Count() <= 0)
            {
                await ReportCommandInfo(source, ctx, "There are no active elections.");
                return false;
            }

            List<DiscordLinkEmbed> reports = new List<DiscordLinkEmbed>();
            foreach (Election election in elections)
            {
                DiscordLinkEmbed report = MessageBuilder.Discord.GetElectionReport(election);
                if (report.Fields.Count() > 0)
                    reports.Add(report);
            }

            if (reports.Count() <= 0)
            {
                await ReportCommandInfo(source, ctx, "None of the active elections have a voting option.");
                return false;
            }

            if (source == ApplicationInterfaceType.Eco)
                await DisplayCommandData(source, ctx, $"Elections Report", string.Join("\n\n", reports.Select(r => r.AsEcoText())), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, ctx, $"Elections Report", reports);

            return true;
        }

        public static async Task<bool> WorkPartyReport(ApplicationInterfaceType source, CommandContext ctx, string workPartyNameOrId)
        {
            WorkParty workParty = Lookups.ActiveWorkPartyByNameOrId(workPartyNameOrId);
            if (workParty == null)
            {
                await ReportCommandError(source, ctx, $"No work party with the name or ID \"{workPartyNameOrId}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = MessageBuilder.Discord.GetWorkPartyReport(workParty);
            if (source == ApplicationInterfaceType.Eco)
                await DisplayCommandData(source, ctx, $"Work party report for {workParty}", report.AsEcoText(), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, ctx, $"Work party report for {workParty}", report);

            return true;
        }

        public static async Task<bool> WorkPartiesReport(ApplicationInterfaceType source, CommandContext ctx)
        {
            IEnumerable<WorkParty> workParties = Lookups.ActiveWorkParties;
            if (workParties.Count() <= 0)
            {
                await ReportCommandInfo(source, ctx, "There are no active work parties");
                return false;
            }

            List<DiscordLinkEmbed> reports = new List<DiscordLinkEmbed>();
            foreach (WorkParty workParty in workParties)
            {
                reports.Add(MessageBuilder.Discord.GetWorkPartyReport(workParty));
            }

            if (source == ApplicationInterfaceType.Eco)
                await DisplayCommandData(source, ctx, $"Work Parties Report", string.Join("\n\n", reports.Select(r => r.AsEcoText())), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, ctx, "Work Parties Report", reports);

            return true;
        }

        #endregion

        #region Invites

        public static async Task<bool> PostInviteMessage(ApplicationInterfaceType source, CommandContext ctx)
        {
            DLConfigData config = DLConfig.Data;
            string discordAddress = NetworkManager.Config.DiscordAddress;
            if (string.IsNullOrEmpty(discordAddress))
            {
                await ReportCommandError(source, ctx, "This server does not have an associated Discord server.");
                return false;
            }

            string inviteMessage = config.InviteMessage;
            if (!inviteMessage.ContainsCaseInsensitive(DLConstants.INVITE_COMMAND_TOKEN))
            {
                await ReportCommandError(source, ctx, "This server has not specified a valid invite message.");
                return false;
            }

            inviteMessage = Regex.Replace(inviteMessage, Regex.Escape(DLConstants.INVITE_COMMAND_TOKEN), discordAddress);
            bool sent = Message.SendChatToDefaultChannel(null, inviteMessage);
            if (sent)
                await ReportCommandInfo(source, ctx, "Invite sent.");
            else
                await ReportCommandError(source, ctx, "Failed to send invite.");

            return sent;
        }
        #endregion

        #region Trades

        public static async Task<bool> Trades(ApplicationInterfaceType source, CommandContext ctx, string searchName)
        {
            if (string.IsNullOrWhiteSpace(searchName))
            {
                await ReportCommandInfo(source, ctx, "Please provide the name of a player, tag, item or store to search for.");
                return false;
            }

            string matchedName = FindOffers(searchName, out TradeTargetType offerType, out StoreOfferList groupedBuyOffers, out StoreOfferList groupedSellOffers);
            if (offerType == TradeTargetType.Invalid)
            {
                await ReportCommandError(source, ctx, $"No player, tag, item or store with the name \"{searchName}\" could be found.");
                return false;
            }

            if (source == ApplicationInterfaceType.Eco)
            {
                Moose.Plugin.Commands.Trades(((EcoCommandContext)ctx).User, searchName);
            }
            else
            {
                MessageBuilder.Discord.FormatTrades(matchedName, offerType, groupedBuyOffers, groupedSellOffers, out DiscordLinkEmbed embed);
                await DisplayCommandData(source, ctx, null, embed);
            }

            return true;
        }

        public static async Task<bool> AddTradeWatcher(ApplicationInterfaceType source, CommandContext ctx, string searchName, Modules.ModuleArchetype type)
        {
            if (string.IsNullOrWhiteSpace(searchName))
            {
                await ReportCommandInfo(source, ctx, "Please provide the name of a player, tag, item or store to watch trades for.");
                return false;
            }

            LinkedUser linkedUser = source == ApplicationInterfaceType.Eco
                ? UserLinkManager.LinkedUserByEcoUser(((EcoCommandContext)ctx).User, ((EcoCommandContext)ctx).User, "Trade Watcher Registration")
                : UserLinkManager.LinkedUserByDiscordUser(((DiscordCommandContext)ctx).Interaction.User, ((DiscordCommandContext)ctx).Interaction.Member, "Trade Watcher Registration");
            if (linkedUser == null)
                return false;

            ulong discordMemberId = ulong.Parse(linkedUser.DiscordId);
            if (type == Modules.ModuleArchetype.Display)
            {
                if (DLConfig.Data.MaxTradeWatcherDisplaysPerUser <= 0)
                {
                    await ReportCommandError(source, ctx, "Trade watcher displays are not enabled on this server.");
                    return false;
                }

                int watchedTradesCount = DLStorage.WorldData.GetTradeWatcherCountForMember(discordMemberId);
                if (watchedTradesCount >= DLConfig.Data.MaxTradeWatcherDisplaysPerUser)
                {
                    await ReportCommandError(source, ctx, $"You are already watching {watchedTradesCount} trades and the limit is {DLConfig.Data.MaxTradeWatcherDisplaysPerUser} trade watcher displays per user.\nUse the `/DL-RemoveTradeWatcherDisplay` command to remove a trade watcher to make space if you wish to add a new one.");
                    return false;
                }
            }
            else if (!DLConfig.Data.UseTradeWatcherFeeds)
            {
                await ReportCommandError(source, ctx, "Trade watcher feeds are not enabled on this server.");
                return false;
            }

            string matchedName = FindOffers(searchName, out TradeTargetType offerType, out _, out _);
            if (offerType == TradeTargetType.Invalid)
                return false;

            bool added = await DLStorage.WorldData.AddTradeWatcher(discordMemberId, new TradeWatcherEntry(matchedName, type));
            if (added)
            {
                await ReportCommandInfo(source, ctx, $"Watching all trades for {matchedName}.");
                return true;
            }
            else
            {
                await ReportCommandError(source, ctx, $"Failed to start watching trades for {matchedName}. \nUse `/DL-TradeWatchers` to see what is currently being watched.");
                return false;
            }
        }

        public static async Task<bool> RemoveTradeWatcher(ApplicationInterfaceType source, CommandContext ctx, string searchName, Modules.ModuleArchetype type)
        {
            if (string.IsNullOrWhiteSpace(searchName))
            {
                await ReportCommandInfo(source, ctx, "Please provide the name of a player, tag, item or store to watch trades for.");
                return false;
            }

            LinkedUser linkedUser = source == ApplicationInterfaceType.Eco
                ? UserLinkManager.LinkedUserByEcoUser(((EcoCommandContext)ctx).User, ((EcoCommandContext)ctx).User, "Trade Watcher Unregistration")
                : UserLinkManager.LinkedUserByDiscordUser(((DiscordCommandContext)ctx).Interaction.User, ((DiscordCommandContext)ctx).Interaction.Member, "Trade Watcher Unregistration");
            if (linkedUser == null)
                return false;

            ulong discordId = ulong.Parse(linkedUser.DiscordId);
            bool removed = await DLStorage.WorldData.RemoveTradeWatcher(discordId, new TradeWatcherEntry(searchName, type));
            if (removed)
            {
                await ReportCommandInfo(source, ctx, $"Stopped watching trades for {searchName}.");
                return true;
            }
            else
            {
                await ReportCommandError(source, ctx, $"Failed to stop watching trades for {searchName}.\nUse `/DL-TradeWatchers` to see what is currently being watched.");
                return false;
            }
        }

        public static async Task<bool> ListTradeWatchers(ApplicationInterfaceType source, CommandContext ctx)
        {
            LinkedUser linkedUser = source == ApplicationInterfaceType.Eco
                ? UserLinkManager.LinkedUserByEcoUser(((EcoCommandContext)ctx).User, ((EcoCommandContext)ctx).User, "Trade Watchers Listing")
                : UserLinkManager.LinkedUserByDiscordUser(((DiscordCommandContext)ctx).Interaction.User, ((DiscordCommandContext)ctx).Interaction.Member, "Trade Watchers Listing");
            if (linkedUser == null)
                return false;

            await ReportCommandInfo(source, ctx, $"Watched Trades\n{DLStorage.WorldData.ListTradeWatchers(ulong.Parse(linkedUser.DiscordId))}");
            return true;
        }

        #endregion

        #region Snippets

        public static async Task Snippet(ApplicationInterfaceType source, CommandContext ctx, ApplicationInterfaceType target, string userName, string snippetKey)
        {
            var snippets = DLStorage.Instance.Snippets;
            if (string.IsNullOrWhiteSpace(snippetKey)) // List all snippets if no key is given
            {
                if (snippets.Count > 0)
                    await DisplayCommandData(source, ctx, string.Empty, new DiscordLinkEmbed().AddField("Snippets", string.Join("\n", snippets.Keys)), DLConstants.ECO_PANEL_SIMPLE_LIST);
                else
                    await ReportCommandInfo(source, ctx, "There are no registered snippets.");
            }
            else
            {
                // Find and post the snippet requested by the user
                if (snippets.TryGetValue(snippetKey, out string snippetText))
                {
                    if (target == ApplicationInterfaceType.Eco)
                    {
                        Message.SendChatToDefaultChannel(null, $"{userName} invoked snippet \"{snippetKey}\"\n- - -\n{snippetText}\n- - -");
                        _ = ReportCommandInfo(source, ctx, "Snippet posted.");
                    }
                    else
                    {
                        await DiscordCommands.DisplayCommandData((DiscordCommandContext)ctx, snippetKey, snippetText);
                    }
                }
                else
                {
                    await ReportCommandError(source, ctx, $"No snippet with key \"{snippetKey}\" could be found.");
                }
            }
        }

        #endregion
    }
}
