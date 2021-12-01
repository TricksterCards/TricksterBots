using Trickster.cloud;

namespace Trickster.Bots
{
    internal class Jacoby2NT
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (bid.bidIsDeclare && bid.Index >= 2 && bid.History[bid.Index - 2].BidConvention == BidConvention.Jacoby2NT)
            {
                Answer(bid);
                return true;
            }

            return false;
        }

        //  If responder jumps to 2NT over a 1H or 1S opening, that is Jacoby 2NT, asking
        //  opener to show a singleton or void. If opener has no short suit, he shows his
        //  hand strength;
        private static void Answer(InterpretedBid answer)
        {
            if (answer.declareBid.level == 3)
            {
                if (answer.declareBid.suit == Suit.Hearts)
                {
                    //  3H = maximum hand (18+)
                    answer.Points.Min = 18;
                    answer.BidConvention = BidConvention.AnswerJacoby2NT;
                    foreach (var s in SuitRank.stdSuits) answer.HandShape[s].Min = 2;
                    answer.Description = "maximum hand; no short suit";
                }
                else if (answer.declareBid.suit == Suit.Unknown)
                {
                    //  3N = medium hand (15–17)
                    answer.Points.Min = 15;
                    answer.Points.Max = 17;
                    answer.BidConvention = BidConvention.AnswerJacoby2NT;
                    foreach (var s in SuitRank.stdSuits) answer.HandShape[s].Min = 2;
                    answer.Description = "medium hand; no short suit";
                }
                else
                {
                    //  3C, 3D, 3S = singleton or void in that suit; other bids deny a short suit
                    answer.BidConvention = BidConvention.AnswerJacoby2NT;
                    answer.HandShape[answer.declareBid.suit].Max = 1;
                    answer.Description = $"singleton or void in {answer.declareBid.suit}";
                }
            }
            else if (answer.declareBid.level == 4)
            {
                if (answer.declareBid.suit == Suit.Hearts)
                {
                    //  4H = minimum hand
                    answer.Points.Min = 13;
                    answer.Points.Max = 14;
                    answer.BidConvention = BidConvention.AnswerJacoby2NT;
                    foreach (var s in SuitRank.stdSuits) answer.HandShape[s].Min = 2;
                    answer.Description = "minimum hand; no short suit";
                }
                else if (BridgeBot.IsMinor(answer.declareBid.suit))
                {
                    //  4C, 4D = 2nd suit
                    answer.BidConvention = BidConvention.AnswerJacoby2NT;
                    answer.HandShape[answer.declareBid.suit].Min = 5;
                    foreach (var s in SuitRank.stdSuits) answer.HandShape[s].Min = 2;
                    answer.Description = $"5+ {answer.declareBid.suit}; no short suit";
                }
            }
        }
    }
}