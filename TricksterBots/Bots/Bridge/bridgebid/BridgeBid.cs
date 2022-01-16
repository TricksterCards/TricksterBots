using Trickster.cloud;

namespace Trickster.Bots
{
    public abstract class BridgeBid
    {
        public const int Declare = Redouble + 1; // Declare takes 9 bits (512) to encode everything
        public const int Defend = (int)BidSpace.Bridge + 1;
        public const int Double = Defend + 1;
        public const int Redouble = Double + 1;
    }
}