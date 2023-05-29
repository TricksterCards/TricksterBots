using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	class StandardHandEvaluator
	{
		private static bool Stopped(Hand hand, Suit suit, int countSuit)
		{
			return BasicBidding.ComputeHighCardPoints(hand, suit) + countSuit >= 5;
		}



		// This is the Audrey Grant defined way of looking at dummy points.  It is not
		// ideal or advanced in any way.  
        public static int AudreyDummyPoints(Hand hand, Suit trumpSuit)
        {
            var trumpCount = hand.Count(c => c.suit == trumpSuit);
            int adjust = 0;
            if (trumpCount >= 3)
            {
                int[] bonus = { 5, 3, 1 };
                // This is the Audrey Grant version of counting as dummy.  Don't care about shortness of honors...
                foreach (Suit suit in BasicBidding.BasicSuits)
                {
                    var count = hand.Count(c => c.suit == suit);
                    if (count < 3)
                    {
                        adjust += bonus[count];
                    }
                }
            }
            return adjust;
        }


        private static SuitQuality Quality(Hand hand, Suit suit)
		{
			var q = SuitQuality.Poor;
			switch (BasicBidding.ComputeHighCardPoints(hand, suit))
			{
				case 10:
					q = SuitQuality.Solid; break;
				case 8:
				case 9:
					q = SuitQuality.Excellent; break;
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
					q = (BasicBidding.IsGoodSuit(hand, suit)) ? SuitQuality.Good : SuitQuality.Decent; break;
				default:
					q = SuitQuality.Poor;
					break;
			}
			return q;
		}
		public static void Evaluate(Hand hand, HandSummary.ShowState hs)
		{
			var hcp = BasicBidding.ComputeHighCardPoints(hand);
			hs.ShowHighCardPoints(hcp, hcp);
			var startPoints = hcp + BasicBidding.ComputeDistributionPoints(hand); 
			hs.ShowStartingPoints(startPoints, startPoints);
			var counts = BasicBidding.CountsBySuit(hand);
			hs.ShowIsBalanced(BasicBidding.IsBalanced(hand));
			hs.ShowIsFlat(BasicBidding.Is4333(counts));
			int countAces = hand.Count(c => c.rank == Rank.Ace);
            hs.ShowCountAces(countAces);
			hs.ShowCountKings(hand.Count(c => c.rank == Rank.King));
            foreach (Suit suit in BasicBidding.BasicSuits)
			{
				var dp = hcp + AudreyDummyPoints(hand, suit);
				var c = counts[suit];
				var q = Quality(hand, suit);
				hs.Suits[suit].ShowShape(c, c);
				hs.Suits[suit].ShowDummyPoints(dp, dp);
				hs.Suits[suit].ShowLongHandPoints(startPoints, startPoints);
				hs.Suits[suit].ShowQuality(q, q);
				var keyCards = countAces;
				if (hand.Contains(new Card(suit, Rank.King)))
				{
					keyCards += 1;
				}
				hs.Suits[suit].ShowKeycards(keyCards, keyCards);
				hs.Suits[suit].ShowHaveQueen(hand.Contains(new Card(suit, Rank.Queen)));
				hs.Suits[suit].ShowStopped(Stopped(hand, suit, c));
			}
			hs.Suits[Suit.Unknown].ShowDummyPoints(startPoints, startPoints);
			hs.Suits[Suit.Unknown].ShowLongHandPoints(startPoints, startPoints);
		}
	}
}
