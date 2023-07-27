using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public enum Call { Pass, Bid, Double, Redouble, NotActed }


	public struct Bid : IEquatable<Bid>, IComparable<Bid>
	{
		public int? Level { get; }
		public Suit? Suit { get; }

		public Call Call { get; }


		public bool Is(int level, Suit suit)
		{
			return Call == Call.Bid && Level == level && Suit == suit;
		}

		public bool Equals(Bid other)
		{
			return (Call == other.Call && Level == other.Level && Suit == other.Suit);
		}

		public bool IsBid
		{
			get { return Call == Call.Bid; }
		}
		public bool IsPass
		{
			get { return Call == Call.Pass; }
		}

		public bool HasActed => Call != Call.NotActed;

		public Suit SuitIfNot(Suit? suit)
		{
			return (suit == null) ? (Suit)Suit : (Suit)suit;
		}


        public static Dictionary<string, Suit> SymbolToSuit = new Dictionary<string, Suit>
        {
            {  "♣",  Trickster.cloud.Suit.Clubs },
            {  "♦",  Trickster.cloud.Suit.Diamonds},
            {  "♥",  Trickster.cloud.Suit.Hearts  },
            {  "♠",  Trickster.cloud.Suit.Spades  },
            {  "NT", Trickster.cloud.Suit.Unknown  }
        };

        static public Bid FromString(string str)
		{
			if (str == "Pass") { return new Bid(Call.Pass);  }
			if (str == "X") { return new Bid(Call.Double); }
			if (str == "XX") { return new Bid(Call.Redouble); }
			int level = int.Parse(str.Substring(0, 1));
			var suit = SymbolToSuit[str.Substring(1)];
			return new Bid(level, suit);
		}

		public static Bid Pass = new Bid(Call.Pass);
		public static Bid Null = new Bid(Call.NotActed);
		public static Bid Double = new Bid(Call.Double);
		public static Bid Redouble = new Bid(Call.Redouble);

		public Bid(Call call)
		{
			Debug.Assert(call != Call.Bid);
			this.Call = call;
			this.Level = null;
			this.Suit = null;
		}

		public Bid(int level, Suit suit)
		{
			this.Call = Call.Bid;
			Debug.Assert(level >= 1 && level <= 7);
			this.Level = level;
			this.Suit = suit;
		}

		// TODO: I am sure this exists somewhere else...  Find it

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

		public override string ToString()
		{
			if (Call == Call.Bid)
			{
				return $"{Level}{SuitToSymbol[(Suit)this.Suit]}";
			}
			if (Call == Call.Pass)
			{
				return "Pass";
			}
			if (Call == Call.Double) { return "X"; }
			if (Call == Call.Redouble) { return "XX"; }
			Debug.Assert(false);
			return "";
		}

		internal int RawLevel
		{
			get
			{
				Debug.Assert(this.IsBid);
				return ((int)this.Level - 1) * 5 + StrainToInt[(Suit)this.Suit];
			}
		}

		public (bool Valid, int Jump) IsValid(PositionState position, Contract contract)
		{
			if (this.IsPass) { return (true, 0); }
			if (this.Call == Call.Double)
			{
				if (!contract.Bid.IsBid || contract.Doubled) { return (false, 0); }
				return ((position.LeftHandOpponent == contract.By || position.RightHandOpponent == contract.By), 0);
			}
			if (this.Call == Call.Redouble)
			{
				if (contract.Doubled && !contract.Redoubled)
				{
					return ((position == contract.By || position.Partner == contract.By), 0);
				}
				return (false, 0);
			}
			Debug.Assert(this.Call == Call.Bid);
			if (contract.Bid.Call == Call.NotActed)
			{
				return (true, 1 - (int)this.Level);
			}
			int thisLevel = this.RawLevel;
			int contractLevel = contract.Bid.RawLevel;
			if (thisLevel <= contractLevel) { return (false, 0); }
			return (true, (thisLevel - contractLevel) / 5);
		}

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
    }



}
