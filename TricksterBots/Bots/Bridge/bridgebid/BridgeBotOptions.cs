using Trickster.cloud;

namespace Trickster.Bots
{
    public class BridgeBotOptions
    {
        public BridgeBotOptions(BridgeOptions options)
        {
            noTransfers = options.noTransfers;
        }

        public readonly bool noTransfers;
    }
}
