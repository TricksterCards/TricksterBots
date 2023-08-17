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
		protected int RawValue { get; private set; }

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
            {  "♣",  Suit.Clubs },
            {  "♦",  Suit.Diamonds},
            {  "♥",  Suit.Hearts  },
            {  "♠",  Suit.Spades  },
            {  "NT", Suit.Unknown  }
        };



        public static Dictionary<Suit, string> SuitToSymbol = new Dictionary<Suit, string>
        {
            { Suit.Clubs,    "♣" },
            { Suit.Diamonds, "♦" },
            { Suit.Hearts,   "♥" },
            { Suit.Spades,   "♠" },
            { Suit.Unknown,  "NT" }
        };

        public static Dictionary<Suit, int> StrainToInt = new Dictionary<Suit, int>
        {
            { Suit.Clubs,    0 },
            { Suit.Diamonds, 1 },
            { Suit.Hearts,   2 },
            { Suit.Spades,   3 },
            { Suit.Unknown,  4 }
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
    }
}
