using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestSpadesBot
    {
        [TestMethod]
        public void CountSureTricks()
        {
            var players = new[]
            {
                new TestPlayer(4, "QSTCTSJS", 3, cardsTaken: "7DQD5SJDAS2S6S3S3H8H7H7C"),
                new TestPlayer(2, handScore: 2, cardsTaken: "4DAD3D2DAC2C5C3C"),
                new TestPlayer(4, handScore: 3, cardsTaken: "8DKD5D2HKC9C4C6C8CJC7SQC"),
                new TestPlayer(1, handScore: 1, cardsTaken: "6H4HTHAH")
            };

            var bot = GetBot();
            var cardState = new TestCardState<SpadesOptions>(bot, players, "KS4S");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("QS", suggestion.ToString(), $"Suggested {suggestion.StdNotation}; expected QS");
        }

        [TestMethod]
        public void DontTakeBagsIfWeCantSetOpponents()
        {
            var players = new[]
            {
                new TestPlayer(3, "3H6H", 3),
                new TestPlayer(2, handScore: 2),
                new TestPlayer(3, handScore: 3),
                new TestPlayer(2, handScore: 2)
            };

            var bot = GetBot();
            var cardState = new TestCardState<SpadesOptions>(bot, players, "2H4H5H");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("3H", suggestion.ToString(), "Don't take a bag when we can't set opponents");
        }

        [TestMethod]
        public void DontTakeBagsIfWellWinAnyway()
        {
            var players = new[]
            {
                new TestPlayer(3, "3H6H", 3, 490),
                new TestPlayer(3, handScore: 2),
                new TestPlayer(3, handScore: 3, gameScore: 490),
                new TestPlayer(3, handScore: 2)
            };

            var bot = GetBot();
            var cardState = new TestCardState<SpadesOptions>(bot, players, "2H4H5H");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("3H", suggestion.ToString(), "Don't take a bag if we'll win anyway");
        }

        [TestMethod]
        public void DontTakeBagsIfWereTooCloseToPenalty()
        {
            var players = new[]
            {
                new TestPlayer(3, "3H6H", 3, 9),
                new TestPlayer(3, handScore: 2),
                new TestPlayer(3, handScore: 3, gameScore: 9),
                new TestPlayer(3, handScore: 2)
            };

            var bot = GetBot();
            var cardState = new TestCardState<SpadesOptions>(bot, players, "2H4H5H");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("3H", suggestion.ToString(), "Don't take a bag if we're too close to penalty");
        }

        [TestMethod]
        public void IgnoreNilWithHighBid()
        {
            var players = new[]
            {
                new TestPlayer(5, "AH5H4H3H2H9S8S7S6S5S4S3S2S"),
                new TestPlayer(3),
                new TestPlayer(5),
                new TestPlayer(0)
            };

            var bot = GetBot();
            var cardState = new TestCardState<SpadesOptions>(bot, players, notLegalSuit: Suit.Spades);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("AH", suggestion.ToString(), $"Led {suggestion.StdNotation} when ignoring nil");
        }

        [TestMethod]
        public void LeadBossToProtectNil()
        {
            var players = new[]
            {
                new TestPlayer(4, "TH9HKC8C7C6C5C4C3C2CKDKS", 1, cardsTaken: "AHKHQHJH"),
                new TestPlayer(4),
                new TestPlayer(0),
                new TestPlayer(4)
            };

            var bot = GetBot();
            var cardState = new TestCardState<SpadesOptions>(bot, players, notLegalSuit: Suit.Spades);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("TH", suggestion.ToString(), $"Led {suggestion.StdNotation} to protect Nil");
        }

        [TestMethod]
        public void LeadHighestLegalCardToProtectNil()
        {
            var players = new[]
            {
                new TestPlayer(4, "9H4H2HKC7C6C5C4C3C2CQDQS", 1, cardsTaken: "AHKHQHJH"),
                new TestPlayer(4),
                new TestPlayer(0),
                new TestPlayer(4)
            };

            var bot = GetBot();
            var cardState = new TestCardState<SpadesOptions>(bot, players, notLegalSuit: Suit.Spades);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("KC", suggestion.ToString(), $"Led {suggestion.StdNotation} to protect Nil");
        }

        [TestMethod]
        public void LeadHighestSpadeToProtectNil()
        {
            var players = new[]
            {
                new TestPlayer(4, "TH9HKC8C7C6C5C4C3CKDKS2S", 1, cardsTaken: "AHKHQHJH"),
                new TestPlayer(4),
                new TestPlayer(0),
                new TestPlayer(4)
            };

            var bot = GetBot(new SpadesOptions { leadSpades = LeadSpadesWhen.Anytime });
            var cardState = new TestCardState<SpadesOptions>(bot, players);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("KS", suggestion.ToString(), $"Led {suggestion.StdNotation} to protect Nil");
        }

        [TestMethod]
        public void LeadJustAbovePartnersHighestCardInSuit()
        {
            var players = new[]
            {
                new TestPlayer(4, "6H4HKC7C6C5C4C3C2CKDKS", 2, cardsTaken: "AHKHQHJHTH8H3H7H"),
                new TestPlayer(4),
                new TestPlayer(0) { PlayedCards = new List<PlayedCard> { new PlayedCard(new Card("TH"), new Card("3H")) } },
                new TestPlayer(4)
            };

            var bot = GetBot();
            var cardState = new TestCardState<SpadesOptions>(bot, players, notLegalSuit: Suit.Spades);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("4H", suggestion.ToString(), $"Led {suggestion.StdNotation} only as high as needed");
        }

        [TestMethod]
        public void LeadLowInPartnersVoidSuitToProtectNil()
        {
            var players = new[]
            {
                new TestPlayer(4, "4H2HKC7C6C5C4C3C2CKDKS", 2, cardsTaken: "AHKHQHJHTH5HQC3H"),
                new TestPlayer(4),
                new TestPlayer(0) { VoidSuits = new List<Suit> { Suit.Hearts } },
                new TestPlayer(4)
            };

            var bot = GetBot();
            var cardState = new TestCardState<SpadesOptions>(bot, players, notLegalSuit: Suit.Spades);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("2H", suggestion.ToString(), $"Led {suggestion.StdNotation} to protect Nil");
        }

        [TestMethod]
        public void OpportunisticallySetNilWithHighBid()
        {
            var players = new[]
            {
                new TestPlayer(5, "AH3HTS9S8S7S6S5S4S3S2S2C2D"),
                new TestPlayer(3),
                new TestPlayer(5),
                new TestPlayer(0)
            };

            var bot = GetBot();
            var cardState = new TestCardState<SpadesOptions>(bot, players, "2H4H5H");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("3H", suggestion.ToString(), $"Played {suggestion.StdNotation} to get under nil bidder in 4th seat");
        }

        [TestMethod]
        public void PlayOverHighCardToProtectNilIfNeeded()
        {
            var players = new[]
            {
                new TestPlayer(5, "2D3D4D3C4CKCQH5S7S8SJSKS"),
                new TestPlayer(0),
                new TestPlayer(0),
                new TestPlayer(4)
            };

            var bot = GetBot();
            var cardState = new TestCardState<SpadesOptions>(bot, players, "JC");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("KC", suggestion.ToString(), $"Played {suggestion.StdNotation} to follow high to cover gaps");
        }

        [TestMethod]
        public void SaveHighCardToProtectNilIfNoGaps()
        {
            var players = new[]
            {
                new TestPlayer(5, "2D3D4D3C4CACQH5S7S8SJSKS"),
                new TestPlayer(2),
                new TestPlayer(0),
                new TestPlayer(4, handScore: 1, cardsTaken: "AH9HKH4H")
            };

            var bot = GetBot();
            var cardState = new TestCardState<SpadesOptions>(bot, players, "KC");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("3C", suggestion.ToString(), $"Led {suggestion.StdNotation} to follow low if no gaps");
        }

        [TestMethod]
        public void TakeBagsIfWellEndUpShort_BustedNilCase()
        {
            var players = new[]
            {
                new TestPlayer(4, "3H6H", 4, 490),
                new TestPlayer(4, handScore: 3),
                new TestPlayer(0, handScore: 1, gameScore: 490),
                new TestPlayer(4, handScore: 3)
            };

            var bot = GetBot();
            var cardState = new TestCardState<SpadesOptions>(bot, players, "2H4H5H");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("6H", suggestion.ToString(), "Take a bag to set opponents when we can't win this hand");
        }

        [TestMethod]
        public void TakeBagsToSetOpponents()
        {
            var players = new[]
            {
                new TestPlayer(3, "3H6H", 3),
                new TestPlayer(3, handScore: 2),
                new TestPlayer(3, handScore: 3),
                new TestPlayer(3, handScore: 2)
            };

            var bot = GetBot();
            var cardState = new TestCardState<SpadesOptions>(bot, players, "2H4H5H");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("6H", suggestion.ToString(), "Take a bag to set opponents");
        }

        [TestMethod]
        public void TestBids()
        {
            Assert.AreEqual(4, GetSuggestedBid("2D3D5D6D8DTD  QH 2S3S5S7S9SAS", out var hand), $"Expect bid of 4 for hand {Util.PrettyHand(hand)}");
            Assert.AreEqual(13, GetSuggestedBid("   2S3S4S5S6S7S8S9STSJSQSKSAS", out hand), $"Expect bid of 13 for hand {Util.PrettyHand(hand)}");
            Assert.AreEqual(13, GetSuggestedBid("  AH 3S4S5S6S7S8S9STSJSQSKSAS", out hand), $"Expect bid of 13 for hand {Util.PrettyHand(hand)}");
            Assert.AreEqual(13, GetSuggestedBid("AD AC AH 5S6S7S8S9STSJSQSKSAS", out hand), $"Expect bid of 13 for hand {Util.PrettyHand(hand)}");
            Assert.AreEqual(0, GetSuggestedBid("2C3C4C5C6C7C8C 2D3D4D5D6D7D  ", out hand), $"Expect bid of Nil for hand {Util.PrettyHand(hand)}");
            Assert.AreEqual(1, GetSuggestedBid("2C3C4C5C6C7C8C 2D3D4D5D6D7D  ", out hand, 0),
                $"Expect bid of 1 for hand {Util.PrettyHand(hand)} and partner bid Nil");
        }

        [TestMethod]
        public void TestPassNilBid()
        {
            //  assert we are passing high cards for nil bid
            Assert.AreEqual("ASKSQSAH", GetSuggestedPass(0), "Pass high cards for Nil bid");
        }

        [TestMethod]
        public void TestPassNonNilBid()
        {
            //  assert we are passing low cards for non-nil bid
            Assert.AreEqual("2C2H3D3C", GetSuggestedPass(3), "Pass low cards for non-Nil bid");
        }

        private static SpadesBot GetBot()
        {
            return GetBot(new SpadesOptions());
        }

        private static SpadesBot GetBot(SpadesOptions options)
        {
            return new SpadesBot(options, Suit.Spades);
        }

        private static int GetSuggestedBid(string handString, out Hand hand, int partnerBid = BidBase.NoBid)
        {
            var players = new[]
            {
                new TestPlayer(seat: 0, hand: handString.Replace(" ", string.Empty)),
                new TestPlayer(seat: 1),
                new TestPlayer(seat: 2, bid: partnerBid),
                new TestPlayer(seat: 3)
            };

            var legalBids = new List<BidBase>();
            for (var v = 0; v <= 13; ++v) legalBids.Add(new BidBase(v));

            hand = new Hand(players[0].Hand);
            var bidState = new SuggestBidState<SpadesOptions>
            {
                player = players[0],
                players = players,
                legalBids = legalBids,
                hand = hand
            };

            return GetBot().SuggestBid(bidState).value;
        }

        private static string GetSuggestedPass(int bid)
        {
            const string handString = "ASKSQSJSAD9D8D3DAH2HAC3C2C";

            var passState = new SuggestPassState<SpadesOptions>
            {
                player = new TestPlayer(bid, handString),
                hand = new Hand(handString),
                passCount = 4
            };

            var bot = GetBot(new SpadesOptions { nilPass = 4 });
            var cards = bot.SuggestPass(passState);
            return new Hand(cards).ToString();
        }
    }
}