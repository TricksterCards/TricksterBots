using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;


namespace BridgeBidding
{
    public enum Strain { Clubs = 0, Diamonds = 1, Hearts = 2, Spades = 3, NoTrump = 4 }



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

        public static Strain SuitToStrain(Suit? suit)
        {
            switch (suit)
            {
                case null: return Strain.NoTrump;
                case Suit.Clubs: return Strain.Clubs;
                case Suit.Diamonds: return Strain.Diamonds;
                case Suit.Hearts: return Strain.Hearts;
                case Suit.Spades: return Strain.Spades;
                default:
                    Debug.Fail("Should always be a suit");
                    return Strain.NoTrump;
            }
        }

        public static Suit? StrainToSuit(Strain strain)
        {
            switch (strain)
            {
                case Strain.Clubs:    return Suit.Clubs;
                case Strain.Diamonds: return Suit.Diamonds;
                case Strain.Hearts:   return Suit.Hearts;
                case Strain.Spades:   return Suit.Spades;
                default:              return null;
            }
        }

        static public Call FromString(string str)
        {
            if (str == "Pass") { return Pass; }
            if (str == "X") { return Double; }
            if (str == "XX") { return Call.Redouble; }
            int level = int.Parse(str.Substring(0, 1));
            var strain = SymbolToStrain[str.Substring(1)];
            return new Bid(level, strain);
        }

        public bool Equals(Call other)
        {
            return RawValue == other.RawValue;
        }


        // TODO: I am sure this exists somewhere else...  Find it


        public static Dictionary<string, Strain> SymbolToStrain = new Dictionary<string, Strain>
        {
            {  "♣",  Strain.Clubs    },
            {  "♦",  Strain.Diamonds },
            {  "♥",  Strain.Hearts   },
            {  "♠",  Strain.Spades   },
            {  "NT", Strain.NoTrump  }
        };



        public static Dictionary<Strain, string> StrainToSymbol = new Dictionary<Strain, string>
        {
            { Strain.Clubs,    "♣" },
            { Strain.Diamonds, "♦" },
            { Strain.Hearts,   "♥" },
            { Strain.Spades,   "♠" },
            { Strain.NoTrump,  "NT" }
        };
        /* - Think this is just a simple cast now...
        public static Dictionary<Suit, int> SuitToInt = new Dictionary<Suit, int>
        {
            { Suit.Clubs,    0 },
            { Suit.Diamonds, 1 },
            { Suit.Hearts,   2 },
            { Suit.Spades,   3 }
        };
        */


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
        public Strain Strain { get; }
        public Suit? Suit => StrainToSuit(Strain);


        public Bid(int level, Suit suit) : base((level - 1) * 5 + (int)suit + 3)
        {
            Debug.Assert(level >= 1 && level <= 7);
            this.Level = level;
            this.Strain = SuitToStrain(suit);
        }

        public Bid(int level, Strain strain) : base((level - 1) * 5 + (int)strain + 3)
        {
            Debug.Assert(level >= 1 && level <= 7);
            this.Level = level;
            this.Strain = strain;
        }

		public override string ToString()
		{
			return $"{Level}{StrainToSymbol[this.Strain]}";
		}

        // A jump shows a bid that could have been made at a lower level.  This means that the next 5 bids from 1X -> 2X for example
        // are not jumps.  Math is 2X(raw) - 2X(raw) - 1.
        public int JumpOver(Bid other)
        {
            return (RawValue - other.RawValue - 1) / 5;
        }
    }
}
