using System.Linq;
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
            var db = bid.declareBid;

            if (bid.bidIsDeclare && db.level == 5 && db.suit == Suit.Unknown)
            {
                bid.BidConvention = BidConvention.Blackwood;
                bid.Description = "asking for Kings";
                //  TODO: add a bid.Validate to determine when we should ask for Kings
                return true;
            }

            var answer = bid.History[bid.Index - 2];
            if (answer.BidConvention != BidConvention.AnswerBlackwood || bid.History[bid.Index - 1].bid != BidBase.Pass)
                return false;

            //  place the contract based on how many Aces our side is missing
            var trump = SlamTry.TrumpSuit(bid);
            var partnerAces = answer.Aces;
            int Missing(Hand hand)
            {
                var myAces = hand.Count(c => c.rank == Rank.Ace);
                //  "0 or 4" is resolved by our own Ace count
                var theirs = partnerAces.Count == 1 ? partnerAces[0] : myAces > 0 ? 0 : 4;
                return 4 - myAces - theirs;
            }

            var answerIsTrump = answer.bidIsDeclare && answer.declareBid.suit == trump;

            if (bid.bid == BidBase.Pass)
            {
                bid.Description = "Sign-off; missing 2+ Aces";
                bid.Validate = hand => answerIsTrump && Missing(hand) >= 2;
                return true;
            }

            if (!bid.bidIsDeclare || db.suit != trump)
            {
                bid.Validate = hand => false;
                return true;
            }

            bid.BidMessage = BidMessage.Signoff;
            if (db.level == 5)
            {
                bid.Description = "Sign-off; missing 2+ Aces";
                bid.Validate = hand => Missing(hand) >= 2;
            }
            else if (db.level == 6)
            {
                bid.Description = "Small slam";
                bid.Validate = hand => Missing(hand) <= 1;
            }
            else
            {
                //  TODO: bid a grand slam when we can count 13 tricks
                bid.Validate = hand => false;
            }

            return true;
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