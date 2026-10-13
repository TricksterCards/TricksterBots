using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal class MichaelsCuebid
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (bid.bidIsDeclare
                && bid.Index >= 2
                && bid.History[bid.Index - 2].BidConvention == BidConvention.MichaelsCuebid)
            {
                InterpretResponse(bid);
                return true;
            }

            if (bid.bidIsDeclare
                && bid.Index >= 4
                && bid.History[bid.Index - 4].BidConvention == BidConvention.MichaelsCuebid)
            {
                return InterpretRebid(bid);
            }

            return false;
        }

        public static void InterpretResponse(InterpretedBid bid)
        {
            var cuebid = bid.History[bid.Index - 2];
            if (BridgeBot.IsMajor(cuebid.declareBid.suit) && bid.declareBid.suit == Suit.Unknown &&
                (bid.declareBid.level == 2 || bid.declareBid.level == 4 && bid.History[bid.Index - 1].bidIsDeclare))
            {
                var otherMajor = cuebid.declareBid.suit == Suit.Hearts ? Suit.Spades : Suit.Hearts;
                bid.BidConvention = BidConvention.AskingForMinor;
                bid.BidMessage = BidMessage.Forcing;
                bid.HandShape[otherMajor].Max = 2;
                bid.Description = $"3+ {Suit.Diamonds} or 3+ {Suit.Clubs}, 0-2 {otherMajor}";
                bid.Validate = hand => hand.Count(c => c.suit == Suit.Diamonds) >= 3 || hand.Count(c => c.suit == Suit.Clubs) >= 3;
                //  TODO: validate when a 4NT ask over the opponents' game is worthwhile
                if (bid.declareBid.level == 4)
                    bid.Validate = hand => false;
            }
            else if (BridgeBot.IsMajor(cuebid.declareBid.suit) && bid.declareBid.suit == Suit.Clubs &&
                     bid.declareBid.level == bid.LowestAvailableLevel(Suit.Clubs, true) && bid.declareBid.level <= 4)
            {
                //  partner's minor is unknown, so the cheapest club bid asks partner to pass or correct to diamonds
                var otherMajor = cuebid.declareBid.suit == Suit.Hearts ? Suit.Spades : Suit.Hearts;
                bid.BidConvention = BidConvention.AskingForMinor;
                bid.HandShape[otherMajor].Max = 2;
                bid.HandShape[Suit.Clubs].Min = 3;
                bid.HandShape[Suit.Diamonds].Min = 3;
                bid.Description = $"Pass or correct; 3+ {Suit.Clubs} and 3+ {Suit.Diamonds}";
                //  when 2NT is available, prefer it to ask for partner's minor
                var twoNotrumpAvailable = bid.LowestAvailableLevel(Suit.Unknown, true) <= 2;
                bid.Validate = hand => !twoNotrumpAvailable;
            }
            else if (BridgeBot.IsMinor(cuebid.declareBid.suit) && BridgeBot.IsMajor(bid.declareBid.suit) && bid.declareBid.level == bid.LowestAvailableLevel(bid.declareBid.suit))
            {
                var otherMajor = bid.declareBid.suit == Suit.Hearts ? Suit.Spades : Suit.Hearts;
                bid.HandShape[bid.declareBid.suit].Min = 3;
                bid.Description = $"3+ {bid.declareBid.suit}";

                // bid the better major (longer suit, or more HCP if both suits are same length)
                bid.Validate = hand =>
                {
                    if (hand.Count(c => c.suit == bid.declareBid.suit) > hand.Count(c => c.suit == otherMajor))
                        return true;

                    if (hand.Count(c => c.suit == bid.declareBid.suit) < hand.Count(c => c.suit == otherMajor))
                        return false;

                    return BasicBidding.ComputeHighCardPoints(hand.Where(c => c.suit == bid.declareBid.suit).ToList())
                        >= BasicBidding.ComputeHighCardPoints(hand.Where(c => c.suit == otherMajor).ToList());
                };
            }
            else if (BridgeBot.IsMajor(cuebid.declareBid.suit) && BridgeBot.IsMajor(bid.declareBid.suit) && bid.declareBid.suit != cuebid.declareBid.suit && bid.declareBid.level == bid.LowestAvailableLevel(bid.declareBid.suit))
            {
                bid.HandShape[bid.declareBid.suit].Min = 3;
                bid.Description = $"3+ {bid.declareBid.suit}";
            }
        }

        public static bool InterpretRebid(InterpretedBid bid)
        {
            var cuebid = bid.History[bid.Index - 4];
            var advance = bid.History[bid.Index - 2];
            if (advance.BidConvention == BidConvention.AskingForMinor)
            {
                if (advance.declareBid.suit == Suit.Clubs)
                {
                    //  correct partner's pass-or-correct clubs to our diamonds
                    if (bid.declareBid.suit == Suit.Diamonds && bid.declareBid.level == bid.LowestAvailableLevel(Suit.Diamonds, true))
                    {
                        bid.HandShape[Suit.Diamonds].Min = 5;
                        bid.HandShape[Suit.Clubs].Max = 4;
                        bid.Description = $"5+ {Suit.Diamonds}";
                        return true;
                    }

                    bid.Validate = hand => false;
                    return true;
                }

                if (BridgeBot.IsMinor(bid.declareBid.suit) && bid.declareBid.level == bid.LowestAvailableLevel(bid.declareBid.suit, true))
                {
                    bid.HandShape[bid.declareBid.suit].Min = 5;
                    bid.Description = $"5+ {bid.declareBid.suit}";
                    return true;
                }
            }
            else if (advance.bidIsDeclare && BridgeBot.IsMajor(cuebid.declareBid.suit) && advance.declareBid.suit != Suit.Unknown &&
                     cuebid.HandShape[advance.declareBid.suit].Min >= 5 && advance.declareBid.level < advance.GameLevel &&
                     advance.declareBid.level > advance.LowestAvailableLevel(advance.declareBid.suit, true))
            {
                //  partner's jump in our known major is invitational: accept with a strong Michaels
                if (bid.declareBid.suit == advance.declareBid.suit && bid.declareBid.level == bid.GameLevel)
                {
                    bid.Points.Min = 16;
                    bid.BidMessage = BidMessage.Signoff;
                    bid.Description = "Accept invitation";
                    return true;
                }
            }
            else if (bid.declareBid.suit != Suit.Unknown
                && bid.declareBid.suit != cuebid.declareBid.suit
                && !bid.History[bid.Index - 1].bidIsDeclare
                && !bid.History[bid.Index - 2].bidIsDeclare
                && !bid.History[bid.Index - 3].bidIsDeclare)
            {
                // Ensure we don't get stuck playing in the cuebid suit if opponents double and partner passes
                bid.HandShape[bid.declareBid.suit].Min = 5;
                bid.Description = $"5+ {bid.declareBid.suit}";
                return true;
            }

            return false;
        }
    }
}
