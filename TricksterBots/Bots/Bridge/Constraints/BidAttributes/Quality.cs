using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{

	public enum SuitQuality { Poor = 0, Decent = 1, Good = 2, Excellent = 3, Solid = 4 }


	public class HasQuality : Constraint
	{
		protected Suit? _suit;
		protected SuitQuality _min;
		protected SuitQuality _max;

		public HasQuality(Suit? suit, SuitQuality min, SuitQuality max)
		{
			this._suit = suit;
			this._min = min;
			this._max = max;
		}


		public override bool Conforms(Bid bid, PositionState ps, HandSummary hs)
		{
			var quality = hs.Suits[bid.SuitIfNot(_suit)].GetQuality();
			return ((int)_min <= (int)quality.Max && (int)_max >= (int)quality.Min);
		}
	}

	public class ShowsQuality : HasQuality, IShowsState
	{
		public ShowsQuality(Suit? suit, SuitQuality min, SuitQuality max) : base(suit, min, max)
		{
		}

		void IShowsState.ShowState(Bid bid, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
			
		{
			showHand.Suits[bid.SuitIfNot(_suit)].ShowQuality(_min, _max);
		}
	}


}
