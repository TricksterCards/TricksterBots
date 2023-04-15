using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{

	abstract public class BidAttribute { }

	public interface IHandConstraint 
	{
		bool Conforms(Bid bid, HandSummary handSummary, PositionState positionState);
		void UpdateState(Bid bid, ModifiableHandSummary handSummary, PositionState positionState);
	}

	public interface IPositionConstraint 
	{
		bool Conforms(Bid bid, PositionState positionState);
		void UpdateState(Bid bid, ModifiablePositionState positionState);
	}




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
