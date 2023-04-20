using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public enum CallType { Pass, Bid, Double, Redouble, NotActed }

	public struct Bid : IEquatable<Bid>
	{


		public int? Level { get; }
		public Suit? Suit { get; }

		public BidConvention Convention { get; }
		public CallType CallType { get; }

		public BidMessage Message { get; }

		public bool Is(int level, Suit suit)
		{
			return CallType == CallType.Bid && Level == level && Suit == suit;
		}

		public bool Equals(Bid other)
		{
			return (CallType == other.CallType && Level == other.Level && Suit == other.Suit && Convention == other.Convention);
		}

		public bool IsBid
		{
			get { return CallType == CallType.Bid; }
		}
		public bool IsPass
		{
			get { return CallType == CallType.Pass; }
		}

		public bool HasActed => CallType != CallType.NotActed;

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

        static Bid FromString(string str)
		{
			if (str == "Pass") { return new Bid(CallType.Pass);  }
			if (str == "X") { return new Bid(CallType.Double); }
			if (str == "XX") { return new Bid(CallType.Redouble); }
			int level = int.Parse(str.Substring(0, 1));
			var suit = SymbolToSuit[str.Substring(1)];
			return new Bid(level, suit);
		}

		public Bid(CallType callType, BidConvention convention = BidConvention.None, BidMessage message = BidMessage.Invitational)
		{
			Debug.Assert(callType != CallType.Bid);
			this.CallType = callType;
			this.Level = null;
			this.Suit = null;
			this.Convention = convention;
			this.Message = message;
		}

		public Bid(int level, Suit suit, BidConvention convention = BidConvention.None, BidMessage message = BidMessage.Invitational)
		{
			this.CallType = CallType.Bid;
			Debug.Assert(level >= 1 && level <= 7);
			this.Level = level;
			this.Suit = suit;
			this.Convention = convention;
			this.Message = message;
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

        public override string ToString()
		{
			if (CallType == CallType.Bid)
			{
				return $"{Level}{SuitToSymbol[(Suit)this.Suit]}";
			}
			if (CallType == CallType.Pass)
			{
				return "Pass";
			}
			if (CallType == CallType.Double) { return "X"; }
			if (CallType == CallType.Redouble) { return "XX"; }
			Debug.Assert(false);
			return "";
		}
    }

}
