using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{


	public class BidRule
	{
        public enum BidForce { Nonforcing, Invitational, Forcing, Signoff }

        public Bid Bid { get; }

		public BidForce Force { get; }


		private IEnumerable<Constraint> _constraints;
		public BidRule(Bid bid, BidForce force, params Constraint[] constraints)
		{
			this.Bid = bid;
			this.Force = force;
			this._constraints = constraints;
		}


		public void AddConstraint(Constraint constraint)
		{
			this._constraints = this._constraints.Append(constraint);
		}

		public bool SatisifiesStaticConstraints(PositionState ps)
		{
			foreach (Constraint constraint in _constraints)
			{
				if (constraint.OnceAndDone && !constraint.Conforms(Bid, ps, null))
				{
					return false;
				}
			}
			return true;


		}



        public bool SatisifiesDynamicConstraints(PositionState ps, HandSummary hs)
        {
            foreach (Constraint constraint in _constraints)
            {
                if (!constraint.OnceAndDone &&
					!constraint.Conforms(Bid, ps, hs)) { return false; }
            }
            return true;
        }

		/*
        public bool Conforms(bool onceAndDoneOnly, PositionState ps, HandSummary hs)
		{
			foreach (Constraint constraint in _constraints)
			{
				if (onceAndDoneOnly)
				{
					Debug.Assert(hs == null);	// Once and done rules can not rely on hand summary
					if (constraint.OnceAndDone && !constraint.Conforms(Bid, ps, hs)) { return false; }
				}
				else
				{ 
					if (!constraint.OnceAndDone && !constraint.Conforms(Bid, ps, hs)) { return false; }
				}
			}
			return true;
		}
		*/

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


	public class PartnerBidRule : BidRule
	{
		public BidChoicesFactory PartnerBidFactory { get; private set; }
		public Bid GoodThrough { get; private set; }

        public PartnerBidRule(Bid bid, Bid goodThrough, BidChoicesFactory partnerBids, params Constraint[] constraints) :
			base(bid, BidForce.Nonforcing, constraints)
        {
            this.PartnerBidFactory = partnerBids;
			this.GoodThrough = goodThrough;
        }
    }

}
