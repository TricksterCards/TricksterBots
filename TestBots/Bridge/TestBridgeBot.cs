using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestBridgeBot
    {
        [TestMethod]
        public void BidTests()
        {
            var bot = new BridgeBot(new BridgeOptions(), Suit.Unknown);

            foreach (var test in JsonTests.Tests.Select(ti => new SaycTest(ti)))
            {
                var suggestion = bot.SuggestBid(new BridgeBidHistory(test.bidHistory), test.hand).value;
                Assert.AreEqual(test.expectedBid, suggestion, test.type);
            }
        }

        [TestMethod]
        public void SaycTestSuite()
        {
            var bot = new BridgeBot(new BridgeOptions(), Suit.Unknown);
            var totalTests = 0;
            var totalPasses = 0;
            var hasVulnerable = 0;

            foreach (var testItem in Test_Sayc.Tests)
            {
                var tests = testItem.Value.Select(ti => new SaycTest(ti)).ToList();
                var passes = tests.Count(test => bot.SuggestBid(new BridgeBidHistory(test.bidHistory), test.hand).value == test.expectedBid);

                Logger.LogMessage($"{(double)passes / tests.Count:P1} ({passes} / {tests.Count}) of tests in \"{testItem.Key}\" passed");

                totalTests += tests.Count;
                totalPasses += passes;
                hasVulnerable += tests.Count(test => test.vulnerable != SaycTest.Vulnerable.Unset);
            }

            Logger.LogMessage($"{hasVulnerable} tests have vulnerablility set");

            Assert.IsTrue((double)totalPasses / totalTests > 0.55, "More than 55% of the tests passed");
            Assert.AreEqual(500, totalPasses, "The expected number of tests passed");
            //Assert.AreEqual(totalTests, totalPasses, "All the tests passed");
        }
    }
}