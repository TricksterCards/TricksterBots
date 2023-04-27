﻿using System;
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
using System.Windows.Markup;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public enum CallType { Pass, Bid, Double, Redouble, NotActed }

	public enum BidForce { Nonforcing, Invitational, Forcing, Signoff }
	public struct Bid : IEquatable<Bid>
	{
		public int? Level { get; }
		public Suit? Suit { get; }

		public CallType CallType { get; }

		public BidForce Force { get; }

		public bool Is(int level, Suit suit)
		{
			return CallType == CallType.Bid && Level == level && Suit == suit;
		}

		public bool Equals(Bid other)
		{
			return (CallType == other.CallType && Level == other.Level && Suit == other.Suit);
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

        static public Bid FromString(string str)
		{
			if (str == "Pass") { return new Bid(CallType.Pass, BidForce.Nonforcing);  }
			if (str == "X") { return new Bid(CallType.Double, BidForce.Nonforcing); }
			if (str == "XX") { return new Bid(CallType.Redouble, BidForce.Nonforcing); }
			int level = int.Parse(str.Substring(0, 1));
			var suit = SymbolToSuit[str.Substring(1)];
			return new Bid(level, suit, BidForce.Nonforcing);
		}

		public Bid(CallType callType, BidForce force)
		{
			Debug.Assert(callType != CallType.Bid);
			this.CallType = callType;
			this.Level = null;
			this.Suit = null;
			this.Force = force;
		}

		public Bid(int level, Suit suit, BidForce force)
		{
			this.CallType = CallType.Bid;
			Debug.Assert(level >= 1 && level <= 7);
			this.Level = level;
			this.Suit = suit;
			this.Force = force;
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
			if (this.CallType == CallType.Double)
			{
				if (!contract.Bid.IsBid || contract.Doubled) { return (false, 0); }
				return ((position.LeftHandOppenent == contract.By || position.RightHandOpponent == contract.By), 0);
			}
			if (this.CallType == CallType.Redouble)
			{
				if (contract.Doubled && !contract.Redoubled)
				{
					return ((position == contract.By || position.Partner == contract.By), 0);
				}
				return (false, 0);
			}
			Debug.Assert(this.CallType == CallType.Bid);
			if (contract.Bid.CallType == CallType.NotActed)
			{
				return (true, 1 - (int)this.Level);
			}
			int thisLevel = this.RawLevel;
			int contractLevel = contract.Bid.RawLevel;
			if (thisLevel <= contractLevel) { return (false, 0); }
			return (true, (thisLevel - contractLevel) % 5);
		}
    }

}
