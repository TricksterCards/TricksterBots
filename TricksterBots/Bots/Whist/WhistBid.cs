using System;
using System.Collections.Generic;
using Trickster.cloud;

namespace Trickster.Bots
{
    public class WhistBid : IComparable<WhistBid>, IEquatable<WhistBid>
    {
        public const int LevelZeroTricks = 6;
        public const int MaxLevel = 13 - LevelZeroTricks;
        public const int MinLevel = 7 - LevelZeroTricks;
        public static WhistBid DeclarerPartnerBid = new WhistBid(Special.DeclarerPartner);
        public static WhistBid NotDeclareBid = new WhistBid(Special.NotDeclarer);

        private static readonly Dictionary<Suit, int> BidSuitOrder = new Dictionary<Suit, int>
        {
            { Suit.Unknown, (int)Suit.Joker },
            { Suit.Joker, 0 },
            { Suit.Clubs, (int)Suit.Clubs },
            { Suit.Spades, (int)Suit.Spades },
            { Suit.Hearts, (int)Suit.Hearts },
            { Suit.Diamonds, (int)Suit.Diamonds }
        };

        private readonly int theBid;

        public WhistBid(int bid)
        {
            theBid = bid;
        }

        public WhistBid(BidBase bid)
        {
            theBid = bid.value;
        }

        public WhistBid(int level, bool highWins)
            : this(Suit.Joker, level, true, highWins)
        {
        }

        public WhistBid(Suit s, int level, bool roundOne = false, bool highWins = true)
        {
            if (level < MinLevel)
                theBid = NotDeclareBid.theBid;
            else
            {
                if (level > MaxLevel)
                    level = MaxLevel;

                theBid = (int)BidSpace.Whist + (level * 10 + (int)s) * 4 + MakeTwoBits(highWins, roundOne);
            }
        }

        private WhistBid(Special special)
        {
            switch (special)
            {
                case Special.NotDeclarer:
                    theBid = (int)BidSpace.Whist + (int)Special.NotDeclarer;
                    break;
                case Special.DeclarerPartner:
                    theBid = (int)BidSpace.Whist + (int)Special.DeclarerPartner;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(special), special, null);
            }
        }

        public bool HighWins => IsDeclareBid && (LowTwoBits & 1) == 0;

        public bool IsDeclareBid => IsWhistBid && theBid != NotDeclareBid.theBid && theBid != DeclarerPartnerBid.theBid;

        public bool IsDeclarePartnerBid => IsWhistBid && theBid == DeclarerPartnerBid.theBid;

        public bool IsFirstRoundBid => IsDeclareBid && (LowTwoBits & 2) == 2;

        public bool IsNotDeclareBid => IsWhistBid && theBid == NotDeclareBid.theBid;

        public int Level => IsDeclareBid ? (theBid - (int)BidSpace.Whist) / 4 / 10 : 0;

        public bool LowWins => IsDeclareBid && (LowTwoBits & 1) == 1;

        public Suit Suit => IsDeclareBid ? (Suit)((theBid - (int)BidSpace.Whist) / 4 % 10) : Suit.Unknown;

        public int Tricks => LevelZeroTricks + Level;

        private bool IsWhistBid => theBid != BidBase.Pass && theBid != BidBase.NoBid && theBid != BidBase.NotPlaying;

        private int LowTwoBits => (theBid - (int)BidSpace.Whist) % 4;

        public int CompareTo(WhistBid other)
        {
            if (IsWhistBid && other.IsWhistBid)
            {
                var dif = Level.CompareTo(other.Level);

                if (dif == 0)
                    dif = IsFirstRoundBid.CompareTo(other.IsFirstRoundBid);

                if (dif == 0)
                    dif = BidSuitOrder[Suit].CompareTo(BidSuitOrder[other.Suit]);

                if (dif == 0)
                    dif = LowWins.CompareTo(other.LowWins);

                return dif;
            }

            return theBid.CompareTo(other.theBid);
        }

        public bool Equals(WhistBid other)
        {
            return theBid.Equals(other?.theBid);
        }

        public static string GetBidText(int bid, out bool canTakePoints, out int? expectedPoints, out int? level)
        {
            var wb = new WhistBid(bid);
            canTakePoints = !wb.IsDeclarePartnerBid;
            expectedPoints = wb.IsDeclareBid ? (int?)wb.Tricks : null;
            level = wb.IsDeclareBid && wb.IsFirstRoundBid ? (int?)wb.Level : null;
            return wb.ToString();
        }

        public static implicit operator BidBase(WhistBid wb)
        {
            return new BidBase(wb.theBid);
        }

        public static implicit operator int(WhistBid wb)
        {
            return wb.theBid;
        }

        public string LevelSuitString()
        {
            var suit = Suit;
            var suitSymbol = suit == Suit.Unknown ? "NT" : Card.SuitSymbol(suit);
            return $"{Level}{suitSymbol}";
        }

        public override string ToString()
        {
            if (!IsDeclareBid)
                return string.Empty;

            if (IsFirstRoundBid)
                return $"{Level}{(Suit == Suit.Unknown ? "NT" : HighWins ? "↑" : "↓")}";

            return $"{LevelSuitString()}{(HighWins ? "↑" : "↓")}";
        }

        private static int MakeTwoBits(bool highWins, bool roundOne)
        {
            return (highWins ? 0 : 1) | (roundOne ? 2 : 0);
        }

        private enum Special
        {
            NotDeclarer,
            DeclarerPartner
        }
    }
}