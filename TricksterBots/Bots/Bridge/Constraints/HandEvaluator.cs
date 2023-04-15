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
		public static void Evaluate(Hand hand, ModifiableHandSummary hs)
		{
			///this._hcp = BasicBidding.ComputeHighCardPoints(hand);
			var p = BasicBidding.ComputeDistributionPoints(hand);
			hs.ShowOpeningPoints(p, p);
			var counts = BasicBidding.CountsBySuit(hand);
			hs.ShowIsBalanced(BasicBidding.IsBalanced(hand));
			hs.ShowIsFlat(BasicBidding.Is4333(counts));
			foreach (Suit suit in BasicBidding.BasicSuits)
			{
				var dp = BasicBidding.DummyPoints(hand, suit);
				var c = counts[suit];
				var q = Quality(hand, suit);
				hs.ModifiableSuits[suit].ShowShape(c, c);
				hs.ModifiableSuits[suit].ShowDummyPoints(dp, dp);
				hs.ModifiableSuits[suit].ShowLongHandPoints(p, p);
				hs.ModifiableSuits[suit].ShowQuality(q, q);
			}
			hs.ModifiableSuits[Suit.Unknown].ShowShape(0, 0);
			hs.ModifiableSuits[Suit.Unknown].ShowDummyPoints(p, p);
			hs.ModifiableSuits[Suit.Unknown].ShowLongHandPoints(p, p);

		}
}
