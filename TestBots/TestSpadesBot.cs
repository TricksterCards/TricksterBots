using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestSpadesBot
    {
        private const string passingPlayerHand = "ASKSQSJSAD9D8D3DAH2HAC3C2C";

        [TestMethod]
        public void TakeBagsToSetOpponents()
        {
            var players = new[]
            {
                new PlayerBase { Seat = 0, Bid = 3, Hand = "3H6H", HandScore = 3, PlayedCards = new List<PlayedCard>() },
                new PlayerBase { Seat = 1, Bid = 3, Hand = "2D", HandScore = 2, PlayedCards = new List<PlayedCard>() },
                new PlayerBase { Seat = 2, Bid = 3, Hand = "2C", HandScore = 3, PlayedCards = new List<PlayedCard>() },
                new PlayerBase { Seat = 3, Bid = 3, Hand = "3C", HandScore = 2, PlayedCards = new List<PlayedCard>() }
            };

            var cardState = new SuggestCardState<SpadesOptions>
            {
                options = new SpadesOptions(),
                player = players[0],
                trumpSuit = Suit.Spades,

                cardsPlayed = new List<Card>(),
                cardTakingTrick = new Card("5H"),
                isPartnerTakingTrick = false,
                legalCards = new Hand(players[0].Hand),
                players = players,
                trick = new Hand("2H4H5H"),
                trickTaker = players[3]
            };

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
            var player = new PlayerBase { Seat = 0, Hand = handString.Replace(" ", string.Empty), Bid = BidBase.NoBid };
            var player1 = new PlayerBase { Seat = 1, Hand = string.Empty, Bid = BidBase.NoBid };
            var player2 = new PlayerBase { Seat = 2, Hand = string.Empty, Bid = partnerBid };
            var player3 = new PlayerBase { Seat = 3, Hand = string.Empty, Bid = BidBase.NoBid };

            var legalBids = new List<BidBase>();
            for (var v = 0; v <= 13; ++v) legalBids.Add(new BidBase(v));
            legalBids.Add(new BidBase(BidBase.Pass));

            hand = new Hand(player.Hand);
            var bidState = new SuggestBidState<SpadesOptions>
            {
                options = new SpadesOptions(),
                player = player,
                trumpSuit = Suit.Spades,

                players = new List<PlayerBase> { player, player1, player2, player3 },
                legalBids = legalBids,
                hand = hand
            };

            var bot = new SpadesBot(bidState.options, bidState.trumpSuit);
            return bot.SuggestBid(bidState).value;
        }

        private static string GetSuggestedPass(int bid)
        {
            var passState = new SuggestPassState<SpadesOptions>
            {
                options = new SpadesOptions { nilPass = 4 },
                player = new PlayerBase { Hand = passingPlayerHand, Bid = bid },
                trumpSuit = Suit.Spades,
                hand = new Hand(passingPlayerHand),
                passCount = 4
            };

            var bot = new SpadesBot(passState.options, passState.trumpSuit);
            var cards = bot.SuggestPass(passState);
            return new Hand(cards).ToString();
        }
    }
}