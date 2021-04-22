using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.Economy;
using Eco.Gameplay.Economy.WorkParties;
using Eco.Gameplay.Players;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Networking;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using StoreOfferList = System.Collections.Generic.IEnumerable<System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.StoreComponent, Eco.Gameplay.Components.TradeOffer>>>;

namespace Eco.Plugins.DiscordLink
{
    /**
     * Platform independent implementations of commands used in both Discord and Eco.
     */
    public static class SharedCommands
    {
        #region Commands Base

        public enum CommandSource
        {
            Eco,
            Discord
        }

        private static async Task ReportCommandError(CommandSource source, object callContext, string message)
        {
            if (source == CommandSource.Eco)
                EcoCommands.ReportCommandError(callContext as User, message);
            else
                await DiscordCommands.ReportCommandError(callContext as CommandContext, message);
        }

        private static async Task ReportCommandInfo(CommandSource source, object callContext, string message)
        {
            if (source == CommandSource.Eco)
                EcoCommands.ReportCommandInfo(callContext as User, message);
            else
                await DiscordCommands.ReportCommandInfo(callContext as CommandContext, message);
        }

        private static async Task DisplayCommandData(CommandSource source, object callContext, string title, object data)
        {
            if (source == CommandSource.Eco)
                EcoCommands.DisplayCommandData(callContext as User, title, data as string);
            else
                await DiscordCommands.DisplayCommandData(callContext as CommandContext, title, data as DiscordLinkEmbed);
        }

        #endregion

        #region Plugin Management

        public static async Task<bool> Restart(CommandSource source, object callContext)
        {
            DiscordLink plugin = DiscordLink.Obj;
            Logger.Info("Restart command executed - Restarting");
            bool restarted = plugin.Restart().Result;

            string result;
            if (restarted)
            {
                result = "Restarting...";
                await ReportCommandInfo(source, callContext, result);
            }
            else
            {
                result = "Restart failed or a restart was already in progress";
                await ReportCommandError(source, callContext, result);
            }

            Logger.Info(result);
            return restarted;
        }

        public static async Task<bool> ResetWorldData(CommandSource source, object callContext)
        {
            Logger.Info("ResetWorldData command invoked - Resetting world storage data");
            DLStorage.Instance.Reset();
            await ReportCommandInfo(source, callContext, "World storage data has been reset.");
            return true;
        }

        #endregion

        #region Message Relaying

        public static async Task<bool> SendServerMessage(CommandSource source, object callContext, string message, string recipientUserName, string persistanceType)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                await ReportCommandError(source, callContext, "Message cannot be empty.");
                return false;
            }

            bool permanent;
            if (persistanceType.EqualsCaseInsensitive("temporary"))
            {
                permanent = true;
            }
            else if (persistanceType.EqualsCaseInsensitive("permanent"))
            {
                permanent = false;
            }
            else
            {
                await ReportCommandError(source, callContext, "Persistance type must either be \"Temporary\" or \"Permanent\".");
                return false;
            }

            User recipient = null;
            if (!string.IsNullOrWhiteSpace(recipientUserName))
            {
                recipient = UserManager.OnlineUsers.FirstOrDefault(x => x.Name.EqualsCaseInsensitive(recipientUserName));
                if (recipient == null)
                {
                    await ReportCommandError(source, callContext, $"No online user with the name \"{recipientUserName}\" could be found.");
                    return false;
                }
            }

            string senderName = source == CommandSource.Eco
                ? (callContext as User).Name
                : (callContext as CommandContext).Member.DisplayName;

            bool sent = EcoUtils.SendServerMessage($"[{senderName}] {message}", permanent, recipient);
            if (sent)
                await ReportCommandInfo(source, callContext, "Message delivered.");
            else
                await ReportCommandError(source, callContext, "Failed to send message.");

            return sent;
        }

        public static async Task<bool> SendPopup(CommandSource source, object callContext, string message, string recipientUserName)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                await ReportCommandError(source, callContext, "Message cannot be empty.");
                return false;
            }

            User recipient = null;
            if (!string.IsNullOrWhiteSpace(recipientUserName))
            {
                recipient = UserManager.OnlineUsers.FirstOrDefault(x => x.Name.EqualsCaseInsensitive(recipientUserName));
                if (recipient == null)
                {
                    await ReportCommandError(source, callContext, $"No online user with the name \"{recipientUserName}\" could be found.");
                    return false;
                }
            }

            string senderName = source == CommandSource.Eco
                ? (callContext as User).Name
                : (callContext as CommandContext).Member.DisplayName;

            bool sent = EcoUtils.SendPopupMessage($"[{senderName}]\n\n{message}", recipient);
            if (sent)
                await ReportCommandInfo(source, callContext, "Message delivered.");
            else
                await ReportCommandError(source, callContext, "Failed to send message.");

            return sent;
        }

        public static async Task<bool> SendAnnouncement(CommandSource source, object callContext, string title, string message, string recipientUserName)
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
            if (!string.IsNullOrWhiteSpace(recipientUserName))
            {
                recipient = UserManager.OnlineUsers.FirstOrDefault(x => x.Name.EqualsCaseInsensitive(recipientUserName));
                if (recipient == null)
                {
                    await ReportCommandError(source, callContext, $"No online user with the name \"{recipientUserName}\" could be found.");
                    return false;
                }
            }

            string senderName = source == CommandSource.Eco
                ? (callContext as User).Name
                : (callContext as CommandContext).Member.DisplayName;

            bool sent = EcoUtils.SendAnnouncementMessage(title, $"{message}\n\n[{senderName}]", recipient);
            if (sent)
                await ReportCommandInfo(source, callContext, "Message delivered.");
            else
                await ReportCommandError(source, callContext, "Failed to send message.");

            return sent;
        }

        #endregion

        #region Lookups

        public static async Task<bool> PlayerReport(CommandSource source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if(user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.All);
            if (source == CommandSource.Eco)
                await DisplayCommandData(source, callContext, $"Player report for {user}", report.AsText());
            else
                await DisplayCommandData(source, callContext, $"Player report for {user}", report);
            return true;
        }

        public static async Task<bool> PlayerOnlineReport(CommandSource source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.OnlineStatus);
            if (source == CommandSource.Eco)
                await DisplayCommandData(source, callContext, $"Player online report for {user}", report.AsText());
            else
                await DisplayCommandData(source, callContext, $"Player online report for {user}", report);
            return true;
        }

        public static async Task<bool> PlayerTimeReport(CommandSource source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.PlayTime);
            if (source == CommandSource.Eco)
                await DisplayCommandData(source, callContext, $"Player time report for {user}", report.AsText());
            else
                await DisplayCommandData(source, callContext, $"Player time report for {user}", report);
            return true;
        }

        public static async Task<bool> PlayerPermissionsReport(CommandSource source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.Permissions);
            if (source == CommandSource.Eco)
                await DisplayCommandData(source, callContext, $"Player permissions report for {user}", report.AsText());
            else
                await DisplayCommandData(source, callContext, $"Player permissions report for {user}", report);
            return true;
        }

        public static async Task<bool> PlayerAccessReport(CommandSource source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.AccessLists);
            if (source == CommandSource.Eco)
                await DisplayCommandData(source, callContext, $"Player access report for {user}", report.AsText());
            else
                await DisplayCommandData(source, callContext, $"Player access report for {user}", report);
            return true;
        }

        public static async Task<bool> PlayerReputationReport(CommandSource source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.Reputation);
            if (source == CommandSource.Eco)
                await DisplayCommandData(source, callContext, $"Player reputation report for {user}", report.AsText());
            else
                await DisplayCommandData(source, callContext, $"Player reputation report for {user}", report);
            return true;
        }
        public static async Task<bool> PlayerXPReport(CommandSource source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.Experience);
            if (source == CommandSource.Eco)
                await DisplayCommandData(source, callContext, $"Player XP report for {user}", report.AsText());
            else
                await DisplayCommandData(source, callContext, $"Player XP report for {user}", report);
            return true;
        }
        public static async Task<bool> PlayerSkillsReport(CommandSource source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.Skills);
            if (source == CommandSource.Eco)
                await DisplayCommandData(source, callContext, $"Player skills report for {user}", report.AsText());
            else
                await DisplayCommandData(source, callContext, $"Player skills report for {user}", report);
            return true;
        }
        public static async Task<bool> PlayerDemographicsReport(CommandSource source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.Demographics);
            if (source == CommandSource.Eco)
                await DisplayCommandData(source, callContext, $"Player demographics report for {user}", report.AsText());
            else
                await DisplayCommandData(source, callContext, $"Player demographics report for {user}", report);
            return true;
        }
        public static async Task<bool> PlayerTitlesReport(CommandSource source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.Titles);
            if (source == CommandSource.Eco)
                await DisplayCommandData(source, callContext, $"Player titles report for {user}", report.AsText());
            else
                await DisplayCommandData(source, callContext, $"Player titles report for {user}", report);
            return true;
        }
        public static async Task<bool> PlayerPropertiesReport(CommandSource source, object callContext, string playerNameOrID)
        {
            User user = EcoUtils.UserByNameOrEcoID(playerNameOrID);
            if (user == null)
            {
                await ReportCommandError(source, callContext, $"No player with the name or ID \"{playerNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = await MessageBuilder.Discord.GetPlayerReport(user, MessageBuilder.PlayerReportComponentFlag.Properties);
            if (source == CommandSource.Eco)
                await DisplayCommandData(source, callContext, $"Player property report for {user}", report.AsText());
            else
                await DisplayCommandData(source, callContext, $"Player property report for {user}", report);
            return true;
        }

        public static async Task<bool> CurrencyReport(CommandSource source, object callContext, string currencyNameOrID)
        {
            Currency currency = EcoUtils.CurrencyByNameOrID(currencyNameOrID);
            if (currency == null)
            {
                await ReportCommandError(source, callContext, $"No currency with the name or ID \"{currencyNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = MessageBuilder.Discord.GetCurrencyReport(currency, DLConfig.DefaultValues.MaxTopCurrencyHolderCount);
            if (report == null)
            {
                await ReportCommandError(source, callContext, $"Could not create a report for {currency} as no one holds this currency and no trades has been made with it.");
                return false;
            }

            if(source == CommandSource.Eco)
                await DisplayCommandData(source, callContext, $"Currency report for {currency}", report.AsText());
            else
                await DisplayCommandData(source, callContext, $"Currency report for {currency}", report);
            return true;
        }

        public static async Task<bool> CurrenciesReport(CommandSource source, object callContext, string currencyType, string maxCurrenciesPerTypeStr, string holdersPerCurrencyStr)
        {
            if (!currencyType.EqualsCaseInsensitive("all") && !currencyType.EqualsCaseInsensitive("minted") && !currencyType.EqualsCaseInsensitive("personal"))
            {
                await ReportCommandError(source, callContext, "The CurrencyType parameter must be \"All\", \"Personal\" or \"Minted\".");
                return false;
            }
            if(int.TryParse(maxCurrenciesPerTypeStr, out int maxCurrenciesPerType) || maxCurrenciesPerType <= 0)
            {
                await ReportCommandError(source, callContext, "The MaxCurrenciesPerType parameter must be a positive number.");
                return false;
            }
            if (int.TryParse(holdersPerCurrencyStr, out int holdersPerCurrency) || holdersPerCurrency <= 0)
            {
                await ReportCommandError(source, callContext, "The HoldersPerCurrency parameter must be a positive number.");
                return false;
            }

            bool useMinted = currencyType.EqualsCaseInsensitive("all") || currencyType.EqualsCaseInsensitive("minted");
            bool usePersonal = currencyType.EqualsCaseInsensitive("all") || currencyType.EqualsCaseInsensitive("personal");

            IEnumerable<Currency> currencies = EcoUtils.Currencies;
            var currencyTradesMap = DLStorage.WorldData.CurrencyToTradeCountMap;
            List<DiscordLinkEmbed> reports = new List<DiscordLinkEmbed>();

            if (useMinted)
            {
                IEnumerable<Currency> mintedCurrencies = currencies.Where(c => c.Backed).OrderByDescending(c => currencyTradesMap.Keys.Contains(c.Id) ? currencyTradesMap[c.Id] : 0);
                var currencyEnumerator = mintedCurrencies.GetEnumerator();
                for (int i = 0; i < maxCurrenciesPerType && currencyEnumerator.MoveNext(); ++i)
                {
                    DiscordLinkEmbed currencyReport = MessageBuilder.Discord.GetCurrencyReport(currencyEnumerator.Current, holdersPerCurrency);
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

                    DiscordLinkEmbed currencyReport = MessageBuilder.Discord.GetCurrencyReport(currencyEnumerator.Current, holdersPerCurrency);
                    if (currencyReport != null)
                        reports.Add(currencyReport);
                }
            }

            if (source == CommandSource.Eco)
            {
                string fullReport = string.Join("\n\n", reports.Select(r => r.AsText()));
                await DisplayCommandData(source, callContext, $"Currencies Report", fullReport);
            }
            else
            {
                foreach (DiscordLinkEmbed report in reports)
                {
                    await DisplayCommandData(source, callContext, $"Currency report for {report.Title}", report);
                }
            }

            return true;
        }

        public static async Task<bool> ElectionReport(CommandSource source, object callContext, string electionNameOrID)
        {
            Election election = EcoUtils.ActiveElectionByNameOrID(electionNameOrID);
            if(election == null)
            {
                await ReportCommandError(source, callContext, $"No election with the name or ID \"{electionNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = MessageBuilder.Discord.GetElectionReport(election);
            if (source == CommandSource.Eco)
                await DisplayCommandData(source, callContext, $"Election report for {election}", report.AsText());
            else
                await DisplayCommandData(source, callContext, $"Election report for {election}", report);
            return true;
        }

        public static async Task<bool> ElectionsReport(CommandSource source, object callContext)
        {
            IEnumerable<Election> elections = EcoUtils.ActiveElections;
            if(elections.Count() <= 0)
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

            if(reports.Count() <= 0)
            {
                await ReportCommandInfo(source, callContext, "None of the active elections have a voting option.");
                return false;
            }

            if (source == CommandSource.Eco)
            {
                string fullReport = string.Join("\n\n", reports.Select(r => r.AsText()));
                await DisplayCommandData(source, callContext, $"Elections Report", fullReport);
            }
            else
            {
                foreach (DiscordLinkEmbed report in reports)
                {
                    await DisplayCommandData(source, callContext, $"Election report for {report.Title}", report);
                }
            }
            return true;
        }

        public static async Task<bool> WorkPartyReport(CommandSource source, object callContext, string workPartyNameOrID)
        {
            WorkParty workParty = EcoUtils.ActiveWorkPartyByNameOrID(workPartyNameOrID);
            if (workParty == null)
            {
                await ReportCommandError(source, callContext, $"No work party with the name or ID \"{workPartyNameOrID}\" could be found.");
                return false;
            }

            DiscordLinkEmbed report = MessageBuilder.Discord.GetWorkPartyReport(workParty);
            if (source == CommandSource.Eco)
                await DisplayCommandData(source, callContext, $"Work party report for {workParty}", report.AsText());
            else
                await DisplayCommandData(source, callContext, $"Work party report for {workParty}", report);
            return true;
        }

        public static async Task<bool> WorkPartiesReport(CommandSource source, object callContext)
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

            if (source == CommandSource.Eco)
            {
                string fullReport = string.Join("\n\n", reports.Select(r => r.AsText()));
                await DisplayCommandData(source, callContext, $"Work Parties Report", fullReport);
            }
            else
            {
                foreach (DiscordLinkEmbed report in reports)
                {
                    await DisplayCommandData(source, callContext, $"Work Party report for {report.Title}", report);
                }
            }
            return true;
        }

        #endregion

        #region Invites

        public static async Task<bool> DiscordInvite(CommandSource source, object callContext, string targetUserName)
        {
            DLConfigData config = DLConfig.Data;
            ServerInfo serverInfo = Networking.NetworkManager.GetServerInfo();

            string inviteMessage = config.InviteMessage;
            if (!inviteMessage.ContainsCaseInsensitive(DLConstants.INVITE_COMMAND_TOKEN) || string.IsNullOrEmpty(serverInfo.DiscordAddress))
            {
                await ReportCommandError(source, callContext, "This server is not configured for using the Invite commands.");
                return false;
            }

            User targetUser = null; // If no target user is specified; use broadcast
            if (!string.IsNullOrEmpty(targetUserName))
            {
                targetUser = UserManager.FindUserByName(targetUserName);

                if (targetUser == null)
                {
                    User offlineUser = EcoUtils.UserByName(targetUserName);
                    if (offlineUser != null)
                        await ReportCommandError(source, callContext, $"{MessageUtils.StripTags(offlineUser.Name)} is not online");
                    else
                        await ReportCommandError(source, callContext, $"Could not find user with name {targetUserName}");
                    return false;
                }
            }

            inviteMessage = Regex.Replace(inviteMessage, Regex.Escape(DLConstants.INVITE_COMMAND_TOKEN), serverInfo.DiscordAddress);

            bool sent = EcoUtils.SendServerMessage(inviteMessage, permanent: true, targetUser);
            if (sent)
                await ReportCommandInfo(source, callContext, "Invite sent.");
            else
                await ReportCommandError(source, callContext, "Failed to send invite.");

            return sent;
        }

        #endregion

        #region Trades

        public static async Task<bool> Trades(CommandSource source, object callContext, string searchName)
        {
            string matchedName = string.Empty;

            if (string.IsNullOrWhiteSpace(searchName))
            {
                await ReportCommandInfo(source, callContext, "Please provide the name of an item, a tag or a player to search for.");
                return false;
            }

            matchedName = TradeUtils.GetMatchAndOffers(searchName, out TradeTargetType offerType, out StoreOfferList groupedBuyOffers, out StoreOfferList groupedSellOffers);
            if(offerType == TradeTargetType.Invalid)
            {
                await ReportCommandError(source, callContext, $"No item, tag or player with the name \"{searchName}\" could be found.");
                return false;
            }

            if(source == CommandSource.Eco)
            {
                MessageBuilder.Eco.FormatTrades(offerType, groupedBuyOffers, groupedSellOffers, out string message);
                await DisplayCommandData(source, callContext, matchedName, message);
            }
            else
            {
                MessageBuilder.Discord.FormatTrades(matchedName, offerType, groupedBuyOffers, groupedSellOffers, out DiscordLinkEmbed embed);
                await DisplayCommandData(source, callContext, matchedName, embed);
            }

            return true;
        }

        public static async Task<bool> TrackTrades(CommandSource source, object callContext, string userOrItemName)
        {
            LinkedUser linkedUser = source == CommandSource.Eco
                ? LinkedUserManager.LinkedUserByEcoUser(callContext as User)
                : LinkedUserManager.LinkedUserByDiscordID((callContext as CommandContext).Member.Id);
            if (linkedUser == null)
            {
                await ReportCommandError(source, callContext, $"You have not linked your Discord Account to DiscordLink on this Eco Server.\nUse the `\\DL-link` command to initialize account linking.");
                return false;
            }

            int trackedTradesCount = DLStorage.WorldData.GetTrackedTradesCountForUser(ulong.Parse(linkedUser.DiscordID));
            if (trackedTradesCount >= DLConfig.Data.MaxTrackedTradesPerUser)
            {
                await ReportCommandError(source, callContext, $"You are already tracking {trackedTradesCount} trades and the limit is {DLConfig.Data.MaxTrackedTradesPerUser} tracked trades per user.\nUse the `\\DL-StopTrackTrades` command to remove a tracked trade to make space if you wish to add a new one.");
                return false;
            }

            string matchedName = TradeUtils.GetMatchAndOffers(userOrItemName, out TradeTargetType offerType, out StoreOfferList groupedBuyOffers, out StoreOfferList groupedSellOffers);
            if (offerType == TradeTargetType.Invalid)
                return false;

            bool added = DLStorage.WorldData.AddTrackedTradeItem(ulong.Parse(linkedUser.DiscordID), matchedName).Result;
            if (added)
            {
                await ReportCommandInfo(source, callContext, $"Tracking all trades for {matchedName}.");
                return true;
            }
            else
            {
                await ReportCommandError(source, callContext, $"Failed to start tracking trades for {matchedName}.");
                return false;
            }
        }

        public static async Task<bool> StopTrackTrades(CommandSource source, object callContext, string userOrItemName)
        {
            LinkedUser linkedUser = source == CommandSource.Eco
                ? LinkedUserManager.LinkedUserByEcoUser(callContext as User)
                : LinkedUserManager.LinkedUserByDiscordID((callContext as CommandContext).Member.Id);
            if (linkedUser == null)
            {
                await ReportCommandError(source, callContext, $"You have not linked your Discord Account to DiscordLink on this Eco Server.\nLog into the game and use the `\\DL-link` command to initialize account linking.");
                return false;
            }

            bool removed = DLStorage.WorldData.RemoveTrackedTradeItem(ulong.Parse(linkedUser.DiscordID), userOrItemName).Result;
            if (removed)
            {
                await ReportCommandInfo(source, callContext, $"Stopped tracking trades for {userOrItemName}.");
                return true;
            }
            else
            {
                await ReportCommandError(source, callContext, $"Failed to stop tracking trades for {userOrItemName}.\nUse `\\DL-ListTrackedStores` to see what is currently being tracked.");
                return false;
            }
        }

        public static async Task<bool> ListTrackedTrades(CommandSource source, object callContext)
        {
            LinkedUser linkedUser = source == CommandSource.Eco
                ? LinkedUserManager.LinkedUserByEcoUser(callContext as User)
                : LinkedUserManager.LinkedUserByDiscordID((callContext as CommandContext).Member.Id);
            if (linkedUser == null)
            {
                await ReportCommandError(source, callContext, $"You have not linked your Discord Account to DiscordLink on this Eco Server.\nLog into the game and use the `\\DL-link` command to initialize account linking.");
                return false;
            }

            await DisplayCommandData(source, callContext, "Tracked Trades", DLStorage.WorldData.ListTrackedTrades(ulong.Parse(linkedUser.DiscordID)));
            return true;
        }

        #endregion
    }
}
