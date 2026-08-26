using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    //  Modern "Standard English" Acol: weak 1NT, 4-card majors, weak twos, 2C as the only strong opening
    internal class AcolOpening
    {
        public static void Interpret(InterpretedBid opening)
        {
            if (opening.bid == BidBase.Pass)
            {
                opening.Points.Max = 11;
                opening.Description = "Weak hand & no good, long suits";
                return;
            }

            if (!opening.bidIsDeclare)
                return;

            var db = opening.declareBid;

            switch (db.level)
            {
                case 1:
                    opening.Points.Min = 12;
                    opening.Points.Max = 19;

                    if (db.suit != Suit.Unknown && opening.Index < 2)
                    {
                        //  use the Rule of 19 in 1st or 2nd seat
                        opening.AlternateMatches = hand =>
                        {
                            //  our HCP + count of cards in our two longest suits must be 19 or more to open
                            var hcp = BasicBidding.ComputeHighCardPoints(hand);
                            var counts = BasicBidding.CountsBySuit(hand);
                            var topCounts = counts.Values.OrderByDescending(v => v).ToList();
                            var total = hcp + topCounts[0] + topCounts[1];
                            //  still validate if we match the correct hand shape to ensure we pick the right suit
                            return total >= 19 && counts[opening.declareBid.suit] >= opening.HandShape[opening.declareBid.suit].Min &&
                                   (opening.Validate == null || opening.Validate(hand));
                        };
                        opening.AlternatePoints = "Rule of 19";
                    }

                    switch (db.suit)
                    {
                        //  1C
                        //  1D
                        //  1H
                        //  1S
                        case Suit.Clubs:
                        case Suit.Diamonds:
                        case Suit.Hearts:
                        case Suit.Spades:
                            opening.HandShape[db.suit].Min = 4;
                            opening.Description = $"4+ {db.suit}";
                            opening.Validate = hand => IsPreferredSuit(hand, db.suit);

                            if (db.suit == Suit.Spades && opening.Index == 3)
                            {
                                //  use the Rule of 15 in 4th seat
                                opening.AlternateMatches = hand =>
                                {
                                    //  we must have less than an opening hand
                                    //  and our HCP + # of Spades must be 15 or more to open 1S
                                    var hcp = BasicBidding.ComputeHighCardPoints(hand);
                                    var points = hcp + BasicBidding.ComputeDistributionPoints(hand);
                                    var nSpades = BasicBidding.CountsBySuit(hand)[db.suit];
                                    return points < opening.Points.Min && nSpades >= opening.HandShape[db.suit].Min && hcp + nSpades >= 15;
                                };
                                opening.AlternatePoints = "Rule of 15";
                            }

                            break;

                        //  1N
                        case Suit.Unknown:
                            opening.Points.Min = 12;
                            opening.Points.Max = 14;
                            opening.BidPointType = BidPointType.Hcp;
                            opening.IsBalanced = true;
                            opening.Description = string.Empty;
                            //  always prefer the weak 1NT over a suit opening with a balanced minimum
                            opening.Priority = 50;
                            break;
                    }

                    break;

                case 2:
                    switch (db.suit)
                    {
                        //  2C (overridden by StrongOpening)
                        //  2D
                        //  2H
                        //  2S
                        case Suit.Clubs:
                        case Suit.Diamonds:
                        case Suit.Hearts:
                        case Suit.Spades:
                            if (opening.Index < 3)
                            {
                                //  consider a weak 2 if we're not in 4th seat
                                opening.Points.Min = 6;
                                opening.Points.Max = 10;
                                opening.BidPointType = BidPointType.Hcp;
                                opening.IsGood = true;
                                opening.IsPreemptive = true;
                                opening.Description = $"6-card {db.suit} suit";
                                opening.HandShape[db.suit].Min = 6;
                                opening.HandShape[db.suit].Max = 6;

                                //  ensure we don't have any voids
                                foreach (var s in SuitRank.stdSuits.Where(s => s != db.suit)) opening.HandShape[s].Min = 1;

                                //  also ensure we don't have a side 4-card major
                                //  a weak two could cause us to miss a 4-4 fit with partner in this case
                                if (db.suit != Suit.Hearts) opening.HandShape[Suit.Hearts].Max = 3;
                                if (db.suit != Suit.Spades) opening.HandShape[Suit.Spades].Max = 3;
                            }

                            break;

                        //  2N
                        case Suit.Unknown:
                            opening.Points.Min = 20;
                            opening.Points.Max = 22;
                            opening.BidPointType = BidPointType.Hcp;
                            opening.IsBalanced = true;
                            opening.Description = string.Empty;
                            break;
                    }

                    break;

                case 3:
                    switch (db.suit)
                    {
                        //  3C
                        //  3D
                        //  3H
                        //  3S
                        case Suit.Clubs:
                        case Suit.Diamonds:
                        case Suit.Hearts:
                        case Suit.Spades:
                            if (opening.Index < 3)
                            {
                                //  preempt only if we're not in 4th seat
                                opening.Points.Max = 11;
                                opening.IsGood = true;
                                opening.IsPreemptive = true;
                                opening.Description = $"7-card {db.suit} suit";
                                opening.HandShape[db.suit].Min = 7;
                                opening.HandShape[db.suit].Max = 7;
                            }

                            break;

                        //  3N
                        case Suit.Unknown:
                            opening.Points.Min = 25;
                            opening.Points.Max = 27;
                            opening.BidPointType = BidPointType.Hcp;
                            opening.IsBalanced = true;
                            opening.Description = string.Empty;
                            break;
                    }

                    break;

                case 4:
                    switch (db.suit)
                    {
                        //  4C
                        //  4D
                        //  4H
                        //  4S
                        case Suit.Clubs:
                        case Suit.Diamonds:
                        case Suit.Hearts:
                        case Suit.Spades:
                            if (opening.Index < 3)
                            {
                                //  preempt only if we're not in 4th seat
                                opening.Points.Max = 11;
                                opening.IsGood = true;
                                opening.IsPreemptive = true;
                                opening.Description = $"8-card {db.suit} suit";
                                opening.HandShape[db.suit].Min = 8;
                            }

                            break;

                        //  4N
                        case Suit.Unknown:
                            opening.Points.Min = 25;
                            opening.BidPointType = BidPointType.Hcp;
                            opening.BidConvention = BidConvention.Blackwood;
                            opening.BidMessage = BidMessage.Forcing;
                            opening.Description = "asking for Aces";
                            //  TODO: validate knowing count of Aces will help decision to bid slam
                            opening.Validate = hand => false;
                            break;
                    }

                    break;

                case 5:
                    switch (db.suit)
                    {
                        //  5C
                        //  5D
                        case Suit.Clubs:
                        case Suit.Diamonds:
                            if (opening.Index < 3)
                            {
                                //  preempt only if we're not in 4th seat
                                opening.Points.Max = 11;
                                opening.IsGood = true;
                                opening.IsPreemptive = true;
                                opening.Description = $"9-card {db.suit} suit";
                                opening.HandShape[db.suit].Min = 9;
                            }

                            break;
                    }

                    break;
            }
        }

        //  open the longest suit; with equal-length suits prefer the higher-ranking,
        //  except open 1H with exactly four cards in each major
        private static bool IsPreferredSuit(Hand hand, Suit suit)
        {
            var counts = BasicBidding.CountsBySuit(hand);
            var max = counts.Values.Max();

            if (counts[suit] != max)
                return false;

            var tied = SuitRank.stdSuits.Where(s => counts[s] == max).ToList();
            if (tied.Count == 1)
                return true;

            if (max == 4 && tied.Contains(Suit.Hearts) && tied.Contains(Suit.Spades))
                return suit == Suit.Hearts;

            return suit == tied.OrderByDescending(s => BridgeBot.suitRank[s]).First();
        }
    }
}
