﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace TestBots.Bridge
{
    internal class PBN
    {
        public static BasicTests.BasicTest[] ImportTests(string text)
        {
            var tests = new List<BasicTests.BasicTest>();
            var hands = new List<string>();
            var tags = TokenizeTags(text);
            var name = "";

            foreach (var tag in tags)
            {
                if (tag.Name == "Event")
                {
                    name = tag.Description;
                }
                else if (tag.Name == "Deal")
                {
                    hands = ImportHands(tag.Description);
                }
                else if (tag.Name == "Auction")
                {
                    var sides = "NESW";
                    var dealerSide = sides.IndexOf(tag.Description);
                    var bids = ImportBids(tag.Data);
                    var history = new List<string>();
                    for (var i = 0; i < bids.Count; i++)
                    {
                        var bid = bids[i];
                        var seat = i % 4;
                        var seatName = sides[(4 - dealerSide + seat) % 4];
                        var bidNumber = 1 + (i / 4);
                        tests.Add(
                            new BasicTests.BasicTest
                            {
                                history = history.ToArray(),
                                hand = hands[seat],
                                bid = bid,
                                type = $"{name} (Seat {seatName}, Bid {bidNumber})"
                            }
                        );
                        history.Add(bid);
                    }
                }
                // Ignore all other tags
            }

            return tests.ToArray();
        }

        private static List<PBNTag> TokenizeTags(string text)
        {
            var tag = new PBNTag { Data = new List<string>() };
            var tags = new List<PBNTag>();
            var lines = text.Split(new string[] { "\n", "\n\r" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("%"));
            foreach (var line in lines)
            {
                if (line.StartsWith("["))
                {
                    tag = new PBNTag { Data = new List<string>() };
                    tags.Add(tag);
                    tag.Name = line.Substring(1, line.IndexOf(' ') - 1);
                    var start = line.IndexOf('"') + 1;
                    var end = line.LastIndexOf('"') - start;
                    tag.Description = line.Substring(start, end);
                }
                else
                {
                    tag.Data.Add(line);
                }
            }
            return tags;
        }

        private static List<string> ImportHands(string handsString)
        {
            var suitLetters = "SHDC";
            var hands = new List<string>();
            var handStrings = handsString.Substring(2).Split(' ');
            foreach (var handString in handStrings)
            {
                var hand = "";
                var suits = handString.Split('.');
                for (var i = 0; i < suits.Length; i++)
                {
                    foreach (var card in suits[i])
                    {
                        hand += $"{card}{suitLetters[i]}";
                    }
                }
                hands.Add(hand);
            }
            return hands;
        }

        private static List<string> ImportBids(List<string> bidLines)
        {
            return string.Join(" ", bidLines)
                .Split(' ')
                .Select(bid => bid.Replace('S', '♠').Replace('H', '♥').Replace('D', '♦').Replace('C', '♣'))
                .ToList();
        }

        private class PBNTag
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public List<string> Data { get; set; }
        }
    }
}
