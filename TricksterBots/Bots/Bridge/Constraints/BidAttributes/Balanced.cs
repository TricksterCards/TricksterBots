using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public class IsBalanced : Constraint
	{
		protected bool _desiredValue;
		public IsBalanced(bool desiredValue)
		{
			this._desiredValue = desiredValue;
		}

		public override bool Conforms(Bid bid, PositionState ps, HandSummary hs)
		{
			return hs.IsBalanced == null || hs.IsBalanced == _desiredValue;
		}
	}

	public class ShowsBalanced : IsBalanced, IShowsState
	{
		public ShowsBalanced(bool desiredValue) : base(desiredValue) { }
	    void IShowsState.ShowState(Bid bid, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
		{
			showHand.ShowIsBalanced(_desiredValue);
			/*
			if (_desiredValue == true)
			{
				foreach (var suit in BasicBidding.BasicSuits)
				{
					showHand.Suits[suit].ShowShape(2, 5);
				}
			}
			*/
		}
	}

}
