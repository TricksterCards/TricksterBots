using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class ConventionRule
    {

        public BidConvention Convention { get; }
        private Constraint[] _constraints;

        public ConventionRule(params Constraint[] constraints)
        {
            this._constraints = constraints;
        }

        public bool Conforms(Bid nextLegalBid, PositionState ps)
        {
            foreach (var constraint in _constraints)
            {
                if (!constraint.Conforms(nextLegalBid, ps, ps.PublicHandSummary, ps.BiddingSummary)) { return false; }
            }
            return true;
        }
    }


    public class RedirectRule : ConventionRule
    {
        private BidderFactory _redirectTo;
        public RedirectRule(BidderFactory redirectTo, params Constraint[] constraints) : base(constraints)
        {
            this._redirectTo = redirectTo;
        }
        public Bidder Redirect(Bid imnotsure, PositionState ps)
        {
            return (this.Conforms(imnotsure, ps)) ? this._redirectTo() : null;
        }
    }
}
