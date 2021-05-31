using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using StoreOfferList = System.Collections.Generic.IEnumerable<System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.StoreComponent, Eco.Gameplay.Components.TradeOffer>>>;
using Eco.Plugins.DiscordLink.Extensions;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class TradeTrackerDisplay : Display
    {
        protected override string BaseTag { get { return "[Trade Tracker]"; } }
        protected override int TimerUpdateIntervalMS { get { return 300000; } }
        protected override int TimerStartDelayMS { get { return 10000; } }

        private List<DiscordTarget> UserLinks = new List<DiscordTarget>();

        public override string GetDisplayText(string childInfo, bool verbose)
        {
            string info = $"Tracked Trades: {DLStorage.WorldData.GetTrackedTradesCountTotal()}";
            return base.GetDisplayText($"{childInfo}{info}\r\n", verbose);
        }

        public override void Setup()
        {
            LinkedUserManager.OnLinkedUserRemoved += HandleLinkedUserRemoved;
            DLStorage.TrackedTradeAdded += OnTrackedTradeAdded;
            DLStorage.TrackedTradeRemoved += OnTrackedTradeRemoved;

            base.Setup();
        }

        public override void Destroy()
        {
            LinkedUserManager.OnLinkedUserRemoved -= HandleLinkedUserRemoved;
            DLStorage.TrackedTradeAdded -= OnTrackedTradeAdded;
            DLStorage.TrackedTradeRemoved -= OnTrackedTradeRemoved;

            base.Destroy();
        }

        protected override async Task Initialize()
        {
            await BuildUserLinkList();
            await base.Initialize();
        }

        public override string ToString()
        {
            return "Trade Tracker Display";
        }

        protected override DLEventType GetTriggers()
        {
            return base.GetTriggers() | DLEventType.DiscordClientStarted | DLEventType.Timer | DLEventType.TrackedTradeAdded | DLEventType.TrackedTradeRemoved;
        }

        protected override List<DiscordTarget> GetDiscordTargets()
        {
            if (UserLinks.Count < DLStorage.WorldData.PlayerTrackedTrades.Keys.Count())
                BuildUserLinkList().Wait();

            return UserLinks;
        }

        protected override void GetDisplayContent(DiscordTarget target, out List<Tuple<string, DiscordLinkEmbed>> tagAndContent)
        {
            tagAndContent = new List<Tuple<string, DiscordLinkEmbed>>();
            List<string> trackedTrades = DLStorage.WorldData.PlayerTrackedTrades[(target as UserLink).Member.Id];
            foreach(string trade in trackedTrades)
            {
                string matchedName = TradeUtils.GetMatchAndOffers(trade, out TradeTargetType offerType, out StoreOfferList groupedBuyOffers, out StoreOfferList groupedSellOffers);
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

                foreach (ulong discordUserId in DLStorage.WorldData.PlayerTrackedTrades.Keys)
                {
                    LinkedUser dlUser = LinkedUserManager.LinkedUserByDiscordID(discordUserId);
                    if (dlUser == null)
                        continue;

                    DiscordGuild guild = DiscordLink.Obj.Client.GuildByNameOrID(dlUser.GuildID);
                    if (guild == null)
                        continue;

                    DiscordMember member = await guild.GetMemberAsync(discordUserId);
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

        private async Task OnTrackedTradeAdded(object sender, EventArgs e, string tradeItem)
        {
            if (!await HandleStartOrStop()) // If the module wasn't started by this, there is already an update on the way
                await Update(DiscordLink.Obj, DLEventType.TrackedTradeAdded, null);
        }

        private async Task OnTrackedTradeRemoved(object sender, EventArgs e, string tradeItem)
        {
            await Update(DiscordLink.Obj, DLEventType.TrackedTradeRemoved, null);
            await HandleStartOrStop();
        }
    }
}
