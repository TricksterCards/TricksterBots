using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Trickster.cloud;

namespace TestBots.Bridge
{
    internal class PTN
    {
        private const string SuitLetters = "SHDCJ";
        private const string UnknownCard = "0U";
        private static readonly Regex rxDealerSeat = new Regex("^(?<side>N|E|W|S|SE|SW|NE|NW):", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex rxTrumpSuit = new Regex("(?<suit>♠|♥|♦|♣|S|H|D|C)", RegexOptions.Compiled);
        private static readonly Regex rxReplaceSuit = new Regex("(?<suit>S|H|D|C)$", RegexOptions.Compiled);

        private static readonly Dictionary<char, Suit> SuitLetterToSuit = new Dictionary<char, Suit> {
            { 'S', Suit.Spades },
            { 'H', Suit.Hearts },
            { 'D', Suit.Diamonds },
            { 'C', Suit.Clubs },
            { 'N', Suit.Unknown }
        };

        private static readonly Dictionary<char, string> SuitLetterToSymbol = new Dictionary<char, string> {
            { 'S', "♠" },
            { 'H', "♥" },
            { 'D', "♦" },
            { 'C', "♣" },
            { 'N', "N" }
        };

        private static readonly Dictionary<char, Suit> SuitSymbolToSuit = new Dictionary<char, Suit> {
            { '♠', Suit.Spades },
            { '♥', Suit.Hearts },
            { '♦', Suit.Diamonds },
            { '♣', Suit.Clubs },
            { 'N', Suit.Unknown }
        };

        private static readonly Dictionary<int, List<string>> sidesByNumPlayers = new Dictionary<int, List<string>>
        {
            { 2, new List<string> { "S", "N" } },
            { 3, new List<string> { "S", "W", "E" } },
            { 4, new List<string> { "S", "W", "N", "E" } },
            { 5, new List<string> { "S", "SW", "NW", "NE", "SE" } },
            { 6, new List<string> { "S", "SW", "NW", "N", "NE", "SE" } }
        };

        public static Card GetTopCard(List<Card> trick, Suit trump, GameOptions options)
        {
            var ledSuit = options.EffectiveSuit(trick[0], trump);
            return trick.Where(c => options.EffectiveSuit(c, trump) == trump).OrderByDescending(c => options.RankSort(c, trump)).FirstOrDefault()
                   ?? trick.Where(c => options.EffectiveSuit(c, trump) == ledSuit).OrderByDescending(c => options.RankSort(c, trump)).First();
        }

        public static BasicTest[] ImportTests<Options>(string text, Options defaultOptions) where Options : GameOptions
        {
            var tests = new List<BasicTest>();
            var contract = string.Empty;
            var dealerSeat = 0;
            var declarerSeat = -1;
            var firstBidderSeat = 0;
            var hands = new List<string>();
            var history = new List<string>();
            var tags = TokenizeTags(text);
            var name = string.Empty;
            var nPlayers = 0; // filled in after importing hands
            var nCardsPerPlayer = 0; // filled in after importing hands
            var optionsJson = string.Empty;
            var options = defaultOptions;

            foreach (var tag in tags)
                switch (tag.Name)
                {
                    case "Event":
                        dealerSeat = 0;
                        declarerSeat = -1;
                        firstBidderSeat = 0;
                        name = tag.Description;
                        options = defaultOptions;
                        optionsJson = string.Empty;
                        break;
                    case "GameOptionsJson":
                        optionsJson = tag.Description;
                        options = JsonConvert.DeserializeObject<Options>(optionsJson);
                        break;
                    case "Deal":
                        contract = string.Empty;
                        (dealerSeat, hands, nPlayers, nCardsPerPlayer) = ImportHands(tag.Description);
                        history = new List<string>();
                        break;
                    case "Auction":
                    {
                        firstBidderSeat = GetSide(tag.Description.ToUpperInvariant(), nPlayers);

                        if (defaultOptions.gameCode == GameCode.Bridge && firstBidderSeat != dealerSeat)
                            throw new Exception("Invalid Bridge PBN: Dealer is not first to bid in auction");

                        var bids = ImportBids(tag.Data);
                        history = new List<string>();
                        for (var i = 0; i < bids.Count; i++)
                        {
                            var bid = bids[i];
                            var seat = (firstBidderSeat + i) % nPlayers;
                            var hand = hands[seat];
                            var seatName = GetSideName(seat, nPlayers);
                            var bidNumber = 1 + i / nPlayers;
                            if (!IsUnknownHand(hand) && bid != "-")
                                tests.Add(
                                    new BasicTest
                                    {
                                        optionsJson = optionsJson,
                                        nPlayers = nPlayers,
                                        nCardsPerPlayer = nCardsPerPlayer,
                                        dealerSeat = dealerSeat,
                                        firstBidderSeat = firstBidderSeat,
                                        history = history.ToArray(),
                                        hand = hand,
                                        bid = bid,
                                        type = $"{name} (Seat {seatName}, Bid {bidNumber})"
                                    }
                                );
                            history.Add(bid);
                        }

                        break;
                    }
                    case "Contract":
                        contract = tag.Description;
                        break;
                    case "Declarer":
                        declarerSeat = GetSide(tag.Description.ToUpperInvariant(), nPlayers);
                        break;
                    case "Play":
                    {
                        Debug.Assert(2 <= nPlayers && nPlayers <= 6, $"nPlayers is {nPlayers}, which is not valid");
                        Debug.Assert(nCardsPerPlayer > 0, $"nCardsPerPlayer is {nCardsPerPlayer}, which is not valid");

                        var firstLeadSeat = GetSide(tag.Description.ToUpperInvariant(), nPlayers);
                        var leadSeat = firstLeadSeat;
                        var dummySeat = (leadSeat + 1) % nPlayers;  // why not (declarerSeat + 2) % nPlayers ?
                        var trick = new List<Card>();

                        if (declarerSeat == -1)
                            declarerSeat = (nPlayers + leadSeat - 1) % nPlayers;

                        var trump = Suit.Unknown;
                        var trumpStr = rxTrumpSuit.Match(contract).Groups["suit"];
                        if (trumpStr.Success)
                        {
                            if (!SuitSymbolToSuit.TryGetValue(trumpStr.Value[0], out trump))
                                SuitLetterToSuit.TryGetValue(trumpStr.Value[0], out trump);
                        }

                        var plays = ImportPlays(trump, tag.Data, nPlayers, options);

                        for (var i = 0; i < plays.Count; i++)
                        {
                            var play = plays[i];
                            var seat = (leadSeat + i) % nPlayers;
                            var hand = hands[seat];
                            var seatName = GetSideName(seat, nPlayers);
                            var playNumber = 1 + i / nPlayers;

                            // Don't validate plays for unknown hands
                            // And don't validate dummy plays when declarer's hand is unknown
                            if (!IsUnknownHand(hand) && !(seat == dummySeat && IsUnknownHand(hands[(dummySeat + nPlayers/2) % nPlayers])))
                                tests.Add(
                                    new BasicTest
                                    {
                                        optionsJson = optionsJson,
                                        nPlayers = nPlayers,
                                        nCardsPerPlayer = nCardsPerPlayer,
                                        contract = contract,
                                        dealerSeat = dealerSeat,
                                        declarerSeat = declarerSeat,
                                        firstBidderSeat = firstBidderSeat,
                                        firstLeadSeat = firstLeadSeat,
                                        history = history.ToArray(),
                                        dummy = i > 0 ? seat == dummySeat ? hands[declarerSeat] : hands[dummySeat] : string.Empty,
                                        hand = hand,
                                        play = play,
                                        plays = plays.GetRange(0, i).ToArray(),
                                        type = $"{name} (Seat {seatName}, Trick {playNumber})"
                                    }
                                );

                            trick.Add(new Card(play));

                            // Remove played card from hand
                            var regex = new Regex(IsUnknownHand(hand) ? UnknownCard : play);
                            hands[seat] = regex.Replace(hands[seat], string.Empty, 1);

                            // Update lead seat if end of trick
                            if (i % nPlayers == nPlayers - 1)
                            {
                                var card = GetTopCard(trick, trump, options);
                                leadSeat = (leadSeat + trick.IndexOf(card)) % nPlayers;
                                trick.Clear();
                            }
                        }

                        break;
                    }
                }

            // Ignore all other tags
            return tests.ToArray();
        }

        private static int GetSide(string sideString, int nPlayers)
        {
            if (sidesByNumPlayers.TryGetValue(nPlayers, out var sides))
            {
                var side = sides.IndexOf(sideString);

                if (side == -1)
                    throw new Exception($"Side string {sideString} not valid for {nPlayers} players");

                return side;
            }

            throw new Exception($"{nPlayers} is not a valid number of players");
        }

        private static string GetSideName(int seat, int nPlayers)
        {
            if (sidesByNumPlayers.TryGetValue(nPlayers, out var sides))
            {
                if (seat < sides.Count)
                    return sides[seat];

                throw new Exception($"Seat {seat} not valid for {nPlayers} players");
            }

            throw new Exception($"{nPlayers} is not a valid number of players");
        }

        private static List<string> ImportBids(List<string> bidLines)
        {
            return string.Join(" ", bidLines)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(bid => rxReplaceSuit.Replace(bid, (match) => SuitLetterToSymbol[match.Value[0]]))
                .ToList();
        }

        private static (int dealerSeat, List<string> hands, int nPlayers, int nCardsPerPlayer) ImportHands(string handsString)
        {
            var dealerSideMatch = rxDealerSeat.Match(handsString);
            var dealerSideString = dealerSideMatch.Success ? dealerSideMatch.Groups["side"].Value : string.Empty;
            var dealerSkip = dealerSideMatch.Success ? dealerSideMatch.Value.Length : 0;

            var handStrings = handsString.Substring(dealerSkip).Split(' ');
            var nPlayers = handStrings.Length;
            var dealerSeat = GetSide(dealerSideString.ToUpperInvariant(), nPlayers);

            var hands = new List<string>();
            for (var h = 0; h < nPlayers; ++h) hands.Add(string.Empty);

            for (var i = 0; i < nPlayers; i++)
            {
                var seat = (dealerSeat + i) % nPlayers;
                var handString = handStrings[i];
                var hand = string.Empty;

                if (handString != "-")
                {
                    var suits = handString.Split('.');

                    for (var j = 0; j < suits.Length; j++)
                        foreach (var card in suits[j])
                            hand = $"{card}{SuitLetters[j]}" + hand; // Put high cards on right to match Trickster Cards production behavior
                }

                hands[seat] = hand;
            }

            var nCardsPerPlayer = hands.Max(h => h.Length / 2);

            //  fill in the hands that were unspecified with unknown cards
            for (var i = 0; i < hands.Count; ++i)
                if (hands[i] == string.Empty)
                    hands[i] = string.Join(string.Empty, Enumerable.Repeat(UnknownCard, nCardsPerPlayer));

            // validate known hands are of the correct length
            var knownHands = hands.Where(h => !IsUnknownHand(h)).ToList();
            foreach (var hand in knownHands)
            {
                if (hand.Length != nCardsPerPlayer * 2)
                    throw new ArgumentException($"Hand without exactly {nCardsPerPlayer} cards found in '{handsString}'");
            }

            return (dealerSeat, hands, nPlayers, nCardsPerPlayer);
        }

        private static List<string> ImportPlays(Suit trump, List<string> playLines, int nPlayers, GameOptions options)
        {
            var plays = new List<string>();
            var leadSeat = 0;
            foreach (var line in playLines)
            {
                var cardPlays = line.ToUpperInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var cards = cardPlays.Select(cp => cp.Length == 2 ? new Card(string.Concat(cp[1], cp[0])) : null).ToList();

                var trick = new List<Card>();
                for (var i = 0; i < nPlayers; i++)
                {
                    var seat = (leadSeat + i) % nPlayers;
                    if (seat < cards.Count && cards[seat] != null)
                    {
                        trick.Add(cards[seat]);
                        plays.Add(cards[seat].ToString());
                    }
                }

                var topCard = GetTopCard(trick, trump, options);
                leadSeat = cards.IndexOf(topCard);
            }

            return plays;
        }

        private static bool IsUnknownHand(string hand)
        {
            return hand.Substring(0, 2) == UnknownCard;
        }

        private static List<PBNTag> TokenizeTags(string text)
        {
            var tag = new PBNTag { Data = new List<string>() };
            var tags = new List<PBNTag>();
            var lines = text.Split(new[] { "\n", "\n\r" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("%"));
            foreach (var line in lines)
                if (line.StartsWith("["))
                {
                    tag = new PBNTag { Data = new List<string>() };
                    tags.Add(tag);
                    tag.Name = line.Substring(1, line.IndexOf(' ') - 1);
                    var start = line.IndexOf('"') + 1;
                    var end = line.LastIndexOf('"') - start;
                    tag.Description = Regex.Unescape(line.Substring(start, end));
                }
                else
                {
                    tag.Data.Add(line);
                }

            return tags;
        }

        private class PBNTag
        {
            public List<string> Data { get; set; }
            public string Description { get; set; }
            public string Name { get; set; }
        }
    }
}