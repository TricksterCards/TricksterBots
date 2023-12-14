using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BridgeBidding
{
    internal class AgreedStrain : StaticConstraint
    {
        private Strain[] _strains;
        public AgreedStrain(params Strain[] strains)
        {
            this._strains = strains;

        }
        public override bool Conforms(Call call, PositionState ps)
        {
            var strains = _strains;
            if (strains.Length == 0)
            {
                if (GetStrain(null, call) is Strain strain)
                {
                    strains = new Strain[] { strain };
                }
                else
                {
                    Debug.Fail("Call has no strain.");
                    return false;
                }
            }
            foreach (var s in strains)
            {
                if (ps.PairState.Agreements.AgreedStrain == s) return true;
            }
            return false;
        }
    }

    // TODO: Maybe move this...
    internal class ContractIsAgreedStrain : StaticConstraint
    {
        public override bool Conforms(Call call, PositionState ps)
        {
            if (ps.BiddingState.Contract.Bid is Bid contractBid)
            {
                return ps.BiddingState.Contract.IsOurs(ps) && 
                    contractBid.Strain == ps.PairState.Agreements.AgreedStrain;
            }
            return false;
        }
    }
}
