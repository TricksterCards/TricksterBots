using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public class RuleOf9 : DynamicConstraint
	{
		public override bool Conforms(Call call, PositionState ps, HandSummary hs)
		{
			if (ps.IsOpponentsContract && ps.BiddingState.Contract.Bid is Bid oppsBid && oppsBid.Suit is Suit oppsSuit)
			{
				int level = oppsBid.Level;
				if (hs.Suits[oppsSuit].RuleOf9Points is int ruleOf9Points)
				{
					return (level + ruleOf9Points >= 9);
				}
				return true;	// If we don't know the rule of 9 points then this is a private hand summary so
				// it is possible that it could conform.
			}
			return false;
		}
	}
}
