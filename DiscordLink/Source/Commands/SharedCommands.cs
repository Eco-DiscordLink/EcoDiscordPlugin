using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.Economy;
using Eco.Gameplay.Economy.WorkParties;
using Eco.Gameplay.Players;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Plugins.Networking;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Eco.Plugins.DiscordLink.Utilities.Utils;
using StoreOfferList = System.Collections.Generic.IEnumerable<System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.StoreComponent, Eco.Gameplay.Components.TradeOffer>>>;

namespace Eco.Plugins.DiscordLink
{
    /**
     * Platform independent implementations of commands used in both Discord and Eco.
     */
    public static class SharedCommands
    {
        #region Commands Base

        public enum CommandInterface
        {
            [ChoiceName("Eco")]
            Eco,
            [ChoiceName("Discord")]
            Discord
        }


        private static async Task ReportCommandError(CommandInterface source, object callContext, string message)
        {
            if (source == CommandInterface.Eco)
                EcoCommands.ReportCommandError(callContext as User, message);
            else
                await DiscordCommands.ReportCommandError(callContext as InteractionContext, message);
        }

        private static async Task ReportCommandInfo(CommandInterface source, object callContext, string message)
        {
            if (source == CommandInterface.Eco)
                EcoCommands.ReportCommandInfo(callContext as User, message);
            else
                await DiscordCommands.ReportCommandInfo(callContext as InteractionContext, message);
        }

        private static async Task DisplayCommandData(CommandInterface source, object callContext, string title, object data, string panelInstance = "")
        {
            if (source == CommandInterface.Eco)
                EcoCommands.DisplayCommandData(callContext as User, panelInstance, title, data as string);
            else if (data is DiscordLinkEmbed embed)
                await DiscordCommands.DisplayCommandData(callContext as InteractionContext, title, embed);
            else if (data is IEnumerable<DiscordLinkEmbed> embeds)
                await DiscordCommands.DisplayCommandData(callContext as InteractionContext, title, embeds);
            else if (data is string content)
                await DiscordCommands.DisplayCommandData(callContext as InteractionContext, title, content);
            else
                Logger.Error($"Attempted to display command data of unhandled type \"{data.GetType()}\"");
        }

        #endregion

        #region Plugin Management

        public static async Task<bool> Update(CommandInterface source, object callContext)
        {
            if(DiscordLink.Obj.Client.ConnectionStatus != DLDiscordClient.ConnectionState.Connected)
            {
                await ReportCommandError(source, callContext, "Failed to force update - Disoord client not connected");
                return false;
            }

            DiscordLink plugin = DiscordLink.Obj;
            plugin.Modules.ForEach(async module => await module.HandleStartOrStop());
            plugin.HandleEvent(Events.DLEventType.ForceUpdate);
            await ReportCommandInfo(source, callContext, "Forced update");
            return true;
        }

        public static async Task<bool> Restart(CommandInterface source, object callContext)
        {
            DiscordLink plugin = DiscordLink.Obj;
            Logger.Info("Restart command executed - Restarting");
            await ReportCommandInfo(source, callContext, "Attempting Restart!");
            bool restarted = plugin.Restart().Result;

            string result;
            if (restarted)
            {
                result = "Restart Successful!";
                if (source == CommandInterface.Eco)
                {
                    await ReportCommandInfo(source, callContext, result);
                }
                else if (source == CommandInterface.Discord)
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

        public static async Task<bool> ResetPersistentData(CommandInterface source, object callContext)
        {
            Logger.Info("ResetPersistentData command invoked - Resetting persistent storage data");
            DLStorage.Instance.ResetPersistentData();
            await ReportCommandInfo(source, callContext, "Persistent storage data has been reset.");
            return true;
        }

        public static async Task<bool> ResetWorldData(CommandInterface source, object callContext)
        {
            Logger.Info("ResetWorldData command invoked - Resetting world storage data");
            DLStorage.Instance.ResetWorldData();
            await ReportCommandInfo(source, callContext, "World storage data has been reset.");
            return true;
        }

        public static async Task<bool> ClearRoles(CommandInterface source, object callContext)
        {
            Logger.Info("ClearRoles command invoked - Deleting all created roles");
            DiscordLink plugin = DiscordLink.Obj;
            DLDiscordClient client = plugin.Client;
            foreach(ulong roleID in DLStorage.PersistentData.RoleIDs)
            {
                DiscordRole role = client.Guild.RoleByID(roleID);
                await client.DeleteRoleAsync(role);
            }
            DLStorage.PersistentData.RoleIDs.Clear();
            DLStorage.Instance.Write();
            await ReportCommandInfo(source, callContext, "Deleted all tracked roles.");
            return true;
        }

        #endregion

        #region Troubleshooting

        public static async Task<bool> ListChannelLinks(CommandInterface source, object callContext)
        {
            await DisplayCommandData(source, callContext, "List of Linked Channels", MessageBuilder.Shared.GetChannelLinkList());
            return true;
        }

        public static async Task<bool> VerifyConfig(CommandInterface source, object callContext)
        {
            await DisplayCommandData(source, callContext, "Config Verification Report", MessageBuilder.Shared.GetConfigVerificationReport());
            return true;
        }

        public static async Task<bool> VerifyPermissions(CommandInterface source, object callContext, MessageBuilder.PermissionReportComponentFlag flag)
        {
            await DisplayCommandData(source, callContext, "Permission Verification Report", MessageBuilder.Shared.GetPermissionsReport(flag));
            return true;
        }

        public static async Task<bool> VerifyPermissionsForChannel(CommandInterface source, object callContext, string channelNameOrID)
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

        public static async Task<bool> VerifyPermissionsForChannel(CommandInterface source, object callContext, DiscordChannel channel)
        {
            await DisplayCommandData(source, callContext, $"Permission Verification Report for {channel.Name}", MessageBuilder.Shared.GetPermissionsReportForChannel(channel));
            return true;
        }

        #endregion

        #region Lookups

        public static async Task<bool> PlayerReport(CommandInterface source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.All);
            if (source == CommandInterface.Eco)
                await DisplayCommandData(source, callContext, $"Player report for {user}", MessageUtils.FormatEmbedForEco(report), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, string.Empty, report);
            return true;
        }

        public static async Task<bool> PlayerOnlineReport(CommandInterface source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.OnlineStatus);
            if (source == CommandInterface.Eco)
                await DisplayCommandData(source, callContext, $"Player online report for {user}", MessageUtils.FormatEmbedForEco(report), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, string.Empty, report);
            return true;
        }

        public static async Task<bool> PlayerTimeReport(CommandInterface source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.PlayTime);
            if (source == CommandInterface.Eco)
                await DisplayCommandData(source, callContext, $"Player time report for {user}", MessageUtils.FormatEmbedForEco(report), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, string.Empty, report);
            return true;
        }

        public static async Task<bool> PlayerPermissionsReport(CommandInterface source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.Permissions);
            if (source == CommandInterface.Eco)
                await DisplayCommandData(source, callContext, $"Player permissions report for {user}", MessageUtils.FormatEmbedForEco(report), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, string.Empty, report);
            return true;
        }

        public static async Task<bool> PlayerAccessReport(CommandInterface source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.AccessLists);
            if (source == CommandInterface.Eco)
                await DisplayCommandData(source, callContext, $"Player access report for {user}", MessageUtils.FormatEmbedForEco(report), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, string.Empty, report);
            return true;
        }

        public static async Task<bool> PlayerReputationReport(CommandInterface source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.Reputation);
            if (source == CommandInterface.Eco)
                await DisplayCommandData(source, callContext, $"Player reputation report for {user}", MessageUtils.FormatEmbedForEco(report), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, string.Empty, report);
            return true;
        }
        public static async Task<bool> PlayerXPReport(CommandInterface source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.Experience);
            if (source == CommandInterface.Eco)
                await DisplayCommandData(source, callContext, $"Player XP report for {user}", MessageUtils.FormatEmbedForEco(report), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, string.Empty, report);
            return true;
        }
        public static async Task<bool> PlayerSkillsReport(CommandInterface source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.Skills);
            if (source == CommandInterface.Eco)
                await DisplayCommandData(source, callContext, $"Player skills report for {user}", MessageUtils.FormatEmbedForEco(report), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, string.Empty, report);
            return true;
        }
        public static async Task<bool> PlayerDemographicsReport(CommandInterface source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.Demographics);
            if (source == CommandInterface.Eco)
                await DisplayCommandData(source, callContext, $"Player demographics report for {user}", MessageUtils.FormatEmbedForEco(report), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, string.Empty, report);
            return true;
        }
        public static async Task<bool> PlayerTitlesReport(CommandInterface source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.Titles);
            if (source == CommandInterface.Eco)
                await DisplayCommandData(source, callContext, $"Player titles report for {user}", MessageUtils.FormatEmbedForEco(report), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, string.Empty, report);
            return true;
        }
        public static async Task<bool> PlayerPropertiesReport(CommandInterface source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.Properties);
            if (source == CommandInterface.Eco)
                await DisplayCommandData(source, callContext, $"Player property report for {user}", MessageUtils.FormatEmbedForEco(report), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, string.Empty, report);
            return true;
        }

        public static async Task<bool> CurrencyReport(CommandInterface source, object callContext, string currencyNameOrID)
        {
            Currency currency = EcoUtils.CurrencyByNameOrID(currencyNameOrID);
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

            if (source == CommandInterface.Eco)
                await DisplayCommandData(source, callContext, $"Currency report for {currency}", MessageUtils.FormatEmbedForEco(report), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, $"Currency report for {currency}", report);
            return true;
        }

        public static async Task<bool> CurrenciesReport(CommandInterface source, object callContext, CurrencyType currencyType, int maxCurrenciesPerType, int holdersPerCurrency)
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

            IEnumerable<Currency> currencies = EcoUtils.Currencies;
            var currencyTradesMap = DLStorage.WorldData.CurrencyToTradeCountMap;
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
                    if (currencyEnumerator.Current.Creator == DiscordLink.Obj.EcoUser)
                        continue; // Ignore the bot currency

                    DiscordLinkEmbed currencyReport = MessageBuilder.Discord.GetCurrencyReport(currencyEnumerator.Current, holdersPerCurrency, useBackingInfo: true, useTradeCount: true);
                    if (currencyReport != null)
                        reports.Add(currencyReport);
                }
            }

            if (source == CommandInterface.Eco)
            {
                string fullReport = string.Join("\n\n", reports.Select(r => r.AsText()));
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

        public static async Task<bool> ElectionReport(CommandInterface source, object callContext, string electionNameOrID)
        {
            Election election = EcoUtils.ActiveElectionByNameOrID(electionNameOrID);
            if (election == null)
            {
                await ReportCommandError(source, callContext, $"No election with the name or ID \"{electionNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = MessageBuilder.Discord.GetElectionReport(election);
            if (source == CommandInterface.Eco)
                await DisplayCommandData(source, callContext, $"Election report for {election}", MessageUtils.FormatEmbedForEco(report), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, $"Election report for {election}", report);
            return true;
        }

        public static async Task<bool> ElectionsReport(CommandInterface source, object callContext)
        {
            IEnumerable<Election> elections = EcoUtils.ActiveElections;
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

            if (source == CommandInterface.Eco)
                await DisplayCommandData(source, callContext, $"Elections Report", string.Join("\n\n", reports.Select(r => r.AsText())), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, $"Elections Report", reports);

            return true;
        }

        public static async Task<bool> WorkPartyReport(CommandInterface source, object callContext, string workPartyNameOrID)
        {
            WorkParty workParty = EcoUtils.ActiveWorkPartyByNameOrID(workPartyNameOrID);
            if (workParty == null)
            {
                await ReportCommandError(source, callContext, $"No work party with the name or ID \"{workPartyNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = MessageBuilder.Discord.GetWorkPartyReport(workParty);
            if (source == CommandInterface.Eco)
                await DisplayCommandData(source, callContext, $"Work party report for {workParty}", MessageUtils.FormatEmbedForEco(report), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, $"Work party report for {workParty}", report);

            return true;
        }

        public static async Task<bool> WorkPartiesReport(CommandInterface source, object callContext)
        {
            IEnumerable<WorkParty> workParties = EcoUtils.ActiveWorkParties;
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

            if (source == CommandInterface.Eco)
                await DisplayCommandData(source, callContext, $"Work Parties Report", string.Join("\n\n", reports.Select(r => r.AsText())), DLConstants.ECO_PANEL_REPORT);
            else
                await DisplayCommandData(source, callContext, "Work Parties Report", reports);

            return true;
        }

        #endregion

        #region Invites

        public static async Task<bool> PostDiscordInvite(CommandInterface source, object callContext, string targetUserName)
        {
            DLConfigData config = DLConfig.Data;
            string discordAddress = NetworkManager.Config.DiscordAddress;
            string inviteMessage = config.InviteMessage;
            if (!inviteMessage.ContainsCaseInsensitive(DLConstants.INVITE_COMMAND_TOKEN) || string.IsNullOrEmpty(discordAddress))
            {
                await ReportCommandError(source, callContext, "This server is not configured for using Invite commands.");
                return false;
            }

            User recipient = null; // If no target user is specified; use broadcast
            if (!string.IsNullOrEmpty(targetUserName))
            {
                recipient = UserManager.FindUserByName(targetUserName);
                if (recipient == null)
                {
                    User offlineUser = EcoUtils.UserByName(targetUserName);
                    if (offlineUser != null)
                        await ReportCommandError(source, callContext, $"{MessageUtils.StripTags(offlineUser.Name)} is not online");
                    else
                        await ReportCommandError(source, callContext, $"Could not find user with name {targetUserName}");
                    return false;
                }
            }

            inviteMessage = Regex.Replace(inviteMessage, Regex.Escape(DLConstants.INVITE_COMMAND_TOKEN), discordAddress);

            bool sent = recipient == null
                ? EcoUtils.SendChatToDefaultChannel(inviteMessage)
                : EcoUtils.SendChatToUser(recipient, inviteMessage);

            if (sent)
                await ReportCommandInfo(source, callContext, "Invite sent.");
            else
                await ReportCommandError(source, callContext, "Failed to send invite.");

            return sent;
        }

        #endregion

        #region Trades

        public static async Task<bool> Trades(CommandInterface source, object callContext, string searchName)
        {
            if (string.IsNullOrWhiteSpace(searchName))
            {
                await ReportCommandInfo(source, callContext, "Please provide the name of a player, tag, item or store to search for.");
                return false;
            }

            string matchedName = TradeUtils.GetMatchAndOffers(searchName, out TradeTargetType offerType, out StoreOfferList groupedBuyOffers, out StoreOfferList groupedSellOffers);
            if (offerType == TradeTargetType.Invalid)
            {
                await ReportCommandError(source, callContext, $"No player, tag, item or store with the name \"{searchName}\" could be found.");
                return false;
            }

            if (source == CommandInterface.Eco)
            {
                MessageBuilder.Eco.FormatTrades(callContext as User, offerType, groupedBuyOffers, groupedSellOffers, out string message);
                await DisplayCommandData(source, callContext, matchedName, message, DLConstants.ECO_PANEL_DL_TRADES);
            }
            else
            {
                MessageBuilder.Discord.FormatTrades(matchedName, offerType, groupedBuyOffers, groupedSellOffers, out DiscordLinkEmbed embed);
                await DisplayCommandData(source, callContext, null, embed);
            }

            return true;
        }

        public static async Task<bool> AddTradeWatcher(CommandInterface source, object callContext, string searchName, Modules.ModuleArchetype type)
        {
            if (string.IsNullOrWhiteSpace(searchName))
            {
                await ReportCommandInfo(source, callContext, "Please provide the name of a player, tag, item or store to watch trades for.");
                return false;
            }

            LinkedUser linkedUser = source == CommandInterface.Eco
                ? UserLinkManager.LinkedUserByEcoUser(callContext as User, callContext as User, "Trade Watcher Registration")
                : UserLinkManager.LinkedUserByDiscordUser((callContext as InteractionContext).User, (callContext as InteractionContext).Member, "Trade Watcher Registration");
            if (linkedUser == null)
                return false;

            ulong discordID = ulong.Parse(linkedUser.DiscordID);
            if (type == Modules.ModuleArchetype.Display)
            {
                if(DLConfig.Data.MaxTradeWatcherDisplaysPerUser <= 0)
                {
                    await ReportCommandError(source, callContext, "Trade watcher displays are not enabled on this server.");
                    return false;
                }

                int watchedTradesCount = DLStorage.WorldData.GetTradeWatcherCountForUser(discordID);
                if (watchedTradesCount >= DLConfig.Data.MaxTradeWatcherDisplaysPerUser)
                {
                    await ReportCommandError(source, callContext, $"You are already watching {watchedTradesCount} trades and the limit is {DLConfig.Data.MaxTradeWatcherDisplaysPerUser} trade watcher displays per user.\nUse the `{MessageUtils.GetCommandTokenForContext(source)}DL-RemoveTradeWatcherDisplay` command to remove a trade watcher to make space if you wish to add a new one.");
                    return false;
                }
            }
            else if(!DLConfig.Data.UseTradeWatcherFeeds)
            {
                await ReportCommandError(source, callContext, "Trade watcher feeds are not enabled on this server.");
                return false;
            }

            string matchedName = TradeUtils.GetMatchAndOffers(searchName, out TradeTargetType offerType, out _, out _);
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
                await ReportCommandError(source, callContext, $"Failed to start watching trades for {matchedName}. \nUse `{MessageUtils.GetCommandTokenForContext(source)}DL-TradeWatchers` to see what is currently being watched.");
                return false;
            }
        }

        public static async Task<bool> RemoveTradeWatcher(CommandInterface source, object callContext, string searchName, Modules.ModuleArchetype type)
        {
            if (string.IsNullOrWhiteSpace(searchName))
            {
                await ReportCommandInfo(source, callContext, "Please provide the name of a player, tag, item or store to watch trades for.");
                return false;
            }

            LinkedUser linkedUser = source == CommandInterface.Eco
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
                await ReportCommandError(source, callContext, $"Failed to stop watching trades for {searchName}.\nUse `{MessageUtils.GetCommandTokenForContext(source)}DL-TradeWatchers` to see what is currently being watched.");
                return false;
            }
        }

        public static async Task<bool> ListTradeWatchers(CommandInterface source, object callContext)
        {
            LinkedUser linkedUser = source == CommandInterface.Eco
                ? UserLinkManager.LinkedUserByEcoUser(callContext as User, callContext as User, "Trade Watchers Listing")
                : UserLinkManager.LinkedUserByDiscordUser((callContext as InteractionContext).User, (callContext as InteractionContext).Member, "Trade Watchers Listing");
            if (linkedUser == null)
                return false;

            await ReportCommandInfo(source, callContext, $"Watched Trades\n{DLStorage.WorldData.ListTradeWatchers(ulong.Parse(linkedUser.DiscordID))}");
            return true;
        }

        #endregion

        #region Snippets

        public static async Task Snippet(CommandInterface source, object callContext, CommandInterface target, string userName, string snippetKey)
        {
            var snippets = DLStorage.Instance.Snippets;
            if (string.IsNullOrWhiteSpace(snippetKey)) // List all snippets if no key is given
            {
                if (snippets.Count > 0)
                    await DisplayCommandData(source, callContext, "", new DiscordLinkEmbed().AddField("Snippets", string.Join("\n", snippets.Keys)), DLConstants.ECO_PANEL_SIMPLE_LIST);
                else
                    await ReportCommandInfo(source, callContext, "There are no registered snippets.");
            }
            else
            {
                // Find and post the snippet requested by the user
                if (snippets.TryGetValue(snippetKey, out string snippetText))
                {
                    if (target == CommandInterface.Eco)
                    {
                        EcoUtils.SendChatToDefaultChannel($"{userName} invoked snippet \"{snippetKey}\"\n- - -\n{snippetText}\n- - -");
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

        #region Message Relaying

        public static async Task<bool> SendServerMessage(CommandInterface source, object callContext, string message, string recipientUserNameOrID)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                await ReportCommandError(source, callContext, "Message cannot be empty.");
                return false;
            }

            User recipient = null;
            if (!string.IsNullOrWhiteSpace(recipientUserNameOrID))
            {
                recipient = EcoUtils.OnlineUsers.FirstOrDefault(user => user.Name.EqualsCaseInsensitive(recipientUserNameOrID) || user.Id.ToString().EqualsCaseInsensitive(recipientUserNameOrID));
                if (recipient == null)
                {
                    await ReportCommandError(source, callContext, $"No online user with the name or ID \"{recipientUserNameOrID}\" could be found.");
                    return false;
                }
            }

            string senderName = source == CommandInterface.Eco
                ? (callContext as User).Name
                : (callContext as InteractionContext).Member.DisplayName;

            string formattedMessage = $"[{senderName}] {message}";
            bool sent = recipient == null
                ? EcoUtils.SendChatToDefaultChannel(formattedMessage)
                : EcoUtils.SendChatToUser(recipient, formattedMessage);

            if (sent)
                await ReportCommandInfo(source, callContext, "Message delivered.");
            else
                await ReportCommandError(source, callContext, "Failed to send message.");

            return sent;
        }

        public static async Task<bool> SendBoxMessage(EcoUtils.BoxMessageType type, CommandInterface source, object callContext, string message, string recipientUserNameOrID)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                await ReportCommandError(source, callContext, "Message cannot be empty.");
                return false;
            }

            User recipient = null;
            if (!string.IsNullOrWhiteSpace(recipientUserNameOrID))
            {
                recipient = EcoUtils.OnlineUsers.FirstOrDefault(user => user.Name.EqualsCaseInsensitive(recipientUserNameOrID) || user.Id.ToString().EqualsCaseInsensitive(recipientUserNameOrID));
                if (recipient == null)
                {
                    await ReportCommandError(source, callContext, $"No online user with the name or ID \"{recipientUserNameOrID}\" could be found.");
                    return false;
                }
            }

            string senderName = source == CommandInterface.Eco
                ? (callContext as User).Name
                : (callContext as InteractionContext).Member.DisplayName;

            bool sent = false;
            string formattedMessage = $"[{senderName}]\n\n{message}";
            switch (type)
            {
                case EcoUtils.BoxMessageType.Info:
                    sent = recipient == null
                        ? EcoUtils.SendInfoBoxToAll(message)
                        : EcoUtils.SendInfoBoxToUser(recipient, formattedMessage);
                    break;

                case EcoUtils.BoxMessageType.Warning:
                    sent = recipient == null
                        ? EcoUtils.SendWarningBoxToAll(message)
                        : EcoUtils.SendWarningBoxToUser(recipient, formattedMessage);
                    break;

                case EcoUtils.BoxMessageType.Error:
                    sent = recipient == null
                        ? EcoUtils.SendErrorBoxToAll(message)
                        : EcoUtils.SendErrorBoxToUser(recipient, formattedMessage);
                    break;
            }

            if (sent)
                await ReportCommandInfo(source, callContext, "Message delivered.");
            else
                await ReportCommandError(source, callContext, "Failed to send message.");

            return sent;
        }

        public static async Task<bool> SendPopup(CommandInterface source, object callContext, string message, string recipientUserNameOrID)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                await ReportCommandError(source, callContext, "Message cannot be empty.");
                return false;
            }

            User recipient = null;
            if (!string.IsNullOrWhiteSpace(recipientUserNameOrID))
            {
                recipient = EcoUtils.OnlineUsers.FirstOrDefault(user => user.Name.EqualsCaseInsensitive(recipientUserNameOrID) || user.Id.ToString().EqualsCaseInsensitive(recipientUserNameOrID));
                if (recipient == null)
                {
                    await ReportCommandError(source, callContext, $"No online user with the name or ID \"{recipientUserNameOrID}\" could be found.");
                    return false;
                }
            }

            string senderName = source == CommandInterface.Eco
                ? (callContext as User).Name
                : (callContext as InteractionContext).Member.DisplayName;

            string formattedMessage = $"[{senderName}]\n\n{message}";
            bool sent = recipient == null
                ? EcoUtils.SendOKBoxToAll(formattedMessage)
                : EcoUtils.SendOKBoxToUser(recipient, formattedMessage);

            if (sent)
                await ReportCommandInfo(source, callContext, "Message delivered.");
            else
                await ReportCommandError(source, callContext, "Failed to send message.");

            return sent;
        }

        public static async Task<bool> SendNotification(CommandInterface source, object callContext, string message, string recipientUserNameOrID, bool includeOfflineUsers)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                await ReportCommandError(source, callContext, "Message cannot be empty.");
                return false;
            }

            User recipient = null;
            if (!string.IsNullOrWhiteSpace(recipientUserNameOrID))
            {
                recipient = UserManager.Users.FirstOrDefault(x => x.Name.EqualsCaseInsensitive(recipientUserNameOrID) || x.Id.ToString().EqualsCaseInsensitive(recipientUserNameOrID));
                if (recipient == null)
                {
                    await ReportCommandError(source, callContext, $"No online user with the name or ID \"{recipientUserNameOrID}\" could be found.");
                    return false;
                }
            }

            string senderName = source == CommandInterface.Eco
                ? (callContext as User).Name
                : (callContext as InteractionContext).Member.DisplayName;

            string formattedMessage = $"[{senderName}]\n\n{message}";
            bool sent = true;
            if (includeOfflineUsers)
            {
                sent = recipient == null
                    ? EcoUtils.SendNotificationToAll(formattedMessage)
                    : EcoUtils.SendNotificationToUser(recipient, formattedMessage);
            }
            else
            {
                foreach (User user in UserManager.Users)
                {
                    if (!EcoUtils.SendNotificationToUser(user, formattedMessage))
                        sent = false;
                }
            }

            if (sent)
                await ReportCommandInfo(source, callContext, "Message delivered.");
            else
                await ReportCommandError(source, callContext, "Failed to send message.");

            return sent;
        }

        public static async Task<bool> SendInfoPanel(CommandInterface source, object callContext, string instance, string title, string message, string recipientUserNameOrID)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                await ReportCommandError(source, callContext, "Title cannot be empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                await ReportCommandError(source, callContext, "Message cannot be empty.");
                return false;
            }

            User recipient = null;
            if (!string.IsNullOrWhiteSpace(recipientUserNameOrID))
            {
                recipient = EcoUtils.OnlineUsers.FirstOrDefault(user => user.Name.EqualsCaseInsensitive(recipientUserNameOrID) || user.Id.ToString().EqualsCaseInsensitive(recipientUserNameOrID));
                if (recipient == null)
                {
                    await ReportCommandError(source, callContext, $"No online user with the name or ID \"{recipientUserNameOrID}\" could be found.");
                    return false;
                }
            }

            string senderName = source == CommandInterface.Eco
                ? (callContext as User).Name
                : (callContext as InteractionContext).Member.DisplayName;

            string formattedMessage = $"{message}\n\n[{senderName}]";
            bool sent = recipient == null
                ? EcoUtils.SendInfoPanelToAll(instance, title, formattedMessage)
                : EcoUtils.SendInfoPanelToUser(recipient, instance, title, formattedMessage);

            if (sent)
                await ReportCommandInfo(source, callContext, "Message delivered.");
            else
                await ReportCommandError(source, callContext, "Failed to send message.");

            return sent;
        }

        #endregion
    }
}
