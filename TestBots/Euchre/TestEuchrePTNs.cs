﻿using System.Collections.Generic;
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
                var tests = PTN.ImportTests(text, new EuchreOptions());
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
            var bot = new EuchreBot(options, contract.suit);
            var cardsPlayedInOrder = "";
            var players = new List<TestPlayer>();
            var nPlayers = options.players;

            for (var i = 0; i < nPlayers; i++)
            {
                var player = new TestPlayer();
                players.Add(player);

                player.Seat = (test.declarerSeat + i) % nPlayers;

                if (player.Seat == test.declarerSeat)
                    player.Bid = contract.value;
                else
                    player.Bid = (int)EuchreBid.NotMaker;
            }

            // resort the players in seat order to simplify adding data
            players = players.OrderBy(p => p.Seat).ToList();

            // fill in taken cards per player (and track who's turn it will be to play)
            var nextSeat = test.declarerSeat % nPlayers;
            for (var i = 0; i < test.plays.Length; i += nPlayers)
            {
                var trick = new Hand(string.Join("", test.plays.Skip(i).Take(nPlayers).ToList()));
                var ledSuit = trick.Count > 0 ? bot.EffectiveSuit(trick[0]) : Suit.Unknown;
                for (var j = 0; j < trick.Count; j++)
                {
                    var card = trick[j];
                    var seat = (nextSeat + j) % nPlayers;
                    var player = players[seat];
                    cardsPlayedInOrder += $"{seat}{card}";
                    if (j > 0 && bot.EffectiveSuit(card) != ledSuit)
                        player.VoidSuits.Add(ledSuit);
                }
                if (trick.Count == nPlayers)
                {
                    var topCard = PTN.GetTopCard(trick, contract.suit, options);
                    nextSeat = (nextSeat + trick.IndexOf(topCard)) % nPlayers;
                    players[nextSeat].CardsTaken += string.Join("", trick);
                }
                else
                {
                    nextSeat = (nextSeat + trick.Count) % nPlayers;
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
                    var seat = (test.dealerSeat + i) % nPlayers;
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
            players = players.OrderBy(p => (nPlayers + p.Seat - nextSeat) % nPlayers).ToList();

            {
                var trickLength = test.plays.Length % nPlayers;
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
