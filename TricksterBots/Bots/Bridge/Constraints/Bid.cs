using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public abstract class Call : IEquatable<Call>, IComparable<Call>
	{
		public int RawValue { get; private set; }

        public override int GetHashCode()
        {
            return RawValue;
        }

        protected Call(int rawValue)
        {
            this.RawValue = rawValue;
        }

        public int CompareTo(Call other)
        {
            return RawValue - other.RawValue;
        }

		public static Call Pass = new Pass();
		public static Call Double = new Double();
		public static Call Redouble = new Redouble();

        static public Call FromString(string str)
        {
            if (str == "Pass") { return Pass; }
            if (str == "X") { return Double; }
            if (str == "XX") { return Call.Redouble; }
            int level = int.Parse(str.Substring(0, 1));
            var suit = SymbolToSuit[str.Substring(1)];
            return new Bid(level, suit);
        }

        public bool Equals(Call other)
        {
            return RawValue == other.RawValue;
        }


        // TODO: I am sure this exists somewhere else...  Find it


        public static Dictionary<string, Suit> SymbolToSuit = new Dictionary<string, Suit>
        {
            {  "♣",  Trickster.cloud.Suit.Clubs },
            {  "♦",  Trickster.cloud.Suit.Diamonds},
            {  "♥",  Trickster.cloud.Suit.Hearts  },
            {  "♠",  Trickster.cloud.Suit.Spades  },
            {  "NT", Trickster.cloud.Suit.Unknown  }
        };



        public static Dictionary<Suit, string> SuitToSymbol = new Dictionary<Suit, string>
        {
            { Trickster.cloud.Suit.Clubs,    "♣" },
            { Trickster.cloud.Suit.Diamonds, "♦" },
            { Trickster.cloud.Suit.Hearts,   "♥" },
            { Trickster.cloud.Suit.Spades,   "♠" },
            { Trickster.cloud.Suit.Unknown,  "NT" }
        };

        public static Dictionary<Suit, int> StrainToInt = new Dictionary<Suit, int>
        {
            { Trickster.cloud.Suit.Clubs,    0 },
            { Trickster.cloud.Suit.Diamonds, 1 },
            { Trickster.cloud.Suit.Hearts,   2 },
            { Trickster.cloud.Suit.Spades,   3 },
            { Trickster.cloud.Suit.Unknown,  4 }
        };


    }

    public class Pass : Call
	{
        public Pass() : base(0) { }
        public override string ToString()
		{
			return "Pass";
		}
	}

	public class Double: Call
	{
        public Double() : base(1) { }
        public override string ToString()
        {
            return "X";
        }
    }

    public class Redouble : Call
    {
        public Redouble() : base(2) { }
        public override string ToString()
        {
            return "XX";
        }
    }



    public class Bid : Call
	{

		public int Level { get; }
		public Suit Suit { get; }


//		public bool Is(int level, Suit suit)
//		{
//			return Call == Call.Bid && Level == level && Suit == suit;
//		}

//		public bool Equals(Bid other)
//		{
//			return (Call == other.Call && Level == other.Level && Suit == other.Suit);
//		}


	//	public Suit SuitIfNot(Suit? suit)
//		{
//			return (suit == null) ? (Suit)Suit : (Suit)suit;
//		}



		public Bid(int level, Suit suit) : base((level - 1) * 5 + StrainToInt[suit] + 3)
        {

			Debug.Assert(level >= 1 && level <= 7);
			this.Level = level;
			this.Suit = suit;
		}


		public override string ToString()
		{
			return $"{Level}{SuitToSymbol[(Suit)this.Suit]}";
		}

        public int JumpOver(Bid other)
        {
            return (RawValue - other.RawValue) / 5;
        }

        /* -- TODO: Part of contract?  Or part of Bid???
		public int JumpOver(Contract contract)
		{
			if (!this.IsBid) 
			{
				Debug.Fail("Can not ask about a jump bid for a call that is not a bid");
				return -1;
			}
			if (contract.Bid.Equals(Bid.Pass) || contract.Bid.Equals(Bid.Null))
			{
				return 1 - (int)Level;
			}
			Debug.Assert(contract.Bid.IsBid);
			return (this.RawLevel - contract.Bid.RawLevel) / 5;
		}
        */

        /*
        private int OvercallValue
        {
			get
			{
				switch (this.Call)
				{
					case Call.NotActed: return 0;
					case Call.Pass: return 1;
					case Call.Double: return 2;
					case Call.Redouble: return 3;
					default:
						Debug.Assert(IsBid);
						return 4 + RawLevel;
				}
			}
        }


        public int CompareTo(Bid other)
        {
            return OvercallValue - other.OvercallValue;
        }
        */
        

    }
}
