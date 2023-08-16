using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksterBots.Bots.Bridge
{
	public class JumpBid : Constraint
	{
		private int[] _jumpLevels;
		public JumpBid(params int[] jumpLevels)
		{
			this._jumpLevels = jumpLevels;
			this.StaticConstraint = true;
		}

		public override bool Conforms(Call call, PositionState ps, HandSummary hs)
		{
			if (call is Bid bid)
			{
				return this._jumpLevels.Contains(ps.BiddingState.Contract.Jump(bid));
			}
			return false;
		}
	}
}
