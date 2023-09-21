using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    public class PinochleBid
    {
        public const int MaxDoubleDeckPoints = (int)NonPointBid.MaxDoubleDeckPoints;
        public const int MaxSingleDeckPoints = (int)NonPointBid.MaxSingleDeckPoints;
        private const int FirstDoubleDeckOnlyBid = (int)BidSpace.Pinochle + (int)NonPointBid.FirstDoubleDeckOnlyPoints * PointsMultiplier;
        private const int MaxDoubleDeckBid = (int)BidSpace.Pinochle + (int)NonPointBid.MaxDoubleDeckPoints * PointsMultiplier + PointsMultiplier - 1;
        private const int MaxSingleDeckBid = (int)BidSpace.Pinochle + (int)NonPointBid.MaxSingleDeckPoints * PointsMultiplier + PointsMultiplier - 1;
        private const int PointsMultiplier = 5;
        private const int ShootBidsStart = MaxDoubleDeckBid + 1;
        private const int NoShootBidsStart = ShootBidsStart + 10;

        private static readonly Dictionary<Suit, int> NoShootBids = SuitRank.stdSuits.ToDictionary(s => s, s => NoShootBidsStart + (int)s);
        private static readonly Dictionary<Suit, int> ShootBids = SuitRank.stdSuits.ToDictionary(s => s, s => ShootBidsStart + (int)s);
        private static readonly Dictionary<Suit, int> TrumpBids = SuitRank.stdSuits.ToDictionary(s => s, s => (int)BidSpace.Pinochle + (int)NonPointBid.TrumpBidsStart * PointsMultiplier + (int)s);

        private readonly int theBid;

        public PinochleBid(int bid)
        {
            theBid = bid;
        }

        public PinochleBid(BidBase bid)
        {
            theBid = bid.value;
        }

        public static PinochleBid DeclarerPartnerBid => NonPointsBid(NonPointBid.DeclarerPartner);
        public static PinochleBid DefenderBid => NonPointsBid(NonPointBid.Defender);

        public bool IsDeclarerParnter => BidIsDeclarerPartner(theBid);
        public bool IsDefender => BidIsDefender(theBid);
        public bool IsLikePass => BidIsLikePass(theBid);
        public bool IsMisDeal => BidIsMisDeal(theBid);
        public bool IsPassWithHelp => BidIsPassWithHelp(theBid);
        public bool IsPointsBid => BidIsPoints(theBid);
        public bool IsNoShootBid => NoShootBids.Values.Contains(theBid);
        public bool IsShootBid => ShootBids.Values.Contains(theBid);
        public bool IsShootOrNoShootBid => IsShootBid || IsNoShootBid;
        public bool IsTrumpBid => TrumpBids.Values.Contains(theBid);
        public static PinochleBid MisDealBid => NonPointsBid(NonPointBid.MisDealPoints);

        public static PinochleBid PassWithHelpBid => NonPointsBid(NonPointBid.PassWithHelpPoints);

        public int Points => IsPointsBid ? (theBid - (int)BidSpace.Pinochle) / PointsMultiplier : 0;

        public Suit Trump
        {
            get
            {
                var suit = IsPointsBid ? (Suit)((theBid - (int)BidSpace.Pinochle) % PointsMultiplier) : Suit.Unknown;

                if (Suit.Unknown <= suit && suit <= Suit.Joker)
                    return suit;

                return Suit.Unknown;
            }
        }

        public Suit NoShootBidSuit => IsNoShootBid ? NoShootBids.Single(tb => tb.Value == theBid).Key : Suit.Unknown;

        public Suit ShootBidSuit => IsShootBid ? ShootBids.Single(tb => tb.Value == theBid).Key : Suit.Unknown;

        public Suit TrumpBidSuit => IsTrumpBid ? TrumpBids.Single(tb => tb.Value == theBid).Key : Suit.Unknown;

        public static bool BidIsDeclarerPartner(int bid)
        {
            return bid == DeclarerPartnerBid.theBid;
        }

        public static bool BidIsDefender(int bid)
        {
            return bid == DefenderBid.theBid;
        }

        public static bool BidIsLikePass(int bid)
        {
            return bid == BidBase.Pass || BidIsPassWithHelp(bid);
        }

        public static bool BidIsMisDeal(int bid)
        {
            return bid == MisDealBid.theBid;
        }

        public static bool BidIsPassWithHelp(int bid)
        {
            return bid == PassWithHelpBid.theBid;
        }

        public static bool BidIsPoints(int bid)
        {
            return (int)BidSpace.Pinochle <= bid && bid <= MaxSingleDeckBid ||
                   FirstDoubleDeckOnlyBid <= bid && bid <= MaxDoubleDeckBid ||
                   ShootBids.Values.Contains(bid);
        }

        public static PinochleBid FromPoints(int points)
        {
            return new PinochleBid((int)BidSpace.Pinochle + points * PointsMultiplier);
        }

        public static PinochleBid FromPointsAndSuit(int points, Suit trump)
        {
            return new PinochleBid((int)BidSpace.Pinochle + points * PointsMultiplier + (int)trump);
        }

        public static string GetBidText(int bid, out bool cantakepoints, out int? expectedpoints, out int? level)
        {
            var pb = new PinochleBid(bid);

            cantakepoints = pb.IsLikePass || pb.IsDefender || pb.IsPointsBid;
            expectedpoints = pb.IsPointsBid ? pb.Points : (int?)null;
            level = pb.IsPointsBid ? 1 : (int?)null;

            return pb.ToString();
        }

        public static implicit operator BidBase(PinochleBid pb)
        {
            return new BidBase(pb.theBid);
        }

        public static implicit operator int(PinochleBid pb)
        {
            return pb.theBid;
        }
        
        public static PinochleBid NoShootBidFromSuit(Suit s)
        {
            return new PinochleBid(NoShootBids[s]);
        }
        
        public static PinochleBid ShootBidFromSuit(Suit s)
        {
            return new PinochleBid(ShootBids[s]);
        }

        public static PinochleBid TrumpBidFromSuit(Suit s)
        {
            return new PinochleBid(TrumpBids[s]);
        }

        public override string ToString()
        {
            if (IsDeclarerParnter || IsDefender)
                return string.Empty;

            if (IsPassWithHelp)
                return "Pass with Help";

            if (IsMisDeal)
                return "Misdeal";

            if (IsTrumpBid)
                return Card.SuitSymbol(TrumpBidSuit);

            if (IsShootBid)
                return $"Shooting in {Card.SuitSymbol(ShootBidSuit)}";

            if (IsNoShootBid)
                return "Not shooting";

            if (IsPointsBid)
                return Trump != Suit.Unknown ? Card.SuitSymbol(Trump) : Points.ToString();

            return "Invalid";
        }

        private static PinochleBid NonPointsBid(NonPointBid points)
        {
            return new PinochleBid((int)BidSpace.Pinochle + (int)points * PointsMultiplier);
        }

        //  note about these values. For single-deck, we limit bidding to 990. For double-deck, we allow bidding to continue to 5000.
        //  but, because double-deck bids over 600 go by 50, we're safe leaving these values where they are because they can't be bid
        //  in double-deck.
        private enum NonPointBid
        {
            MaxSingleDeckPoints = 990,
            PassWithHelpPoints,
            Defender,
            DeclarerPartner,
            TrumpBidsStart,
            MisDealPoints = TrumpBidsStart + 5, // 999
            FirstDoubleDeckOnlyPoints,
            MaxDoubleDeckPoints = 5000
        }
    }
}