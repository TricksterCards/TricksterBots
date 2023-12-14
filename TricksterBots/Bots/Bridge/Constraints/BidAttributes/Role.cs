using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BridgeBidding
{ 
    public class Role : StaticConstraint
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
        }

        public override bool Conforms(Call call, PositionState ps)
        {
            return _desiredValue == (_role == ps.Role && (_round == 0 || ps.RoleRound == _round));
        }
    }
}
