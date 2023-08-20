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
        public bool StaticConstraint = false;
        public abstract bool Conforms(Call call, PositionState ps, HandSummary hs);


        public static Suit? GetSuit(Suit? s, Call call)
        {
            if (s != null) { return s; }
            if (call is Bid bid)
            {
                return bid.Suit;
            }
            return null;
        }

        public static Strain? GetStrain(Strain? strain, Call call)
        {
            if (strain != null) { return strain; }
            if (call is Bid bid) { return bid.Strain; }
            return null;
        }

    }

    public interface IShowsState 
    {
        void ShowState(Call call, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements);
    }


}


