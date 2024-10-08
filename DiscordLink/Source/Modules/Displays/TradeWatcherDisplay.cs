using DSharpPlus.Entities;
using Eco.Moose.Data;
using Eco.Moose.Features;
using Eco.Moose.Tools.Logger;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Eco.Moose.Data.Enums;
using static Eco.Moose.Features.Trade;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class TradeWatcherDisplay : DisplayModule
    {
        protected override string BaseTag { get { return "[Trade Watcher Display]"; } }
        protected override int TimerUpdateIntervalMs { get { return 300000; } }
        protected override int TimerStartDelayMs { get { return 10000; } }

        private readonly List<DiscordTarget> UserLinks = new List<DiscordTarget>();

        public override string ToString() => "Trade Watcher Display";
        protected override DlEventType GetTriggers() => base.GetTriggers() | DlEventType.DiscordClientConnected // Not triggering on AccumulatedTrade messages as this could result in a lot of calls
            | DlEventType.Timer | DlEventType.TradeWatcherDisplayAdded | DlEventType.TradeWatcherDisplayRemoved;

        protected override async Task<bool> ShouldRun()
        {
            return await base.ShouldRun() && (DLConfig.Data.MaxTradeWatcherDisplaysPerUser > 0);
        }

        public override string GetDisplayText(string childInfo, bool verbose)
        {
            string info = $"Trade Watcher Displays: {DLStorage.WorldData.TradeWatcherDisplayCountTotal}";
            return base.GetDisplayText($"{childInfo}{info}\r\n", verbose);
        }

        public override void Setup()
        {
            UserLinkManager.OnLinkedUserRemoved += HandleLinkedUserRemoved;
            DLStorage.TradeWatcherAdded += OnTradeWatcherAdded;
            DLStorage.TradeWatcherRemoved += OnTradeWatcherRemoved;

            base.Setup();
        }

        public override void Destroy()
        {
            UserLinkManager.OnLinkedUserRemoved -= HandleLinkedUserRemoved;
            DLStorage.TradeWatcherAdded -= OnTradeWatcherAdded;
            DLStorage.TradeWatcherRemoved -= OnTradeWatcherRemoved;

            base.Destroy();
        }

        protected override async Task Initialize()
        {
            await BuildUserLinkList();
            await base.Initialize();
        }

        protected override async Task<IEnumerable<DiscordTarget>> GetDiscordTargets()
        {
            if (UserLinks.Count != DLStorage.WorldData.TradeWatcherDisplayCountTotal)
                await BuildUserLinkList();

            return UserLinks;
        }

        protected override void GetDisplayContent(DiscordTarget target, out List<DisplayContent> displayContent)
        {
            displayContent = new List<DisplayContent>();
            IEnumerable<string> tradeWatchers = DLStorage.WorldData.TradeWatchers[(target as UserLink).Member.Id].Where(watcher => watcher.Type == ModuleArchetype.Display).Select(watcher => watcher.Key);
            foreach (string trade in tradeWatchers)
            {
                LookupResult lookupRes = DynamicLookup.Lookup(trade, Constants.TRADE_LOOKUP_MASK);
                if (lookupRes.Result != LookupResultTypes.SingleMatch)
                {
                    Logger.Warning($"Trade watcher lookup yielded unexpected result: {Enum.GetName(typeof(LookupResultTypes), lookupRes.Result)}");
                    continue;
                }
                object matchedEntity = lookupRes.Matches.First();
                LookupTypes matchedEntityType = lookupRes.MatchedTypes;
                string matchedEntityName = DynamicLookup.GetEntityName(matchedEntity);

                TradeOfferList offerList = FindOffers(matchedEntity, matchedEntityType);
                MessageBuilder.Discord.FormatTrades(matchedEntityName, matchedEntityType, offerList, out DiscordLinkEmbed embed);
                displayContent.Add(new DisplayContent($"{BaseTag} [{matchedEntityName}]", embedContent: embed));
            }
        }

        private async Task BuildUserLinkList()
        {
            using (await _overlapLock.LockAsync())
            {
                UserLinks.Clear();

                foreach (ulong discordUserId in DLStorage.WorldData.DisplayTradeWatchers.Select(userAndWatchers => userAndWatchers.Key))
                {
                    LinkedUser linkedUser = UserLinkManager.LinkedUserByDiscordId(discordUserId);
                    if (linkedUser == null)
                        continue;

                    DiscordMember member = linkedUser.DiscordMember;
                    if (member != null)
                        UserLinks.Add(new UserLink(member));
                }
            }
        }

        private void HandleLinkedUserRemoved(object sender, LinkedUser user)
        {
            using (_overlapLock.Lock())
            {
                int removeIndex = UserLinks.FindIndex(u => (u as UserLink).Member.Id == ulong.Parse(user.DiscordId));
                if (removeIndex >= 0)
                    UserLinks.RemoveAt(removeIndex);
            }
        }

        private async Task OnTradeWatcherAdded(object sender, EventArgs e, TradeWatcherEntry watcher)
        {
            if (!await HandleStartOrStop()) // If the module wasn't started by this, there is already an update on the way
                await Update(DiscordLink.Obj, DlEventType.TradeWatcherDisplayAdded, watcher);
        }

        private async Task OnTradeWatcherRemoved(object sender, EventArgs e, TradeWatcherEntry watcher)
        {
            await Update(DiscordLink.Obj, DlEventType.TradeWatcherDisplayRemoved, watcher);
            await HandleStartOrStop();
        }
    }
}
