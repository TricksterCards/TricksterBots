using Trickster.cloud;

namespace Trickster.Bots
{
    internal class JacobyTransfer
    {
        public static bool CanUseTransfers(InterpretedBid bid)
        {
            return Stayman.CanUseStayman(bid);
        }

        public static bool Interpret(InterpretedBid bid)
        {
            if (CanUseTransfers(bid)) return InterpretTransfers(bid);

            if (bid.bidIsDeclare && bid.Index >= 4 && bid.History[bid.Index - 2].BidConvention == BidConvention.JacobyTransfer)
            {
                AcceptTransfer(bid.History[bid.Index - 2], bid.History[bid.Index - 1], bid);
                return true;
            }

            if (bid.bidIsDeclare && bid.Index >= 6 && bid.History[bid.Index - 4].BidConvention == BidConvention.JacobyTransfer)
                return InterpretResponderRebid(bid);

            return false;
        }

        public static bool InterpretTransfers(InterpretedBid response)
        {
            if (!response.bidIsDeclare)
                return false;

            if (response.declareBid.suit != Suit.Diamonds && response.declareBid.suit != Suit.Hearts)
                return false;

            if (response.declareBid.level != response.LowestAvailableLevel(response.declareBid.suit))
                return false;

            //  1N-2D
            //  1N-2H
            //  2N-3D
            //  2N-3H
            //  3N-4D
            //  3N-4H
            //  2C-2D-2N-3D
            //  2C-2D-2N-3H
            //  2C-2D-3N-4D
            //  2C-2D-3N-4H
            response.BidConvention = BidConvention.JacobyTransfer;
            response.BidMessage = BidMessage.Forcing;
            var transferSuit = response.declareBid.suit == Suit.Diamonds ? Suit.Hearts : Suit.Spades;
            response.HandShape[transferSuit].Min = 5;
            response.Description = $" to {response.declareBid.level}{Card.SuitSymbol(transferSuit)}; 5+ {transferSuit}";

            return true;
        }

        private static void AcceptTransfer(InterpretedBid transfer, InterpretedBid interference, InterpretedBid accept)
        {
            if (!accept.bidIsDeclare || accept.declareBid.level > transfer.declareBid.level + 1)
                return;

            if (transfer.declareBid.suit == Suit.Diamonds && accept.declareBid.suit != Suit.Hearts)
                return;

            if (transfer.declareBid.suit == Suit.Hearts && accept.declareBid.suit != Suit.Spades)
                return;

            //  1N-2D-2H
            //  1N-2H-2S
            //  ...
            accept.BidConvention = BidConvention.AcceptJacobyTransfer;
            accept.Description = string.Empty;

            if (interference.bid == BridgeBid.Double)
                //  if transfer is doubled, opener only completes the transfer with 3+ trumps
                accept.HandShape[accept.declareBid.suit].Min = 3;
            else
                //  otherwise we always accept the transfer if opponent hasn't bid (in which case this won't be an option anyway)
                accept.AlternateMatches = hand => true;

            if (accept.declareBid.level == transfer.declareBid.level + 1)
            {
                //  1N-2D-3H
                //  1N-2D-3S
                //  ...
                accept.Points.Min = 17;
                accept.BidPointType = BidPointType.Dummy;
                accept.HandShape[accept.declareBid.suit].Min = 4;
                accept.Description = $"super-accept; 4+ {accept.declareBid.suit}";
                accept.AlternateMatches = null;
            }
        }

        private static bool InterpretResponderRebid(InterpretedBid rebid)
        {
            //  TODO: check how much this overlaps with ResponderRebid.Interpret (maybe all of it?)
            //  If, after the transfer is accepted, responder bids a new suit, that is natural and
            //  game forcing. Possible calls after the accepted transfer are:
            //  1NT — 2H
            //  2S  — Pass = content to play 2S.
            //      — 2NT, 3S = invitational. Over 2NT opener may pass or bid 3S
            //        with a minimum hand; bid 3NT or 4S with a maximum.
            //      — 3C, 3D, 3H = natural and game forcing.
            //      — 3NT = giving opener a choice between 3NT and 4S.
            //      — 4S = placing the contract, with a six-card or longer suit.
            return false;
        }
    }
}