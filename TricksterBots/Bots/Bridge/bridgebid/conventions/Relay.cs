using Trickster.cloud;

namespace Trickster.Bots
{
    internal class Relay
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (JacobyTransfer.CanUseTransfers(bid)) return InterpretRelay(bid);

            if (bid.bidIsDeclare && bid.Index >= 2 && bid.History[bid.Index - 2].BidConvention == BidConvention.Relay)
            {
                AcceptRelay(bid);
                return true;
            }

            if (bid.Index >= 2 && bid.History[bid.Index - 2].BidConvention == BidConvention.AcceptRelay)
            {
                CompleteRelay(bid, bid.History[bid.Index - 1]);
                return true;
            }

            return false;
        }

        private static void AcceptRelay(InterpretedBid accept)
        {
            if (!accept.bidIsDeclare || accept.declareBid.level != 3 || accept.declareBid.suit != Suit.Clubs)
                return;

            //  1N-2S-3C
            accept.BidConvention = BidConvention.AcceptRelay;
            accept.Description = string.Empty;
            accept.AlternateMatches = hand => true;
        }

        private static void CompleteRelay(InterpretedBid complete, InterpretedBid interference)
        {
            if (complete.bid == BidBase.Pass && !interference.bidIsDeclare)
            {
                complete.BidMessage = BidMessage.Signoff;
                complete.HandShape[Suit.Clubs].Min = 6;
                complete.Description = $"6+ {Suit.Clubs}";
            }
            else if (complete.bidIsDeclare && complete.declareBid.level == 3 && complete.declareBid.suit == Suit.Diamonds)
            {
                complete.BidMessage = BidMessage.Signoff;
                complete.HandShape[Suit.Diamonds].Min = 6;
                complete.Description = $"6+ {Suit.Diamonds}";
            }
        }

        private static bool InterpretRelay(InterpretedBid response)
        {
            if (!response.bidIsDeclare)
                return false;

            if (response.declareBid.suit != Suit.Spades)
                return false;

            if (response.declareBid.level != 2)
                return false;

            //  1N-2S
            response.BidConvention = BidConvention.Relay;
            response.BidMessage = BidMessage.Forcing;
            response.HandShape[Suit.Hearts].Max = 4;
            response.HandShape[Suit.Spades].Max = 4;
            response.Description = " to 3♣; 6+ Clubs or Diamonds";
            response.Validate = hand =>
            {
                //  validate matched hands have 6+ cards in a minor
                var counts = BasicBidding.CountsBySuit(hand);
                return counts[Suit.Clubs] >= 6 || counts[Suit.Diamonds] >= 6;
            };

            return true;
        }
    }
}