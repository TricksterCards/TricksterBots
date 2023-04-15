using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{


	public class BiddingState
	{

		public Dictionary<Direction, PositionState> Positions { get; }
		public enum Vulxxx { None, Both, NS, EW };
		public Vulxxx VulXXX { get; }

		public bool IsVulnerable(Direction direction)
		{
			switch (VulXXX)
			{
				case Vulxxx.None:
					return false;
				case Vulxxx.Both:
					return true;
				case Vulxxx.NS:
					return (direction == Direction.North || direction == Direction.South);
				case Vulxxx.EW:
					return (direction == Direction.East || direction == Direction.West);
				default:
					Debug.Assert(false);	// Should never get here...
					return false;
			}
			
		}

		public BiddingState(Hand[] hands, Direction dealer, Vulxxx vulxxx)
		{
			this.VulXXX = vulxxx;
			this.Positions = new Dictionary<Direction, PositionState>();
			Debug.Assert(hands.Length == 4);
			var d = dealer;
			for (int i = 0; i < hands.Length; i++)
			{
				this.Positions[d] = new PositionState(this, d, i + 1, hands[i]);
			}
		}
	}

	public class PositionState
	{
		// When the first bid is made for a role, this variable is assigned to the length of _bids.  This allows
		// the property RoleRound to return the proper value.  For example, if a position has passed twice and then
		// places a bid as Advancer, the offset would be 2, indicating that advancer has 
		private int _roleAssignedOffset = 0;
		private bool _roleAssigned = false;

		private ModifiableHandSummary _publicHandSummary;

		private HandSummary _privateHandSummary;

		private List<BidRuleGroup> _bids { get; }

		public ShownState ShownState { get; }
		public BiddingState BiddingState { get; }

		public PositionRole Role { get; internal set; }

		public HandSummary ShownHandSummary
		{
			get { return _publicHandSummary; }
		}

		public Direction Direction { get; }

		public int Seat { get; }

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

		public Dictionary<Suit, (int Min, int Max)> ShownShape {  get; }

		//public Dictionary<Suit, SuitSummary> Suits { get; }


		public bool Vulnerable
		{
			get {
				return BiddingState.IsVulnerable(this.Direction); 
			}
		}

	

		public PositionState(BiddingState biddingState, Direction direction, int seat, Hand hand)
		{
			Debug.Assert(seat >= 1 && seat <= 4);
			this.BiddingState = biddingState;
			this.Direction = direction;
			this.Seat = seat;
			this.Role = PositionRole.Opener;    // Best start for any position.  Will change with time.
		//	this.ShownState = new ShownState();
			this._publicHandSummary = new ModifiableHandSummary();

			if (hand != null)
			{
				var hs = new ModifiableHandSummary();
				// TODO: This is where we would need to use a differnet implementation of HandSummary evaluator...
				StandardHandEvaluator.Evaluate(hand, hs);
				this._privateHandSummary = hs;
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

	}

	// TODO: Is this correct?  Probalby always create the modifiable one but pass the other to the constraint interfaces..
	public class ModifiablePositionState : PositionState
	{
		public ModifiablePositionState(BiddingState biddingState, Direction direction, int seat, Hand hand) : base(biddingState, direction, seat, hand)
		{
		}
	}

}
