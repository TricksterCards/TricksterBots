using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    public class BridgeBidHistory
    {
        private List<int> bids = new List<int>();

        public BridgeBidHistory()
        {
        }

        public BridgeBidHistory(IReadOnlyCollection<PlayerBase> players, int dealerSeat)
        {
            //  in our implementation, the first bidder is the dealer
            var seat = dealerSeat;
            for (var round = 0;; ++round)
                do
                {
                    var pl = players.Single(p => p.Seat == seat);

                    if (pl.BidHistory.Count > round)
                        bids.Add(pl.BidHistory[round]);
                    else
                        return; // we're done when we hit a player without a bid this round

                    seat = (seat + 1) % players.Count;
                } while (seat != dealerSeat);
        }

        public BridgeBidHistory(IEnumerable<int> history)
        {
            bids.AddRange(history);
        }

        public int Count => bids.Count;

        public int this[int i]
        {
            get
            {
                if (0 <= i && i < bids.Count)
                    return bids[i];

                return i < 0 ? this[bids.Count + i] : BidBase.Pass;
            }
        }

        public bool AreAllBids(int bid)
        {
            return bids.All(b => b == bid);
        }

        public bool AreAllTeamBids(int bid)
        {
            for (var i = bids.Count - 2; i >= 0; i -= 2)
                if (bids[i] != bid)
                    return false;

            return true;
        }

        public BridgeBidHistory CopyUpTo(int bidIndex)
        {
            return new BridgeBidHistory { bids = bids.Take(bidIndex < 0 ? bids.Count + bidIndex : bidIndex).ToList() };
        }

        public static int FirstBidIndex(IReadOnlyCollection<PlayerBase> players, PlayerBase player, int dealerSeat)
        {
            var nPlayers = players.Count;
            return (nPlayers - dealerSeat + player.Seat) % nPlayers;
        }

        public bool IsBidLegal(int value)
        {
            if (value == BidBase.Pass)
                return true;

            if (DeclareBid.Is(value))
                return IsDeclareBidLegal(new DeclareBid(value));

            var lastBidAndIndex = bids.Select((b, i) => new { bid = b, index = i })
                .LastOrDefault(bi => bi.bid != BidBase.Pass);

            if (lastBidAndIndex == null || lastBidAndIndex.index == bids.Count - 2)
                return false;

            if (value == BridgeBid.Double && DeclareBid.Is(lastBidAndIndex.bid))
                return true;

            if (value == BridgeBid.Redouble && lastBidAndIndex.bid == BridgeBid.Double)
                return true;

            return false;
        }

        public bool IsBidLegal(BidBase bid)
        {
            return IsBidLegal(bid.value);
        }

        public bool IsBidLegal(int level, Suit suit)
        {
            return IsDeclareBidLegal(new DeclareBid(level, suit));
        }

        public bool IsDeclareBidLegal(DeclareBid db)
        {
            var lastDeclareBid = bids.Where(DeclareBid.Is).DefaultIfEmpty(BidBase.NoBid).Last();

            //  if there's no declare bid in the history, then all declare bids are legal
            if (lastDeclareBid == BidBase.NoBid)
                return true;

            //  otherwise, the incoming bid must have a higher level or the same level with a higher suit
            var lastDB = new DeclareBid(lastDeclareBid);
            return db.level > lastDB.level || db.level == lastDB.level && BridgeBot.suitRank[db.suit] > BridgeBot.suitRank[lastDB.suit];
        }
    }
}