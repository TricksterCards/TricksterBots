using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        public void BasicTests()
        {
            var bot = new BridgeBot(new BridgeOptions(), Suit.Unknown);

            foreach (var test in TestBots.BasicTests.Tests.Select(ti => new BidTest(ti)))
            {
                var suggestion = bot.SuggestBid(new BridgeBidHistory(test.bidHistory), test.hand).value;
                Assert.AreEqual(test.expectedBid, suggestion,
                    $"Test '{test.type}' suggested {BidString(suggestion)} ({suggestion}) but expected {BidString(test.expectedBid)} ({test.expectedBid})"
                );
            }
        }

        [TestMethod]
        public void SaycTestSuite()
        {
            var bot = new BridgeBot(new BridgeOptions(), Suit.Unknown);
            var totalTests = 0;
            var totalPasses = 0;
            var hasVulnerable = 0;
            var changesFromPrevious = 0;

            var results = new Dictionary<string, List<SaycResult>>();

            foreach (var testItem in Test_Sayc.Tests)
            {
                var tests = testItem.Value.Select(ti => new BidTest(ti)).ToList();

                results.Add(testItem.Key, new List<SaycResult>(tests.Count));
                var previousResults = Test_Sayc_Results.Results[testItem.Key];

                var passes = 0;
                for (var i = 0; i < tests.Count; i++)
                {
                    var test = tests[i];
                    var pr = previousResults[i];
                    var suggestion = bot.SuggestBid(new BridgeBidHistory(test.bidHistory), test.hand).value;
                    var passed = suggestion == test.expectedBid;
                    results[testItem.Key].Add(new SaycResult(passed, suggestion));

                    if (pr.passed != passed || pr.suggested != suggestion)
                    {
                        changesFromPrevious++;

                        if (pr.passed != passed)
                            Logger.LogMessage($"!!! Previously, {testItem.Key}[{i}] {PassFail(pr.passed)} but now it {PassFail(passed)}");

                        if (pr.suggested != suggestion)
                            Logger.LogMessage(
                                $"!!! Previously, {testItem.Key}[{i}] suggested {BidString(pr.suggested)} ({pr.suggested}) but now it returned {BidString(suggestion)} ({suggestion})");
                    }

                    if (passed) passes++;
                }

                Logger.LogMessage($"{(double)passes / tests.Count:P0} ({passes} / {tests.Count}) of tests in \"{testItem.Key}\" passed");

                totalTests += tests.Count;
                totalPasses += passes;
                hasVulnerable += tests.Count(test => test.vulnerable != BidTest.Vulnerable.Unset);
            }

            Logger.LogMessage($"{Environment.NewLine}Overall, {(double)totalPasses / totalTests:P2} ({totalPasses} / {totalTests}) of tests passed");
            Logger.LogMessage($"{hasVulnerable} tests have vulnerablility set");

            if (changesFromPrevious > 0)
                UpdateSaycResults(results);

            Assert.IsTrue(totalPasses >= 500, "At least expected number of tests passed");
            Assert.AreEqual(0, changesFromPrevious, $"{changesFromPrevious} test(s) changed results from previous");
        }

        private static string BidString(int bidValue)
        {
            switch (bidValue)
            {
                case BidBase.Pass:
                    return "Pass";
                case BridgeBid.Double:
                    return "X";
                case BridgeBid.Redouble:
                    return "XX";
                default:
                    var db = new DeclareBid(bidValue);
                    return $"{db.level}{(db.suit == Suit.Unknown ? "NT" : Card.SuitSymbol(db.suit))}";
            }
        }

        private static string PassFail(bool passed)
        {
            return passed ? "passed" : "failed";
        }

        private static void UpdateSaycResults(Dictionary<string, List<SaycResult>> results)
        {
            //  skip this if being run by DevOps Service
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")))
                return;

            var path = Path.GetFullPath(@"..\..\Bridge\Test_Sayc_Results.cs");
            if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                return;

            var sb = new StringBuilder();
            sb.AppendLine(@"using System.Collections.Generic;

namespace TestBots
{
    public static class Test_Sayc_Results
    {
        public static readonly Dictionary<string, SaycResult[]> Results = new Dictionary<string, SaycResult[]>
        {");

            foreach (var result in results)
            {
                var s = result.Value.Select(r => $"new SaycResult({r.passed.ToString().ToLowerInvariant()}, {r.suggested})");

                sb.AppendLine($@"             {{
                ""{result.Key}"", new[]
                {{
                    {string.Join($",{Environment.NewLine}                    ", s)}
                }}
             }},");
            }

            sb.AppendLine(@"        };
    }
}");

            var existing = File.ReadAllText(path);
            var endLine1 = existing.IndexOf(Environment.NewLine, StringComparison.Ordinal);
            existing = existing.Substring(endLine1 + Environment.NewLine.Length);

            if (sb.ToString() != existing)
            {
                sb.Insert(0, $"// last updated {DateTime.Now:M/d/yyyy h:mm tt (K)}{Environment.NewLine}");
                Logger.LogMessage($"{Environment.NewLine}Updating {path}...");
                File.WriteAllText(path, sb.ToString());
            }
        }
    }
}