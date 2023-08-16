using System;
using System.Collections.Generic;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{

   
    public class PairAgreements: State, IEquatable<PairAgreements>
    {
        public class ShowState
        {
            public PairAgreements PairAgreements { get; protected set; }
            public Dictionary<Suit, SuitAgreements.ShowState> Suits { get; protected set; }

            public ShowState(PairAgreements startState = null)
            {
                PairAgreements = startState == null ? new PairAgreements() : new PairAgreements(startState);
                this.Suits = new Dictionary<Suit, SuitAgreements.ShowState>();
                foreach (var strain in BasicBidding.Strains)
                {
                    this.Suits[strain] = new SuitAgreements.ShowState(PairAgreements.Suits[strain]);
                }
            }

            public void ShowTrump(Suit trumpSuit)
            {
                // TODO: Need to think this out carefully.  What is someone chagnes it?

                //PairAgreements.TrumpSuit = CombineBool(PairAgreements.TrumpSuit, trumpSuit, CombineRule.Show);
                PairAgreements.TrumpSuit = trumpSuit;   // TODO: THIS IS NOT RIGHT!!!  CANT JUST OVERWRITE IT...
            }
            public void Combine(PairAgreements other, CombineRule combineRule)
            {
                PairAgreements.Combine(other, combineRule);
            }
            // TODO: Need to actually do something here.....
        }

        // TODO: Add conventions here...
        // Anything else about global agreements that are not specific to the hand.
        public class SuitAgreements: IEquatable<SuitAgreements>
        {
            public class ShowState
            {
                public SuitAgreements SuitAgreements { get; protected set; }

                public ShowState(SuitAgreements suitAgreements)
                {
                    this.SuitAgreements = suitAgreements;          
                }

                // TODO.  What's up here?  Merge? etc?
                public void ShowLongHand(PositionState longHand)
                {
                    SuitAgreements.LongHand = longHand;
                }


            }
            public PositionState LongHand { get; protected set; }
            public PositionState Dummy
            {
                get
                {
                    if (LongHand == null) { return null; }
                    return LongHand.Partner;
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

            public void Combine(SuitAgreements other, CombineRule combineRule)
            {
                if (combineRule == CombineRule.CommonOnly)
                {
                    if (this.LongHand == null || other.LongHand == null)
                    {
                        this.LongHand = null;
                    }

                }
                else if (this.LongHand == null)
                {
                    // Is this right?  If other.LongHand exists it will over
                    this.LongHand = other.LongHand;
                }
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

        protected void Combine(PairAgreements other, CombineRule cr)
        {
            // TODO: Need to actually do something here. 
            // For now this works...
            if (this.TrumpSuit == null && cr != CombineRule.CommonOnly)
            {
                this.TrumpSuit = other.TrumpSuit;
            }
            foreach (var suit in BasicBidding.Strains)
            {
                Suits[suit].Combine(other.Suits[suit], cr);
            }
            // TODO: What to do if trump overridden?  Seems possible, but we really need the idea of "LAST ONE DECIDED"

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
