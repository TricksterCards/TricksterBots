using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksterBots.Bots.Bridge
{
	public class CompositeConstraint : Constraint
	{
		protected Constraint _c1;
		protected Constraint _c2;
		public CompositeConstraint(Constraint c1, Constraint c2)
		{
			this._c1 = c1;
			this._c2 = c2;
		}

		public override bool Conforms(Bid bid, HandSummary handSummary, PositionState positionState)
		{
			return _c1.Conforms(bid, handSummary, positionState) &&
					_c2.Conforms(bid, handSummary, positionState);
		}
	}

	public class CompositeShowsState : CompositeConstraint, IShowsState
	{
		public CompositeShowsState(Constraint c1, Constraint c2) : base (c1, c2)
		{ }
		public void UpdateState(Bid bid, ModifiableHandSummary handSummary, ModifiablePositionState positionState)
		{
			if (_c1 is IShowsState c1)
			{
				c1.UpdateState(bid, handSummary, positionState);
			}
			if (_c2 is IShowsState c2)
			{
				c2.UpdateState(bid, handSummary, positionState);
			}
		}
	}

}
