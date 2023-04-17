using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public enum PositionRole { Opener, Overcaller, Responder, Advancer }


	public class BiddingState
	{

		public Dictionary<Direction, PositionState> Positions { get; }


		public PositionState Dealer { get; private set; }
		
		public PositionState NextToAct { get; private set; }


		public static bool IsVulnerable(string vul, Direction direction)
		{
			switch (vul)
			{
				case "None":
				case "Love": 
				case "-":
					return false;
				case "All":
				case "Both":
					return true;
				case "NS":
					return (direction == Direction.North || direction == Direction.South);
				case "EW":
					return (direction == Direction.East || direction == Direction.West);
				default:
					// TODO: Throw???
					Debug.Assert(false);    // Should never get here...
					return false;
			}

		}

		public BiddingState(Hand[] hands, Direction dealer, string vul)
		{
			this.Positions = new Dictionary<Direction, PositionState>();
			Debug.Assert(hands.Length == 4);
			var d = dealer;
			for (int i = 0; i < hands.Length; i++)
			{
				this.Positions[d] = new PositionState(this, d, i + 1, IsVulnerable(vul, d), hands[i]);
				d = BasicBidding.LeftHandOpponent(d);
			}
			this.Dealer = Positions[dealer];
			this.NextToAct = Dealer;
		}
	}


	public class PositionState
	{
		// When the first bid is made for a role, this variable is assigned to the length of _bids.  This allows
		// the property RoleRound to return the proper value.  For example, if a position has passed twice and then
		// places a bid as Advancer, the offset would be 2, indicating that advancer has 
		private int _roleAssignedOffset = 0;
		private bool _roleAssigned = false;

		private HandSummary _publicHandSummary;

		private HandSummary _privateHandSummary;

		private BiddingSummary _biddingSummary;

		private List<BidRuleGroup> _bids;

		public BiddingState BiddingState { get; }

		public PositionRole Role { get; internal set; }

		public HandSummary PublicHandSummary
		{
			// TODO: Consider returning a copy so that it can never be modified...  
			get { return _publicHandSummary; }
		}

		public Direction Direction { get; }

		public int Seat { get; }
		public bool Vulnerable { get; }

		public Bid LastBid
		{
			get
			{
				return (_bids.Count == 0) ? new Bid(CallType.NotActed) : _bids.Last().Bid;
			}
		}

		public PositionState Partner => BiddingState.Positions[BasicBidding.Partner(Direction)];
		public PositionState RightHandOpponent => BiddingState.Positions[BasicBidding.RightHandOpponent(Direction)];
		public PositionState LeftHandOppenent => BiddingState.Positions[BasicBidding.LeftHandOpponent(Direction)];


		// TODO: Potentially LHO Interferred...  Maybe just in 


		public PositionState(BiddingState biddingState, Direction direction, int seat, bool vulnerable, Hand hand)
		{
			Debug.Assert(seat >= 1 && seat <= 4);
			this.BiddingState = biddingState;
			this.Direction = direction;
			this.Seat = seat;
			this.Role = PositionRole.Opener;    // Best start for any position.  Will change with time.
			this.Vulnerable = vulnerable;
			this._publicHandSummary = new HandSummary();

			if (hand != null)
			{
				var hs = new HandSummary();
				// TODO: This is where we would need to use a differnet implementation of HandSummary evaluator...
				StandardHandEvaluator.Evaluate(hand, hs);
				this._privateHandSummary = hs;
			} 
			else
			{ 
				this._privateHandSummary = null; 
			}

			this._biddingSummary = new BiddingSummary();
		}

		public int BidRound
		{
			get { return this._bids.Count + 1; }
		}

		public int RoleRound
		{
			get { return BidRound - _roleAssignedOffset;  }
		}

	

		// THIS IS AN INTERNAL FUNCITON:
		private void MakeBid(BidRuleGroup bidGroup)
		{
			if (!bidGroup.Bid.IsPass && !this._roleAssigned)
			{
				if (Role == PositionRole.Opener)
				{
					AssignRole(PositionRole.Opener);
					Partner.AssignRole(PositionRole.Responder);

				}
				else if (this.Role == PositionRole.Overcaller)
				{
					AssignRole(PositionRole.Overcaller);
					Partner.AssignRole(PositionRole.Advancer);
				}
			}
			_bids.Append(bidGroup);
		}

		private void AssignRole(PositionRole role)
		{
			Debug.Assert(_roleAssigned == false);
			Role = role;
			_roleAssigned = true;
			_roleAssignedOffset = _bids.Count;
		}

		internal bool ConformsToPublicHand(Constraint constraint, Bid bid)
		{
			return constraint.Conforms(bid, this, this._publicHandSummary, this._biddingSummary);
		}

		internal bool ConformsToPrivateHand(Constraint constraint, Bid bid)
		{
			return constraint.Conforms(bid, this, this._privateHandSummary, this._biddingSummary);
		}

		internal (HandSummary, BiddingSummary) Update(IShowsState showsState, Bid bid)
		{
			var hs = new HandSummary(this._publicHandSummary);
			var bs = new BiddingSummary(this._biddingSummary);
			showsState.Update(bid, this, hs, bs);
			return (hs, bs);
		}
	}
}
