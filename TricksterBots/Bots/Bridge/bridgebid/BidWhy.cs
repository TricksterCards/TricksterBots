using Trickster.cloud;

namespace Trickster.Bots
{
    internal class BidWhy : BidBase
    {
        private InterpretedBid _why;  // explanation + interpretation of this bid (explanation alone in bidbase)

        public InterpretedBid why
        {
            get
            {
                return _why;
            }
            set
            {
                _why = value;
                this.explanation = value != null ? new BidExplanation(value) : null;
            }
        }

        public BidWhy(BidBase bb)
            : base(bb.value)
        {
            why = null;
        }
    }
}