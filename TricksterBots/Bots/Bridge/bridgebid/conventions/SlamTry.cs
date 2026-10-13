using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal enum SlamAsk
    {
        None,
        GrandSlamForce,
        TrumpQuality,
        Quantitative
    }

    //  Slam tries other than Blackwood/Gerber: Grand Slam Force (5NT), trump-quality ask (5 of an agreed major),
    //  and quantitative 5NT after our notrump opening.
    internal static class SlamTry
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (bid.Index >= 2 && bid.History[bid.Index - 2].SlamAsk != SlamAsk.None && bid.History[bid.Index - 1].bid == BidBase.Pass)
            {
                InterpretAnswer(bid.History[bid.Index - 2], bid);
                return true;
            }

            if (!bid.bidIsDeclare || bid.declareBid.level != 5 || OpponentsHaveBid(bid) || OurSideBid4NT(bid))
                return false;

            if (bid.declareBid.suit == Suit.Unknown)
            {
                var trump = TrumpSuit(bid);
                if (OurSideOpenedNotrump(bid) && !AgreedSuits(bid).Any())
                {
                    bid.SlamAsk = SlamAsk.Quantitative;
                    bid.BidMessage = BidMessage.Forcing;
                    bid.BidPointType = BidPointType.Hcp;
                    bid.Description = "Quantitative; forcing to 6NT, inviting 7NT";
                    //  TODO: validate when to make this bid
                    bid.Validate = hand => false;
                    return true;
                }

                if (trump == Suit.Unknown)
                    return false;

                bid.SlamAsk = SlamAsk.GrandSlamForce;
                bid.SlamAskSuit = trump;
                bid.BidMessage = BidMessage.Forcing;
                bid.Description = $"Grand Slam Force; asking for top {trump} honors";
                //  TODO: validate when to make this bid
                bid.Validate = hand => false;
                return true;
            }

            var suit = bid.declareBid.suit;
            if (!BridgeBot.IsMajor(suit) || !AgreedSuits(bid).Contains(suit) || bid.LowestAvailableLevel(suit, true) > 5)
                return false;

            //  5 of an agreed major (without other suits cue-bid) asks partner to bid slam with a top trump honor
            var partnerMin = new InterpretedBid.PlayerSummary(bid.History, bid.Index - 2).Points.Min;
            bid.SlamAsk = SlamAsk.TrumpQuality;
            bid.SlamAskSuit = suit;
            bid.BidMessage = BidMessage.Forcing;
            bid.Priority = 80;
            bid.Points.Min = InterpretedBid.SmallSlamPoints - partnerMin;
            bid.Description = $"Slam try; asking about {suit} quality";
            bid.Validate = hand => AllSideSuitsControlled(hand, suit) && TopHonors(hand, suit, Rank.King) < 2;
            return true;
        }

        private static void InterpretAnswer(InterpretedBid ask, InterpretedBid answer)
        {
            var trump = ask.SlamAskSuit;

            switch (ask.SlamAsk)
            {
                case SlamAsk.GrandSlamForce:
                    if (answer.bidIsDeclare && answer.declareBid.suit == trump && answer.declareBid.level >= 6)
                    {
                        var grand = answer.declareBid.level == 7;
                        answer.BidMessage = BidMessage.Signoff;
                        answer.Description = grand ? $"2+ of the top 3 {trump} honors" : $"Fewer than 2 of the top 3 {trump} honors";
                        answer.Validate = hand => TopHonors(hand, trump, Rank.Queen) >= 2 == grand;
                        return;
                    }

                    break;

                case SlamAsk.TrumpQuality:
                    if (answer.bid == BidBase.Pass)
                    {
                        answer.Description = $"No top {trump} honor";
                        answer.Validate = hand => TopHonors(hand, trump, Rank.King) == 0;
                        return;
                    }

                    if (answer.bidIsDeclare && answer.declareBid.suit == trump && answer.declareBid.level == 6)
                    {
                        answer.BidMessage = BidMessage.Signoff;
                        answer.Description = $"Ace or King of {trump}";
                        answer.Validate = hand => TopHonors(hand, trump, Rank.King) > 0;
                        return;
                    }

                    break;

                case SlamAsk.Quantitative:
                    if (answer.bidIsDeclare && answer.declareBid.suit == Suit.Unknown && answer.declareBid.level == 6)
                    {
                        answer.BidMessage = BidMessage.Signoff;
                        answer.Description = "Accept small slam";
                        answer.Validate = hand => true;
                        return;
                    }

                    break;
            }

            answer.Validate = hand => false;
        }

        //  the strain our side has agreed (or partner's suit if none is agreed); Unknown means notrump
        internal static Suit TrumpSuit(InterpretedBid bid)
        {
            var agreed = AgreedSuits(bid);
            if (agreed.Count > 0)
                return agreed.First();

            var me = new InterpretedBid.PlayerSummary(bid.History, bid.Index - 4);
            var partner = new InterpretedBid.PlayerSummary(bid.History, bid.Index - 2);
            var best = SuitRank.stdSuits.OrderByDescending(s => me.HandShape[s].Min + partner.HandShape[s].Min).First();
            return me.HandShape[best].Min + partner.HandShape[best].Min >= 5 ? best : Suit.Unknown;
        }

        //  suits bid naturally by both partners, most recently bid first
        private static List<Suit> AgreedSuits(InterpretedBid bid)
        {
            var ourBids = bid.History.Take(bid.Index)
                .Where(b => (bid.Index - b.Index) % 2 == 0 && b.bidIsDeclare && b.declareBid.suit != Suit.Unknown && IsNatural(b))
                .ToList();
            var mine = ourBids.Where(b => (bid.Index - b.Index) % 4 == 0).Select(b => b.declareBid.suit).ToList();
            var partners = ourBids.Where(b => (bid.Index - b.Index) % 4 == 2).Select(b => b.declareBid.suit).ToList();
            return ourBids.Select(b => b.declareBid.suit).Reverse().Distinct().Where(s => mine.Contains(s) && partners.Contains(s)).ToList();
        }

        private static bool IsNatural(InterpretedBid b)
        {
            return b.BidConvention == BidConvention.None || b.BidConvention == BidConvention.AcceptJacobyTransfer ||
                   b.BidConvention == BidConvention.AnswerStayman || b.BidConvention == BidConvention.AnswerJacoby2NT;
        }

        private static bool OpponentsHaveBid(InterpretedBid bid)
        {
            return bid.History.Take(bid.Index).Any(b => (bid.Index - b.Index) % 2 == 1 && b.bid != BidBase.Pass);
        }

        private static bool OurSideBid4NT(InterpretedBid bid)
        {
            return bid.History.Take(bid.Index).Any(b => (bid.Index - b.Index) % 2 == 0 && b.bidIsDeclare &&
                                                        b.declareBid.level == 4 && b.declareBid.suit == Suit.Unknown);
        }

        private static bool OurSideOpenedNotrump(InterpretedBid bid)
        {
            var opening = bid.History.Take(bid.Index).FirstOrDefault(b => b.bid != BidBase.Pass);
            return opening != null && (bid.Index - opening.Index) % 2 == 0 && opening.bidIsDeclare && opening.declareBid.suit == Suit.Unknown;
        }

        private static bool AllSideSuitsControlled(Hand hand, Suit trump)
        {
            return SuitRank.stdSuits.Where(s => s != trump).All(s =>
            {
                var cards = hand.Where(c => c.suit == s).ToList();
                return cards.Count <= 1 || cards.Any(c => c.rank == Rank.Ace || c.rank == Rank.King);
            });
        }

        //  count of honors in the suit ranked at or above lowestRank (e.g. Rank.King counts Ace and King)
        private static int TopHonors(Hand hand, Suit suit, Rank lowestRank)
        {
            return hand.Count(c => c.suit == suit && c.rank >= lowestRank);
        }
    }
}
