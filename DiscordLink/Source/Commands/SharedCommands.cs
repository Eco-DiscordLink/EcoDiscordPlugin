using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
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
    /**
     * Platform independent implementations of commands used in both Discord and Eco.
     */
    public static class SharedCommands
    {
        #region Commands Base

        private static async Task ReportCommandError(ApplicationInterfaceType source, object callContext, string message)
        {
            if (source == ApplicationInterfaceType.Eco)
                EcoCommands.ReportCommandError(callContext as User, message);
            else
                await DiscordCommands.ReportCommandError(callContext as InteractionContext, message);
        }

        private static async Task ReportCommandInfo(ApplicationInterfaceType source, object callContext, string message)
        {
            if (source == ApplicationInterfaceType.Eco)
                EcoCommands.ReportCommandInfo(callContext as User, message);
            else
                await DiscordCommands.ReportCommandInfo(callContext as InteractionContext, message);
        }

        private static async Task DisplayCommandData(ApplicationInterfaceType source, object callContext, string title, object data, string panelInstance = "")
        {
            if (source == ApplicationInterfaceType.Eco)
            {
                User context = callContext as User;
                if (data is string message)
                {
                    EcoCommands.DisplayCommandData(context, panelInstance, MessageUtils.FormatMessageForEco(title), MessageUtils.FormatMessageForEco(message));
                }
                else if(data is DiscordLinkEmbed embed)
                {
                    string titleToUse = string.Empty;
                    if(!title.IsEmpty())
                        titleToUse = title;
                    else if(!embed.Title.IsEmpty())
                        titleToUse = embed.Title;
                    else if(!embed.Fields.First().Title.IsEmpty())
                        titleToUse = embed.Fields.First().Title;
                    EcoCommands.DisplayCommandData(context, panelInstance, MessageUtils.FormatMessageForEco(titleToUse), embed.AsEcoText() );
                }
            }
            else if(source == ApplicationInterfaceType.Discord)
            {
                InteractionContext context = callContext as InteractionContext;
                if (data is DiscordLinkEmbed embed)
                    await DiscordCommands.DisplayCommandData(context, title, embed);
                else if (data is IEnumerable<DiscordLinkEmbed> embeds)
                    await DiscordCommands.DisplayCommandData(context, title, embeds);
                else if (data is string content)
                    await DiscordCommands.DisplayCommandData(context, title, content);
                else
                    Logger.Error($"Attempted to display command data of unhandled type \"{data.GetType()}\"");
            }
        }

        #endregion

        #region Plugin Management

        public static async Task<bool> Update(ApplicationInterfaceType source, object callContext)
        {
            if (DiscordLink.Obj.Client.ConnectionStatus != DiscordClient.ConnectionState.Connected)
            {
                await ReportCommandError(source, callContext, "Failed to force update - Discord client not connected");
                return false;
            }

            DiscordLink plugin = DiscordLink.Obj;
            plugin.Modules.ForEach(async module => await module.HandleStartOrStop());
            await plugin.HandleEvent(Events.DLEventType.ForceUpdate);
            await ReportCommandInfo(source, callContext, "Forced update");
            return true;
        }

        public static async Task<bool> RestartPlugin(ApplicationInterfaceType source, object callContext)
        {
            DiscordLink plugin = DiscordLink.Obj;
            Logger.Info("Restart command executed - Restarting");
            await ReportCommandInfo(source, callContext, "Attempting Restart!");
            bool restarted = plugin.Restart().Result;

            string result;
            if (restarted)
            {
                result = "Restart Successful!";
                if (source == ApplicationInterfaceType.Eco)
                {
                    await ReportCommandInfo(source, callContext, result);
                }
                else if (source == ApplicationInterfaceType.Discord)
                {
                    // Special handling since the call context is broken by the restart and can't be used to respond to the command
                    DiscordChannel channel = plugin.Client.ChannelByNameOrID(((InteractionContext)callContext).Channel.Id.ToString());
                    _ = plugin.Client.SendMessageAsync(channel, result);
                }
            }
            else
            {
                result = "Restart failed or a restart was already in progress.";
                await ReportCommandError(source, callContext, result);
            }

            Logger.Info(result);
            return restarted;
        }

        public static async Task<bool> ResetPersistentData(ApplicationInterfaceType source, object callContext)
        {
            Logger.Info("ResetPersistentData command invoked - Resetting persistent storage data");
            DLStorage.Instance.ResetPersistentData();
            await ReportCommandInfo(source, callContext, "Persistent storage data has been reset.");
            return true;
        }

        public static async Task<bool> ResetWorldData(ApplicationInterfaceType source, object callContext)
        {
            Logger.Info("ResetWorldData command invoked - Resetting world storage data");
            DLStorage.Instance.ResetWorldData();
            await ReportCommandInfo(source, callContext, "World storage data has been reset.");
            return true;
        }

        public static async Task<bool> ClearRoles(ApplicationInterfaceType source, object callContext)
        {
            Logger.Info("ClearRoles command invoked - Deleting all created roles");
            DiscordLink plugin = DiscordLink.Obj;
            DiscordClient client = plugin.Client;
            foreach (ulong roleID in DLStorage.PersistentData.RoleIDs)
            {
                DiscordRole role = client.Guild.RoleByID(roleID);
                await client.DeleteRoleAsync(role);
            }
            DLStorage.PersistentData.RoleIDs.Clear();
            DLStorage.Instance.Write();
            await ReportCommandInfo(source, callContext, "Deleted all tracked roles.");
            return true;
        }

        public static async Task<bool> PersistentStorageData(ApplicationInterfaceType source, object callContext)
        {
            DiscordLinkEmbed embed = await DLStorage.PersistentData.GetDataDescription();
            await DisplayCommandData(source, callContext, "Persistent Storage Data", embed, DLConstants.ECO_PANEL_COMPLEX_LIST);
            return true;
        }

        public static async Task<bool> WorldStorageData(ApplicationInterfaceType source, object callContext)
        {
            DiscordLinkEmbed embed = await DLStorage.WorldData.GetDataDescription();
            await DisplayCommandData(source, callContext, "World Storage Data", embed, DLConstants.ECO_PANEL_COMPLEX_LIST);
            return true;
        }

        #endregion

        #region Server Management

        public static async Task<bool> ServerShutdown(ApplicationInterfaceType source, object callContext)
        {
            Logger.Info("Server shutdown command issued");
            await ReportCommandInfo(source, callContext, "Shutdown command issued");
            PluginManager.Controller.FireShutdown(ApplicationExitCodes.NormalShutdown);
            return true;
        }

        #endregion

        #region Troubleshooting

        public static async Task<bool> ListChannelLinks(ApplicationInterfaceType source, object callContext)
        {
            await DisplayCommandData(source, callContext, "List of Linked Channels", MessageBuilder.Shared.GetChannelLinkList());
            return true;
        }

        public static async Task<bool> VerifyConfig(ApplicationInterfaceType source, object callContext)
        {
            await DisplayCommandData(source, callContext, "Config Verification Report", MessageBuilder.Shared.GetConfigVerificationReport());
            return true;
        }

        public static async Task<bool> VerifyPermissions(ApplicationInterfaceType source, object callContext, MessageBuilder.PermissionReportComponentFlag flag)
        {
            await DisplayCommandData(source, callContext, "Permission Verification Report", MessageBuilder.Shared.GetPermissionsReport(flag));
            return true;
        }

        public static async Task<bool> VerifyPermissionsForChannel(ApplicationInterfaceType source, object callContext, string channelNameOrID)
        {
            DiscordChannel channel = DiscordLink.Obj.Client.ChannelByNameOrID(channelNameOrID);
            if (channel == null)
            {
                await ReportCommandError(source, callContext, $"No channel with the named \"{channelNameOrID}\" could be found.");
                return false;
            }

            await VerifyPermissionsForChannel(source, callContext, channel);
            return true;
        }

        public static async Task<bool> VerifyPermissionsForChannel(ApplicationInterfaceType source, object callContext, DiscordChannel channel)
        {
            await DisplayCommandData(source, callContext, $"Permission Verification Report for {channel.Name}", MessageBuilder.Shared.GetPermissionsReportForChannel(channel));
            return true;
        }

        #endregion

        #region Lookups

        public static async Task<bool> PlayerReport(ApplicationInterfaceType source, object callContext, string playerNameOrID, PlayerReportComponentFlag ReportType)
        {
            User user = Lookups.UserByNameOrID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, ReportType);
            if (source == ApplicationInterfaceType.Eco)
                await DisplayCommandData(source, callContext, $"Player report for {user.MarkedUpName}", report, DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, $"Player report for {MessageUtils.StripTags(user.Name)}", report);
            return true;
        }

        public static async Task<bool> CurrencyReport(ApplicationInterfaceType source, object callContext, string currencyNameOrID)
        {
            Currency currency = Lookups.CurrencyByNameOrID(currencyNameOrID);
            if (currency == null)
            {
                await ReportCommandError(source, callContext, $"No currency with the name or ID \"{currencyNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = MessageBuilder.Discord.GetCurrencyReport(currency, DLConfig.DefaultValues.MaxTopCurrencyHolderCount, useBackingInfo: true, useTradeCount: true);
            if (report == null)
            {
                await ReportCommandError(source, callContext, $"Could not create a report for {currency} as no one holds this currency and no trades has been made with it.");
                return false;
            }

            if (source == ApplicationInterfaceType.Eco)
                await DisplayCommandData(source, callContext, $"Currency report for {currency.MarkedUpName}", report, DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, $"Currency report for {currency}", report);
            return true;
        }

        public static async Task<bool> CurrenciesReport(ApplicationInterfaceType source, object callContext, CurrencyType currencyType, int maxCurrenciesPerType, int holdersPerCurrency)
        {
            if (maxCurrenciesPerType <= 0)
            {
                await ReportCommandError(source, callContext, "The MaxCurrenciesPerType parameter must be a positive number.");
                return false;
            }
            if (holdersPerCurrency <= 0)
            {
                await ReportCommandError(source, callContext, "The HoldersPerCurrency parameter must be a positive number.");
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
                await DisplayCommandData(source, callContext, $"Currencies Report", fullReport, DLConstants.ECO_PANEL_REPORT);
            }
            else
            {
                if (reports.Count > 0)
                    await DisplayCommandData(source, callContext, $"Currencies Report", reports);
                else
                    await ReportCommandError(source, callContext, "No matching currencies found.");
            }

            return true;
        }

        public static async Task<bool> ElectionReport(ApplicationInterfaceType source, object callContext, string electionNameOrID)
        {
            Election election = Lookups.ActiveElectionByNameOrID(electionNameOrID);
            if (election == null)
            {
                await ReportCommandError(source, callContext, $"No election with the name or ID \"{electionNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = MessageBuilder.Discord.GetElectionReport(election);
            if (source == ApplicationInterfaceType.Eco)
                await DisplayCommandData(source, callContext, $"Election report for {election.MarkedUpName}", report, DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, $"Election report for {election}", report);
            return true;
        }

        public static async Task<bool> ElectionsReport(ApplicationInterfaceType source, object callContext)
        {
            IEnumerable<Election> elections = Lookups.ActiveElections;
            if (elections.Count() <= 0)
            {
                await ReportCommandInfo(source, callContext, "There are no active elections.");
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
                await ReportCommandInfo(source, callContext, "None of the active elections have a voting option.");
                return false;
            }

            if (source == ApplicationInterfaceType.Eco)
                await DisplayCommandData(source, callContext, $"Elections Report", string.Join("\n\n", reports.Select(r => r.AsEcoText())), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, $"Elections Report", reports);

            return true;
        }

        public static async Task<bool> WorkPartyReport(ApplicationInterfaceType source, object callContext, string workPartyNameOrID)
        {
            WorkParty workParty = Lookups.ActiveWorkPartyByNameOrID(workPartyNameOrID);
            if (workParty == null)
            {
                await ReportCommandError(source, callContext, $"No work party with the name or ID \"{workPartyNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = MessageBuilder.Discord.GetWorkPartyReport(workParty);
            if (source == ApplicationInterfaceType.Eco)
                await DisplayCommandData(source, callContext, $"Work party report for {workParty}", report.AsEcoText(), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, $"Work party report for {workParty}", report);

            return true;
        }

        public static async Task<bool> WorkPartiesReport(ApplicationInterfaceType source, object callContext)
        {
            IEnumerable<WorkParty> workParties = Lookups.ActiveWorkParties;
            if (workParties.Count() <= 0)
            {
                await ReportCommandInfo(source, callContext, "There are no active work parties");
                return false;
            }

            List<DiscordLinkEmbed> reports = new List<DiscordLinkEmbed>();
            foreach (WorkParty workParty in workParties)
            {
                reports.Add(MessageBuilder.Discord.GetWorkPartyReport(workParty));
            }

            if (source == ApplicationInterfaceType.Eco)
                await DisplayCommandData(source, callContext, $"Work Parties Report", string.Join("\n\n", reports.Select(r => r.AsEcoText())), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, "Work Parties Report", reports);

            return true;
        }

        #endregion

        #region Invites

        public static async Task<bool> PostInviteMessage(ApplicationInterfaceType source, object callContext)
        {
            DLConfigData config = DLConfig.Data;
            string discordAddress = NetworkManager.Config.DiscordAddress;
            if (string.IsNullOrEmpty(discordAddress))
            {
                await ReportCommandError(source, callContext, "This server does not have an associated Discord server.");
                return false;
            }

            string inviteMessage = config.InviteMessage;
            if (!inviteMessage.ContainsCaseInsensitive(DLConstants.INVITE_COMMAND_TOKEN))
            {
                await ReportCommandError(source, callContext, "This server has not specified a valid invite message.");
                return false;
            }

            inviteMessage = Regex.Replace(inviteMessage, Regex.Escape(DLConstants.INVITE_COMMAND_TOKEN), discordAddress);
            bool sent = Message.SendChatToDefaultChannel(null, inviteMessage);
            if (sent)
                await ReportCommandInfo(source, callContext, "Invite sent.");
            else
                await ReportCommandError(source, callContext, "Failed to send invite.");

            return sent;
        }
        #endregion

        #region Trades

        public static async Task<bool> Trades(ApplicationInterfaceType source, object callContext, string searchName)
        {
            if (string.IsNullOrWhiteSpace(searchName))
            {
                await ReportCommandInfo(source, callContext, "Please provide the name of a player, tag, item or store to search for.");
                return false;
            }

            string matchedName = FindOffers(searchName, out TradeTargetType offerType, out StoreOfferList groupedBuyOffers, out StoreOfferList groupedSellOffers);
            if (offerType == TradeTargetType.Invalid)
            {
                await ReportCommandError(source, callContext, $"No player, tag, item or store with the name \"{searchName}\" could be found.");
                return false;
            }

            if (source == ApplicationInterfaceType.Eco)
            {
                Moose.Plugin.Commands.Trades(callContext as User, searchName);
            }
            else
            {
                MessageBuilder.Discord.FormatTrades(matchedName, offerType, groupedBuyOffers, groupedSellOffers, out DiscordLinkEmbed embed);
                await DisplayCommandData(source, callContext, null, embed);
            }

            return true;
        }

        public static async Task<bool> AddTradeWatcher(ApplicationInterfaceType source, object callContext, string searchName, Modules.ModuleArchetype type)
        {
            if (string.IsNullOrWhiteSpace(searchName))
            {
                await ReportCommandInfo(source, callContext, "Please provide the name of a player, tag, item or store to watch trades for.");
                return false;
            }

            LinkedUser linkedUser = source == ApplicationInterfaceType.Eco
                ? UserLinkManager.LinkedUserByEcoUser(callContext as User, callContext as User, "Trade Watcher Registration")
                : UserLinkManager.LinkedUserByDiscordUser((callContext as InteractionContext).User, (callContext as InteractionContext).Member, "Trade Watcher Registration");
            if (linkedUser == null)
                return false;

            ulong discordID = ulong.Parse(linkedUser.DiscordID);
            if (type == Modules.ModuleArchetype.Display)
            {
                if (DLConfig.Data.MaxTradeWatcherDisplaysPerUser <= 0)
                {
                    await ReportCommandError(source, callContext, "Trade watcher displays are not enabled on this server.");
                    return false;
                }

                int watchedTradesCount = DLStorage.WorldData.GetTradeWatcherCountForUser(discordID);
                if (watchedTradesCount >= DLConfig.Data.MaxTradeWatcherDisplaysPerUser)
                {
                    await ReportCommandError(source, callContext, $"You are already watching {watchedTradesCount} trades and the limit is {DLConfig.Data.MaxTradeWatcherDisplaysPerUser} trade watcher displays per user.\nUse the `/DL-RemoveTradeWatcherDisplay` command to remove a trade watcher to make space if you wish to add a new one.");
                    return false;
                }
            }
            else if (!DLConfig.Data.UseTradeWatcherFeeds)
            {
                await ReportCommandError(source, callContext, "Trade watcher feeds are not enabled on this server.");
                return false;
            }

            string matchedName = FindOffers(searchName, out TradeTargetType offerType, out _, out _);
            if (offerType == TradeTargetType.Invalid)
                return false;

            bool added = await DLStorage.WorldData.AddTradeWatcher(discordID, new TradeWatcherEntry(matchedName, type));
            if (added)
            {
                await ReportCommandInfo(source, callContext, $"Watching all trades for {matchedName}.");
                return true;
            }
            else
            {
                await ReportCommandError(source, callContext, $"Failed to start watching trades for {matchedName}. \nUse `/DL-TradeWatchers` to see what is currently being watched.");
                return false;
            }
        }

        public static async Task<bool> RemoveTradeWatcher(ApplicationInterfaceType source, object callContext, string searchName, Modules.ModuleArchetype type)
        {
            if (string.IsNullOrWhiteSpace(searchName))
            {
                await ReportCommandInfo(source, callContext, "Please provide the name of a player, tag, item or store to watch trades for.");
                return false;
            }

            LinkedUser linkedUser = source == ApplicationInterfaceType.Eco
                ? UserLinkManager.LinkedUserByEcoUser(callContext as User, callContext as User, "Trade Watcher Unregistration")
                : UserLinkManager.LinkedUserByDiscordUser((callContext as InteractionContext).User, (callContext as InteractionContext).Member, "Trade Watcher Unregistration");
            if (linkedUser == null)
                return false;

            ulong discordID = ulong.Parse(linkedUser.DiscordID);
            bool removed = await DLStorage.WorldData.RemoveTradeWatcher(discordID, new TradeWatcherEntry(searchName, type));
            if (removed)
            {
                await ReportCommandInfo(source, callContext, $"Stopped watching trades for {searchName}.");
                return true;
            }
            else
            {
                await ReportCommandError(source, callContext, $"Failed to stop watching trades for {searchName}.\nUse `/DL-TradeWatchers` to see what is currently being watched.");
                return false;
            }
        }

        public static async Task<bool> ListTradeWatchers(ApplicationInterfaceType source, object callContext)
        {
            LinkedUser linkedUser = source == ApplicationInterfaceType.Eco
                ? UserLinkManager.LinkedUserByEcoUser(callContext as User, callContext as User, "Trade Watchers Listing")
                : UserLinkManager.LinkedUserByDiscordUser((callContext as InteractionContext).User, (callContext as InteractionContext).Member, "Trade Watchers Listing");
            if (linkedUser == null)
                return false;

            await ReportCommandInfo(source, callContext, $"Watched Trades\n{DLStorage.WorldData.ListTradeWatchers(ulong.Parse(linkedUser.DiscordID))}");
            return true;
        }

        #endregion

        #region Snippets

        public static async Task Snippet(ApplicationInterfaceType source, object callContext, ApplicationInterfaceType target, string userName, string snippetKey)
        {
            var snippets = DLStorage.Instance.Snippets;
            if (string.IsNullOrWhiteSpace(snippetKey)) // List all snippets if no key is given
            {
                if (snippets.Count > 0)
                    await DisplayCommandData(source, callContext, string.Empty, new DiscordLinkEmbed().AddField("Snippets", string.Join("\n", snippets.Keys)), DLConstants.ECO_PANEL_SIMPLE_LIST);
                else
                    await ReportCommandInfo(source, callContext, "There are no registered snippets.");
            }
            else
            {
                // Find and post the snippet requested by the user
                if (snippets.TryGetValue(snippetKey, out string snippetText))
                {
                    if (target == ApplicationInterfaceType.Eco)
                    {
                        Message.SendChatToDefaultChannel(null, $"{userName} invoked snippet \"{snippetKey}\"\n- - -\n{snippetText}\n- - -");
                        _ = ReportCommandInfo(source, callContext, "Snippet posted.");
                    }
                    else
                    {
                        await DiscordCommands.DisplayCommandData((InteractionContext)callContext, snippetKey, snippetText);
                    }
                }
                else
                {
                    await ReportCommandError(source, callContext, $"No snippet with key \"{snippetKey}\" could be found.");
                }
            }
        }

        #endregion
    }
}
