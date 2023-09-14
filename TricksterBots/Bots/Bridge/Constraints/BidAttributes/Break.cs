using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksterBots.Bots.Bridge
{
    internal class Break : DynamicConstraint
    {
        // TODO: Implement static break class !!!
        public string Name { get; private set; }
        public int CountPublic = 0;
        public int CountPrivate = 0;
        public Break(bool isStatic, string name)
        {
            this.Name = name;
        }
        public override bool Conforms(Call call, PositionState ps, HandSummary hs)
        {
            var pairSummary = new PairSummary(ps);
            var oppsSummary = new PairSummary(ps.LeftHandOpponent);
            if (hs == ps.PublicHandSummary)
            {
                CountPublic++;
            } else
            {
                CountPrivate++;
            }
            return true;
        }
    }
}
