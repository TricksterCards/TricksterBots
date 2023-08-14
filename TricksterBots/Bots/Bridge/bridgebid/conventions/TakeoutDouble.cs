using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal class TakeoutDouble
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (bid.BidPhase == BidPhase.Overcall && bid.bid == BridgeBid.Double)
            {
                //  we overcalled with a double - check if it is a takeout double
                InterpretedBid opening;
                InterpretedBid response = null;
                if (bid.History[bid.Index - 1].BidPhase == BidPhase.Opening)
                {
                    opening = bid.History[bid.Index - 1];
                }
                else
                {
                    opening = bid.History[bid.Index - 3];
                    response = bid.History[bid.Index - 1];
                }

                return Overcall(opening, response, bid);
            }

            if (bid.BidPhase == BidPhase.Response && bid.History[bid.Index - 1].BidConvention == BidConvention.TakeoutDouble)
                return Response(bid.History[bid.Index - 2], bid);
            if (bid.Index >= 2 && bid.History[bid.Index - 2].BidConvention == BidConvention.TakeoutDouble)
            {
                Advance(bid);
                return true;
            }

            if (bid.Index >= 4 && bid.History[bid.Index - 4].BidConvention == BidConvention.TakeoutDouble)
            {
                var advance = bid.History[bid.Index - 2];
                if (advance.bidIsDeclare)
                {
                    Rebid(advance, bid);
                    return true;
                }
            }

            return false;
        }

        private static void Advance(InterpretedBid advance)
        {
            if (!advance.bidIsDeclare)
                return;

            //  TODO: consider passing if RHO bid
            var opening = advance.History.First(b => b.bid != BidBase.Pass);
            var lowestAvailableLevel = advance.LowestAvailableLevel(advance.declareBid.suit);

            //  in notrump (with strength in the opponents' suit and no better option)
            if (advance.declareBid.suit == Suit.Unknown)
            {
                if (advance.declareBid.level < 4)
                {
                    advance.IsBalanced = true;
                    advance.Description = $"stopper in {opening.declareBid.suit}";
                    advance.Validate = hand => BasicBidding.HasStopper(hand, opening.declareBid.suit);
                }

                if (advance.declareBid.level == lowestAvailableLevel)
                {
                    //  6-10 points: bid notrump at the cheapest level
                    advance.Points.Min = 6;
                    advance.Points.Max = 10;
                }
                else if (advance.declareBid.level == lowestAvailableLevel + 1)
                {
                    //  11-12 points: bid notrump, jumping a level
                    advance.Points.Min = 11;
                    advance.Points.Max = 12;
                }
                else if (advance.declareBid.level == 3)
                {
                    //  13+ points: bid game in notrump
                    advance.Points.Min = 13;
                }
                else if (advance.declareBid.level == 4)
                {
                    advance.BidConvention = BidConvention.Blackwood;
                    advance.Points.Min = 20;
                    advance.Validate = hand => false;
                }
            }
            //  in a suit
            else
            {
                var gameLevel = BridgeBot.IsMajor(advance.declareBid.suit) ? 4 : 5;

                if (advance.declareBid.suit == opening.declareBid.suit)
                {
                    //  cuebid
                    advance.BidConvention = BidConvention.Cuebid;
                    advance.BidMessage = BidMessage.Forcing;
                    advance.Points.Min = 10;
                    advance.Description = "asking for more information";
                    advance.Validate = hand => false;
                }
                else if (advance.declareBid.level == lowestAvailableLevel && advance.declareBid.level <= 2)
                {
                    //  0-8 points: bid at the cheapest level
                    advance.Points.Max = 8;
                    advance.HandShape[advance.declareBid.suit].Min = 4;
                    advance.Description = $"4+ {advance.declareBid.suit}";
                }
                else if (advance.declareBid.level == lowestAvailableLevel + 1 && advance.declareBid.level <= 3)
                {
                    //  9-11 points: make an invitational bid by jumping a level
                    advance.Points.Min = 9;
                    advance.Points.Max = 11;
                    advance.HandShape[advance.declareBid.suit].Min = 4;
                    advance.Description = $"4+ {advance.declareBid.suit}; inviting game";
                }
                else if (advance.declareBid.level == gameLevel - 1)
                {
                    //  4-8 points: make a preemptive bid below game with 6+ cards
                    advance.Points.Min = 4;
                    advance.Points.Max = 8;
                    advance.HandShape[advance.declareBid.suit].Min = 6;
                    advance.IsPreemptive = true;
                    advance.Description = $"6+ {advance.declareBid.suit}";
                }
                else if (advance.declareBid.level == gameLevel)
                {
                    //  12+ points: get the partnership to game
                    var minCards = BridgeBot.IsMajor(advance.declareBid.suit) ? 4 : 5;
                    advance.Points.Min = 12;
                    advance.HandShape[advance.declareBid.suit].Min = minCards;
                    advance.Description = $"{minCards}+ {advance.declareBid.suit}";
                }
            }
        }

        private static bool Overcall(InterpretedBid opening, InterpretedBid response, InterpretedBid overcall)
        {
            //  a double is for takeout over an opening partscore bid (4D or lower)
            if (overcall.LowestAvailableLevel(Suit.Hearts) > 4)
                return false;

            var bidSuits = SuitRank.stdSuits.Where(s =>
                opening.bidIsDeclare && opening.declareBid.suit == s || response != null && response.bidIsDeclare && response.declareBid.suit == s);
            var unbidSuits = SuitRank.stdSuits.Where(s => !bidSuits.Contains(s));

            //  handle takeout doubles worth 13+ dummy points
            //  shows 4+ support in unbid suits and 0-2 shortness in bid suits
            overcall.Points.Min = 13;
            overcall.BidPointType = BidPointType.Dummy;
            overcall.BidConvention = BidConvention.TakeoutDouble;
            overcall.BidMessage = BidMessage.Forcing;
            foreach (var s in bidSuits) overcall.HandShape[s].Max = 2;
            foreach (var s in unbidSuits) overcall.HandShape[s].Min = 4;
            overcall.Description = "4+ cards in every unbid suit";
            overcall.AlternateMatches = hand =>
            {
                //  if we can bid 1NT (balanced; 15-18 HCP) we'll defer to that instead
                var hcp = BasicBidding.ComputeHighCardPoints(hand);
                if (15 <= hcp && hcp <= 18 && overcall.LowestAvailableLevel(Suit.Unknown, true) == 1 && BasicBidding.IsBalanced(hand))
                    return false;

                //  otherwise a takeout double can also show 18+ points (too strong for a simple overcall)
                var points = hcp + BasicBidding.ComputeDistributionPoints(hand);
                return 18 <= points;
            };

            return true;
        }

        private static void Rebid(InterpretedBid advance, InterpretedBid rebid)
        {
            if (!rebid.bidIsDeclare)
                return;

            var opening = rebid.History.First(b => b.bid != BidBase.Pass);

            //  TODO: should this be level limited?
            if (rebid.declareBid.suit == opening.declareBid.suit)
            {
                rebid.BidConvention = BidConvention.Cuebid;
                rebid.BidMessage = BidMessage.Forcing;
                rebid.Points.Min = 19;
                rebid.Description = "asking for more information";
                //  TODO: should the bot sometimes bid this?
                rebid.Validate = hand => false;
            }
            else if (rebid.declareBid.suit == advance.declareBid.suit)
            {
                //  TODO: combine our points with strength shown by advancer to decide how to bid
                //var advancerSummary = new InterpretedBid.PlayerSummary(advance.History, advance.Index % 4);
            }
            else if (rebid.declareBid.suit == Suit.Unknown)
            {
                //  TODO: what do we do here?
            }
            else if (rebid.declareBid.level == rebid.LowestAvailableLevel(rebid.declareBid.suit))
            {
                //  new suit at lowest available level shows 18+ points and 5+ cards
                rebid.Points.Min = 18;
                rebid.HandShape[rebid.declareBid.suit].Min = 5;
                rebid.Description = $"5+ {rebid.declareBid.suit}";
            }
        }

        private static bool Response(InterpretedBid opening, InterpretedBid response)
        {
            //  TODO: handle other changes to response meanings due to the takeout double overcall
            if (response.bid != BridgeBid.Redouble)
                return false;

            //  a redouble shows 10+ points over a takeout double
            response.Points.Min = 10;
            response.BidPointType = BidPointType.Hcp;
            response.Description = "Good hand";

            return true;
        }
    }
}