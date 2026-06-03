using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestWhistBot
    {
        // Declare bid on the actual declarer (partner across the table from the lead)
        private static readonly int DeclarerSeatBid = (int)new WhistBid(Suit.Clubs, 3, false, true);

        // Bid for the leading player so Whist treats them as offense (partner bid), not defense
        private static readonly int DeclarersPartnerSeatBid = (int)WhistBid.DeclarerPartnerBid;

        [TestMethod]
        public void DiscardJokersInNT()
        {
            var players = new[]
            {
                new TestPlayer(1561, "6DKDJDJH8S7H5DLJTDQSHJKS5SAHQHJCJSQC"),
                new TestPlayer(1400),
                new TestPlayer(1401),
                new TestPlayer(1400)
            };

            var bot = GetBot(new WhistOptions
                { variation = WhistVariation.BidWhist, bidderGetsKitty = true, bidderLeads = true });

            var discardState = new SuggestDiscardState<WhistOptions>
            {
                player = players[0],
                hand = new Hand(players[0].Hand)
            };

            var suggestion = bot.SuggestDiscard(discardState);
            Assert.AreEqual(6, suggestion.Count, "Discarded 6 cards");
            Assert.AreEqual(2, suggestion.Count(c => c.suit == Suit.Joker), $"Suggestion {Util.PrettyCards(suggestion)} contains both jokers");
        }

        [TestMethod]
        public void DontLeadTrumpWhenDefending()
        {
            var players = new[]
            {
                new TestPlayer(1400, "HJACKDQDAH"),
                new TestPlayer(1564),
                new TestPlayer(1400),
                new TestPlayer(1401)
            };

            var bot = GetBot(Suit.Clubs);
            var cardState = new TestCardState<WhistOptions>(bot, players, trumpSuit: Suit.Clubs);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.IsTrue(suggestion.suit != Suit.Clubs, "Suggested lead is not trump");
        }

        [TestMethod]
        public void DontLeadJokersInNT()
        {
            var players = new[]
            {
                new TestPlayer(1400, "HJQD"),
                new TestPlayer(1561),
                new TestPlayer(1400),
                new TestPlayer(1401)
            };

            var bot = GetBot(Suit.Unknown);
            var cardState = new TestCardState<WhistOptions>(bot, players);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.IsTrue(suggestion.suit != Suit.Joker, "Suggested lead is not a Joker");
        }

        [TestMethod]
        public void DontLeadPartnerVoidSuitInNT_WhenAlternativeExists()
        {
            // Partner is known void in hearts (e.g. from earlier play); leader can lead hearts or diamonds — prefer diamonds (issue #146).
            var partner = new TestPlayer(1400, "");
            partner.VoidSuits.Add(Suit.Hearts);
            var players = new[]
            {
                new TestPlayer(1400, "2H3D"),
                new TestPlayer(1561),
                partner,
                new TestPlayer(1401)
            };

            var bot = GetBot(Suit.Unknown);
            var cardState = new TestCardState<WhistOptions>(bot, players);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual(Suit.Diamonds, suggestion.suit, "Should not lead a suit partner is known to be void in");
        }

        [TestMethod]
        public void MayLeadBossInPartnerVoidSuitInNT_WhenItWinsTheTrick()
        {
            // Partner is void in hearts; we still allow leading the top remaining heart to cash a trick.
            var partner = new TestPlayer(1400, "");
            partner.VoidSuits.Add(Suit.Hearts);
            var players = new[]
            {
                new TestPlayer(1400, "AH3D", cardsTaken: ""),
                new TestPlayer(1561, "", cardsTaken: "2H3H4H5H6H7H8H9HTHJHQHKH"),
                partner,
                new TestPlayer(1401)
            };

            var bot = GetBot(Suit.Unknown);
            var cardState = new TestCardState<WhistOptions>(bot, players);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("AH", suggestion.ToString(), "Boss heart should remain a legal lead even if partner is void in hearts");
        }

        [TestMethod]
        public void NT_OpeningLead_TryTakeEmCannotLeadNonBossFromPartnerVoidSuit()
        {
            // Regression for PR #377: legalCards is narrowed before TryTakeEm so non-boss hearts are removed
            // when partner is known void. TryTakeEm only draws from that list (including LowestCardFromWeakestSuit),
            // so it must never return 5H even though 5H is in the physical hand.
            var partner = new TestPlayer(1400, "");
            partner.VoidSuits.Add(Suit.Hearts);
            var players = new[]
            {
                new TestPlayer(1400, "AH5H9D2D", cardsTaken: ""),
                new TestPlayer(1561, "", cardsTaken: "2H3H4H6H7H8H"),
                partner,
                new TestPlayer(1401, "", cardsTaken: "9HTHJHQHKH")
            };

            var bot = GetBot(Suit.Unknown);
            var cardState = new TestCardState<WhistOptions>(bot, players, trumpSuit: Suit.Unknown);
            var suggestion = bot.SuggestNextCard(cardState);

            Assert.AreNotEqual(
                "5H",
                suggestion.ToString(),
                "Non-boss heart must be filtered out before TryTakeEm; otherwise dumping logic could lead it from a partner-void suit");
            if (suggestion.suit == Suit.Hearts)
                Assert.AreEqual(Rank.Ace, suggestion.rank, "Only the boss heart should remain playable in hearts for this layout");
        }

        [TestMethod]
        public void SloughJokerFirstWhenVoidInNT()
        {
            var players = new[]
            {
                new TestPlayer(1561, "HJTD3D4S5S6D"),
                new TestPlayer(1400),
                new TestPlayer(1401),
                new TestPlayer(1400)
            };

            var bot = GetBot(Suit.Unknown);
            var cardState = new TestCardState<WhistOptions>(bot, players, "2C", trumpSuit: Suit.Unknown);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual(Suit.Joker, suggestion.suit, $"Suggested {suggestion.StdNotation} should be a Joker when void in NT");
        }

        [TestMethod]
        public void SignalGoodSuitOnFirstSlough_Lead()
        {
            var players = new[]
            {
                new TestPlayer(1564, "HJ4D3DTH2S", cardsTaken: "2C3C4C5C6C7C8C9CTCJCQCLJ"),
                new TestPlayer(1400),
                new TestPlayer(1401),
                new TestPlayer(1400)
            };

            var bot = GetBot(Suit.Clubs);
            var cardState = new TestCardState<WhistOptions>(bot, players, trumpSuit: Suit.Clubs);
            Assert.IsTrue(new Hand(cardState.player.Hand).Any(c => bot.EffectiveSuit(c) == Suit.Clubs),
                $"Player's hand {Util.PrettyHand(cardState.player.Hand)} contains trump");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.IsTrue(bot.EffectiveSuit(suggestion) == Suit.Clubs, "Suggested lead is trump");
        }

        [TestMethod]
        public void SignalGoodSuitOnFirstSlough_LeadBack()
        {
            var players = new[]
            {
                new TestPlayer(DeclarersPartnerSeatBid, "4D3DTH2S", cardsTaken: "2C3C4C5C6C7C8C9CTCJCQCLJHJKCTDAC"),
                new TestPlayer(BidBase.NoBid),
                new TestPlayer(DeclarerSeatBid, "") { GoodSuit = Suit.Diamonds },
                new TestPlayer(BidBase.NoBid)
            };

            var bot = GetBot(Suit.Clubs);
            var cardState = new TestCardState<WhistOptions>(bot, players, trumpSuit: Suit.Clubs);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("4D", suggestion.ToString(), $"Suggested {suggestion.StdNotation} is highest card in suit sloughed by partner");
        }

        [TestMethod]
        public void LeadBackPartnerGoodSuit_NT()
        {
            var players = new[]
            {
                new TestPlayer(DeclarersPartnerSeatBid, "5D9DAS3H", seat: 0),
                new TestPlayer(BidBase.NoBid, seat: 1),
                new TestPlayer(DeclarerSeatBid, "", seat: 2) { GoodSuit = Suit.Diamonds },
                new TestPlayer(BidBase.NoBid, seat: 3)
            };

            var bot = GetBot(Suit.Unknown);
            var cardState = new TestCardState<WhistOptions>(bot, players, trumpSuit: Suit.Unknown);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("9D", suggestion.ToString(), "Come back in partner's suit with the highest card before an off-suit boss");
        }

        [TestMethod]
        public void LeadBackPartnerGoodSuit_NT_BidderLeadsLowest()
        {
            var declarerNtBid = (int)new WhistBid(Suit.Unknown, 1, true, false);
            var players = new[]
            {
                new TestPlayer(declarerNtBid, "5H8H2C3C", seat: 0),
                new TestPlayer(BidBase.NoBid, seat: 1),
                new TestPlayer(DeclarersPartnerSeatBid, "", seat: 2),
                new TestPlayer(BidBase.NoBid, seat: 3)
            };

            var bot = GetBot(Suit.Unknown);
            var cardState = new TestCardState<WhistOptions>(bot, players, trumpSuit: Suit.Unknown)
            {
                cardsPlayedInOrder = "23H32H0AS1KH"
            };
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("5H", suggestion.ToString(), "NT declarer with no bosses leads lowest back in suit partner signaled from the lead");
        }

        [TestMethod]
        public void LeadBackSuitPartnerTriedToPromote_NT()
        {
            var players = new[]
            {
                //  Our hand has a boss elsewhere (AS) and multiple hearts in the suit partner appears to be promoting.
                new TestPlayer(DeclarersPartnerSeatBid, "3H8HAS", seat: 0),
                new TestPlayer(BidBase.NoBid, seat: 1),
                //  Partner (seat 2) led 3H; KH later in the trick is higher in the lead suit (promoting hearts).
                new TestPlayer(DeclarerSeatBid, "", seat: 2),
                new TestPlayer(BidBase.NoBid, seat: 3)
            };

            var bot = GetBot(Suit.Unknown);
            var cardState = new TestCardState<WhistOptions>(bot, players, trumpSuit: Suit.Unknown)
            {
                cardsPlayedInOrder = "23H32H0AS1KH"
            };
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("8H", suggestion.ToString(), "Lead highest back in suit partner appeared to promote before a boss in another suit");
        }

        [TestMethod]
        public void LeadBackPartnerPromotedSuitAgainBeforeOtherBoss_NT()
        {
            var firstLeadPlayers = new[]
            {
                new TestPlayer(DeclarersPartnerSeatBid, "8HQHAS", seat: 0),
                new TestPlayer(BidBase.NoBid, seat: 1),
                new TestPlayer(DeclarerSeatBid, "", seat: 2),
                new TestPlayer(BidBase.NoBid, seat: 3)
            };

            var bot = GetBot(Suit.Unknown);
            var firstLeadState = new TestCardState<WhistOptions>(bot, firstLeadPlayers, trumpSuit: Suit.Unknown)
            {
                cardsPlayedInOrder = "23H32H0AS1KH"
            };
            var firstSuggestion = bot.SuggestNextCard(firstLeadState);
            Assert.AreEqual("QH", firstSuggestion.ToString(), "First lead should be the highest card in partner's promoted suit");

            var secondLeadPlayers = new[]
            {
                new TestPlayer(DeclarersPartnerSeatBid, "8HAS", seat: 0),
                new TestPlayer(BidBase.NoBid, seat: 1),
                new TestPlayer(DeclarerSeatBid, "", seat: 2),
                new TestPlayer(BidBase.NoBid, seat: 3)
            };

            var secondLeadState = new TestCardState<WhistOptions>(bot, secondLeadPlayers, trumpSuit: Suit.Unknown)
            {
                cardsPlayedInOrder = "23H32H0AS1KH"
            };
            var secondSuggestion = bot.SuggestNextCard(secondLeadState);
            Assert.AreEqual("8H", secondSuggestion.ToString(), "If still on lead, continue with highest remaining card in partner's suit before off-suit boss cards");
        }

        [TestMethod]
        public void LeadSignalFromLead_NT_LeadsLowestFromSelectedSuit()
        {
            var players = new[]
            {
                new TestPlayer(DeclarersPartnerSeatBid, "AH9H3HAS", seat: 0),
                new TestPlayer(BidBase.NoBid, seat: 1),
                new TestPlayer(DeclarerSeatBid, "", seat: 2),
                new TestPlayer(BidBase.NoBid, seat: 3)
            };

            var bot = GetBot(Suit.Unknown);
            var cardState = new TestCardState<WhistOptions>(bot, players, trumpSuit: Suit.Unknown);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("3H", suggestion.ToString(), "With no partner suit to lead back, select the good suit and lead lowest in it");
        }

        [TestMethod]
        public void LeadSignalFromLead_NT_BossAndCoverLeadsLowerCard()
        {
            var players = new[]
            {
                new TestPlayer(DeclarersPartnerSeatBid, "AH3HAS", seat: 0),
                new TestPlayer(BidBase.NoBid, seat: 1),
                new TestPlayer(DeclarerSeatBid, "", seat: 2),
                new TestPlayer(BidBase.NoBid, seat: 3)
            };

            var bot = GetBot(Suit.Unknown);
            var cardState = new TestCardState<WhistOptions>(bot, players, trumpSuit: Suit.Unknown);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("3H", suggestion.ToString(), "Doubleton with deck-top boss may signal by leading the lower card");
        }

        [TestMethod]
        public void LeadSignalFromLead_NT_KingDoubletonDoesNotUnstopSuit()
        {
            var players = new[]
            {
                new TestPlayer(DeclarersPartnerSeatBid, "KH3HQS6S5S", seat: 0),
                new TestPlayer(BidBase.NoBid, seat: 1),
                new TestPlayer(DeclarerSeatBid, "", seat: 2),
                new TestPlayer(BidBase.NoBid, seat: 3)
            };

            var bot = GetBot(Suit.Unknown);
            var cardState = new TestCardState<WhistOptions>(bot, players, trumpSuit: Suit.Unknown);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("5S", suggestion.ToString(), "Do not lead from king-doubleton because it strips the stopper");
        }

        [TestMethod]
        public void LeadSignalFromLead_NT_KingStopperTripletonLeadsLowestInGoodSuit()
        {
            var players = new[]
            {
                new TestPlayer(DeclarersPartnerSeatBid, "KHQH3HAS", seat: 0),
                new TestPlayer(BidBase.NoBid, seat: 1),
                new TestPlayer(DeclarerSeatBid, "", seat: 2),
                new TestPlayer(BidBase.NoBid, seat: 3)
            };

            var bot = GetBot(Suit.Unknown);
            var cardState = new TestCardState<WhistOptions>(bot, players, trumpSuit: Suit.Unknown);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("3H", suggestion.ToString(), "With top + stopper cover + one more, lead the lowest card in the selected good suit");
        }

        [TestMethod]
        public void LeadSignalFromLead_NT_LeadsLowestFromLongestSuitFallback()
        {
            // No partner-introduced suit, no qualifying "good suit" signal pattern, but we still choose a lead inside
            // TrySignalGoodSuitFromLead: lowest card in the longest suit (before TryTakeEm would cash a boss).
            var players = new[]
            {
                new TestPlayer(DeclarersPartnerSeatBid, "3D4D5D3H2HAS", seat: 0),
                new TestPlayer(BidBase.NoBid, seat: 1),
                new TestPlayer(DeclarerSeatBid, "", seat: 2),
                new TestPlayer(BidBase.NoBid, seat: 3)
            };

            var bot = GetBot(Suit.Unknown);
            var cardState = new TestCardState<WhistOptions>(bot, players, trumpSuit: Suit.Unknown);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("3D", suggestion.ToString(), "Longest suit is diamonds; lead lowest there, not spade boss");
        }

        [TestMethod]
        public void SkipLeadTowardPartnerWhenBossCardsCoverRemainingContract_NT()
        {
            var partnerHeartBid = new WhistBid(Suit.Hearts, 3, true, false);
            //  Declarer bid must match a 7-trick contract so that tricks already won (6) plus one boss covers the bid.
            var declarerNt7Tricks = (int)new WhistBid(Suit.Unknown, 1, true, false);
            //  Six tricks already won by declarer + partner; contract is 7. One boss (AS) is enough to cover the last trick,
            //  so we skip leading back toward partner's auction suit and cash a boss instead.
            //  24 cards (48 chars) = 6 tricks; must be an even-length card string for Hand parsing
            var sixTricksWon =
                "2C3C4C5C6C7C8C9CTCJCQCKCAC2D3D4D5D6D7D8D9DTDJD";
            var players = new[]
            {
                new TestPlayer(declarerNt7Tricks, "8HAS", seat: 0) { CardsTaken = sixTricksWon },
                new TestPlayer(1400, seat: 1),
                new TestPlayer(BidBase.NoBid, seat: 2) { BidHistory = new List<int> { partnerHeartBid } },
                new TestPlayer(1400, seat: 3)
            };

            var bot = GetBot(Suit.Unknown);
            var cardState = new TestCardState<WhistOptions>(bot, players, trumpSuit: Suit.Unknown);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("AS", suggestion.ToString(), "Cash boss when tricks already won plus bosses meet the contract");
        }

        [TestMethod]
        public void SignalGoodSuitOnFirstSlough_Slough()
        {
            var players = new[]
            {
                new TestPlayer(1401, "ADTD6H7S8S"),
                new TestPlayer(1400),
                new TestPlayer(1564, cardsTaken: "2C3C4C5C6C7C8C9CTCJCQCLJ"),
                new TestPlayer(1400)
            };

            var bot = GetBot(Suit.Clubs);
            var cardState = new TestCardState<WhistOptions>(bot, players, "HJKC", trumpSuit: Suit.Clubs);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("TD", suggestion.ToString(), $"Suggested {suggestion.StdNotation} is lowest card of best suit");
        }

        private static WhistBot GetBot(WhistOptions options)
        {
            return new WhistBot(options, Suit.Unknown);
        }

        private static WhistBot GetBot(Suit trumpSuit)
        {
            return GetBot(trumpSuit, new WhistOptions { variation = WhistVariation.BidWhist });
        }

        private static WhistBot GetBot(Suit trumpSuit, WhistOptions options)
        {
            return new WhistBot(options, trumpSuit);
        }
    }
}