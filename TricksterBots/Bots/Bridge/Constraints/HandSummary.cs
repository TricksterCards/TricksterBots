using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{


	public abstract class HandSummary 
	{
		public Hand Hand { get; }

		public Dictionary<Suit, int> Counts { get; }
		public bool IsBalanced { get; }
		public bool Is4333 { get; }

		public abstract int OpeningPoints { get; }

		public abstract int DummyPoints(Suit trumpSuit);

		public HandSummary(Hand hand)
		{
			Hand = hand;
			Counts = BasicBidding.CountsBySuit(hand);
			IsBalanced = BasicBidding.IsBalanced(hand);
			Is4333 = BasicBidding.Is4333(Counts);
		}

	}




	public class StandardEvaluator : HandSummary
	{
		private int _hcp;
		private int _distributionPoints;
		public StandardEvaluator(Hand hand) : base(hand) 
		{
			this._hcp = BasicBidding.ComputeHighCardPoints(hand);
			this._distributionPoints = BasicBidding.ComputeDistributionPoints(hand);
		}

		public override int DummyPoints(Suit suit)
		{
			return _hcp + BasicBidding.DummyPoints(Hand, suit);
		}

		public override int OpeningPoints => _distributionPoints;
	}
}
