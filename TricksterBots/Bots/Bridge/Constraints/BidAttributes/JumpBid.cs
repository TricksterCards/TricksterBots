using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BridgeBidding
{
	public class JumpBid : StaticConstraint
	{
		private int[] _jumpLevels;
		public JumpBid(params int[] jumpLevels)
		{
			this._jumpLevels = jumpLevels;
		}

		public override bool Conforms(Call call, PositionState ps)
		{
			if (call is Bid bid)
			{
				return this._jumpLevels.Contains(ps.BiddingState.Contract.Jump(bid));
			}
			return false;
		}
	}
}
