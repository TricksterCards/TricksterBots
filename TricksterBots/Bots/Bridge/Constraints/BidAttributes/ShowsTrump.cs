using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BridgeBidding
{
    public class ShowsTrump : DynamicConstraint, IShowsState
    {
        private Strain? _trumpStrain;
        public ShowsTrump(Strain? trumpStrain)
        {
            this._trumpStrain = trumpStrain;
        }
        public override bool Conforms(Call call, PositionState ps, HandSummary hs)
        {
            if (GetStrain(_trumpStrain, call) == null)
            {
                Debug.Fail("Strain must be specified or call must be a bid for TrumpSuit constraint");
                return false;
            }
            return true;
        }

        void IShowsState.ShowState(Call call, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
        {
            if (GetStrain(_trumpStrain, call) is Strain strain)
            {
                showAgreements.ShowAgreedStrain(strain);
            }
        }
    }
}
