using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal class NegativeDouble
    {
        public static bool CanUseAfter(InterpretedBid opening, InterpretedBid overcall)
        {
            //  negative doubles only apply after a suit opening (a double of an overcall of 1NT is penalty)
            if (!opening.bidIsDeclare || opening.declareBid.suit == Suit.Unknown)
                return false;

            //  the negative double is used through 2S over a suited overcall to show support for unbid major(s)
            if (!overcall.bidIsDeclare || overcall.declareBid.suit == Suit.Unknown || overcall.declareBid.level > 2)
                return false;

            //  if we're in-range, make sure we have at least one unbid major
            return DetermineUnbidMajors(opening, overcall).Length > 0;
        }

        public static bool Interpret(InterpretedBid bid)
        {
            if (bid.BidPhase == BidPhase.Response && bid.bid == BridgeBid.Double)
                //  we responded with a double - check if it is a negative double and interpret appropriately
                return Response(bid.History[bid.Index - 2], bid.History[bid.Index - 1], bid);

            if (bid.BidPhase == BidPhase.OpenerRebid && bid.History[bid.Index - 2].BidConvention == BidConvention.NegativeDouble &&
                bid.Options.bidding != BridgeBiddingScheme.Acol)
                return OpenerRebid(bid.History[bid.Index - 4], bid.History[bid.Index - 3], bid.History[bid.Index - 2], bid);

            if (bid.Index >= 4 && bid.History[bid.Index - 4].BidConvention == BidConvention.NegativeDouble)
            {
                var overcall = bid.History[bid.Index - 5];
                var openerRebid = bid.History[bid.Index - 2];
                if (openerRebid.bidIsDeclare) return ResponderRebid(overcall, openerRebid, bid);
            }

            return false;
        }

        private static Suit[] DetermineUnbidMajors(InterpretedBid opening, InterpretedBid overcall)
        {
            var bidSuits = new[] { opening.declareBid.suit, overcall.declareBid.suit };
            return SuitRank.stdSuits.Where(BridgeBot.IsMajor).Where(s => !bidSuits.Contains(s)).ToArray();
        }

        private static bool OpenerRebid(InterpretedBid opening, InterpretedBid overcall, InterpretedBid negativeDouble, InterpretedBid rebid)
        {
            var theirSuit = overcall.declareBid.suit;
            var shownMajors = SuitRank.stdSuits.Where(s => BridgeBot.IsMajor(s) && negativeDouble.HandShape[s].Min >= 4).ToList();

            if (rebid.bid == BidBase.Pass)
            {
                //  converting partner's negative double needs length and strength in their suit
                if (rebid.History[rebid.Index - 1].bid != BidBase.Pass)
                    return false;

                rebid.Description = $"Penalty pass; 5+ {theirSuit}";
                rebid.Validate = hand => BasicBidding.CountsBySuit(hand)[theirSuit] >= 5 && BasicBidding.IsGoodSuit(hand, theirSuit, 5);
                return true;
            }

            if (!rebid.bidIsDeclare)
                return false;

            var suit = rebid.declareBid.suit;
            var level = rebid.declareBid.level;
            var lowestAvailableLevel = rebid.LowestAvailableLevel(suit, true);

            if (suit == theirSuit && level == lowestAvailableLevel)
            {
                //  cuebid: game-forcing (usually with support for partner's major)
                rebid.BidConvention = BidConvention.Cuebid;
                rebid.BidMessage = BidMessage.GameForcing;
                rebid.BidPointType = BidPointType.Hcp;
                rebid.Points.Min = 17;
                rebid.Priority = 90;
                rebid.Description = "Game forcing";
                rebid.Validate = hand => shownMajors.Any(m => BasicBidding.CountsBySuit(hand)[m] >= 4);
                return true;
            }

            if (shownMajors.Contains(suit))
            {
                //  support partner's major: minimum at the cheapest level, a jump with 16-18, game with 19+ (or cuebid)
                rebid.BidPointType = BidPointType.Hcp;
                rebid.HandShape[suit].Min = 4;
                if (level == lowestAvailableLevel)
                {
                    rebid.Points.Max = 15;
                    rebid.Description = $"Minimum; 4+ {suit}";
                }
                else if (level == lowestAvailableLevel + 1 && level < rebid.GameLevel)
                {
                    rebid.Points.Min = 16;
                    rebid.Points.Max = 18;
                    rebid.Description = $"Jump raise; 4+ {suit}";
                }
                else if (level == rebid.GameLevel)
                {
                    rebid.Points.Min = 19;
                    rebid.BidMessage = BidMessage.Signoff;
                    rebid.Description = $"Game; 4+ {suit}";
                }
                else
                {
                    rebid.Validate = hand => false;
                }

                return true;
            }

            if (suit == opening.declareBid.suit && level == lowestAvailableLevel + 1 && level <= 3)
            {
                //  jump rebid of our long suit (without support for partner's major)
                rebid.BidPointType = BidPointType.Hcp;
                rebid.Points.Min = 16;
                rebid.Points.Max = 19;
                rebid.HandShape[suit].Min = 6;
                foreach (var m in shownMajors)
                    rebid.HandShape[m].Max = 3;
                rebid.Description = $"Jump rebid; 6+ {suit}";
                return true;
            }

            return false;
        }

        private static bool ResponderRebid(InterpretedBid overcall, InterpretedBid openerRebid, InterpretedBid rebid)
        {
            if (!rebid.bidIsDeclare || rebid.Options.bidding == BridgeBiddingScheme.Acol || rebid.History[rebid.Index - 1].bid != BidBase.Pass)
                return false;

            var theirSuit = overcall.declareBid.suit;
            var opener = new InterpretedBid.PlayerSummary(rebid.History, rebid.Index - 2);
            var openerMin = System.Math.Max(opener.Points.Min, 13);
            var openerMax = System.Math.Min(opener.Points.Max, 21);
            var suit = rebid.declareBid.suit;
            var level = rebid.declareBid.level;

            if (suit == theirSuit && level == rebid.LowestAvailableLevel(suit, true))
            {
                //  cuebid: game force, with slam interest if a fit is known (otherwise asking for a stopper)
                var fitSuit = openerRebid.declareBid.suit;
                rebid.BidConvention = BidConvention.Cuebid;
                rebid.BidMessage = BidMessage.GameForcing;
                rebid.Priority = 90;
                rebid.Description = "Game forcing";
                if (fitSuit != Suit.Unknown && rebid.History[rebid.Index - 4].HandShape[fitSuit].Min >= 4)
                {
                    rebid.BidPointType = BidPointType.Dummy;
                    rebid.Points.Min = InterpretedBid.SmallSlamPoints - openerMax;
                }
                else
                {
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.Points.Min = 25 - openerMin;
                    rebid.Validate = hand => !BasicBidding.HasStopper(hand, theirSuit);
                }

                return true;
            }

            if (suit == Suit.Unknown && level == 3)
            {
                //  3NT with game values and a stopper in their suit (but prefer a known major fit)
                var openerMajor = BridgeBot.IsMajor(openerRebid.declareBid.suit) ? openerRebid.declareBid.suit : Suit.Unknown;
                rebid.BidPointType = BidPointType.Hcp;
                rebid.Points.Min = 25 - openerMin;
                rebid.BidMessage = BidMessage.Signoff;
                rebid.Priority = 95;
                rebid.Description = $"Sign-off at game; stopper in {theirSuit}";
                rebid.Validate = hand => BasicBidding.HasStopper(hand, theirSuit) &&
                                         (openerMajor == Suit.Unknown || BasicBidding.CountsBySuit(hand)[openerMajor] < 4);
                return true;
            }

            return false;
        }

        private static bool Response(InterpretedBid opening, InterpretedBid overcall, InterpretedBid response)
        {
            if (!CanUseAfter(opening, overcall))
                return false;

            //  we have a negative double
            var unbidMajors = DetermineUnbidMajors(opening, overcall);
            response.Points.Min = overcall.declareBid.level == 1 ? 6 : overcall.declareBid.level == 2 ? 8 : 10;
            response.BidPointType = BidPointType.Hcp;
            //  at the 2-level, a long side suit adds playing strength
            if (overcall.declareBid.level == 2)
                response.PointCounter = hand => BasicBidding.ComputeHighCardPoints(hand) + SuitRank.stdSuits
                    .Where(s => !unbidMajors.Contains(s)).Select(s => BasicBidding.CountsBySuit(hand)[s]).Where(n => n >= 5).Sum(n => n - 4);
            response.BidConvention = BidConvention.NegativeDouble;
            response.BidMessage = BidMessage.Forcing;
            foreach (var s in unbidMajors) response.HandShape[s].Min = 4;
            if (unbidMajors.Length == 1 && unbidMajors[0] == Suit.Spades) response.HandShape[Suit.Spades].Max = 4;
            response.Description = unbidMajors.Length == 1 ? $"4 {unbidMajors[0]}" : "4-4 or better in Hearts & Spades";

            return true;
        }
    }
}