using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace BridgeBidding
{
    public class Contract
    {
        public Bid Bid = null;    
        public PositionState LastBidBy = null;
		public PositionState Declarer = null;
        public bool Doubled = false;
        public bool Redoubled = false;
		public int CallsRemaining = 4;
		public Dictionary<Strain, List<PositionState>> FirstToNameStrain = new Dictionary<Strain, List<PositionState>>();


		public bool IsOurs(PositionState ps)
		{
			return (Declarer != null && Declarer.PairState == ps.PairState);
		}


		public bool IsOpponents(PositionState ps)
		{
			return (Declarer != null && Declarer.PairState != ps.PairState);
		}

		public bool IsValid(Call call, PositionState by)
		{
			if (AuctionComplete || call == null) return false;
			if (call is Pass) return true;
			if (call is Double)
			{
				return (!Doubled && IsOpponents(by));
			}
			if (call is Redouble)
			{
				return (!Redoubled && Doubled && IsOurs(by));
			}
			if (call is Bid newBid)
			{
				return (this.Bid == null || newBid.CompareTo(this.Bid) > 0);
			}
			Debug.Fail("Should never get to this line");
			return false;
		}

		private void MakeBid(Bid bid, PositionState by)
		{
            Bid = bid;
            LastBidBy = by;
            Doubled = false;
            Redoubled = false;
            CallsRemaining = 3;
			// Now figure out who is declarer.  First of our pair to bid a stain.
			// For simplicity we assume the current bidder will be declarer.  Code below
			// will change it if necessary
			Declarer = by;
			if (this.FirstToNameStrain.ContainsKey(bid.Strain))
			{
				foreach (var namedStrain in FirstToNameStrain[bid.Strain])
				{
					if (namedStrain == by) return;
					if (namedStrain == by.Partner)
					{
						Declarer = by.Partner;
						return;
					}
				}
				// If we get here then the current pair (the "by" position) has not
				// bid this strain, but the opponents have.  Add the current bidder
				// to the list for this strain.
				FirstToNameStrain[bid.Strain].Add(by);
			}
			else
			{
				FirstToNameStrain[bid.Strain] = new List<PositionState> { by };
			}
        }

        public bool MakeCall(Call call, PositionState by)
		{
			if (IsValid(call, by))
			{
				if (call is Pass)
				{
					CallsRemaining -= 1;
				}
				else if (call is Bid bid)
				{
					MakeBid(bid, by);
				}
				else if (call is Double)
				{
					Doubled = true;
					CallsRemaining = 3;
				}
				else if (call is Redouble)
				{
					Redoubled = true;
					Debug.Assert(this.Doubled);
					CallsRemaining = (Bid.Level == 7 && Bid.Strain == Strain.NoTrump) ? 0 : 3;
				}
				return true;
			}
			return false;
		}

		public int Jump(Bid bid)
		{
			return (bid == null) ? bid.Level - 1 : bid.JumpOver(Bid);
		}

		public bool PassEndsAuction { get { return this.CallsRemaining == 1; } }

		public bool AuctionComplete { get { return this.CallsRemaining == 0; } }
	}
}
