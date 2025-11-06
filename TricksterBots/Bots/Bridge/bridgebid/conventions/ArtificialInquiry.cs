using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal class ArtificialInquiry
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (bid.Index >= 4 && bid.History[bid.Index - 2].BidConvention == BidConvention.ArtificialInquiry)
            {
                InterpretRebid(bid.History[bid.Index - 4], bid);
                return true;
            }

            return false;
        }

        private static void InterpretRebid(InterpretedBid opening, InterpretedBid rebid)
        {
            if (!rebid.bidIsDeclare)
                return;

            // 2D-2N-3N
            // 2H-2N-3N
            // 2S-2N-3N
            if (rebid.declareBid.suit == Suit.Unknown)
            {
                if (rebid.declareBid.level == 3)
                {
                    rebid.Points.Min = 9;
                    rebid.Points.Max = 10;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.Description = "Maximum";
                }
            }
            // 2D-2N-3D
            // 2H-2N-3H
            // 2S-2N-3S
            else if (rebid.declareBid.suit == opening.declareBid.suit)
            {
                if (rebid.declareBid.level == 3)
                {
                    rebid.Points.Min = 5;
                    rebid.Points.Max = 8;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.Description = "Minimum";
                }
            }
            // 2D-2N-3C/3H/3S
            // 2H-2N-3C/3D/3S
            // 2S-2N-3C/3D/3H
            else
            {
                //  new suit shows maximum and a feature (Ace or protected King/Queen) in that suit
                rebid.Points.Min = 9;
                rebid.Points.Max = 10;
                rebid.BidPointType = BidPointType.Hcp;
                rebid.Description = $"Maximum with Ace/King/Queen in {rebid.declareBid.suit}";
                rebid.Validate = hand =>
                {
                    var cardsInSuit = hand.Where(c => c.suit == rebid.declareBid.suit).ToList();
                    return cardsInSuit.Any(c =>
                        c.rank == Rank.Ace || c.rank == Rank.King && cardsInSuit.Count > 1 || c.rank == Rank.Queen && cardsInSuit.Count > 2);
                };
            }
        }
    }
}