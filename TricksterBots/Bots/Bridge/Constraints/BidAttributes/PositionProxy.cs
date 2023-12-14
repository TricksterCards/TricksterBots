using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BridgeBidding
{ 
	class PositionProxy : StaticConstraint
	{
		private RelativePosition _relativePosition;
		private StaticConstraint _constraint;

		public enum RelativePosition { Partner, LeftHandOpponent, RightHandOpponent }
		public PositionProxy(RelativePosition relativePosition, Constraint constraint)
		{
			_relativePosition = relativePosition;
			_constraint = constraint as StaticConstraint;
			Debug.Assert(_constraint != null);
		}


		private PositionState GetPosition(PositionState positionState)
		{
			if (_relativePosition == RelativePosition.Partner) { return positionState.Partner; }
			if (_relativePosition == RelativePosition.LeftHandOpponent) { return positionState.LeftHandOpponent; }
			Debug.Assert(_relativePosition == RelativePosition.RightHandOpponent);
			return positionState.RightHandOpponent;
		}


		public override bool Conforms(Call call, PositionState ps)
		{
			var pos = GetPosition(ps);
			return _constraint.Conforms(call, pos);
		}
	}
}
