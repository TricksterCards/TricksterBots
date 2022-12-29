using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;

namespace TricksterBots.Bots
{ 
    public interface IInterpretCall
    {
        bool Interpret(InterpretedBid call);
    }

}
