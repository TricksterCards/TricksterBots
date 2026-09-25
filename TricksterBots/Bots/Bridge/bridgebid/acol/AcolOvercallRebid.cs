using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal class AcolOvercallRebid
    {
        public static void Interpret(InterpretedBid rebid)
        {
            var overcall = rebid.History[rebid.Index - 4];
            var advance = rebid.History[rebid.Index - 2];

            if (!rebid.bidIsDeclare)
            {
                if (rebid.bid != BidBase.Pass || advance.bidIsDeclare && advance.BidMessage == BidMessage.Forcing)
                    return;

                rebid.Points.Max = 15;
                rebid.Description = "Pass";
                return;
            }

            if (!overcall.bidIsDeclare)
                return;

            if (advance.bid == BidBase.Pass)
            {
                RebidAfterPass(overcall, rebid);
                return;
            }

            if (advance.bidIsDeclare)
            {
                if (advance.declareBid.suit != overcall.declareBid.suit && advance.declareBid.suit != Suit.Unknown)
                {
                    RebidAfterNewSuitAdvance(overcall, advance, rebid);
                }
                else if (advance.declareBid.suit == overcall.declareBid.suit)
                {
                    RebidAfterRaise(overcall, advance, rebid);
                }
                else if (advance.declareBid.suit == Suit.Unknown)
                {
                    RebidAfterNTAdvance(overcall, advance, rebid);
                }
            }
        }

        private static void RebidAfterPass(InterpretedBid overcall, InterpretedBid rebid)
        {
            var suit = rebid.declareBid.suit;
            if (suit == Suit.Unknown || rebid.declareBid.level > 4 || rebid.declareBid.level != rebid.LowestAvailableLevel(suit, true))
                return;

            var bidSuits = rebid.History.Where(b => b.bidIsDeclare).Select(b => b.declareBid.suit).ToList();

            if (suit == overcall.declareBid.suit)
            {
                rebid.Points.Min = 16;
                rebid.HandShape[suit].Min = 6;
                rebid.Description = $"Rebid {suit}; 6+ {suit} and extra values";
                //  with a 5+ card unbid suit, show it instead
                rebid.Validate = hand =>
                {
                    var counts = BasicBidding.CountsBySuit(hand);
                    return !SuitRank.stdSuits.Any(s => !bidSuits.Contains(s) && counts[s] >= 5);
                };
            }
            else if (!bidSuits.Contains(suit))
            {
                rebid.Points.Min = 16;
                rebid.HandShape[suit].Min = 5;
                rebid.Description = $"Second suit; 5+ {suit} and extra values";
            }
        }

        private static void RebidAfterNewSuitAdvance(InterpretedBid overcall, InterpretedBid advance, InterpretedBid rebid)
        {
            var lowestAvailableLevel = rebid.LowestAvailableLevel(rebid.declareBid.suit);

            if (rebid.declareBid.suit == advance.declareBid.suit)
            {
                //  support advancer's suit
                var minSupport = BridgeBot.IsMajor(rebid.declareBid.suit) ? 3 : 4;
                if (rebid.declareBid.level == lowestAvailableLevel)
                {
                    rebid.BidPointType = BidPointType.Dummy;
                    rebid.Points.Min = 12;
                    rebid.Points.Max = 15;
                    rebid.HandShape[rebid.declareBid.suit].Min = minSupport;
                    rebid.Description = $"Support {rebid.declareBid.suit}; {minSupport}+ {rebid.declareBid.suit}";
                }
                else if (rebid.declareBid.level == rebid.GameLevel)
                {
                    rebid.BidPointType = BidPointType.Dummy;
                    rebid.Points.Min = 16;
                    rebid.HandShape[rebid.declareBid.suit].Min = minSupport;
                    rebid.Description = "Sign-off at game";
                }
            }
            else if (rebid.declareBid.suit == overcall.declareBid.suit && rebid.declareBid.suit != Suit.Unknown)
            {
                //  rebid overcaller's own suit
                if (rebid.declareBid.level == lowestAvailableLevel)
                {
                    rebid.Points.Min = overcall.Points.Min;
                    rebid.Points.Max = 18;
                    rebid.HandShape[rebid.declareBid.suit].Min = 6;
                    rebid.Description = $"Rebid {rebid.declareBid.suit}; 6+ {rebid.declareBid.suit}";
                    rebid.Validate = hand =>
                    {
                        var counts = BasicBidding.CountsBySuit(hand);
                        var opponentSuits = rebid.History.Where(b => (rebid.Index - b.Index) % 2 == 1 && b.bidIsDeclare).Select(b => b.declareBid.suit).ToList();

                        //  prefer showing an unbid 4+ card side suit that can be bid without reversing
                        foreach (var s in SuitRank.stdSuits)
                        {
                            if (s == overcall.declareBid.suit || s == advance.declareBid.suit || opponentSuits.Contains(s))
                                continue;

                            if (counts[s] >= 4)
                            {
                                var level = rebid.LowestAvailableLevel(s);
                                if (level < rebid.declareBid.level || (level == rebid.declareBid.level && BridgeBot.suitRank[s] < BridgeBot.suitRank[overcall.declareBid.suit]))
                                    return false;
                            }
                        }

                        return true;
                    };
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 1)
                {
                    rebid.Points.Min = 16;
                    rebid.Points.Max = 18;
                    rebid.HandShape[rebid.declareBid.suit].Min = 6;
                    rebid.Description = $"Jump rebid; 6+ {rebid.declareBid.suit}";
                }
            }
            else if (rebid.declareBid.suit != Suit.Unknown)
            {
                var opponentSuits = rebid.History.Where(b => (rebid.Index - b.Index) % 2 == 1 && b.bidIsDeclare).Select(b => b.declareBid.suit).ToList();
                if (opponentSuits.Contains(rebid.declareBid.suit))
                    return;

                //  show a new suit
                if (rebid.declareBid.level == lowestAvailableLevel)
                {
                    if (overcall.declareBid.suit == Suit.Unknown || BridgeBot.suitRank[rebid.declareBid.suit] < BridgeBot.suitRank[overcall.declareBid.suit])
                    {
                        //  non-reversing new suit (12-18)
                        rebid.Points.Min = overcall.Points.Min;
                        rebid.Points.Max = 18;
                        rebid.HandShape[rebid.declareBid.suit].Min = 4;
                        rebid.Description = $"New suit; 4+ {rebid.declareBid.suit}";
                    }
                    else
                    {
                        //  reverse in a new suit (16-19)
                        rebid.Points.Min = 16;
                        rebid.Points.Max = 19;
                        rebid.BidMessage = BidMessage.Forcing;
                        rebid.HandShape[rebid.declareBid.suit].Min = 4;
                        rebid.HandShape[overcall.declareBid.suit].Min = 5;
                        rebid.Description = $"Reverse; 4+ {rebid.declareBid.suit} and 5+ {overcall.declareBid.suit}";
                    }
                }
            }
            else
            {
                //  rebid in notrump
                if (rebid.declareBid.level == lowestAvailableLevel)
                {
                    rebid.Points.Min = 15;
                    rebid.Points.Max = 18;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.IsBalanced = true;
                    rebid.Description = "Balanced with stoppers";
                }
            }
        }

        private static void RebidAfterRaise(InterpretedBid overcall, InterpretedBid advance, InterpretedBid rebid)
        {
            var lowestAvailableLevel = rebid.LowestAvailableLevel(rebid.declareBid.suit);

            if (rebid.declareBid.suit == overcall.declareBid.suit && rebid.declareBid.suit != Suit.Unknown)
            {
                if (rebid.declareBid.level == lowestAvailableLevel)
                {
                    if (rebid.History[rebid.Index - 1].bidIsDeclare)
                    {
                        //  competing after opponent bid over partner's raise
                        rebid.Points.Min = 10;
                        rebid.Points.Max = 16;
                        rebid.HandShape[rebid.declareBid.suit].Min = 5;
                        rebid.Description = $"Competitive rebid; 5+ {rebid.declareBid.suit}";
                    }
                    else if (rebid.declareBid.level < rebid.GameLevel)
                    {
                        //  inviting game after partner's single raise
                        rebid.Points.Min = 16;
                        rebid.Points.Max = 17;
                        rebid.HandShape[rebid.declareBid.suit].Min = 5;
                        rebid.Description = "Inviting game";
                    }
                }
                else if (rebid.declareBid.level == rebid.GameLevel)
                {
                    rebid.Points.Min = 18;
                    rebid.Points.Max = 21;
                    rebid.BidMessage = BidMessage.Signoff;
                    rebid.HandShape[rebid.declareBid.suit].Min = 5;
                    rebid.Description = "Sign-off at game";
                }
            }
        }

        private static void RebidAfterNTAdvance(InterpretedBid overcall, InterpretedBid advance, InterpretedBid rebid)
        {
            var lowestAvailableLevel = rebid.LowestAvailableLevel(rebid.declareBid.suit);

            if (rebid.declareBid.suit == overcall.declareBid.suit && rebid.declareBid.suit != Suit.Unknown && rebid.declareBid.level == lowestAvailableLevel)
            {
                rebid.Points.Min = overcall.Points.Min;
                rebid.Points.Max = 15;
                rebid.HandShape[rebid.declareBid.suit].Min = 6;
                rebid.Description = $"6+ {rebid.declareBid.suit}";
            }
        }
    }
}
