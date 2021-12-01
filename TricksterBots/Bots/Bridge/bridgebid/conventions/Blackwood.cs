using Trickster.cloud;

namespace Trickster.Bots
{
    internal class Blackwood
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (bid.Index >= 2 && bid.History[bid.Index - 2].BidConvention == BidConvention.Blackwood)
            {
                InterpretResponse(bid);
                return true;
            }

            if (bid.Index >= 4 && bid.History[bid.Index - 4].BidConvention == BidConvention.Blackwood && bid.History[bid.Index - 4].declareBid.level == 4)
                return InterpretRebid(bid);

            return false;
        }

        private static bool InterpretRebid(InterpretedBid bid)
        {
            if (!bid.bidIsDeclare)
                return false;

            var db = bid.declareBid;

            if (db.level == 5 && db.suit == Suit.Unknown)
            {
                bid.BidConvention = BidConvention.Blackwood;
                bid.Description = "asking for Kings";
                //  TODO: add a bid.Validate to determine when we should ask for Kings
                return true;
            }

            return false;
        }

        private static void InterpretResponse(InterpretedBid bid)
        {
            var blackwood = bid.History[bid.Index - 2].declareBid;

            //  TODO: handle responding after interference (where double can be used)
            if (!bid.bidIsDeclare)
                return;

            var db = bid.declareBid;
            var list = blackwood.level == 4 ? bid.Aces : bid.Kings;
            var label = blackwood.level == 4 ? "Ace" : "King";

            //  the only accepted responses are in a suit at the next level
            if (db.level != blackwood.level + 1 || db.suit == Suit.Unknown)
                return;

            bid.BidConvention = BidConvention.AnswerBlackwood;

            switch (db.suit)
            {
                //  4N-5C (aces)
                //  5N-6C (kings)
                case Suit.Clubs:
                    list.Add(0);
                    list.Add(4);
                    bid.Description = $"0 or 4 {label}s";
                    break;
                //  4N-5D (aces)
                //  5N-6D (kings)
                case Suit.Diamonds:
                    list.Add(1);
                    bid.Description = $"1 {label}";
                    break;
                //  4N-5H (aces)
                //  5N-6H (kings)
                case Suit.Hearts:
                    list.Add(2);
                    bid.Description = $"2 {label}s";
                    break;
                //  4N-5S (aces)
                //  5N-6S (kings)
                case Suit.Spades:
                    list.Add(3);
                    bid.Description = $"3 {label}s";
                    break;
            }
        }
    }
}