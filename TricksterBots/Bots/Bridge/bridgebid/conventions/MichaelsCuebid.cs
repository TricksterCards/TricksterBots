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
                InterpretRebid(bid);
                return true;
            }

            return false;
        }

        public static void InterpretResponse(InterpretedBid bid)
        {
            var cuebid = bid.History[bid.Index - 2];
            if (BridgeBot.IsMajor(cuebid.declareBid.suit) && bid.declareBid.level == 2 && bid.declareBid.suit == Suit.Unknown)
            {
                var otherMajor = cuebid.declareBid.suit == Suit.Hearts ? Suit.Spades : Suit.Hearts;
                bid.BidConvention = BidConvention.AskingForMinor;
                bid.BidMessage = BidMessage.Forcing;
                bid.HandShape[otherMajor].Max = 2;
                bid.Description = $"3+ {Suit.Diamonds} or 3+ {Suit.Clubs}, 0-2 {otherMajor}";
                bid.Validate = hand => hand.Count(c => c.suit == Suit.Diamonds) >= 3 || hand.Count(c => c.suit == Suit.Clubs) >= 3;
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

        public static void InterpretRebid(InterpretedBid bid)
        {
            var cuebid = bid.History[bid.Index - 4];
            if (bid.History[bid.Index - 2].BidConvention == BidConvention.AskingForMinor)
            {
                if (bid.declareBid.level == 3 && BridgeBot.IsMinor(bid.declareBid.suit))
                {
                    bid.HandShape[bid.declareBid.suit].Min = 5;
                    bid.Description = $"5+ {bid.declareBid.suit}";
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
            }
        }
    }
}
