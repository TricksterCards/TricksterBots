using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksterBots.Bots.Bridge
{
    internal class Break : Constraint
    {
        public string Name { get; private set; }
        public int CountPublic = 0;
        public int CountPrivate = 0;
        public Break(string name)
        {
            this.Name = name;
        }
        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs)
        {
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
