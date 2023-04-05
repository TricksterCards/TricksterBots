using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable StringIndexOfIsCultureSpecific.1

namespace TestBots.Bridge
{
    internal class Fuzz
    {
        public static BasicTests.BasicTest[] GeneratePlayTests(int count)
        {
            var tests = new List<BasicTests.BasicTest>();
            var random = new Random(1); // Specify a seed so re-runs are deterministic

            for (var i = 0; i < count; i++)
            {
                var hands = GetRandomHands(random);
                var history = GetRandomBidHistory(random, out var contract, out var declarerSeat);
                var plays = GetRandomPlayHistory(random, contract[1], hands, declarerSeat, out var nextPlaySeat);
                var hand = hands[nextPlaySeat];
                var dummy = plays.Length > 0 ? string.Join("", hands[(declarerSeat + 2) % 4]) : "";
                tests.Add(new BasicTests.BasicTest()
                {
                    play = "", // accept any suggestion, so long as we don't error
                    contract = contract,
                    dealerSeat = 0,
                    declarerSeat = declarerSeat,
                    dummy = dummy,
                    hand = string.Join("", hand),
                    history = history,
                    plays = plays,
                    type = $"Fuzz {i}",
                });
            }

            return tests.ToArray();
        }

        private static List<string> GetContractBids()
        {
            var bids = new List<string>();
            for (var i = 1; i <= 7; i++)
            {
                foreach (var s in new[] { "C", "D", "H", "S", "NT" })
                {
                    bids.Add($"{i}{s}");
                }
            }
            return bids;
        }

        private static List<string> GetDeck()
        {
            var deck = new List<string>();
            for (var s = 0; s < PBN.SuitLetters.Length; s++)
            {
                for (var r = 1; r < PBN.CardRanks.Length; r++)
                {
                    deck.Add($"{PBN.CardRanks[r]}{PBN.SuitLetters[s]}");
                }
            }
            return deck;
        }

        private static string[] GetRandomBidHistory(Random random, out string finalContract, out int declarerSeat)
        {
            declarerSeat = 0;
            var firstBidSeat = random.Next(4);
            var bids = new List<string>();
            var legalContractBids = GetContractBids();
            // Increase the weight for lower-level bids (7 for level 1 down to 1 for level 7)
            var weightedLegalContractBids = legalContractBids.SelectMany(b => Enumerable.Repeat(b, 8 - int.Parse(b.Substring(0, 1)))).ToList();
            var passCount = 0;
            var contract = "";

            // We're done bidding once we get 3 passes after our first bid
            while (passCount < 3 || bids.Count <= firstBidSeat)
            {
                var pass = bids.Count < firstBidSeat || legalContractBids.Count == 0 || random.Next(3) != 0;
                if (pass && bids.Count != firstBidSeat)
                {
                    bids.Add("Pass");
                    passCount++;
                }
                else
                {
                    var index = random.Next(weightedLegalContractBids.Count);
                    var bid = weightedLegalContractBids[index];
                    var legalIndex = legalContractBids.IndexOf(bid);
                    contract = bid;
                    bids.Add(bid);
                    legalContractBids = legalContractBids.Skip(legalIndex + 1).ToList();
                    weightedLegalContractBids = weightedLegalContractBids.Where(b => legalContractBids.Contains(b)).ToList();
                    passCount = 0;
                }
            }

            // declarer is first player in winning partnership to bid the suit
            var lastContractBidIndex = bids.FindLastIndex(b => b != "Pass");
            var winningPair = lastContractBidIndex % 2;

            for (var i = winningPair; i < bids.Count; i += 2)
            {
                if (bids[i][1] == contract[1])
                {
                    declarerSeat = i;
                    break;
                }
            }

            finalContract = contract;

            return bids.ToArray();
        }

        private static List<string> GetRandomHand(Random random, List<string> deck)
        {
            var hand = new List<string>();
            for (var i = 0; i < 13; i++)
            {
                var index = random.Next(deck.Count);
                hand.Add(deck[index]);
                deck.RemoveAt(index);
            }
            return hand;
        }

        private static List<List<string>> GetRandomHands(Random random)
        {
            var deck = GetDeck();
            return new List<List<string>>
            {
                GetRandomHand(random, deck),
                GetRandomHand(random, deck),
                GetRandomHand(random, deck),
                GetRandomHand(random, deck)
            };
        }

        private static string[] GetRandomPlayHistory(Random random, char trump, List<List<string>> hands, int declarerSeat, out int nextPlaySeat)
        {
            nextPlaySeat = (declarerSeat + 1) % 4;
            var nPlays = random.Next(hands.Sum(h => h.Count)); // 52
            var trick = "";
            var topCard = "";
            var topSeat = 0;
            var plays = new List<string>();
            var ledSuit = 'N';
            for (var i = 0; i < nPlays; i++)
            {
                var hand = hands[nextPlaySeat];
                if (ledSuit != 'N')
                {
                    var legalCards = hand.Where(c => c[1] == ledSuit).ToList();
                    if (!legalCards.Any())
                        legalCards = hand;

                    var card = legalCards[random.Next(legalCards.Count)];
                    trick += card;
                    plays.Add(card);
                    hand.Remove(card);

                    var rank = PBN.CardRanks.IndexOf(card[0]);
                    var topRank = PBN.CardRanks.IndexOf(topCard[0]);
                    var isTop = card[1] == trump && topCard[1] != trump || (card[1] == topCard[1] && rank > topRank);
                    if (isTop)
                    {
                        topCard = card;
                        topSeat = nextPlaySeat;
                    }

                    if (trick.Length < 8)
                    {
                        nextPlaySeat = (nextPlaySeat + 1) % 4;
                    }
                    else
                    {
                        nextPlaySeat = topSeat;
                        ledSuit = 'N';
                        trick = "";
                        topCard = "";
                        topSeat = 0;
                    }
                }
                else
                {
                    var card = hand[random.Next(hand.Count)];
                    trick = card;
                    topCard = card;
                    topSeat = nextPlaySeat;
                    ledSuit = card[1];
                    nextPlaySeat = (nextPlaySeat + 1) % 4;
                    plays.Add(card);
                    hand.Remove(card);
                }
            }
            return plays.ToArray();
        }
    }
}