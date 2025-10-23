using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestOhHellBot
    {
        [TestMethod]
        public void TestBids()
        {
            var options = new OhHellOptions()
            {
                variation = OhHellVariation.SevenToOne
            };

            // No hook
            options.hookRule = false;
            Assert.AreEqual(OhHellBid.FromTricks(1), GetSuggestedBid("AC 2H3H4H5H6H7H", "2C", out var hand, options), $"Expect bid of 1 for hand {Util.PrettyHand(hand)}");
            Assert.AreEqual(OhHellBid.FromTricks(1), GetSuggestedBid("KC 2H3H4H5H6H7H", "AC", out hand, options), $"Expect bid of 1 for hand {Util.PrettyHand(hand)}");

            // Hook
            options.hookRule = true;
            Assert.AreEqual(OhHellBid.FromTricks(2), GetSuggestedBid("AC 2H3H4H5H6H7H", "2C", out hand, options), $"Expect bid of 2 for hand {Util.PrettyHand(hand)}");
            Assert.AreEqual(OhHellBid.FromTricks(2), GetSuggestedBid("KC 2H3H4H5H6H7H", "AC", out hand, options), $"Expect bid of 2 for hand {Util.PrettyHand(hand)}");
        }

        [TestMethod]
        [DataRow("KD", "", "KDQD3D", 3, 0, DisplayName = "Lead high trump if need all tricks")]
        [DataRow("KD", "", "KDQD3D", 4, 1, DisplayName = "Lead high trump if need all tricks")]
        [DataRow("3D", "", "KDQD3D", 2, 0, DisplayName = "Lead low trump if less than all tricks")]
        [DataRow("4D", "", "ACKC4D", 3, 0, DisplayName = "Lead highest trump if need all tricks")]
        [DataRow("KC", "", "KCJCQS", 3, 0, DisplayName = "Lead highest off-suit if need all tricks")]
        public void LeadHighIfNeedAll(string expectedCard, string trick, string hand, int bid, int handScore)
        {
            var options = new OhHellOptions()
            {
                players = 3,
                variation = OhHellVariation.OneToSeven,
            };
            var players = new[]
            {
                new TestPlayer(OhHellBid.FromTricks(bid), hand, handScore),
                new TestPlayer(OhHellBid.FromTricks(0)),
                new TestPlayer(OhHellBid.FromTricks(3)),
            };
            var bot = new OhHellBot(options, Suit.Diamonds);
            var cardState = new TestCardState<OhHellOptions>(
                bot,
                players,
                trick
            );
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual(expectedCard, $"{suggestion}");
        }

        private static int GetSuggestedBid(string handString, string upCardString, out Hand hand, OhHellOptions options)
        {
            var players = new []
            {
                new TestPlayer(seat: 0, hand: handString.Replace(" ", string.Empty)),
                new TestPlayer(seat: 1, bid: OhHellBid.FromTricks(1)),
                new TestPlayer(seat: 2, bid: OhHellBid.FromTricks(2)),
                new TestPlayer(seat: 3, bid: OhHellBid.FromTricks(3))
            };

            hand = new Hand(players[0].Hand);
            var upCard = new Hand(upCardString)[0];

            var legalBids = new List<BidBase>();
            for (var v = 0; v <= hand.Count; ++v)
            {
                if (options.hookRule && v == 1)
                {
                    legalBids.Add(new BidBase(BidBase.NoBid, v.ToString()));
                }
                else
                {
                    legalBids.Add(OhHellBid.FromTricks(v));
                }
            }

            var bidState = new SuggestBidState<OhHellOptions>
            {
                dealerSeat = 0, // last to bid
                player = players[0],
                players = players,
                legalBids = legalBids,
                hand = hand,
                upCard = upCard,
                upCardSuit = upCard.suit
            };

            return new OhHellBot(options, upCard.suit).SuggestBid(bidState).value;
        }
    }
}