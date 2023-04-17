using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;


namespace TricksterBots.Bots.Bridge
{
    public abstract class Constraint
    {
        public abstract bool Conforms(Bid bid, PositionState ps, HandSummary hs, BiddingSummary bs);

	}

    public interface IShowsState 
    {
        void Update(Bid bid, PositionState ps, HandSummary hs, BiddingSummary bs);
	}  
}


