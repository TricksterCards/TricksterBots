using System;
using System.Collections.Generic;
using System.Diagnostics;



namespace BridgeBidding
{
    public class OppsContract : StaticConstraint
    {
        bool _desired;
        int _minLevel;
        public OppsContract(bool desired, int minLevel = 0)
        {
            _desired = desired;
            _minLevel = minLevel;
        }
        public override bool Conforms(Call call, PositionState ps)
        {
            if (_desired)
            {
                return ps.IsOpponentsContract && ps.BiddingState.Contract.Bid.Level >= _minLevel;
            }
            return !ps.IsOpponentsContract;
        }
    }
}
