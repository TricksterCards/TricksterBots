using Newtonsoft.Json;
using Trickster.cloud;

namespace TestBots
{
    public class SaycResult
    {
        public SaycResult(bool passed, int suggested, int expected = BidBase.NoBid)
        {
            this.passed = passed;
            this.suggested = suggested;
            this.expected = expected;
        }

        [JsonIgnore]
        public string csComment => passed ? string.Empty : $" // last run result: {TestBridgeBot.BidString(suggested)}; expected: {TestBridgeBot.BidString(expected)};";

        public int expected { get; set; }
        public bool passed { get; set; }
        public int suggested { get; set; }
    }
}