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
            public Dictionary<Strain, SuitAgreements.ShowState> Strains { get; protected set; }

            public ShowState(PairAgreements startState = null)
            {
                PairAgreements = startState == null ? new PairAgreements() : new PairAgreements(startState);
                this.Strains = new Dictionary<Strain, SuitAgreements.ShowState>();
                foreach (Strain strain in Enum.GetValues(typeof(Strain)))
                {
                    this.Strains[strain] = new SuitAgreements.ShowState(PairAgreements.Strains[strain]);
                }
            }

            // TODO: This name not the best...
            public void ShowAgreedStrain(Strain trumpStrain)
            {
                // TODO: Need to think this out carefully.  What is someone chagnes it?

                //PairAgreements.TrumpSuit = CombineBool(PairAgreements.TrumpSuit, trumpSuit, CombineRule.Show);
                PairAgreements.AgreedStrain = trumpStrain;   // TODO: THIS IS NOT RIGHT!!!  CANT JUST OVERWRITE IT...
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
            private PairAgreements _pairAgreements;
            private Strain _strain;
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
                    // TODO: This is ugly too.   Review 
                    SuitAgreements._pairAgreements.LastShownStrain = SuitAgreements._strain;
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
            public SuitAgreements(PairAgreements pairAgreements, Strain strain)
            {
                this._pairAgreements = pairAgreements;
                this._strain = strain;
                this.LongHand = null;   // This sets Dummy too...
            }
            public SuitAgreements(PairAgreements pairAgreements, Strain strain, SuitAgreements other)
            {
                this._pairAgreements = pairAgreements;
                this._strain = strain;
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
        public Strain? AgreedStrain { get; private set; }

        // TODO: This is ugly an not combined properly.  Review all uses of this
        public Strain? LastShownStrain { get; private set; }

        public Suit? TrumpSuit
        {
            get
            {
                if (AgreedStrain is Strain strain)
                {
                    return Call.StrainToSuit(strain);
                }
                return null;
            }
        }

        public Dictionary<Strain, SuitAgreements> Strains { get; }
        public PairAgreements()
        {
            this.AgreedStrain = null;
            this.Strains = new Dictionary<Strain, SuitAgreements>();
            foreach (Strain strain in Enum.GetValues(typeof(Strain)))
            {
                this.Strains[strain] = new SuitAgreements(this, strain);
            }

        }
        public PairAgreements(PairAgreements other)
        {
            this.AgreedStrain = other.AgreedStrain;
            this.Strains = new Dictionary<Strain, SuitAgreements>();
            foreach (Strain strain in Enum.GetValues(typeof(Strain)))
            {
                this.Strains[strain] = new SuitAgreements(this, strain, other.Strains[strain]);

            }
        }

        protected void Combine(PairAgreements other, CombineRule cr)
        {
            // TODO: THIS IS BROKEN.  FOR NOW JUST USE THE LAST ONE SHOWN-
            if (cr == CombineRule.Show || cr == CombineRule.Merge)
            {
                this.LastShownStrain = other.LastShownStrain;
            }

            // TODO: Need to actually do something here. 
            // For now this works...
            if (this.AgreedStrain == null && cr != CombineRule.CommonOnly)
            {
                this.AgreedStrain = other.AgreedStrain;
            }
            foreach (Strain strain in Enum.GetValues(typeof(Strain)))
            {
                Strains[strain].Combine(other.Strains[strain], cr);
            }
            // TODO: What to do if trump overridden?  Seems possible, but we really need the idea of "LAST ONE DECIDED"

        }
   
        public bool Equals(PairAgreements other)
        {
            if (this.AgreedStrain != other.AgreedStrain) return false;
            foreach (Strain strain in Enum.GetValues(typeof(Strain)))
            {
                if (!this.Strains[strain].Equals(other.Strains[strain])) return false;
            }
            return true;
        }
    }



 



}
