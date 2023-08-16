using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{

    public class HasOppsStopped : Constraint
    {
        protected bool _desiredValue;
        public HasOppsStopped(bool desiredValue)
        {
            this._desiredValue = desiredValue;
        }

        public override bool Conforms(Call call, PositionState ps, HandSummary hs)
        {

            var oppsSummary = PairSummary.Opponents(ps);
            foreach (var suit in oppsSummary.ShownSuits)
            {
                // These variables can be true, false, or null, so look for specific values.
                var thisStop = hs.Suits[suit].Stopped;
                var partnerStop = ps.Partner.PublicHandSummary.Suits[suit].Stopped;
                // If either hand has it stopped then we are good
                if (thisStop != true && partnerStop != true)
                {
                    if (thisStop == false) { return !_desiredValue; }
                    // At this point, we know that thisStop is null (unknown value) and partnerStop is either false or unknown.
                    // This means that the hand summary COULD have the suit stopped - we don't know.  If we were dealing with the
                    // actual hand then thisStop will be either true or false.
                    Debug.Assert(thisStop == null);
                }
            }
            return _desiredValue;
        }
    }

    public class ShowsOppsStopped : HasOppsStopped, IShowsState
    {
        public ShowsOppsStopped(bool desiredValue) : base(desiredValue) { }

        public void ShowState(Call call, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
        {
            if (_desiredValue)
            {
                var oppsSummary = PairSummary.Opponents(ps);
                foreach (var suit in oppsSummary.ShownSuits)
                {
                    // These variables can be true, false, or null, so look for specific values.
                    var partnerStop = ps.Partner.PublicHandSummary.Suits[suit].Stopped;
                    if (partnerStop == null)
                    {
                        Debug.Assert(_desiredValue);    // TODO: How to show suits not stopped.  Complex if more than one suit...
                        showHand.Suits[suit].ShowStopped(true);
                    }
                }
            }
        }
    }
}
