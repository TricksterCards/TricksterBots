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
        public void DontTakeBagsIfWeCantSetOpponents()
        {
            var players = new[]
            {
                new TestPlayer(0, 3, "3H6H") { HandScore = 3 },
                new TestPlayer(1, 2, "2D") { HandScore = 2 },
                new TestPlayer(2, 3, "2C") { HandScore = 3 },
                new TestPlayer(3, 2, "3C") { HandScore = 2 }
            };

            var cardState = new TestCardState<SpadesOptions>(players, "2H4H5H");
            var bot = new SpadesBot(cardState.options, cardState.trumpSuit);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.IsTrue(new Card("3H").SameAs(suggestion), "Don't take a bag when we can't set opponents");
        }

        [TestMethod]
        public void DontTakeBagsIfWellWinAnyway()
        {
            var players = new[]
            {
                new TestPlayer(0, 3, "3H6H") { HandScore = 3, GameScore = 490 },
                new TestPlayer(1, 3, "2D") { HandScore = 2 },
                new TestPlayer(2, 3, "2C") { HandScore = 3, GameScore = 490 },
                new TestPlayer(3, 3, "3C") { HandScore = 2 }
            };

            var cardState = new TestCardState<SpadesOptions>(players, "2H4H5H");
            var bot = new SpadesBot(cardState.options, cardState.trumpSuit);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.IsTrue(new Card("3H").SameAs(suggestion), "Don't take a bag if we'll win anyway");
        }

        [TestMethod]
        public void DontTakeBagsIfWereTooCloseToPenalty()
        {
            var players = new[]
            {
                new TestPlayer(0, 3, "3H6H") { HandScore = 3, GameScore = 9 },
                new TestPlayer(1, 3, "2D") { HandScore = 2 },
                new TestPlayer(2, 3, "2C") { HandScore = 3, GameScore = 9 },
                new TestPlayer(3, 3, "3C") { HandScore = 2 }
            };

            var cardState = new TestCardState<SpadesOptions>(players, "2H4H5H");
            var bot = new SpadesBot(cardState.options, cardState.trumpSuit);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.IsTrue(new Card("3H").SameAs(suggestion), "Don't take a bag if we're too close to penalty");
        }

        [TestMethod]
        public void IgnoreNilWithHighBid()
        {
            var players = new[]
            {
                new TestPlayer(0, 5, "AH5H4H3H2H9S8S7S6S5S4S3S2S"),
                new TestPlayer(1, 3, "ACKCQCJCTC9C8C7C6C5C4C3C2C"),
                new TestPlayer(2, 5, "ASKSQSJSTSKHQHJHTH9H8H7H6H"),
                new TestPlayer(3, 0, "ADKDQDJDTD9D8D7D6D5D4D3D2D")
            };

            var cardState = new TestCardState<SpadesOptions>(players);
            var bot = new SpadesBot(cardState.options, cardState.trumpSuit);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.IsTrue(new Card("AH").SameAs(suggestion), $"Led {suggestion.StdNotation} when ignoring nil");
        }

        [TestMethod]
        public void LeadBossToProtectNil()
        {
            var players = new[]
            {
                new TestPlayer(0, 4, "TH9HKC8C7C6C5C4C3C2CKDKS") { HandScore = 1, CardsTaken = "AHKHQHJH" },
                new TestPlayer(1, 4, "ACQCJCTC9C8H7H6H5H4H3H2H"),
                new TestPlayer(2, 0, "ADQDJDTD9D8D7D6D5D4D3D2D"),
                new TestPlayer(3, 4, "ASQSJSTS9S8S7S6S5S4S3S2S")
            };

            var cardState = new TestCardState<SpadesOptions>(players);
            var bot = new SpadesBot(cardState.options, cardState.trumpSuit);
            var suggestion = bot.SuggestNextCard(cardState);

            //  this is faulty. it relies on the partner (player seat 2) to have Spades in its void suits collection (set by the TestPlayer constructor)
            //  this would not be the case in a real game as we've never played Spades yet
            //  the bot suggestion without that is KS to protect Nil by leading trump
            //  not sure why it passed in the unit test
            Assert.IsTrue(new Card("TH").SameAs(suggestion), $"Led {suggestion.StdNotation} to protect Nil");
        }

        [TestMethod]
        public void LeadHighestSpadeToProtectNil()
        {
            var players = new[]
            {
                new TestPlayer(0, 4, "TH9HKC8C7C6C5C4C3CKDKS2S") { HandScore = 1, CardsTaken = "AHKHQHJH" },
                new TestPlayer(1, 4, "ACQCJCTC9C8H7H6H5H4H3H2H"),
                new TestPlayer(2, 0, "ADQDJDTD9D8D7D6S5D4D3D2D"),
                new TestPlayer(3, 4, "ASQSJSTS9S8S7S6D5S4S3S2C")
            };

            var cardState = new TestCardState<SpadesOptions>(players) { options = new SpadesOptions { leadSpades = LeadSpadesWhen.Anytime } };
            var bot = new SpadesBot(cardState.options, cardState.trumpSuit);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.IsTrue(new Card("KS").SameAs(suggestion), $"Led {suggestion.StdNotation} to protect Nil");
        }

        [TestMethod]
        public void LeadLowInPartnersVoidSuitToProtectNil()
        {
            var players = new[]
            {
                new TestPlayer(0, 4, "4H2HKC7C6C5C4C3C2CKDKS") { HandScore = 2, CardsTaken = "AHKHQHJHTH5HQC3H" },
                new TestPlayer(1, 4, "ADACJCTC9C8C9H8H7H6H2S"),
                new TestPlayer(2, 0, "QDJDTD9D8D7D6D5D4D3D2D"),
                new TestPlayer(3, 4, "ASQSJSTS9S8S7S6S5S4S3S")
            };

            var cardState = new TestCardState<SpadesOptions>(players);
            var bot = new SpadesBot(cardState.options, cardState.trumpSuit);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.IsTrue(new Card("2H").SameAs(suggestion), $"Led {suggestion.StdNotation} to protect Nil");
        }

        [TestMethod]
        public void OpportunisticallySetNilWithHighBid()
        {
            var players = new[]
            {
                new TestPlayer(0, 5, "AH3HTS9S8S7S6S5S4S3S2S2C2D"),
                new TestPlayer(1, 3, "ACKCQCJCTC9C8C7C6C5C4C3C"),
                new TestPlayer(2, 5, "ASKSQSJSKHQHJHTH9H8H7H6H"),
                new TestPlayer(3, 0, "ADKDQDJDTD9D8D7D6D5D4D3D")
            };

            var cardState = new TestCardState<SpadesOptions>(players, "2H4H5H");
            var bot = new SpadesBot(cardState.options, cardState.trumpSuit);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.IsTrue(new Card("3H").SameAs(suggestion), "Play under nil bidder in 4th seat");
        }

        [TestMethod]
        public void TakeBagsIfWellEndUpShort_BustedNilCase()
        {
            var players = new[]
            {
                new TestPlayer(0, 4, "3H6H") { HandScore = 4, GameScore = 490 },
                new TestPlayer(1, 4, "2D") { HandScore = 3 },
                new TestPlayer(2, 0, "2C") { HandScore = 1, GameScore = 490 },
                new TestPlayer(3, 4, "3C") { HandScore = 3 }
            };

            var cardState = new TestCardState<SpadesOptions>(players, "2H4H5H");
            var bot = new SpadesBot(cardState.options, cardState.trumpSuit);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.IsTrue(new Card("6H").SameAs(suggestion), "Take a bag to set opponents when we can't win this hand");
        }

        [TestMethod]
        public void TakeBagsToSetOpponents()
        {
            var players = new[]
            {
                new TestPlayer(0, 3, "3H6H") { HandScore = 3 },
                new TestPlayer(1, 3, "2D") { HandScore = 2 },
                new TestPlayer(2, 3, "2C") { HandScore = 3 },
                new TestPlayer(3, 3, "3C") { HandScore = 2 }
            };

            var cardState = new TestCardState<SpadesOptions>(players, "2H4H5H");
            var bot = new SpadesBot(cardState.options, cardState.trumpSuit);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.IsTrue(new Card("6H").SameAs(suggestion), "Take a bag to set opponents");
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

        private static int GetSuggestedBid(string handString, out Hand hand, int partnerBid = BidBase.NoBid)
        {
            var players = new[]
            {
                new TestPlayer(0, BidBase.NoBid, handString.Replace(" ", string.Empty)),
                new TestPlayer(1),
                new TestPlayer(2, partnerBid),
                new TestPlayer(3)
            };

            var legalBids = new List<BidBase>();
            for (var v = 0; v <= 13; ++v) legalBids.Add(new BidBase(v));
            legalBids.Add(new BidBase(BidBase.Pass));

            hand = new Hand(players[0].Hand);
            var bidState = new SuggestBidState<SpadesOptions>
            {
                options = new SpadesOptions(),
                player = players[0],
                trumpSuit = Suit.Spades,

                players = players,
                legalBids = legalBids,
                hand = hand
            };

            var bot = new SpadesBot(bidState.options, bidState.trumpSuit);
            return bot.SuggestBid(bidState).value;
        }

        private static string GetSuggestedPass(int bid)
        {
            const string handString = "ASKSQSJSAD9D8D3DAH2HAC3C2C";

            var passState = new SuggestPassState<SpadesOptions>
            {
                options = new SpadesOptions { nilPass = 4 },
                player = new TestPlayer(0, bid, handString),
                trumpSuit = Suit.Spades,
                hand = new Hand(handString),
                passCount = 4
            };

            var bot = new SpadesBot(passState.options, passState.trumpSuit);
            var cards = bot.SuggestPass(passState);
            return new Hand(cards).ToString();
        }
    }
}