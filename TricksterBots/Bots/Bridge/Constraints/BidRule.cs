using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{


	public class BidRule
	{
		public Bid Bid { get; }
		public int Priority { get; }

		private Constraint[] _constraints;
		public BidRule(Bid bid, int priority, params Constraint[] constraints)
		{
			this.Bid = bid;
			this.Priority = priority;
			this._constraints = constraints;
		}


		public bool Conforms(PositionState ps, HandSummary hs, BiddingSummary bs)
		{
			foreach (Constraint constraint in _constraints)
			{
				if (!constraint.Conforms(Bid, ps, hs, bs)) { return false; }
			}
			return true;
		}


		public void ShowState(PositionState ps)
		{
			var handSummary = new HandSummary(ps.PublicHandSummary);
			var biddingSummary = new BiddingSummary(ps.BiddingSummary);
			foreach (Constraint constraint in _constraints)
			{
				if (constraint is IShowsState showsState)
				{
					var hs = new HandSummary(ps.PublicHandSummary);
					var bs = new BiddingSummary(ps.BiddingSummary);
					showsState.Update(Bid, ps, hs, bs);
					handSummary.Union(hs);
					biddingSummary.Union(bs);

				}
			} 
		}
	}


}
