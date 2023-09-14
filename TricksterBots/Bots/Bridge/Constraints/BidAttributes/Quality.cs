using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{

	public enum SuitQuality { Poor = 0, Decent = 1, Good = 2, Excellent = 3, Solid = 4 }


	public class HasQuality : DynamicConstraint
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


		public override bool Conforms(Call call, PositionState ps, HandSummary hs)
		{
			if (GetSuit(_suit, call) is Suit suit)
			{
				var quality = hs.Suits[suit].GetQuality();
				return ((int)_min <= (int)quality.Max && (int)_max >= (int)quality.Min);
			}
			Debug.Fail("No suit for HasQuality constraint");
			return false;
		}
	}

	public class ShowsQuality : HasQuality, IShowsState
	{
		public ShowsQuality(Suit? suit, SuitQuality min, SuitQuality max) : base(suit, min, max)
		{
		}

		void IShowsState.ShowState(Call call, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)	
		{
			if (GetSuit(_suit, call) is Suit suit)
			{
				showHand.Suits[suit].ShowQuality(_min, _max);
			}
		}
	}


}
