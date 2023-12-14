using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace BridgeBidding
{
	public class IsBalanced : DynamicConstraint
	{
		protected bool _desiredValue;
		public IsBalanced(bool desiredValue)
		{
			this._desiredValue = desiredValue;
		}

		public override bool Conforms(Call call, PositionState ps, HandSummary hs)
		{
			return hs.IsBalanced == null || hs.IsBalanced == _desiredValue;
		}
	}

	public class ShowsBalanced : IsBalanced, IShowsState
	{
		public ShowsBalanced(bool desiredValue) : base(desiredValue) { }
	    void IShowsState.ShowState(Call call, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
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
