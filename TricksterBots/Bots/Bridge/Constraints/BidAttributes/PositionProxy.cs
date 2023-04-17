using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksterBots.Bots.Bridge
{ 
	class PositionProxy : Constraint
	{
		private RelativePosition _relativePosition;
		private Constraint _constraint;

		public enum RelativePosition { Partner, LeftHandOpponent, RightHandOpponent }
		public PositionProxy(RelativePosition relativePosition, Constraint constraint)
		{
			_relativePosition = relativePosition;
			_constraint = constraint;
			if (constraint as IShowsState != null)
			{
				throw new ArgumentException();
			}
		}


		private PositionState GetPosition(PositionState positionState)
		{
			if (_relativePosition == RelativePosition.Partner) { return positionState.Partner; }
			if (_relativePosition == RelativePosition.LeftHandOpponent) { return positionState.LeftHandOppenent; }
			Debug.Assert(_relativePosition == RelativePosition.RightHandOpponent);
			return positionState.RightHandOpponent;
		}


		public override bool Conforms(Bid bid, PositionState ps, HandSummary hs, BiddingSummary bs)
		{
			var pos = GetPosition(ps);
			return _constraint.Conforms(bid, pos, pos.PublicHandSummary, pos.BiddingSummary);
		}
	}
}
