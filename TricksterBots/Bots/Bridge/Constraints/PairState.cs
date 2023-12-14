using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BridgeBidding
{
    public enum Pair { NorthSouth, EastWest }

    public class PairState
    {
        public Pair Pair { get; }
        // TODO: This should ideally not be public Set.  Perhaps PairState becomes part of PositionState...  Then could be protected...
        public PairAgreements Agreements { get; set;  }
        public IBiddingSystem BiddingSystem { get; }

        public PairState(Pair pair, IBiddingSystem biddingSystem)
        {
            this.Pair = pair;
            this.Agreements = new PairAgreements();
            this.BiddingSystem = biddingSystem;
        }

    }
}
