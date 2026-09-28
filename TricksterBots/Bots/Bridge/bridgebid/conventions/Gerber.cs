using System;
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

            if (bid.Index >= 4 && bid.History[bid.Index - 4].BidConvention == BidConvention.Gerber)
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
            bid.Priority = 10; // prefer asking over settling for game when the ask is useful
            bid.Validate = hand => BasicBidding.AceAskIsUseful(hand, partnerSummary.Points.Min);
        }

        private static bool InterpretRebid(InterpretedBid bid)
        {
            if (!bid.bidIsDeclare)
                return false;

            var ask = bid.History[bid.Index - 4];
            var answer = bid.History[bid.Index - 2];
            var db = bid.declareBid;

            if (ask.declareBid.level == 4 && db.level == 5 && db.suit == Suit.Clubs)
            {
                var partnerSummary = new InterpretedBid.PlayerSummary(bid.History, bid.Index - 2);
                bid.BidConvention = BidConvention.Gerber;
                bid.BidMessage = BidMessage.Forcing;
                bid.Description = "asking for Kings";
                bid.Priority = 5; // prefer asking over settling for a small slam when the ask is useful
                bid.Validate = hand => BasicBidding.KingAskIsUseful(hand, answer.Aces, partnerSummary.Points.Min);
                return true;
            }

            return InterpretPlacement(bid, ask, answer);
        }

        //  (SAYC Booklet) if the player using Gerber makes any bid other than 5C, that is to play (including 4NT)
        private static bool InterpretPlacement(InterpretedBid bid, InterpretedBid ask, InterpretedBid answer)
        {
            var db = bid.declareBid;
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
            else if (db.level == bid.GameLevel && !askedForKings)
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

            if (db.suit != Suit.Unknown)
                //  partner's notrump bid said nothing about our suit, so we need to be able to play it opposite a singleton
                bid.HandShape[db.suit].Min = 6;

            return true;
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

            //  SAYC Booklet: 4C IS GERBER OVER ANY 1NT OR 2NT BY PARTNER, INCLUDING A REBID OF 1NT OR 2NT
            if (partnerBid.BidConvention != BidConvention.None)
                return false;

            if (!partnerBid.bidIsDeclare || partnerBid.declareBid.suit != Suit.Unknown || partnerBid.declareBid.level > 2)
                return false;

            return true;
        }
    }
}