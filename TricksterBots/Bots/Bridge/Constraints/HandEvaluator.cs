using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;



namespace BridgeBidding
{
	class StandardHandEvaluator
	{
		private static bool Stopped(Hand hand, Suit suit, int countSuit)
		{
			return hand.HighCardPoints(suit) + countSuit >= 5;
		}



		// This is the Audrey Grant defined way of looking at dummy points.  It is not
		// ideal or advanced in any way.  
        public static int AudreyDummyPoints(Hand hand, Suit trumpSuit)
        {
            var trumpCount = hand.Count(c => c.Suit == trumpSuit);
            int adjust = 0;
            if (trumpCount >= 3)
            {
                int[] bonus = { 5, 3, 1 };
                // This is the Audrey Grant version of counting as dummy.  Don't care about shortness of honors...
                foreach (Suit suit in Card.Suits)
                {
                    var count = hand.Count(c => c.Suit == suit);
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
			switch (hand.HighCardPoints(suit))
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
					q = (hand.IsGoodSuit(suit)) ? SuitQuality.Good : SuitQuality.Decent; break;
				default:
					q = SuitQuality.Poor;
					break;
			}
			return q;
		}
		public static void Evaluate(Hand hand, HandSummary.ShowState hs)
		{
			var hcp = hand.HighCardPoints();
			hs.ShowHighCardPoints(hcp, hcp);
			var startPoints = hcp + hand.LengthPoints(); 
			hs.ShowStartingPoints(startPoints, startPoints);
            hs.ShowNoTrumpDummyPoints(startPoints, startPoints);
            hs.ShowNoTrumpLongHandPoints(startPoints, startPoints);
            var counts = hand.CountsBySuit();
			hs.ShowIsBalanced(hand.IsBalanced);
			hs.ShowIsFlat(hand.Is4333);
			int countAces = hand.Count(c => c.Rank == Rank.Ace);
            hs.ShowCountAces(new HashSet<int> { countAces });
			int countKings = hand.Count(c => c.Rank == Rank.King);
			hs.ShowCountKings(new HashSet<int> { countKings });
            foreach (Suit suit in Card.Suits)
			{
				var dp = hcp + AudreyDummyPoints(hand, suit);
				var c = counts[suit];
				var q = Quality(hand, suit);
				hs.Suits[suit].ShowShape(c, c);
				hs.Suits[suit].ShowDummyPoints(dp, dp);
				hs.Suits[suit].ShowLongHandPoints(startPoints, startPoints);
				hs.Suits[suit].ShowQuality(q, q);
				var keyCards = countAces;
				if (hand.Contains(new Card(Rank.King, suit)))
				{
					keyCards += 1;
				}
				hs.Suits[suit].ShowKeyCards(new HashSet<int> { keyCards });
				hs.Suits[suit].ShowHaveQueen(hand.Contains(new Card(Rank.Queen, suit)));
				hs.Suits[suit].ShowStopped(Stopped(hand, suit, c));

				// Compute "rule of 9" score for the suit.  This is the count of cards + count of all cards higher
				// than 10 in that suit.
				int rule9 = c;
				foreach (Card card in hand)
				{
					if (card.Suit == suit && card.Rank >= Rank.Ten)
					{
						rule9++;
					}
				}
				hs.Suits[suit].ShowRuleOf9Points(rule9);

			}
		}
	}
}
