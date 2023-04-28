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


		public bool Conforms(bool firstInvocation, PositionState ps, HandSummary hs, PairAgreements pa)
		{
			foreach (Constraint constraint in _constraints)
			{
				if (firstInvocation || constraint.OnceAndDone == false)
				{
					if (!constraint.Conforms(Bid, ps, hs, pa)) { return false; }
				}
			}
			return true;
		}


		public (HandSummary, PairAgreements) ShowState(PositionState ps)
		{
			var handSummary = new HandSummary(ps.PublicHandSummary);
			var PairAgreements = new PairAgreements(ps.PairAgreements);
			foreach (Constraint constraint in _constraints)
			{
				if (constraint is IShowsState showsState)
				{
					var hs = new HandSummary(ps.PublicHandSummary);
					var bs = new PairAgreements(ps.PairAgreements);
					showsState.Update(Bid, ps, hs, bs);
					handSummary.Intersect(hs);
					PairAgreements.Intersect(bs);

				}
			}
			return (handSummary, PairAgreements);
		}
	}


}
