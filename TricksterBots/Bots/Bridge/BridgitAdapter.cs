using System;
using System.Collections.Generic;
using System.Linq;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    internal class BridgitAdapter
    {

        public static Dictionary<int, List<BidBase>> DescribeBidHistoryBySeat(SuggestBidState<BridgeOptions> state)
        {
            var game = new BridgeBidding.Game
            {
                Vulnerable = ToBridgitVulnerable(state.vulnerabilityBySeat)
            };
            var nSeats = state.players.Count;
            var playersInBidOrder = state.players.OrderBy(p => (p.Seat - state.dealerSeat + nSeats) % nSeats).ToList();
            for (var i = 0; i < playersInBidOrder[0].BidHistory.Count; i++)
            {
                foreach (var player in playersInBidOrder)
                {
                    if (player.BidHistory.Count > i)
                    {
                        game.Auction.Add(ToBridgitBid(player.BidHistory[i]));
                    }
                }
            }
            var biddingState = new BridgeBidding.BiddingState(game);
            var auction = biddingState.GetAuction();
            var history = new Dictionary<int, List<BidBase>>();
            for (var i = 0; i < auction.Count; i++)
            {
                var player = playersInBidOrder[i % 4];
                var bidValue = player.BidHistory[i / 4];
                var bid = new BidBase(bidValue)
                {
                    explanation = FormatBridgitDescription(auction[i].GetCallDescriptions())
                };
                if (!history.ContainsKey(player.Seat)) history[player.Seat] = new List<BidBase>();
                history[player.Seat].Add(bid);
            }
            return history;
        }

        public static List<BidBase> DescribeLegalBids(SuggestBidState<BridgeOptions> state)
        {
            var game = new BridgeBidding.Game
            {
                Vulnerable = ToBridgitVulnerable(state.vulnerabilityBySeat)
            };
            var nSeats = state.players.Count;
            var playersBySeat = state.players.OrderBy(p => (p.Seat - state.dealerSeat + nSeats) % nSeats).ToList();
            for (var i = 0; i < playersBySeat[0].BidHistory.Count; i++)
            {
                foreach (var player in playersBySeat)
                {
                    if (player.BidHistory.Count > i)
                    {
                        game.Auction.Add(ToBridgitBid(player.BidHistory[i]));
                    }
                }
            }
            var biddingState = new BridgeBidding.BiddingState(game);
            var legalCalls = biddingState.GetCallChoices();
            return legalCalls.Select(call =>
            {
                return new BidBase(FromBridgitBid(call.Key))
                {
                    explanation = FormatBridgitDescription(call.Value.GetCallDescriptions())
                };
            }).ToList();
        }
        private static BidExplanation FormatBridgitDescription(List<List<string>> descriptions)
        {
            var desc = descriptions.Select(d => string.Join(", ", d)).ToList();
            desc.Reverse();
            return new BidExplanation
            {
                Description = string.Join("\n", desc)
            };
        }

        private static int FromBridgitBid(BridgeBidding.Call call)
        {
            if (call is BridgeBidding.Pass)
                return BidBase.Pass;
            if (call is BridgeBidding.Double)
                return BridgeBid.Double;
            if (call is BridgeBidding.Redouble)
                return BridgeBid.Redouble;
            if (call is BridgeBidding.Bid bid)
                return new DeclareBid(bid.Level, FromBridgitSuit(bid.Strain));

            throw new Exception("Unable to convert from Bridgit bid");
        }

        private static Suit FromBridgitSuit(BridgeBidding.Strain suit)
        {
            switch (suit)
            {
                case BridgeBidding.Strain.Clubs:
                    return Suit.Clubs;
                case BridgeBidding.Strain.Diamonds:
                    return Suit.Diamonds;
                case BridgeBidding.Strain.Hearts:
                    return Suit.Hearts;
                case BridgeBidding.Strain.Spades:
                    return Suit.Spades;
                case BridgeBidding.Strain.NoTrump:
                    return Suit.Unknown;
            }

            throw new Exception("Unable to convert from Bridgit suit");
        }

        private static BridgeBidding.Call ToBridgitBid(int bidValue)
        {
            switch (bidValue)
            {
                case BidBase.Pass:
                    return BridgeBidding.Bid.Pass;
                case BridgeBid.Double:
                    return BridgeBidding.Bid.Double;
                case BridgeBid.Redouble:
                    return BridgeBidding.Bid.Redouble;
                default:
                    var db = new DeclareBid(bidValue);
                    return new BridgeBidding.Bid(db.level, ToBridgitSuit(db.suit));
            }
        }

        private static BridgeBidding.Strain ToBridgitSuit(Suit suit)
        {
            switch (suit)
            {
                case Suit.Clubs:
                    return BridgeBidding.Strain.Clubs;
                case Suit.Diamonds:
                    return BridgeBidding.Strain.Diamonds;
                case Suit.Hearts:
                    return BridgeBidding.Strain.Hearts;
                case Suit.Spades:
                    return BridgeBidding.Strain.Spades;
                case Suit.Unknown:
                    return BridgeBidding.Strain.NoTrump;
            }

            throw new Exception("Unable to convert to Bridgit suit");
        }

        private static BridgeBidding.Vulnerable ToBridgitVulnerable(IReadOnlyList<bool> vulnerableBySeat)
        {
            if (vulnerableBySeat.All(v => v))
                return BridgeBidding.Vulnerable.All;
            if (vulnerableBySeat.All(v => !v))
                return BridgeBidding.Vulnerable.None;
            if (vulnerableBySeat[0])
                return BridgeBidding.Vulnerable.NS;
            return BridgeBidding.Vulnerable.EW;
        }
    }
}
