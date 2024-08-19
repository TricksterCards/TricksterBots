using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using TestBots.Bridge;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestEuchrePTNs
    {
        private static readonly Regex rxBid = new Regex("^(?<level>\\d+)?(?<suit>♠|♥|♦|♣|NT|NT↑|NT↓)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Dictionary<char, Suit> LetterToSuit = new Dictionary<char, Suit> {
            { 'S', Suit.Spades },
            { 'H', Suit.Hearts },
            { 'D', Suit.Diamonds },
            { 'C', Suit.Clubs },
            { 'N', Suit.Unknown }
        };

        private static readonly Dictionary<char, Suit> SuitSymbolToSuit = new Dictionary<char, Suit> {
            { '♠', Suit.Spades },
            { '♥', Suit.Hearts },
            { '♦', Suit.Diamonds },
            { '♣', Suit.Clubs },
            { 'N', Suit.Unknown }
        };

        [TestMethod]
        public void PtnTestFiles()
        {
            var failures = new List<string>();
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // ReSharper disable once AssignNullToNotNullAttribute
            var files = Directory.GetFiles(Path.Combine(dir, "Euchre", "BidEuchre"), "*.ptn");
            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                var tests = PTN.ImportTests(text);
                var filename = Path.GetFileName(file);

                foreach (var test in tests)
                {
                    if (!string.IsNullOrEmpty(test.bid))
                    {
                        // TODO: Run test.bid tests
                        //var failure = RunBidTest(test);
                        //if (failure != null)
                        //    failures.Add($"{filename}: {failure}");
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

        private static int GetBid(string bid)
        {
            if (bid == "Pass")
                return BidBase.Pass;

            var (value, _) =  GetContract(bid);

            return value;
        }

        private static (int value, Suit suit) GetContract(string bid)
        {

            if (SuitSymbolToSuit.TryGetValue(bid[0], out var suitOnly))
            {
                var type = bid.EndsWith("alone") ? EuchreBid.MakeAlone : EuchreBid.Make;
                return ((int)type + (int)suitOnly, suitOnly);
            }

            if (int.TryParse(bid, out var levelOnly))
                return (BidEuchreBid.FromLevel(levelOnly), Suit.Unknown);

            Suit suit;
            int level;
            var bidParts = rxBid.Match(bid);

            if (bidParts.Groups["suit"].Value == "NT↓")
                suit = Suit.Joker;
            else
                suit = SuitSymbolToSuit[bidParts.Groups["suit"].Value[0]];

            if (bid.Contains("call 2"))
                level = BidEuchreBid.AloneCall2Bid;
            else if (bid.Contains("call 1"))
                level = BidEuchreBid.AloneCall1Bid;
            else if (bid.Contains("alone"))
                level = BidEuchreBid.AloneCall0Bid;
            else
                level = int.Parse(bidParts.Groups["level"].Value);

            return (BidEuchreBid.FromSuitAndLevel(suit, level), suit);
        }

        private static string RunPlayTest(BasicTest test)
        {
            var options = string.IsNullOrEmpty(test.optionsJson) ? new EuchreOptions() : JsonConvert.DeserializeObject<EuchreOptions>(test.optionsJson);
            var contract = GetContract(test.contract);
            var cardsPlayedInOrder = "";
            var players = new[] { new TestPlayer(), new TestPlayer(), new TestPlayer(), new TestPlayer() };
            for (var i = 0; i < 4; i++)
            {
                players[i].Seat = (test.declarerSeat + i) % 4;

                if (players[i].Seat == test.declarerSeat)
                    players[i].Bid = contract.value;
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
                var trick = test.plays.Skip(i).Take(4).ToList();
                for (var j = 0; j < trick.Count; j++)
                {
                    var card = trick[j];
                    var seat = (nextSeat + j) % 4;
                    var player = players[seat];
                    cardsPlayedInOrder += $"{seat}{card}";
                    if (j > 0 && card[1] != trick[0][1])
                        player.VoidSuits.Add(LetterToSuit[trick[0][1]]);
                }
                if (trick.Count == 4)
                {
                    var topCard = PTN.GetTopCard(trick, test.contract[1]);
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
                else // TODO: calculate correct length based on who's played in the current trick
                    player.Hand = UnknownCards(test.hand.Length / 2); 
            }

            // fill in bid history per player
            if (test.history.Length > 0)
            {
                for (var i = 0; i < test.history.Length; i++)
                {
                    var seat = (test.dealerSeat + i) % 4;
                    if (test.history[i] != "-")
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
                var bot = new EuchreBot(options, contract.suit);
                var trickLength = test.plays.Length % 4;
                var trick = string.Join("", test.plays.Skip(test.plays.Length - trickLength));
                var cardState = new TestCardState<EuchreOptions>(bot, players, trick) {
                    cardsPlayedInOrder = cardsPlayedInOrder,
                    trumpSuit = contract.suit,
                };
                var suggestion = bot.SuggestNextCard(cardState);

                if (!string.IsNullOrEmpty(test.play))
                    return test.play != suggestion.ToString() ? $"Test '{test.type}' suggested {suggestion} but expected {test.play}" : null;

                return suggestion == null ? $"Test '{test.type}' failed to return a suggestion" : null;
            }
        }

        private static string UnknownCards(int length)
        {
            return string.Concat(Enumerable.Repeat("0U", length));
        }
    }
}
