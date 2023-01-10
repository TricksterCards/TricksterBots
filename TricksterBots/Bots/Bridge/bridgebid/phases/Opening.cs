using System.Linq;
using Trickster.cloud;
using TricksterBots.Bots;

namespace Trickster.Bots
{
    internal class Opening
    {
        public static void Interpret(InterpretedBid opening)
        {
            if (opening.IsPass)
            {
                opening.Points.Max = 12;
                opening.Description = "Weak hand & no good, long suits";
                return;
            }

            if (!opening.bidIsDeclare)
                return;

            var db = opening.declareBid;
            if (db.suit == Suit.Unknown)
            {
                NTFundamentals.Open(opening);
                return;
            }

            switch (db.level)
            {
                case 1:
                    opening.Points.Min = 13;
                    opening.Points.Max = 21;

                    if (db.suit != Suit.Unknown && opening.Index < 2)
                    {
                        //  use the Rule of 20 in 1st or 2nd seat
                        opening.AlternateMatches = hand =>
                        {
                            //  our HCP + count of cards in our two longest suits must be 20 or more to open
                            var hcp = BasicBidding.ComputeHighCardPoints(hand);
                            var counts = BasicBidding.CountsBySuit(hand);
                            var topCounts = counts.Values.OrderByDescending(v => v).ToList();
                            var total = hcp + topCounts[0] + topCounts[1];
                            //  still validate if we match the correct hand shape to ensure we pick the right suit
                            return total >= 20 && counts[opening.declareBid.suit] >= opening.HandShape[opening.declareBid.suit].Min &&
                                   (opening.Validate == null || opening.Validate(hand));
                        };
                        opening.AlternatePoints = "Rule of 20";
                    }

                    switch (db.suit)
                    {
                        //  1C
                        //  1D
                        case Suit.Clubs:
                        case Suit.Diamonds:
                            var otherMinor = db.suit == Suit.Clubs ? Suit.Diamonds : Suit.Clubs;
                            // A 1D opener suggests a four-card or longer suit, since 1C is preferred on hands
                            // where a three - card minor suit must be opened.The exception is a hand with 4–4–3–2
                            // shape: four spades, four hearts, three diamonds, and two clubs, which is opened 1D.
                            opening.HandShape[db.suit].Min = db.suit == Suit.Clubs ? 3 : 4;
                            opening.HandShape[otherMinor].Max = 6;
                            opening.HandShape[Suit.Hearts].Max = 4;
                            opening.HandShape[Suit.Spades].Max = 4;
                            opening.Description = db.suit == Suit.Clubs ? "3+ Clubs; no 5-card major" : "3+ Diamonds (usually 4+); no 5-card major";
                            opening.Validate = hand =>
                            {
                                var counts = BasicBidding.CountsBySuit(hand);
                                return counts[db.suit] > counts[otherMinor] ||
                                       counts[db.suit] == counts[otherMinor] && counts[db.suit] >= 4 && db.suit == Suit.Diamonds ||
                                       counts[db.suit] == 3 && db.suit == Suit.Clubs;
                            };
                            //  assume 4+ Diamonds, but allow matching 3+ to accommodate 4-4-3-2 distribution
                            if (db.suit == Suit.Diamonds) opening.HandShape[db.suit].MinMatch = 3;
                            break;

                        //  1H
                        //  1S
                        case Suit.Hearts:
                        case Suit.Spades:
                            var otherMajor = db.suit == Suit.Hearts ? Suit.Spades : Suit.Hearts;
                            opening.HandShape[db.suit].Min = 5;
                            opening.HandShape[otherMajor].Max = 6;
                            opening.HandShape[Suit.Clubs].Max = 8;
                            opening.HandShape[Suit.Diamonds].Max = 8;
                            opening.Description = $"5+ {db.suit}";
                            opening.Validate = hand =>
                            {
                                var counts = BasicBidding.CountsBySuit(hand);
                                return counts[db.suit] > counts[otherMajor] ||
                                       counts[db.suit] == counts[otherMajor] && db.suit == Suit.Spades;
                            };
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
                                opening.Points.Min = 5;
                                opening.Points.Max = 11;
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
                                opening.Points.Max = 12;
                                opening.IsGood = true;
                                opening.IsPreemptive = true;
                                opening.Description = $"7-card {db.suit} suit";
                                opening.HandShape[db.suit].Min = 7;
                                opening.HandShape[db.suit].Max = 7;
                            }

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
                                opening.Points.Max = 12;
                                opening.IsGood = true;
                                opening.IsPreemptive = true;
                                opening.Description = $"8-card {db.suit} suit";
                                opening.HandShape[db.suit].Min = 8;
                            }

                            break;


                        //  4N
                        /* - TODO: This is too strange.  Can't see why not 2C opener....
                        case Suit.Unknown:
                            opening.Points.Min = 25;
                            opening.BidPointType = BidPointType.Hcp;
                            opening.BidConvention = BidConvention.Blackwood;
                            opening.BidMessage = BidMessage.Forcing;
                            opening.Description = "asking for Aces";
                            //  TODO: validate knowing count of Aces will help decision to bid slam
                            opening.Validate = hand => false;
                            break;
                        */
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
                                opening.Points.Max = 12;
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
    }
}