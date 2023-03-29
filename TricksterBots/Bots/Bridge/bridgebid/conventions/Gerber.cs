using Trickster.cloud;

namespace Trickster.Bots
{
    internal class Gerber
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (IsGerber(bid))
            {
                InterpretGerber(bid);
                return true;
            }

            if (bid.Index >= 2 && bid.History[bid.Index - 2].BidConvention == BidConvention.Gerber)
            {
                InterpretResponse(bid);
                return true;
            }

            if (bid.Index >= 4 && bid.History[bid.Index - 4].BidConvention == BidConvention.Gerber && bid.History[bid.Index - 4].declareBid.level == 4)
                return InterpretRebid(bid);

            return false;
        }

        private static void InterpretGerber(InterpretedBid bid)
        {
            var partnerSummary = new InterpretedBid.PlayerSummary(bid.History, bid.Index - 2);
            bid.Points.Min = 33 - partnerSummary.Points.Min; // 33 is a small slam
            bid.BidPointType = BidPointType.Hcp;
            bid.BidConvention = BidConvention.Gerber;
            bid.BidMessage = BidMessage.Forcing;
            bid.Description = "asking for Aces";
            //  TODO: validate knowing how many aces our partner has will actually help us place the contract (often "control bids" are better here)
            bid.Validate = hand => false;
        }

        private static bool InterpretRebid(InterpretedBid bid)
        {
            if (!bid.bidIsDeclare)
                return false;

            var db = bid.declareBid;

            if (db.level == 5 && db.suit == Suit.Clubs)
            {
                bid.BidConvention = BidConvention.Gerber;
                bid.BidMessage = BidMessage.Forcing;
                bid.Description = "asking for Kings";
                //  TODO (SAYC Booklet): asking for kings guarantees that the partnership holds all the aces
                bid.Validate = hand => false;
                return true;
            }

            //  TODO (SAYC Booklet): if the player using Gerber makes any bid other than 5C, that is to play (including 4NT)

            return false;
        }

        private static void InterpretResponse(InterpretedBid bid)
        {
            if (!bid.bidIsDeclare)
                return;

            var gerber = bid.History[bid.Index - 2].declareBid;

            var db = bid.declareBid;
            var list = gerber.level == 4 ? bid.Aces : bid.Kings;
            var label = gerber.level == 4 ? "Ace" : "King";

            //  the only accepted responses are at the same level
            if (db.level != gerber.level)
                return;

            bid.BidConvention = BidConvention.AnswerGerber;

            switch (db.suit)
            {
                //  4C-4D (aces)
                //  5C-5D (kings)
                case Suit.Diamonds:
                    list.Add(0);
                    list.Add(4);
                    bid.Description = $"0 or 4 {label}s";
                    break;
                //  4C-4H (aces)
                //  5C-5H (kings)
                case Suit.Hearts:
                    list.Add(1);
                    bid.Description = $"1 {label}";
                    break;
                //  4C-4S (aces)
                //  5C-5S (kings)
                case Suit.Spades:
                    list.Add(2);
                    bid.Description = $"2 {label}s";
                    break;
                //  4C-4N (aces)
                //  5C-5N (kings)
                case Suit.Unknown:
                    list.Add(3);
                    bid.Description = $"3 {label}s";
                    break;
            }
        }

        private static bool IsGerber(InterpretedBid bid)
        {
            if (bid.Index < 2)
                return false;

            if (!bid.bidIsDeclare)
                return false;

            if (bid.declareBid.level != 4 || bid.declareBid.suit != Suit.Clubs)
                return false;

            //  we have 4C bid after our partner has bid 
            var partnerBid = bid.History[bid.Index - 2];

            //  SAYC Booklet: 4C IS GERBER OVER ANY 1NT OR 2NT BY PARTNER, INCLUDING A REBid OF 1NT OR 2NT
            if (partnerBid.BidConvention != BidConvention.None)
                return false;

            if (!partnerBid.bidIsDeclare || partnerBid.declareBid.suit != Suit.Unknown || partnerBid.declareBid.level > 2)
                return false;

            return true;
        }
    }
}