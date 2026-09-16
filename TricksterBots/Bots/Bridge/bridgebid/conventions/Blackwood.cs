using System;
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

            if (bid.Index >= 4 && bid.History[bid.Index - 4].BidConvention == BidConvention.Blackwood)
                return InterpretRebid(bid);

            return false;
        }

        private static bool InterpretRebid(InterpretedBid bid)
        {
            if (!bid.bidIsDeclare)
                return false;

            var ask = bid.History[bid.Index - 4];
            var answer = bid.History[bid.Index - 2];
            var db = bid.declareBid;

            if (ask.declareBid.level == 4 && db.level == 5 && db.suit == Suit.Unknown)
            {
                var partnerSummary = new InterpretedBid.PlayerSummary(bid.History, bid.Index - 2);
                bid.BidConvention = BidConvention.Blackwood;
                bid.Description = "asking for Kings";
                bid.Priority = 5; // prefer asking over settling for a small slam when the ask is useful
                bid.Validate = hand => BasicBidding.KingAskIsUseful(hand, answer.Aces, partnerSummary.Points.Min);
                return true;
            }

            return InterpretPlacement(bid, ask, answer);
        }

        private static bool InterpretPlacement(InterpretedBid bid, InterpretedBid ask, InterpretedBid answer)
        {
            var summary = new InterpretedBid.TeamSummary(bid.History, bid.Index - 2);
            var trump = summary.HandShape.Where(hs => hs.Value.Min >= 8).Select(hs => hs.Key).FirstOrDefault();
            var db = bid.declareBid;

            if (db.suit != trump)
                return false;

            var askedForKings = ask.declareBid.level == 5;

            //  when we asked for Kings, partner's count of Aces came from their answer to the previous ask
            var aceAnswer = askedForKings && bid.Index >= 6 ? bid.History[bid.Index - 6] : answer;

            if (db.level == 7)
            {
                bid.Description = "Grand slam";
                bid.Priority = 9; // prefer the grand slam over the small slam when nothing is missing
                bid.Validate = hand => BasicBidding.MissingCount(hand, Rank.Ace, aceAnswer.Aces) == 0 && askedForKings &&
                                       BasicBidding.MissingCount(hand, Rank.King, answer.Kings) == 0;
            }
            else if (db.level == 6)
            {
                bid.Description = "Small slam";
                bid.Validate = hand => BasicBidding.MissingCount(hand, Rank.Ace, aceAnswer.Aces) <= 1;
            }
            else if (db.level == 5 && !askedForKings)
            {
                bid.Description = "Missing too many Aces for slam";
                bid.Validate = hand => BasicBidding.MissingCount(hand, Rank.Ace, aceAnswer.Aces) > 1;
            }
            else
            {
                return false;
            }

            bid.BidMessage = BidMessage.Signoff;
            bid.Priority = Math.Min(bid.Priority, 10); // prefer placing the contract based on the answer we asked for
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