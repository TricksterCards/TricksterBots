using Newtonsoft.Json.Converters;
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
        // TODO: Add conventions here...
        // Anything else about global agreements that are not specific to the hand.

        public Suit? TrumpSuit { get; set; }
        public BiddingSummary()
        {
            this.TrumpSuit = null;

        }
        public BiddingSummary(BiddingSummary other)
        {
            this.TrumpSuit = other.TrumpSuit;
        }

        public void Union(BiddingSummary other)
        {
            // TODO: Do full blown thing eventually, but for now just this...
            if (other.TrumpSuit != null) { this.TrumpSuit = other.TrumpSuit;  }
        }

        public void Intersect(BiddingSummary other)
        {
            if (this.TrumpSuit == null || other.TrumpSuit == null || this.TrumpSuit != other.TrumpSuit)
            {
                this.TrumpSuit = null;
            }
        }

    }



 



}
