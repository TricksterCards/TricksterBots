using Trickster.cloud;

namespace Trickster.Bots
{
    internal class Jacoby2NT
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (bid.bidIsDeclare && bid.Index >= 2 && bid.History[bid.Index - 2].BidConvention == BidConvention.Jacoby2NT)
            {
                var opening = bid.History[bid.Index - 4];
                Answer(opening, bid);
                return true;
            }

            return false;
        }

        //  If responder jumps to 2NT over a 1H or 1S opening, that is Jacoby 2NT, asking
        //  opener to show a singleton or void. If opener has no short suit, he shows his
        //  hand strength;
        private static void Answer(InterpretedBid opening, InterpretedBid answer)
        {
            if (answer.declareBid.level == 3)
            {
                if (answer.declareBid.suit == opening.declareBid.suit)
                {
                    //  maximum hand (18+)
                    //  1H-2N-3H
                    //  1S-2N-2S
                    answer.Points.Min = 18;
                    answer.BidConvention = BidConvention.AnswerJacoby2NT;
                    foreach (var s in SuitRank.stdSuits) answer.HandShape[s].Min = 2;
                    answer.HandShape[opening.declareBid.suit].Min = 5;
                    answer.Description = "maximum hand; no short suit";
                    answer.Priority = 5;
                }
                else if (answer.declareBid.suit == Suit.Unknown)
                {
                    //  medium hand (15–17)
                    //  1H-2N-3N
                    //  1S-2N-3N
                    answer.Points.Min = 15;
                    answer.Points.Max = 17;
                    answer.BidConvention = BidConvention.AnswerJacoby2NT;
                    foreach (var s in SuitRank.stdSuits) answer.HandShape[s].Min = 2;
                    answer.HandShape[opening.declareBid.suit].Min = 5;
                    answer.Description = "medium hand; no short suit";
                    answer.Priority = 3;
                }
                else
                {
                    //  singleton or void in that suit; other bids deny a short suit
                    //  1H-2N-3C
                    //  1H-2N-3D
                    //  1H-2N-3S
                    //  1S-2N-3C
                    //  1S-2N-3D
                    //  1S-2N-3H
                    answer.BidConvention = BidConvention.AnswerJacoby2NT;
                    answer.HandShape[answer.declareBid.suit].Max = 1;
                    answer.HandShape[opening.declareBid.suit].Min = 5;
                    answer.Description = $"singleton or void in {answer.declareBid.suit}";
                    answer.Priority = 2;
                }
            }
            else if (answer.declareBid.level == 4)
            {
                if (answer.declareBid.suit == opening.declareBid.suit)
                {
                    //  minimum hand
                    //  1H-2N-4H
                    //  1S-2N-4S
                    answer.Points.Min = 13;
                    answer.Points.Max = 14;
                    answer.BidConvention = BidConvention.AnswerJacoby2NT;
                    foreach (var s in SuitRank.stdSuits) answer.HandShape[s].Min = 2;
                    answer.HandShape[opening.declareBid.suit].Min = 5;
                    answer.Description = "minimum hand; no short suit";
                    answer.Priority = 4;
                }
                else if (BridgeBot.IsMinor(answer.declareBid.suit))
                {
                    //  2nd suit
                    //  1H-2N-4C
                    //  1H-2N-4D
                    //  1S-2N-4C
                    //  1S-2N-4D
                    answer.BidConvention = BidConvention.AnswerJacoby2NT;
                    answer.HandShape[answer.declareBid.suit].Min = 5;
                    answer.HandShape[opening.declareBid.suit].Min = 5;
                    answer.Description = $"5+ {answer.declareBid.suit}";
                    answer.Priority = 1;
                }
            }
        }
    }
}