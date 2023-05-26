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

		private IEnumerable<Constraint> _constraints;
		public BidRule(Bid bid, int priority, params Constraint[] constraints)
		{
			this.Bid = bid;
			this.Priority = priority;
			this._constraints = constraints;
		}


		public void AddConstraint(Constraint constraint)
		{
			this._constraints = this._constraints.Append(constraint);
		}

		public bool Conforms(bool firstInvocation, PositionState ps, HandSummary hs)
		{
			foreach (Constraint constraint in _constraints)
			{
				if (firstInvocation || constraint.OnceAndDone == false)
				{
					if (!constraint.Conforms(Bid, ps, hs)) { return false; }
				}
			}
			return true;
		}


		public (HandSummary, PairAgreements) ShowState(PositionState ps)
		{
			bool showedSuit = false;
			var showHand = new HandSummary.ShowState();
			var showAgreements = new PairAgreements.ShowState();
			foreach (Constraint constraint in _constraints)
			{
				if (constraint is IShowsState showsState)
				{
					showsState.ShowState(Bid, ps, showHand, showAgreements);
				}
				if (constraint is ShowsSuit) { showedSuit = true; }
			}
			if (!showedSuit && Bid.Suit != null)		// TODO: Should this be the case for Suit.Unknown too?  Think this through.  Right now I think yes.
			{
				var showSuit = new ShowsSuit(true) as IShowsState;
				showSuit.ShowState(Bid, ps, showHand, showAgreements);
			}
			return (showHand.HandSummary, showAgreements.PairAgreements);
		}
	}


}
