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
			this.OnceAndDone = true;
		}

		public override bool Conforms(Bid bid, PositionState ps, HandSummary hs)
		{
			return this._jumpLevels.Contains(bid.JumpOver(ps.BiddingState.Contract));
		}
	}
}
