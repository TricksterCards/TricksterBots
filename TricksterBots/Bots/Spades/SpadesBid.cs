using System;
using Trickster.cloud;

namespace Trickster.Bots
{
    public class SpadesBid : IComparable<SpadesBid>, IEquatable<SpadesBid>
    {
        private const int _BlindBidsStart = 20;
        private const int _BlindBidsStop = 39;
        private const int _ShowHandBid = 19;
        private const int _ZeroBid = 40;
        private const int _SpadesBidBase = (int)BidSpace.Spades;
        private readonly int theBid;

        public SpadesBid(int nTricks, bool blind, bool zeroMeansZero)
        {
            if (zeroMeansZero && (nTricks == 0))
                theBid = _ZeroBid;
            else
                theBid = nTricks;

            if (blind)
                theBid += _BlindBidsStart;
        }

        public SpadesBid(BidBase bid)
            : this(bid.value)
        {
        }

        public SpadesBid(int bidValue)
        {
            theBid = bidValue < 0 ? bidValue : bidValue - _SpadesBidBase;
        }

        public static BidBase ShowHandBid => new BidBase(_SpadesBidBase + _ShowHandBid, "Show Hand");

        public static SpadesBid BlindNilBid => new SpadesBid(0, true, false);

        public bool IsBlindBid => _BlindBidsStart <= theBid && theBid <= _BlindBidsStop;

        public bool IsBlindNil => IsBlindBid && IsNil;

        public bool IsNil
        {
            get
            {
                if (theBid == _ShowHandBid || theBid == _ZeroBid)
                    return false;

                return theBid % _BlindBidsStart == 0;
            }
        }

        public bool IsNoBid => theBid == BidBase.NoBid;

        public bool IsNotNil => !IsNil;

        public bool IsShowHand => theBid == _ShowHandBid;

        public bool IsPlayBid => theBid >= 0 && !IsShowHand;

        public int Tricks
        {
            get
            {
                if (theBid == _ShowHandBid)
                    return _ShowHandBid;

                if (theBid == _ZeroBid)
                    return 0;

                return theBid % _BlindBidsStart;
            }
        }

        public int CompareTo(SpadesBid other) => theBid.CompareTo(other.theBid);

        public bool Equals(SpadesBid other) => theBid.Equals(other?.theBid);

        public static string GetBidText(int bid, out bool canTakePoints, out int? expectedPoints, out int? level)
        {
            var sb = new SpadesBid(bid);
            canTakePoints = !sb.IsShowHand;
            expectedPoints = !sb.IsShowHand ? sb.Tricks : (int?)null;
            level = null;
            return sb.ToString();
        }

        public static implicit operator BidBase(SpadesBid sb) => new BidBase(sb.theBid > 0 ? _SpadesBidBase + sb.theBid : sb.theBid);

        public override string ToString()
        {
            return theBid == _ShowHandBid ? "Show" : $"{(IsBlindBid ? "Blind " : string.Empty)}{(IsNil ? "Nil" : Tricks.ToString())}";
        }
    }
}