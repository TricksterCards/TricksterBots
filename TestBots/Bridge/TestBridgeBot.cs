using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using TestBots.Bridge;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestBridgeBot
    {
        private static Dictionary<char, Suit> LetterToSuit = new Dictionary<char, Suit> {
            { 'S', Suit.Spades },
            { 'H', Suit.Hearts },
            { 'D', Suit.Diamonds },
            { 'C', Suit.Clubs },
            { 'N', Suit.Unknown }
        };

        private static Dictionary<char, char> SuitSymbolToLetter = new Dictionary<char, char> {
            { '♠', 'S' },
            { '♥', 'H' },
            { '♦', 'D' },
            { '♣', 'C' }
        };

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
        public void FuzzPlays()
        {
            foreach (var test in Fuzz.GeneratePlayTests(100000))
            {
                RunPlayTest(test);
            }
        }

        [TestMethod]
        public void SaycTestFiles()
        {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // ReSharper disable once AssignNullToNotNullAttribute
            var files = Directory.GetFiles(Path.Combine(dir, "Bridge", "SAYC"), "*.pbn");
            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                var tests = PBN.ImportTests(text);

                foreach (var test in tests)
                {
                    if (!string.IsNullOrEmpty(test.bid))
                    {
                        RunBidTest(new BidTest(test));
                    }
                    else if (!string.IsNullOrEmpty(test.play))
                    {
                        RunPlayTest(test);
                    }
                    else
                    {
                        throw new ArgumentException("Test must have either an expected bid or expected play.");
                    }
                }
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

        private static BridgeBot GetBot(Suit trump = Suit.Unknown)
        {
            return GetBot(new BridgeOptions(), trump);
        }

        private static BridgeBot GetBot(BridgeOptions options, Suit trump = Suit.Unknown)
        {
            return new BridgeBot(options, trump);
        }

        private static int GetBid(string bid)
        {
            if (bid == "X")
                return BridgeBid.Double;

            if (bid == "XX")
                return BridgeBid.Redouble;

            if (bid == "Pass")
                return BidBase.Pass;

            var level = int.Parse(bid.Substring(0, 1));
            var suitLetter = SuitSymbolToLetter.ContainsKey(bid[1]) ? SuitSymbolToLetter[bid[1]] : bid[1];
            var suit = LetterToSuit[suitLetter];
            return new DeclareBid(level, suit);
        }

        private static DeclareBid GetContract(BasicTests.BasicTest test)
        {
            var level = int.Parse(test.contract.Substring(0, 1));
            var suit = LetterToSuit[test.contract[1]];
            var riskStart = test.contract[1] == 'N' ? 3 : 2; // if suit was NT, risk starts at 3 instead of 2
            var risk = test.contract.Substring(riskStart, test.contract.Length - riskStart);
            var doubleOrRe = risk == "X" ? DeclareBid.DoubleOrRe.Double : risk == "XX" ? DeclareBid.DoubleOrRe.Redouble : DeclareBid.DoubleOrRe.None;
            return new DeclareBid(level, suit, doubleOrRe);
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

        private static void RunBidTest(BidTest test)
        {
            var bot = new BridgeBot(new BridgeOptions(), Suit.Unknown);
            var suggestion = bot.SuggestBid(new BridgeBidHistory(test.bidHistory), test.hand).value;
            Assert.AreEqual(test.expectedBid, suggestion,
                $"Test '{test.type}' suggested {BidString(suggestion)} ({suggestion}) but expected {BidString(test.expectedBid)} ({test.expectedBid})"
            );
        }

        private static void RunPlayTest(BasicTests.BasicTest test)
        {
            var contract = GetContract(test);
            var dummyHand = string.IsNullOrEmpty(test.dummy) && test.hand.Length == 13 * 2 ? UnknownCards(13) : test.dummy;
            var players = new[] { new TestPlayer(), new TestPlayer(), new TestPlayer(), new TestPlayer() };
            for (var i = 0; i < 4; i++)
            {
                var seatRelativeToFirstLead = (test.plays.Length + i) % 4;
                players[i].Bid = seatRelativeToFirstLead == 1 ? BidBase.Dummy : seatRelativeToFirstLead % 2 == 0 ? BridgeBid.Defend : (int)contract;
                players[i].Seat = (test.declarerSeat + 1 + i) % 4;
            }

            // resort the players in seat order to simplify adding data
            players = players.OrderBy(p => p.Seat).ToArray();

            // fill in taken cards per player (and track who's turn it will be to play)
            var nextSeat = (test.declarerSeat + 1) % 4;
            for (var i = 0; i < test.plays.Length; i += 4)
            {
                var trick = test.plays.Skip(i).Take(4).ToList();
                if (trick.Count == 4)
                {
                    var topCard = PBN.GetTopCard(trick, test.contract[1]);
                    nextSeat = (nextSeat + trick.IndexOf(topCard)) % 4;
                    players[nextSeat].CardsTaken += string.Join("", trick);
                }
                else
                {
                    nextSeat = (nextSeat + trick.Count) % 4;
                }
            }

            // fill in hand per player
            foreach (var player in players)
            {
                if (player.Seat == nextSeat)
                    player.Hand = test.hand;
                else if (player.Bid == BidBase.Dummy)
                    player.Hand = dummyHand;
                else // TODO: calculate correct length based on who's played in the current trick
                    player.Hand = UnknownCards(test.hand.Length / 2); 
            }

            // fill in bid history per player
            if (test.history.Length > 0)
            {
                for (var i = 0; i < test.history.Length; i++)
                {
                    var seat = (test.dealerSeat + i) % 4;
                    players[seat].BidHistory.Add(GetBid(test.history[i]));
                }
            }
            else
            {
                // if no history was provided, assume declarer bid first at the contract level and everyone else passed
                foreach (var player in players)
                {
                    if (player.Seat == test.declarerSeat)
                        player.BidHistory.Add(GetBid(test.contract));
                    else
                        player.BidHistory.Add(BidBase.Pass);
                }
            }

            // resort players so the player we're asking to play is listed first (required by TestCardState)
            players = players.OrderBy(p => (4 + p.Seat - nextSeat) % 4).ToArray();

            {
                var bot = GetBot(contract.suit);
                var trickLength = test.plays.Length % 4;
                var trick = string.Join("", test.plays.Skip(test.plays.Length - trickLength));
                var cardState = new TestCardState<BridgeOptions>(bot, players, trick) { trumpSuit = contract.suit };
                var suggestion = bot.SuggestNextCard(cardState);
                if (!string.IsNullOrEmpty(test.play))
                {
                    Assert.AreEqual(test.play, suggestion.ToString(),
                        $"Test '{test.type}' suggested {suggestion} but expected {test.play}"
                    );
                }
                else
                {
                    Assert.IsNotNull(suggestion);
                }
            }
        }

        private static string UnknownCards(int length = 13)
        {
            return string.Concat(Enumerable.Repeat("0U", length));
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