using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksterBots.Bots.Bridge
{ 
    public class Role : Constraint
    {
        PositionRole _role;
        int _round;
        bool _desiredValue;

        // Round == 0 means only check for role, otherwise check for both.
        public Role(PositionRole role, int round, bool desiredValue)
        {
            _role = role;
            _round = round;
            _desiredValue = desiredValue;
            this.OnceAndDone = true;
        }

        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs)
        {
            return _desiredValue == (_role == ps.Role && (_round == 0 || ps.RoleRound == _round));
        }
    }
}
