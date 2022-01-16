using Trickster.cloud;

namespace Trickster.Bots
{
    public class OhHellBid
    {
        private readonly int theBid;

        public OhHellBid(int bid)
        {
            theBid = bid;
        }

        public OhHellBid(BidBase bid)
        {
            theBid = bid.value;
        }

        public int Tricks => theBid - (int)BidSpace.OhHell;

        public static implicit operator BidBase(OhHellBid ohb)
        {
            return new BidBase(ohb.theBid);
        }

        public static implicit operator int(OhHellBid ohb)
        {
            return ohb.theBid;
        }

        public override string ToString()
        {
            return Tricks.ToString();
        }

        public static OhHellBid FromTricks(int nTricks)
        {
            return new OhHellBid(nTricks + (int)BidSpace.OhHell);
        }

        public static string GetBidText(int bid, out bool cantakepoints, out int? expectedpoints, out int? level)
        {
            var ohb = new OhHellBid(bid);
            cantakepoints = true;
            expectedpoints = ohb.Tricks;
            level = null;
            return ohb.ToString();
        }
    }
}