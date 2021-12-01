using Trickster.cloud;

namespace Trickster.Bots
{
    internal class MichaelsCuebid
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (bid.bidIsDeclare && bid.Index >= 2 && bid.History[bid.Index - 2].BidConvention == BidConvention.MichaelsCuebid)
            {
                var cuebid = bid.History[bid.Index - 2];
                if (BridgeBot.IsMajor(cuebid.declareBid.suit) && bid.bidIsDeclare && bid.declareBid.level == 2 && bid.declareBid.suit == Suit.Unknown)
                {
                    bid.BidConvention = BidConvention.AskingForMinor;
                    bid.BidMessage = BidMessage.Forcing;
                    bid.Description = string.Empty;
                    return true;
                }
            }

            if (bid.bidIsDeclare
                && bid.Index >= 4
                && bid.History[bid.Index - 4].BidConvention == BidConvention.MichaelsCuebid
                && bid.History[bid.Index - 2].BidConvention == BidConvention.AskingForMinor)
                if (bid.declareBid.level == 3 && BridgeBot.IsMinor(bid.declareBid.suit))
                {
                    bid.HandShape[bid.declareBid.suit].Min = 5;
                    bid.Description = $"5+ {bid.declareBid.suit}";
                    return true;
                }

            return false;
        }
    }
}