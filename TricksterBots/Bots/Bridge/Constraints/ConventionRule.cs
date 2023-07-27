using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;
using TricksterBots.Bots.Bridge;


/*
namespace TricksterBots.Bots.Bridge
{
   


    public class RedirectRule 
    {
        private Constraint[] _constraints;
        public PrescribedBidsFactory PrescribedBidsFactory { get; private set; }


        public RedirectRule(PrescribedBidsFactory factory, params Constraint[] constraints)
        {
            this.PrescribedBidsFactory = factory;
            this._constraints = constraints;
        }

        public bool Conforms(PositionState ps)
        {
            var nullBid = new Bid(Call.NotActed);
            foreach (var constraint in _constraints)
            {
                Debug.Assert(constraint.OnceAndDone);   // TODO: Is this right???
                if (!constraint.Conforms(nullBid, ps, null)) { return false; }
            }
            return true;
        }
    }
}
*/