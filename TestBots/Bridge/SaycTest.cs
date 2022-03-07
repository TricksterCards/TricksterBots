using System;
using System.Collections.Generic;
using System.Linq;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    public class SaycTest
    {
        public enum Vulnerable
        {
            Unset,
            None,
            Both,
            EW,
            NS
        }

        private const string SuitLetters = "CDHS";

        private static readonly Dictionary<string, string> suitSymbolToLetter =
            SuitRank.stdSuits.ToDictionary(Card.SuitSymbol, s => s.ToString().Substring(0, 1));

        public SaycTest(IReadOnlyList<string> test)
        {
            hand = ParseTestHand(test[0]);
            bidHistory = ParseTestBidHistory(test.Count > 2 ? test[2] : string.Empty);
            expectedBid = ParseTestBid(test[1]);
            vulnerable = Enum.TryParse(test.Count > 3 ? test[3].Replace("-", "") : string.Empty, out Vulnerable v) ? v : Vulnerable.Unset;
        }

        public SaycTest(JsonTests.JsonTest test)
        {
            hand = new Hand(test.hand.Replace(" ", string.Empty));
            bidHistory = ParseJsonTestBidHistory(test.history);
            expectedBid = ParseJsonTestBid(test.bid);
            type = test.type;
        }

        public IReadOnlyList<int> bidHistory { get; set; }
        public int expectedBid { get; set; }
        public Hand hand { get; set; }
        public string type { get; set; } // used only when parsing from JsonTest
        public Vulnerable vulnerable { get; set; }

        private static int ParseJsonTestBid(string bidString)
        {
            if (bidString == "Pass")
                return BidBase.Pass;

            foreach (var suitAndSymbol in suitSymbolToLetter)
                bidString = bidString.Replace(suitAndSymbol.Key, suitAndSymbol.Value);

            return ParseTestBid(bidString.Replace("NT", "N"));
        }

        private static IReadOnlyList<int> ParseJsonTestBidHistory(string[] bidStrings)
        {
            var bids = new List<int>();

            if (bidStrings?.Length > 0)
                foreach (var bidString in bidStrings)
                    bids.Add(ParseJsonTestBid(bidString));

            return bids;
        }

        private static int ParseTestBid(string bidString)
        {
            switch (bidString)
            {
                case "P":
                    return BidBase.Pass;
                case "X":
                    return BridgeBid.Double; // new DeclareBid(lastBid.level, lastBid.suit, DeclareBid.DoubleOrRe.Double);
                case "XX":
                    return BridgeBid.Redouble; // new DeclareBid(lastBid.level, lastBid.suit, DeclareBid.DoubleOrRe.Redouble);
            }

            //  this is a bid of a hack but works because rank of 1 returns 1 (not Rank.Ace) and N will return Suit.Unknown
            var c = new Card(bidString);
            return new DeclareBid((int)c.rank, c.suit);
        }

        private static List<int> ParseTestBidHistory(string bidsString)
        {
            var bids = new List<int>();

            if (!string.IsNullOrWhiteSpace(bidsString))
            {
                var bidStrings = bidsString.Split(' ');

                foreach (var bidString in bidStrings)
                    bids.Add(ParseTestBid(bidString));
            }

            return bids;
        }

        private static Hand ParseTestHand(string handString)
        {
            var hand = new Hand();
            var parts = handString.Split('.');

            for (var i = 0; i < parts.Length; i++)
            {
                var suitLetter = SuitLetters[i];
                hand.AddRange(parts[i].Select(rankLetter => new Card($"{rankLetter}{suitLetter}")));
            }

            return hand;
        }
    }
}