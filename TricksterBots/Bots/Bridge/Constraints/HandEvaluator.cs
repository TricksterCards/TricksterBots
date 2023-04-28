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
		public static void Evaluate(Hand hand, HandSummary hs)
		{
			// TODO: This is a hack but trying to get NT opening to work properly.  If balanced and in NT
			// opening range then don't add length points
			var balanced = BasicBidding.IsBalanced(hand);
			var p = BasicBidding.ComputeHighCardPoints(hand);
			if (!balanced || p < 15) { p += BasicBidding.ComputeDistributionPoints(hand); }
			hs.StartingPoints = (p, p);
			var counts = BasicBidding.CountsBySuit(hand);
			hs.IsBalanced = balanced;
			hs.IsFlat = BasicBidding.Is4333(counts);
            hs.CountAces = hand.Count(c => c.rank == Rank.Ace);
			hs.CountKings = hand.Count(c => c.rank == Rank.King);
            foreach (Suit suit in BasicBidding.BasicSuits)
			{
				var dp = p + BasicBidding.DummyPoints(hand, suit);
				var c = counts[suit];
				var q = Quality(hand, suit);
				hs.Suits[suit].Shape = (c, c);
				hs.Suits[suit].DummyPoints = (dp, dp);
				hs.Suits[suit].LongHandPoints = (p, p);
				hs.Suits[suit].Quality = (q, q);
				var keyCards = (int)hs.CountAces;
				if (hand.Contains(new Card(suit, Rank.King)))
				{
					keyCards += 1;
				}
				hs.Suits[suit].Keycards = (keyCards, keyCards);
				hs.Suits[suit].HaveQueen = hand.Contains(new Card(suit, Rank.Queen));
			}
			hs.Suits[Suit.Unknown].Shape = (0, 0);
			hs.Suits[Suit.Unknown].DummyPoints = (p, p);
			hs.Suits[Suit.Unknown].LongHandPoints = (p, p);
		}
	}
}
