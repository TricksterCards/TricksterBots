using System;
using System.Collections.Generic;
using System.Linq;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    public class BidTest
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

        //  use to construct a test from the Test_Sayc test suite
        public BidTest(IReadOnlyList<string> test)
        {
            hand = ParseSaycTestHand(test[0]);
            bidHistory = ParseSaycTestBidHistory(test.Count > 2 ? test[2] : string.Empty);
            expectedBid = ParseSaycTestBid(test[1]);
            vulnerable = Enum.TryParse(test.Count > 3 ? test[3].Replace("-", "") : string.Empty, out Vulnerable v) ? v : Vulnerable.Unset;
        }

        //  use to contruct a test from JsonTests
        public BidTest(BasicTest test)
        {
            hand = new Hand(test.hand.Replace(" ", string.Empty));
            bidHistory = ParseBasicTestBidHistory(test.history);
            expectedBid = ParseBasicTestBid(test.bid);
            type = test.type;
        }

        public IReadOnlyList<int> bidHistory { get; set; }
        public int expectedBid { get; set; }
        public Hand hand { get; set; }
        public string type { get; set; } // used only when parsing from JsonTest
        public Vulnerable vulnerable { get; set; } // used only when parsing a Test_Sayc test

        private static int ParseBasicTestBid(string bidString)
        {
            if (bidString == "Pass")
                return BidBase.Pass;

            if (bidString.EndsWith("NT"))
                bidString = bidString.Replace("NT", "N");
            else
                foreach (var suitAndSymbol in suitSymbolToLetter.Where(sns => bidString.EndsWith(sns.Key)))
                    bidString = bidString.Replace(suitAndSymbol.Key, suitAndSymbol.Value);

            return ParseSaycTestBid(bidString);
        }

        private static IReadOnlyList<int> ParseBasicTestBidHistory(string[] bidStrings)
        {
            var bids = new List<int>();

            if (bidStrings?.Length > 0)
                bids.AddRange(bidStrings.Select(ParseBasicTestBid));

            return bids;
        }

        private static int ParseSaycTestBid(string bidString)
        {
            switch (bidString)
            {
                case "P":
                    return BidBase.Pass;
                case "X":
                    return BridgeBid.Double;
                case "XX":
                    return BridgeBid.Redouble;
            }

            //  this is a bit of a hack but works because rank of 1 returns 1 (not Rank.Ace) and N will return Suit.Unknown
            var c = new Card(bidString);
            return new DeclareBid((int)c.rank, c.suit);
        }

        private static List<int> ParseSaycTestBidHistory(string bidsString)
        {
            var bids = new List<int>();

            if (!string.IsNullOrWhiteSpace(bidsString))
            {
                var bidStrings = bidsString.Split(' ');
                bids.AddRange(bidStrings.Select(ParseSaycTestBid));
            }

            return bids;
        }

        private static Hand ParseSaycTestHand(string handString)
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