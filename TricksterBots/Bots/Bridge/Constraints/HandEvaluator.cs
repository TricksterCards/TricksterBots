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
			var p = BasicBidding.ComputeHighCardPoints(hand) + BasicBidding.ComputeDistributionPoints(hand);
			hs.OpeningPoints = (p, p);
			var counts = BasicBidding.CountsBySuit(hand);
			hs.IsBalanced = BasicBidding.IsBalanced(hand);
			hs.IsFlat = BasicBidding.Is4333(counts);
			foreach (Suit suit in BasicBidding.BasicSuits)
			{
				var dp = BasicBidding.DummyPoints(hand, suit);
				var c = counts[suit];
				var q = Quality(hand, suit);
				hs.Suits[suit].Shape = (c, c);
				hs.Suits[suit].DummyPoints = (dp, dp);
				hs.Suits[suit].LongHandPoints = (p, p);
				hs.Suits[suit].Quality = (q, q);
			}
			hs.Suits[Suit.Unknown].Shape = (0, 0);
			hs.Suits[Suit.Unknown].DummyPoints = (p, p);
			hs.Suits[Suit.Unknown].LongHandPoints = (p, p);
			hs.CountAces = hand.Count(c => c.rank == Rank.Ace);
			hs.CountKings = hand.Count(c => c.rank == Rank.King);
		}
	}
}
