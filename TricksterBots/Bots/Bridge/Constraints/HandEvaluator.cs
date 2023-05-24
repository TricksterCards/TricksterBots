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
			var p = BasicBidding.ComputeHighCardPoints(hand);
			hs.ShowHighCardPoints(p, p);
			p += BasicBidding.ComputeDistributionPoints(hand); 
			hs.ShowStartingPoints(p, p);
			var counts = BasicBidding.CountsBySuit(hand);
			hs.ShowIsBalanced(BasicBidding.IsBalanced(hand));
			hs.ShowIsFlat(BasicBidding.Is4333(counts));
			int countAces = hand.Count(c => c.rank == Rank.Ace);
            hs.ShowCountAces(countAces);
			hs.ShowCountKings(hand.Count(c => c.rank == Rank.King));
            foreach (Suit suit in BasicBidding.BasicSuits)
			{
				var dp = p + BasicBidding.DummyPoints(hand, suit);
				var c = counts[suit];
				var q = Quality(hand, suit);
				hs.Suits[suit].ShowShape(c, c);
				hs.Suits[suit].ShowDummyPoints(dp, dp);
				hs.Suits[suit].ShowLongHandPoints(p, p);
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
			hs.Suits[Suit.Unknown].ShowDummyPoints(p, p);
			hs.Suits[Suit.Unknown].ShowLongHandPoints(p, p);
		}
	}
}
