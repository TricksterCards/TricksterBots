using System;
using System.Collections.Generic;
using System.Linq;
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

		private Constraint[] _contraints;
		public BidRule(int level, Suit suit, BidConvention convention, int priority, params Constraint[] constraints)
		{
			this.Bid = new Bid(level, suit, convention);
			this.Priority = priority;
			this._contraints = constraints;
		}
		public BidRule(CallType callType, BidConvention convention, int priority, params Constraint[] constraints)
		{
			this.Bid = new Bid(callType, convention);
			this._contraints = constraints;
		}


		public (bool DoesConform, bool CouldConform) Conforms(Direction direction, HandSummary handSummary, BiddingSummary biddingSummary)
		{
			foreach (Constraint constraint in _contraints)
			{
				if ((constraint as HiddenConstraint) == null &&
					!constraint.Conforms(Bid, direction, handSummary, biddingSummary)) { return (false, false); }
			}
			bool conforms = true;
			foreach (Constraint constraint in _contraints)
			{
				if (constraint is HiddenConstraint hiddenConstraint &&
					!hiddenConstraint.Conforms(Bid, direction, handSummary, biddingSummary))
				{
					conforms = false;
					if (!hiddenConstraint.CouldConform(Bid, direction, biddingSummary)) { return (false, false); }
				}
			}
			return (conforms, true);
		}

		public bool CouldConform(Direction direction, BiddingSummary biddingSummary)
		{
			foreach (Constraint constraint in _contraints)
			{
				if (constraint is HiddenConstraint hiddenConstraint)
				{
					if (!hiddenConstraint.CouldConform(Bid, direction, biddingSummary))
					{
						return false;
					}
				}
			}
			return true;
		}

		public ShownState ShownState(Direction direction, BiddingSummary biddingSummary)
		{
			var shownState = new ShownState();
			foreach (Constraint constraint in _contraints)
			{
				if (constraint is HiddenConstraint hiddenConstraint)
				{
					hiddenConstraint.UpdateShownState(Bid, direction, biddingSummary, shownState);
				}
			}
			return shownState;
		}
	}


}
