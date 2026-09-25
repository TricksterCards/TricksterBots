using Trickster.cloud;

namespace Trickster.Bots
{
    public class BridgeBotOptions
    {
        public BridgeBotOptions(BridgeOptions options)
        {
            bidding = options.bidding;
            noTransfers = options.noTransfers;
            withCappelletti = options.withCappelletti;
        }

        public readonly BridgeBiddingScheme bidding;

        public readonly bool noTransfers;

        public readonly bool withCappelletti;
    }
}
