using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
   

    public class BiddingSummary
    {
        public Suit? TrumpSuit { get; set; }
        public BiddingSummary() 
        {
            this.TrumpSuit = null;

        }
        public BiddingSummary(BiddingSummary other)
        {
            this.TrumpSuit = other.TrumpSuit;
        }

    }



 



}
