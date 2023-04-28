using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{


    public class PairAgreements: IEquatable<PairAgreements>
    {
        // TODO: Add conventions here...
        // Anything else about global agreements that are not specific to the hand.
        public class SuitAgreements: IEquatable<SuitAgreements>
        {
            public PositionState LongHand { get; set; }
            public PositionState Dummy
            {
                get
                {
                    if (LongHand == null) { return null; }
                    return LongHand.Partner;
                }
                set
                {
                    if (value == null)
                    {
                        LongHand = null;
                    }
                    else
                    {
                        LongHand = value.Partner;
                    }
                }
            }
            public SuitAgreements()
            {
                this.LongHand = null;   // This sets Dummy too...
            }
            public SuitAgreements(SuitAgreements other)
            {
                this.LongHand = other.LongHand;
            }

            public bool Equals(SuitAgreements other)
            {
                return (this.LongHand == other.LongHand);
            }
        }
        public Suit? TrumpSuit { get; set; }
        public Dictionary<Suit, SuitAgreements> Suits { get; }
        public PairAgreements()
        {
            this.TrumpSuit = null;
            this.Suits = new Dictionary<Suit, SuitAgreements>();
            foreach (var suit in BasicBidding.Strains)
            {
                this.Suits[suit] = new SuitAgreements();
            }

        }
        public PairAgreements(PairAgreements other)
        {
            this.TrumpSuit = other.TrumpSuit;
            this.Suits = new Dictionary<Suit, SuitAgreements>();
            foreach (var suit in BasicBidding.Strains)
            {
                this.Suits[suit] = new SuitAgreements(other.Suits[suit]);

            }
        }

        public void Union(PairAgreements other)
        {
            // TODO: Do full blown thing eventually, but for now just this...
            if (other.TrumpSuit != null) { this.TrumpSuit = other.TrumpSuit;  }
        }

        public void Intersect(PairAgreements other)
        {
            if (this.TrumpSuit == null || other.TrumpSuit == null || this.TrumpSuit != other.TrumpSuit)
            {
                this.TrumpSuit = null;
            }
        }

        public bool Equals(PairAgreements other)
        {
            if (this.TrumpSuit != other.TrumpSuit) return false;
            foreach (var suit in BasicBidding.Strains)
            {
                if (!this.Suits[suit].Equals(other.Suits[suit])) return false;
            }
            return true;
        }
    }



 



}
