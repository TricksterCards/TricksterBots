using Trickster.cloud;

namespace Trickster.Bots
{
    public class BridgeBotOptions
    {
        public BridgeBotOptions(BridgeOptions options)
        {
            noTransfers = options.noTransfers;
            withCappelletti = options.withCappelletti;
        }

        public readonly bool noTransfers;

        public readonly bool withCappelletti;
    }
}
