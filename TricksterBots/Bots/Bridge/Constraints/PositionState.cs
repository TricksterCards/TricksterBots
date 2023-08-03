using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public enum PositionRole { Opener, Overcaller, Responder, Advancer }


	public class PositionState
	{
		// When the first bid is made for a role, this variable is assigned to the length of _bids.  This allows
		// the property RoleRound to return the proper value.  For example, if a position has passed twice and then
		// places a bid as Advancer, the offset would be 2, indicating that advancer has 
		private int _roleAssignedOffset = 0;
		private bool _roleAssigned = false;

		private HandSummary _privateHandSummary;

		private List<BidRuleSet> _bids;

		public PairAgreements PairAgreements { get; private set; }

		public BiddingState BiddingState { get; }

		public PositionRole Role { get; internal set; }

		public HandSummary PublicHandSummary { get; private set; }


		public Direction Direction { get; }

		public int Seat { get; }
		public bool Vulnerable { get; }



		public Bid GetBidHistory(int historyLevel)
		{
			if (_bids.Count <= historyLevel)
			{
				return new Bid(Call.NotActed);
			}
			return _bids[_bids.Count - 1 - historyLevel].Bid;
		}

		public PositionState Partner => BiddingState.Positions[BasicBidding.Partner(Direction)];
		public PositionState RightHandOpponent => BiddingState.Positions[BasicBidding.RightHandOpponent(Direction)];
		public PositionState LeftHandOpponent => BiddingState.Positions[BasicBidding.LeftHandOpponent(Direction)];


		// TODO: Potentially LHO Interferred...  Maybe just in 


		public PositionState(BiddingState biddingState, Direction direction, int seat, bool vulnerable, Hand hand)
		{
			Debug.Assert(seat >= 1 && seat <= 4);
			this.BiddingState = biddingState;
			this.Direction = direction;
			this.Seat = seat;
			this.Role = PositionRole.Opener;    // Best start for any position.  Will change with time.
			this.Vulnerable = vulnerable;
			this.PublicHandSummary = new HandSummary();
			this.PairAgreements = new PairAgreements();
			this._bids = new List<BidRuleSet>();

			if (hand != null)
			{
				var showHand = new HandSummary.ShowState();
				// TODO: This is where we would need to use a differnet implementation of HandSummary evaluator...
				StandardHandEvaluator.Evaluate(hand, showHand);
				this._privateHandSummary = showHand.HandSummary;
			}
			else
			{
				this._privateHandSummary = null;
			}
		}

		public int BidRound
		{
			get { return this._bids.Count + 1; }
		}

		public int RoleRound
		{
			get { return BidRound - _roleAssignedOffset; }
		}

		public Bid LastBid { get { return GetBidHistory(0); } }

		
		public BidChoicesFactory GetBidsFactory()
		{
			return Partner._bids.Count > 0 ? Partner._bids.Last().GetBidsFactory(Partner) : null;
		}
	


		// THIS IS AN INTERNAL FUNCITON:
		public Bid MakeBid(BidRuleSet bidGroup)
		{
            if (!bidGroup.Bid.IsPass && !this._roleAssigned)
			{
				if (Role == PositionRole.Opener)
				{
					AssignRole(PositionRole.Opener);
					Partner.AssignRole(PositionRole.Responder);
					// The opponenents are now 
					LeftHandOpponent.Role = PositionRole.Overcaller;
					RightHandOpponent.Role = PositionRole.Overcaller;
				}
				else if (this.Role == PositionRole.Overcaller)
				{
					AssignRole(PositionRole.Overcaller);
					Partner.AssignRole(PositionRole.Advancer);
				}
			}
			_bids.Add(bidGroup);
			// Now we prune any rules that do not 

			if (RepeatUpdatesUntilStable(bidGroup))
			{
				BiddingState.UpdateStateFromFirstBid();
			}
			/*
			Debug.WriteLine($"   Points shown {PublicHandSummary.StartingPoints}");
			foreach (var suit in BasicBidding.BasicSuits)
			{
				var shape = PublicHandSummary.Suits[suit].Shape;
				if (shape.Min > 0 || shape.Max < 13)
				{
					Debug.WriteLine($"   {suit} shape {shape.Min} -> {shape.Max}");
				}
			}
			*/
			return bidGroup.Bid;
		}

		private void AssignRole(PositionRole role)
		{
			Debug.Assert(_roleAssigned == false);
			Role = role;
			_roleAssigned = true;
			_roleAssignedOffset = _bids.Count;
		}

		internal bool UpdateBidIndex(int bidIndex, out bool updateHappened)
		{
			if (bidIndex >= _bids.Count)
			{
				updateHappened = false;
				return false;
			}
			updateHappened = RepeatUpdatesUntilStable(this._bids[bidIndex]);
			return true;
		}

		internal bool RepeatUpdatesUntilStable(BidRuleSet bidGroup)
		{
			bool stateChanged = false;
			for (int i = 0; i < 1000; i++)
			{
                stateChanged |= bidGroup.PruneRules(this);

                (HandSummary hs, PairAgreements pa) newState = bidGroup.ShowState(this);

				var showHand = new HandSummary.ShowState(PublicHandSummary);
				var showAgreements = new PairAgreements.ShowState(PairAgreements);

				showHand.Combine(newState.hs, State.CombineRule.Merge);
				showAgreements.Combine(newState.pa, State.CombineRule.Merge);


				if (this.PublicHandSummary.Equals(showHand.HandSummary) &&
					this.PairAgreements.Equals(showAgreements.PairAgreements)) 
				{ 
					return stateChanged;
				}
				stateChanged = true;
				this.PublicHandSummary = showHand.HandSummary;
				this.PairAgreements = showAgreements.PairAgreements;
				this.Partner.PairAgreements = this.PairAgreements;
			}
			Debug.Assert(false); // This is bad - we had over 1000 state changes.  Infinite loop time...
			return false;	// Seems the best thing to do to avoid repeated
		}


		public bool IsOpponent(PositionState other)
		{
			return (other == this.LeftHandOpponent || other == this.RightHandOpponent);
		}

		/* -- TODO: Seems unused...
		internal (HandSummary, PairAgreements) Update(IShowsState showsState, Bid bid)
		{
			var hs = new HandSummary(this.PublicHandSummary);
			var bs = new PairAgreements(this.PairAgreements);
			showsState.Update(bid, this, hs, bs);
			return (hs, bs);
		}
		*/


		public bool PrivateHandConforms(BidRule rule)
		{
			return (this._privateHandSummary == null) ? false : rule.SatisifiesDynamicConstraints(this, this._privateHandSummary);
		}

		public bool IsValidNextBid(Bid bid)
		{
			return BiddingState.NextToAct == this && BiddingState.IsValidNextBid(bid);
		}

        // TODO: Just a start of taking a group of rules and returning a subest
        // TODO: NEED TO ADD -PRIORITY BIDS FOR FALL-BACK. THESE SHOULD BE IGNORED IN THE FIRST ROUND
		/*
        public BidRuleSet ChooseBid(Dictionary<Bid, BidRuleSet> rules)
		{
			Debug.Assert(_privateHandSummary != null);
			BidRuleSet choice = null;
			var priority = int.MinValue;
			foreach (var ruleGroup in rules.Values)
			{
				if ((choice == null || ruleGroup.Priority > priority) &&
					ruleGroup.Conforms(this, _privateHandSummary))
				{
					choice = ruleGroup;
					priority = ruleGroup.Priority;
				}
			}

			if (choice == null)
			{
				// UGLY TODO: CLEAN THIS UP!!!
				//Debug.WriteLine("***Generating bogus pass***");
                var pass = new BidRule(new Bid(Call.Pass, BidForce.Nonforcing), 0, new Constraint[0]); 
				choice = new BidRuleSet(pass.Bid, Convention.Natural, null);
				choice.Add(pass);
			}
            return choice;
		}
		*/

	}
}
