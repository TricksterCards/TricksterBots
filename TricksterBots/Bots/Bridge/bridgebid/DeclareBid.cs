using Trickster.cloud;

namespace Trickster.Bots
{
    public class DeclareBid
    {
        public enum DoubleOrRe
        {
            None,
            Double,
            Redouble
        }

        private int bit9;
        private int highTwoBits;

        public DeclareBid(int l, Suit s, DoubleOrRe dor = DoubleOrRe.None)
        {
            level = l;
            suit = s;
            highTwoBits = 0;

            switch (dor)
            {
                case DoubleOrRe.Double:
                    Double();
                    break;
                case DoubleOrRe.Redouble:
                    Redouble();
                    break;
            }
        }

        //  initialize the declare bid to a 0-level Joker bid which won't match anything
        public DeclareBid(int bidValue) : this(0, Suit.Joker)
        {
            if (Is(bidValue))
            {
                suit = (Suit)((bidValue - BridgeBid.Declare) & 0x7); // range 0 - 7 (used 1 - 5)
                level = ((bidValue - BridgeBid.Declare) >> 3) & 0x7; // range 0 - 7 (used 1 - 7)
                highTwoBits = (bidValue - BridgeBid.Declare) & 0xC0; // two bits where we encode double and redouble
                bit9 = (bidValue - BridgeBid.Declare) & 0x100; // one bit where we encode declarer pass
            }
        }

        public bool doubled => highTwoBits == 0x40;
        public int level { get; }

        public bool passed
        {
            get => bit9 == 0x100;
            set => bit9 = value ? 0x100 : 0;
        }

        public bool redoubled => highTwoBits == 0x80;

        public Suit suit { get; }

        public DeclareBid Double()
        {
            highTwoBits = 0x40;
            return this;
        }

        public static bool Is(int bidValue)
        {
            return bidValue >= BridgeBid.Declare && bidValue <= BridgeBid.Declare + 0x1FF;
        }

        public static implicit operator int(DeclareBid db)
        {
            return BridgeBid.Declare + (int)db.suit + (db.level << 3) + db.highTwoBits + db.bit9;
        }

        public static implicit operator BidBase(DeclareBid db)
        {
            return new BidBase((int)db);
        }

        public static implicit operator DeclareBid(int bid)
        {
            return new DeclareBid(bid);
        }

        public DeclareBid Pass()
        {
            passed = true;
            return this;
        }

        public DeclareBid Redouble()
        {
            highTwoBits = 0x80;
            return this;
        }
    }
}