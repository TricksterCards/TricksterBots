using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksterBots.Bots.Bridge
{
	public class IsFlat : Constraint
	{
		protected bool _desiredValue;
		public IsFlat(bool desiredValue = true)
		{
			this._desiredValue = desiredValue;
		}

		public override bool Conforms(Bid bid, HandSummary handSummary, PositionState positionState)
		{
			return handSummary.IsFlat == null || handSummary.IsFlat == _desiredValue;
		}
	}

	public class ShowsFlat: IsFlat, IShowsState
	{
		public ShowsFlat(bool desiredValue = true) : base(desiredValue) { }
		public void UpdateState(Bid bid, ModifiableHandSummary handSummary, ModifiablePositionState positionState)
		{
			handSummary.ShowIsBalanced(_desiredValue);
		}
	}

}
