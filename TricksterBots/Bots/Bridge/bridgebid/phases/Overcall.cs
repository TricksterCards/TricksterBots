using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal class Overcall
    {
        public static void Interpret(InterpretedBid overcall)
        {
            if (overcall.bid == BidBase.Pass)
            {
                overcall.Description = "Unsuitable hand for an overcall";
                return;
            }

            if (!overcall.bidIsDeclare)
                return;

            var db = overcall.declareBid;
            var lowestLevel = overcall.LowestAvailableLevel(db.suit, true);

            var cueSuit = Suit.Unknown;
            var opponentsLastBid = overcall.History[overcall.Index - 1];
            var opponentsPreviousBid = overcall.Index >= 3 ? overcall.History[overcall.Index - 3] : null;
            if (opponentsLastBid.bidIsDeclare && opponentsPreviousBid != null && opponentsPreviousBid.bidIsDeclare)
            {
                if (opponentsLastBid.declareBid.suit == opponentsPreviousBid.declareBid.suit) cueSuit = opponentsLastBid.declareBid.suit;
            }
            else if (opponentsLastBid.bidIsDeclare)
            {
                cueSuit = opponentsLastBid.declareBid.suit;
            }
            else if (opponentsPreviousBid != null && opponentsPreviousBid.bidIsDeclare)
            {
                cueSuit = opponentsPreviousBid.declareBid.suit;
            }

            //  check for a cuebid (bidding opponent's suit at the lowest available level)
            if (cueSuit != Suit.Unknown && db.suit == cueSuit && db.level == lowestLevel)
            {
                //  cuebid case
                overcall.BidConvention = BidConvention.MichaelsCuebid;
                overcall.BidMessage = BidMessage.Forcing;

                //  keep bots below game when using Michaels for now to minimize risky bids
                //  TODO: are there good reasons to use this at higher levels?
                var isBelowGame = overcall.declareBid.level < overcall.GameLevel;

                //  (1C)-2C, (2C)-3C, ...
                //  (1D)-2D, (2D)-2D, ...
                if (BridgeBot.IsMinor(cueSuit))
                {
                    overcall.Points.Min = 8;
                    overcall.HandShape[Suit.Hearts].Min = 5;
                    overcall.HandShape[Suit.Spades].Min = 5;
                    overcall.Description = "5-5 in Hearts & Spades";
                    overcall.Validate = hand => isBelowGame;
                }
                //  (1H)-2H, (2H)-3H, ...
                //  (2S)-2S, (2S)-2S, ...
                else
                {
                    overcall.Points.Min = 10;

                    //  the other major has at least 5 cards
                    var otherMajor = cueSuit == Suit.Hearts ? Suit.Spades : Suit.Hearts;
                    overcall.HandShape[otherMajor].Min = 5;
                    overcall.HandShape[otherMajor].Max = 8;

                    //  the cue'd major can't have more than 3 cards (due to 5-5 in other suits)
                    overcall.HandShape[cueSuit].Max = 3;

                    //  the minors can't have more than 8
                    overcall.HandShape[Suit.Clubs].Max = 8;
                    overcall.HandShape[Suit.Diamonds].Max = 8;

                    //  validate matched hands have 5+ cards in a minor
                    overcall.Validate = hand =>
                    {
                        var counts = BasicBidding.CountsBySuit(hand);
                        return isBelowGame && (counts[Suit.Clubs] >= 5 || counts[Suit.Diamonds] >= 5);
                    };

                    overcall.Description = $"5-5 in {otherMajor} & a minor";
                }

                return;
            }

            switch (db.level)
            {
                case 1:
                    switch (db.suit)
                    {
                        //  (1C)-1D
                        //  (1C)-1H
                        //  (1C)-1S
                        //  (1D)-1H
                        //  (1D)-1S
                        //  (1H)-1S
                        case Suit.Diamonds:
                        case Suit.Hearts:
                        case Suit.Spades:
                            overcall.Points.Min = 7;
                            overcall.Points.Max = 17;
                            overcall.IsGood = true;
                            overcall.Description = $"5+ {db.suit}";
                            overcall.HandShape[db.suit].Min = 5;
                            return;

                        //  (1C)-1N
                        //  (1D)-1N
                        //  (1H)-1N
                        //  (1S)-1N
                        case Suit.Unknown:
                            overcall.BidPointType = BidPointType.Hcp;
                            overcall.Points.Min = 15;
                            overcall.Points.Max = 18;
                            overcall.IsBalanced = true;
                            overcall.Description = string.Empty;
                            return;
                    }

                    break;

                case 2:
                    switch (db.suit)
                    {
                        case Suit.Clubs:
                        case Suit.Diamonds:
                        case Suit.Hearts:
                        case Suit.Spades:

                            //  (1D)-2C
                            //  (1H)-2C
                            //  (1H)-2D
                            //  (1S)-2C
                            //  (1S)-2D
                            //  (1S)-2H
                            if (db.level == lowestLevel)
                            {
                                overcall.Points.Min = 13;
                                overcall.Points.Max = 17;
                                overcall.HandShape[db.suit].Min = 5;
                                overcall.IsGood = true;
                                overcall.Description = $"5+ {db.suit}";
                            }
                            //  (1C)-2D
                            //  (1C)-2H
                            //  (1C)-2S
                            //  (1D)-2H
                            //  (1D)-2S
                            //  (1H)-2S
                            else
                            {
                                overcall.Points.Min = 5;
                                overcall.Points.Max = 11;
                                overcall.HandShape[db.suit].Min = 6;
                                overcall.IsGood = true;
                                overcall.IsPreemptive = true;
                                overcall.Description = $"6+ {db.suit}";
                            }

                            return;
                    }

                    break;
            }

            //  (1C)-2N
            //  (1D)-2N
            //  (1H)-2N
            //  (1S)-2N
            if (db.suit == Suit.Unknown && db.level == 2 && lowestLevel == 1)
            {
                //  a jump overcall of 2NT shows at least 5–5 in the lowest two unbid suits.
                overcall.BidConvention = BidConvention.UnusualNotrump;
                overcall.BidMessage = BidMessage.Forcing;

                var bidSuits = overcall.SuitsBid;
                var twoLow = SuitRank.stdSuits.Where(s => !bidSuits.Contains(s)).OrderBy(s => BridgeBot.suitRank[s]).Take(2).ToList();

                foreach (var s in twoLow)
                    overcall.HandShape[s].Min = 5;

                overcall.Description = $"5-5 in {twoLow[0]} & {twoLow[1]}";

                return;
            }

            //  jump overcalls are preemptive, showing the same value as an opening bid at the same level
            //  versus an opening preempt, an overcall in a suit or notrump is natural; a cuebid is Michaels (handled above)
            Opening.Interpret(overcall);
        }
    }
}