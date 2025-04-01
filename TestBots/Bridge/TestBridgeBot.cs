using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using Newtonsoft.Json;
using TestBots.Bridge;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestBridgeBot
    {
        private static readonly Dictionary<char, Suit> LetterToSuit = new Dictionary<char, Suit> {
            { 'S', Suit.Spades },
            { 'H', Suit.Hearts },
            { 'D', Suit.Diamonds },
            { 'C', Suit.Clubs },
            { 'N', Suit.Unknown }
        };

        private static readonly Dictionary<char, char> SuitSymbolToLetter = new Dictionary<char, char> {
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
        [DataRow("5♥", "{\"cloudBid\":null,\"dealerSeat\":2,\"hand\":[{\"s\":1,\"r\":6},{\"s\":1,\"r\":12},{\"s\":1,\"r\":13},{\"s\":1,\"r\":14},{\"s\":2,\"r\":3},{\"s\":2,\"r\":7},{\"s\":2,\"r\":8},{\"s\":3,\"r\":2},{\"s\":3,\"r\":6},{\"s\":3,\"r\":11},{\"s\":4,\"r\":4},{\"s\":4,\"r\":5},{\"s\":4,\"r\":14}],\"legalBids\":[{\"cTP\":true,\"eP\":11,\"l\":5,\"t\":\"5♣\",\"v\":445},{\"cTP\":true,\"eP\":11,\"l\":5,\"t\":\"5♦\",\"v\":446},{\"cTP\":true,\"eP\":11,\"l\":5,\"t\":\"5♥\",\"v\":448},{\"cTP\":true,\"eP\":11,\"l\":5,\"t\":\"5♠\",\"v\":447},{\"cTP\":true,\"eP\":11,\"l\":5,\"t\":\"5NT\",\"v\":444},{\"cTP\":true,\"eP\":12,\"l\":6,\"t\":\"6♣\",\"v\":453},{\"cTP\":true,\"eP\":12,\"l\":6,\"t\":\"6♦\",\"v\":454},{\"cTP\":true,\"eP\":12,\"l\":6,\"t\":\"6♥\",\"v\":456},{\"cTP\":true,\"eP\":12,\"l\":6,\"t\":\"6♠\",\"v\":455},{\"cTP\":true,\"eP\":12,\"l\":6,\"t\":\"6NT\",\"v\":452},{\"cTP\":true,\"eP\":13,\"l\":7,\"t\":\"7♣\",\"v\":461},{\"cTP\":true,\"eP\":13,\"l\":7,\"t\":\"7♦\",\"v\":462},{\"cTP\":true,\"eP\":13,\"l\":7,\"t\":\"7♥\",\"v\":464},{\"cTP\":true,\"eP\":13,\"l\":7,\"t\":\"7♠\",\"v\":463},{\"cTP\":true,\"eP\":13,\"l\":7,\"t\":\"7NT\",\"v\":460},{\"cTP\":true,\"t\":\"Pass\",\"v\":-2}],\"players\":[{\"Bid\":-1,\"BidHistory\":[440],\"CardsTaken\":\"\",\"Hand\":\"0U0U0U0U0U0U0U0U0U0U0U0U0U\",\"PlayedCards\":[],\"Seat\":0,\"VoidSuits\":[]},{\"Bid\":-1,\"BidHistory\":[-2],\"CardsTaken\":\"\",\"Hand\":\"0U0U0U0U0U0U0U0U0U0U0U0U0U\",\"PlayedCards\":[],\"Seat\":1,\"VoidSuits\":[]},{\"Bid\":436,\"BidHistory\":[416,436],\"CardsTaken\":\"\",\"Hand\":\"0U0U0U0U0U0U0U0U0U0U0U0U0U\",\"PlayedCards\":[],\"Seat\":2,\"VoidSuits\":[]},{\"Bid\":-2,\"BidHistory\":[-2,-2],\"CardsTaken\":\"\",\"Hand\":\"0U0U0U0U0U0U0U0U0U0U0U0U0U\",\"PlayedCards\":[],\"Seat\":3,\"VoidSuits\":[]}],\"upCard\":null,\"upCardSuit\":0,\"vulnerabilityBySeat\":[false,false,false,false],\"options\":{\"_honors\":{\"honors\":[{\"s\":2,\"r\":14},{\"s\":2,\"r\":13},{\"s\":2,\"r\":12},{\"s\":2,\"r\":10}],\"points\":0,\"seat\":-1},\"allowUndo\":true,\"allowUndoBids\":false,\"bidding\":4,\"miniBridgeBidLevels\":1,\"chicagoPartscore\":0,\"gameCode\":5,\"goodPracticeHandToSeatZero\":false,\"honorsBonus\":false,\"rubberDealLimit\":0,\"variation\":0,\"CompeteBuyIn\":0,\"CompeteFee\":0,\"CompeteWinnings\":0,\"gameOverScore\":2199023255552,\"gamePlayMode\":4,\"gameVisibility\":1,\"isCustom\":true,\"isPartnership\":true,\"noSuggestions\":true,\"noWatching\":true,\"reviewLastDeal\":true,\"scheduledStart\":\"\"},\"player\":{\"Bid\":-1,\"BidHistory\":[440],\"CardsTaken\":\"\",\"Hand\":\"6CQCKCAC3D7D8D2S6SJS4H5HAH\",\"PlayedCards\":[],\"Seat\":0,\"VoidSuits\":[]},\"trumpSuit\":0}",
            DisplayName="Bridgit handles Blackwood")]
        public void SnapshotTests(string expected, string snapshot)
        {
            var state = JsonConvert.DeserializeObject<SuggestBidState<BridgeOptions>>(snapshot);
            var bot = new BridgeBot(new BridgeOptions{ bidding = BridgeBiddingScheme.TwoOverOne }, Suit.Unknown);
            BidBase bid = null;

            try
            {
                bid = bot.SuggestBid(state);
            }
            catch (Exception err)
            {
                Assert.Fail($"Bridgit threw an exception: {err}");
            }

            if (bid != null)
                Assert.AreEqual(expected, BidString(bid.value));
        }

        [TestMethod]
        [DataRow("7NT", "ASKSQSJSAHKHQHJHADKDQDJDAC", "KCQCJCTSTHTDTC9S9H9D9C8S8H", "1NT 1S 1H 1D 1C 3NT 4S 4H 5D 5C 6NT 6S 6H 6D 6C 7NT 7S 7H 7D 7C", DisplayName = "Bid grand slam in NT")]
        [DataRow("7♠" , "ASKSQSJSAHKHQHJHADKDQDJDAC", "KCQCJCTCTS9S8S7S6S5S4S3S2S", "1NT 1S 1H 1D 1C 3NT 4S 4H 5D 5C 6NT 6S 6H 6D 6C 7NT 7S 7H 7D 7C", DisplayName = "Bid grand slam in a suit")]
        [DataRow("3NT", "ASKSQSJSAHKHQHJHADKDQDJDAC", "KCQCJCTSTHTDTC9S9H9D9C8S8H", "1NT 1S 1H 1D 1C 3NT 4S 4H 5D 5C", DisplayName = "Bid game in NT if slam is not available")]
        [DataRow("4♠" , "ASKSQSJSAHKHQHJHADKDQDJDAC", "KCQCJCTCTS9S8S7S6S5S4S3S2S", "1NT 1S 1H 1D 1C 3NT 4S 4H 5D 5C", DisplayName = "Bid game in a suit if slam is not available")]
        [DataRow("1NT", "QSJS3S2SQHJH3H2HQDJD3D2D3C", "KCQCJCTSTHTDTC9S9H9D9C8S8H", "1NT 1S 1H 1D 1C 3NT 4S 4H 5D 5C", DisplayName = "Bid partscore in NT")]
        [DataRow("1♠" , "QSJS3S2SQHJH3H2HQDJD3D2D3C", "KCQCJCTCTS9S8S7S6S5S4S3S2S", "1NT 1S 1H 1D 1C 3NT 4S 4H 5D 5C", DisplayName = "Bid partscore in a suit")]
        [DataRow("1♥" , "JS9S7SAHKHQHJH4H3H8C4C3C2D", "ASQS3S7H2HKCTC6CJD8D6D5D4D", "1NT 1S 1H 1D 1C 3NT 4S 4H 5D 5C", DisplayName = "Bid partscore in a suit (case 2)")]
        [DataRow("1♥" , "KC9C8C2C9HASQS5SADJD7D4D3D", "ACTC6C3CAHJHTH8H5H3H2H3S2S", "1NT 1S 1H 1D 1C 3NT 4S 4H 5D 5C", DisplayName = "Prefer bidding a major")]
        [DataRow("2NT", "QSJS3S2SQHJH3H2HQDJD3D2D3C", "KCQCJCTSTHTDTC9S9H9D9C8S8H", "2C 2D 2H 2S 2NT", DisplayName = "Handle fixed bidding in NT")]
        [DataRow("2♠" , "QSJS3S2SQHJH3H2HQDJD3D2D3C", "KCQCJCTCTS9S8S7S6S5S4S3S2S", "2C 2D 2H 2S 2NT", DisplayName = "Handle fixed bidding in a suit")]

        public void MiniBridgeBidding(string bid, string hand, string partnerHand, string bids)
        {
            var legalBids = bids.Split(' ').Select(b => new BidBase(new DeclareBid(int.Parse(b[0].ToString()), LetterToSuit[b[1]]))).ToList();
            var options = new BridgeOptions { variation = BridgeVariation.Mini };
            var bot = new BridgeBot(options, Suit.Unknown);
            var players = new List<PlayerBase>
            {
                new PlayerBase { Seat = 0, Hand = hand },
                new PlayerBase { Seat = 1 },
                new PlayerBase { Seat = 2, Hand = partnerHand },
                new PlayerBase { Seat = 3 }
            };
            var state = new SuggestBidState<BridgeOptions>
            {
                hand = new Hand(players[0].Hand),
                legalBids = legalBids,
                player = players[0],
                players = players
            };
            var suggestion = bot.SuggestBid(state);

            Assert.AreEqual(bid, BidString(suggestion.value));
        }

        [TestMethod]
        public void FuzzPlays()
        {
            var failures = new List<string>();
            foreach (var test in Fuzz.GeneratePlayTests(100000))
            {
                var failure = RunPlayTest(test);
                if (failure != null)
                    failures.Add(failure);
            }
            if (failures.Count > 0)
                Assert.Fail($"{failures.Count} test{(failures.Count == 1 ? "" : "s")} failed.\n{string.Join("\n", failures)}");
        }

        [TestMethod]
        public void BridgitSanityTests()
        {
            var failures = new List<string>();
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // ReSharper disable once AssignNullToNotNullAttribute
            var files = Directory.GetFiles(Path.Combine(dir, "Bridge", "Bridgit"), "*.pbn");
            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                var tests = PTN.ImportTests(text, new BridgeOptions());
                var filename = Path.GetFileName(file);

                if (!tests.All(t => t.nPlayers == 4 && t.nCardsPerPlayer == 13))
                {
                    failures.Add($"{filename}: Not all tests have 4 players with 13 cards each");
                    continue;
                }

                foreach (var test in tests)
                {
                    if (!string.IsNullOrEmpty(test.bid))
                    {
                        // TODO: also validate bid description and metadata match expectations
                        var failure = RunBidTest(new BidTest(test), BridgeBiddingScheme.TwoOverOne);
                        if (failure != null)
                            failures.Add($"{filename}: {failure}");
                    }
                    else
                    {
                        failures.Add($"{filename}: '{test.type}' must have an expected bid.");
                    }
                }
            }
            if (failures.Count > 0)
                Assert.Fail($"{failures.Count} test{(failures.Count == 1 ? "" : "s")} failed.\n{string.Join("\n", failures)}");
        }

        [TestMethod]
        public void SaycTestFiles()
        {
            var failures = new List<string>();
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // ReSharper disable once AssignNullToNotNullAttribute
            var files = Directory.GetFiles(Path.Combine(dir, "Bridge", "SAYC"), "*.pbn");
            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                var tests = PTN.ImportTests(text, new BridgeOptions());
                var filename = Path.GetFileName(file);

                if (!tests.All(t => t.nPlayers == 4 && t.nCardsPerPlayer == 13))
                {
                    failures.Add($"{filename}: Not all tests have 4 players with 13 cards each");
                    continue;
                }

                foreach (var test in tests)
                {
                    if (!string.IsNullOrEmpty(test.bid))
                    {
                        var failure = RunBidTest(new BidTest(test));
                        if (failure != null)
                            failures.Add($"{filename}: {failure}");
                    }
                    else if (!string.IsNullOrEmpty(test.play))
                    {
                        var failure = RunPlayTest(test);
                        if (failure != null)
                            failures.Add($"{filename}: {failure}");
                    }
                    else
                    {
                        failures.Add($"{filename}: '{test.type}' must have either an expected bid or expected play.");
                    }
                }
            }
            if (failures.Count > 0)
                Assert.Fail($"{failures.Count} test{(failures.Count == 1 ? "" : "s")} failed.\n{string.Join("\n", failures)}");
        }

        [TestMethod]
        public void SaycTestSuite()
        {
            var bot = new BridgeBot(new BridgeOptions { withCappelletti = true }, Suit.Unknown);
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
                    results[testItem.Key].Add(new SaycResult(passed, suggestion, test.expectedBid));

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

        private static DeclareBid GetContract(BasicTest test)
        {
            var level = int.Parse(test.contract.Substring(0, 1));
            var suit = LetterToSuit[test.contract[1]];
            var riskStart = test.contract[1] == 'N' ? 3 : 2; // if suit was NT, risk starts at 3 instead of 2
            var risk = test.contract.Substring(riskStart, test.contract.Length - riskStart);
            var doubleOrRe = risk == "X" ? DeclareBid.DoubleOrRe.Double : risk == "XX" ? DeclareBid.DoubleOrRe.Redouble : DeclareBid.DoubleOrRe.None;
            return new DeclareBid(level, suit, doubleOrRe);
        }

        public static string BidString(int bidValue)
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

        private static string RunBidTest(BidTest test, BridgeBiddingScheme bidding = BridgeBiddingScheme.SAYC)
        {
            test.options.bidding = bidding;
            var bot = new BridgeBot(test.options, Suit.Unknown);
            var suggestion = bot.SuggestBid(new BridgeBidHistory(test.bidHistory), test.hand).value;

            if (test.expectedBid != suggestion)
                return $"Test '{test.type}' suggested {BidString(suggestion)} ({suggestion}) but expected {BidString(test.expectedBid)} ({test.expectedBid})";
            else
                return null;
        }

        private static string RunPlayTest(BasicTest test)
        {
            var contract = GetContract(test);
            var cardsPlayedInOrder = "";
            var dummyHand = string.IsNullOrEmpty(test.dummy) && test.hand.Length == 13 * 2 ? UnknownCards(13) : test.dummy;
            var players = new[] { new TestPlayer(), new TestPlayer(), new TestPlayer(), new TestPlayer() };
            for (var i = 0; i < 4; i++)
            {
                players[i].Seat = (test.declarerSeat + i) % 4;

                if (players[i].Seat == test.declarerSeat)
                    players[i].Bid = (int)contract;
                else if (players[i].Seat == (test.declarerSeat + 2) % 4)
                    players[i].Bid = BidBase.Dummy;
                else
                    players[i].Bid = BridgeBid.Defend;
            }

            // resort the players in seat order to simplify adding data
            players = players.OrderBy(p => p.Seat).ToArray();

            // fill in taken cards per player (and track who's turn it will be to play)
            var nextSeat = (test.declarerSeat + 1) % 4;
            for (var i = 0; i < test.plays.Length; i += 4)
            {
                var trick = new Hand(string.Join("", test.plays.Skip(i).Take(4).ToList()));
                for (var j = 0; j < trick.Count; j++)
                {
                    var card = trick[j];
                    var seat = (nextSeat + j) % 4;
                    var player = players[seat];
                    cardsPlayedInOrder += $"{seat}{card}";
                    if (j > 0 && card.suit != trick[0].suit)
                        player.VoidSuits.Add(trick[0].suit);
                }
                if (trick.Count == 4)
                {
                    var topCard = PTN.GetTopCard(trick, LetterToSuit[test.contract[1]], new BridgeOptions());
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
                else if (nextSeat == (player.Seat + 2) % 4 && players[(player.Seat + 2) % 4].Bid == BidBase.Dummy)
                    player.Hand = dummyHand; // Show declarer's hand to dummy if it's dummy's turn to play
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
                var cardState = new TestCardState<BridgeOptions>(bot, players, trick) {
                    cardsPlayedInOrder = cardsPlayedInOrder,
                    trumpSuit = contract.suit,
                };
                var suggestion = bot.SuggestNextCard(cardState);
                if (!string.IsNullOrEmpty(test.play))
                {
                    if (test.play != suggestion.ToString())
                        return $"Test '{test.type}' suggested {suggestion} but expected {test.play}";
                    else
                        return null;
                }
                else
                {
                    if (suggestion == null)
                        return $"Test '{test.type}' failed to return a suggestion";
                    else
                        return null;
                }
            }
        }

        private static string UnknownCards(int length)
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
                var s = result.Value.Select(r => $"new SaycResult({r.passed.ToString().ToLowerInvariant()}, {r.suggested}, {r.expected}),{r.csComment}");

                sb.AppendLine($@"             {{
                ""{result.Key}"", new[]
                {{
                    {string.Join($"{Environment.NewLine}                    ", s)}
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