using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public class Balanced : Constraint
	{
		private bool _desiredValue;
		public Balanced(bool desiredValue = true)
		{
			this._desiredValue = desiredValue;
		}

		public override bool Conforms(Bid bid, HandSummary handSummary, PositionState positionState)
		{
			return handSummary.IsBalanced == null || handSummary.IsBalanced == _desiredValue;
		}
	}


}
