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

        public SaycTest(IReadOnlyList<string> test)
        {
            hand = ParseTestHand(test[0]);
            bidHistory = ParseTestBidHistory(test.Count > 2 ? test[2] : string.Empty);
            expectedBid = ParseTestBid(test[1], bidHistory);
            vulnerable = Enum.TryParse(test.Count > 3 ? test[3].Replace("-", "") : string.Empty, out Vulnerable v) ? v : Vulnerable.Unset;
        }

        public IReadOnlyList<int> bidHistory { get; set; }
        public int expectedBid { get; set; }
        public Hand hand { get; set; }
        public Vulnerable vulnerable { get; set; }

        private static int ParseTestBid(string bidString, IReadOnlyList<int> bidHistory)
        {
            if (bidString == "P")
                return BidBase.Pass;

            if (bidHistory.Any(b => b != BidBase.Pass))
            {
                var lastBid = new DeclareBid(bidHistory.Last(b => b != BidBase.Pass));

                switch (bidString)
                {
                    case "X":
                        return new DeclareBid(lastBid.level, lastBid.suit, DeclareBid.DoubleOrRe.Double);
                    case "XX":
                        return new DeclareBid(lastBid.level, lastBid.suit, DeclareBid.DoubleOrRe.Redouble);
                }
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
                    bids.Add(ParseTestBid(bidString, bids));
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