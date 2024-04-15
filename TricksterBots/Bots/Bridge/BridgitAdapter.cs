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
            var (biddingState, playersInBidOrder) = ConvertState(state);
            var auction = biddingState.GetAuction();
            var history = new Dictionary<int, List<BidBase>>();
            for (var i = 0; i < auction.Count; i++)
            {
                var player = playersInBidOrder[i % 4];
                var bidValue = player.BidHistory[i / 4];
                var bid = new BidBase(bidValue)
                {
                    explanation = FormatBridgitDescription(auction[i])
                };
                if (!history.ContainsKey(player.Seat)) history[player.Seat] = new List<BidBase>();
                history[player.Seat].Add(bid);
            }
            return history;
        }

        public static List<BidBase> DescribeLegalBids(SuggestBidState<BridgeOptions> state)
        {
            var (biddingState, _) = ConvertState(state);
            var legalCalls = biddingState.GetCallChoices();
            return state.legalBids.Select(b =>
            {
                var match = legalCalls.FirstOrDefault(c => FromBridgitBid(c.Key) == b.value);
                if (match.Equals(default(KeyValuePair<BridgeBidding.Call, BridgeBidding.CallDetails>)))
                {
                    b.explanation = new BidExplanation
                    {
                        BidMessage = BidMessage.Invitational,
                        Description = "Unexpected bid"
                    };
                }
                else
                {
                    b.explanation = FormatBridgitDescription(match.Value);
                }
                return b;
            }).ToList();
        }

        private static (BridgeBidding.BiddingState state, List<PlayerBase> playersInBidOrder) ConvertState(SuggestBidState<BridgeOptions> state)
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
            return (new BridgeBidding.BiddingState(game), playersInBidOrder);
        }

        private static BidExplanation FormatBridgitDescription(BridgeBidding.CallDetails details)
        {
            var alert = string.Join(", ", details.Annotations.Where(a => a.Type == BridgeBidding.CallAnnotation.AnnotationType.Alert).Select(a => a.Text));
            var announce = string.Join(", ", details.Annotations.Where(a => a.Type == BridgeBidding.CallAnnotation.AnnotationType.Announce).Select(a => a.Text));
            var convention = string.Join(", ", details.Annotations.Where(a => a.Type == BridgeBidding.CallAnnotation.AnnotationType.Convention).Select(a => a.Text));
            var descriptions = details.GetCallDescriptions();
            var lines = descriptions.Select(d => string.Join(", ", d)).ToList();
            lines.Reverse();
            var description = string.Join("\n", lines);

            if (string.IsNullOrEmpty(description))
                description = "Unknown bid";

            return new BidExplanation
            {
                Alert = alert,
                Announce = announce,
                BidMessage = FromBridgitForcing(details),
                Convention = convention,
                Description = description,
                Role = details.PositionState.Role.ToString(),
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

        private static BidMessage FromBridgitForcing(BridgeBidding.CallDetails details)
        {
            if (details.Properties == null)
                return BidMessage.Invitational;
            if (details.Properties.Forcing1Round)
                return BidMessage.Forcing;
            if (details.Properties.ForcingToGame)
                return BidMessage.GameForcing;

            // TODO: Signoff?

            return BidMessage.Invitational;
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
