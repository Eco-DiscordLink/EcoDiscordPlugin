using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using StoreOfferList = System.Collections.Generic.IEnumerable<System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.Store.StoreComponent, Eco.Gameplay.Components.TradeOffer>>>;
using static Eco.Moose.Features.Trade;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class TradeWatcherDisplay : DisplayModule
    {
        protected override string BaseTag { get { return "[Trade Watcher Display]"; } }
        protected override int TimerUpdateIntervalMS { get { return 300000; } }
        protected override int TimerStartDelayMS { get { return 10000; } }

        private readonly List<DiscordTarget> UserLinks = new List<DiscordTarget>();

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

        public override string ToString()
        {
            return "Trade Watcher Display";
        }

        protected override DLEventType GetTriggers()
        {
            return base.GetTriggers() | DLEventType.DiscordClientConnected | DLEventType.Timer | DLEventType.TradeWatcherDisplayAdded | DLEventType.TradeWatcherDisplayRemoved;
        }

        protected override async Task<bool> ShouldRun()
        {
            return await base.ShouldRun() && (DLConfig.Data.MaxTradeWatcherDisplaysPerUser > 0);
        }

        protected override async Task<List<DiscordTarget>> GetDiscordTargets()
        {
            if (UserLinks.Count != DLStorage.WorldData.TradeWatcherDisplayCountTotal)
                await BuildUserLinkList();

            return UserLinks;
        }

        protected override void GetDisplayContent(DiscordTarget target, out List<Tuple<string, DiscordLinkEmbed>> tagAndContent)
        {
            tagAndContent = new List<Tuple<string, DiscordLinkEmbed>>();
            IEnumerable<string> tradeWatchers = DLStorage.WorldData.TradeWatchers[(target as UserLink).Member.Id].Where(watcher => watcher.Type == ModuleArchetype.Display).Select(watcher => watcher.Key);
            foreach (string trade in tradeWatchers)
            {
                string matchedName = Moose.Features.Trade.FindOffers(trade, out TradeTargetType offerType, out StoreOfferList groupedBuyOffers, out StoreOfferList groupedSellOffers);
                if (offerType == TradeTargetType.Invalid)
                    continue; // There was no match

                MessageBuilder.Discord.FormatTrades(matchedName, offerType, groupedBuyOffers, groupedSellOffers, out DiscordLinkEmbed embedContent);
                tagAndContent.Add(new Tuple<string, DiscordLinkEmbed>($"{BaseTag} [{matchedName}]", embedContent));
            }
        }

        private async Task BuildUserLinkList()
        {
            using (await _overlapLock.LockAsync())
            {
                UserLinks.Clear();

                foreach (ulong discordUserId in DLStorage.WorldData.DisplayTradeWatchers.Select(userAndWatchers => userAndWatchers.Key))
                {
                    LinkedUser linkedUser = UserLinkManager.LinkedUserByDiscordID(discordUserId);
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
                int removeIndex = UserLinks.FindIndex(u => (u as UserLink).Member.Id == ulong.Parse(user.DiscordID));
                if (removeIndex >= 0)
                    UserLinks.RemoveAt(removeIndex);
            }
        }

        private async Task OnTradeWatcherAdded(object sender, EventArgs e, TradeWatcherEntry watcher)
        {
            if (!await HandleStartOrStop()) // If the module wasn't started by this, there is already an update on the way
                await Update(DiscordLink.Obj, DLEventType.TradeWatcherDisplayAdded, watcher);
        }

        private async Task OnTradeWatcherRemoved(object sender, EventArgs e, TradeWatcherEntry watcher)
        {
            await Update(DiscordLink.Obj, DLEventType.TradeWatcherDisplayRemoved, watcher);
            await HandleStartOrStop();
        }
    }
}
