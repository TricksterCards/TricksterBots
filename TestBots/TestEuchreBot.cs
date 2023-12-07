﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestEuchreBot
    {
        [TestMethod]
        public void AlwaysCallMisdeal()
        {
            const string handString = "TD9S9C9H9D";

            var upCard = new Card("JD");
            var bot = GetBot(Suit.Unknown, new EuchreOptions { allowMisdeal = EuchreMisdeal.NoAceNoFace });

            //  get the bid using the state-based suggest bid method
            var bidState = new SuggestBidState<EuchreOptions>
            {
                players = new[]
                {
                    new TestPlayer(hand: handString),
                    new TestPlayer(),
                    new TestPlayer(),
                    new TestPlayer()
                },
                dealerSeat = 3,
                hand = new Hand(handString),
                legalBids = new[]
                {
                    new BidBase((int)EuchreBid.CallMisdeal),
                    new BidBase((int)EuchreBid.NoMisdeal)
                },
                upCard = upCard,
                upCardSuit = upCard.suit
            };
            bidState.player = bidState.players[0];
            var suggestion = bot.SuggestBid(bidState);
            Assert.IsTrue(bidState.legalBids.Any(b => b.value == suggestion.value), "Bid is in legal bids");

            Assert.AreEqual((int)EuchreBid.CallMisdeal, suggestion.value, $"Suggested {suggestion.value}; expected ${(int)EuchreBid.CallMisdeal}");
        }

        [TestMethod]
        public void CallForBest()
        {
            var bot = GetBot(Suit.Diamonds, new EuchreOptions { callForBest = true });

            //  first suggestion is maker passing to non-playing partner
            var passState = new SuggestPassState<EuchreOptions>
            {
                player = new TestPlayer(112, "9D9SQSKSAS"),
                hand = new Hand("9D9SQSKSAS"),
                passCount = 1
            };
            var suggestion = bot.SuggestPass(passState);
            Assert.AreEqual(suggestion.Count, 1, "One card was passed");
            Assert.AreEqual("9S", suggestion[0].ToString(), $"Suggested {suggestion[0].StdNotation}; expected 9S");

            //  second suggestion is non-playing partner passing to maker
            passState.player = new TestPlayer(-3, "9HTHQHKHAHJD");
            passState.hand = new Hand("9HTHQHKHAHJD");
            suggestion = bot.SuggestPass(passState);
            Assert.AreEqual(suggestion.Count, 1, "One card was passed");
            Assert.AreEqual("JD", suggestion[0].ToString(), $"Suggested {suggestion[0].StdNotation}; expected JD");
        }

        [TestMethod]
        public void CallForBestWithoutTrump()
        {
            var bot = GetBot(Suit.Hearts, new EuchreOptions { callForBest = true });

            //  first suggestion is maker passing to non-playing partner
            var passState = new SuggestPassState<EuchreOptions>
            {
                player = new TestPlayer(114, "JHAHKHQHTH"),
                hand = new Hand("JHAHKHQHTH"),
                passCount = 1
            };
            var suggestion = bot.SuggestPass(passState);
            Assert.AreEqual(suggestion.Count, 1, "One card was passed");
            Assert.AreEqual("TH", suggestion[0].ToString(), $"Suggested {suggestion[0].StdNotation}; expected TH");

            //  second suggestion is non-playing partner passing to maker
            passState.player = new TestPlayer(-3, "9D9STSQSAS");
            passState.hand = new Hand("9D9STSQSAS");
            suggestion = bot.SuggestPass(passState);
            Assert.AreEqual(suggestion.Count, 1, "One card was passed");
            Assert.AreEqual("AS", suggestion[0].ToString(), $"Suggested {suggestion[0].StdNotation}; expected AS");
        }

        [TestMethod]
        public void DontTrumpWinningPartnerInLastSeat()
        {
            var players = new[]
            {
                new TestPlayer(102, "ACKCTC9CQD"),
                new TestPlayer(140),
                new TestPlayer(140),
                new TestPlayer(140)
            };

            var bot = GetBot(Suit.Diamonds);
            var cardState = new TestCardState<EuchreOptions>(bot, players, "9SQSTS");
            Assert.AreEqual(cardState.legalCards.Count, 5, "All cards are legal");
            Assert.IsTrue(cardState.isPartnerTakingTrick, "Partner is taking trick");

            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("9C", suggestion.ToString(), $"Suggested {suggestion.StdNotation}; expected 9C");
        }

        [TestMethod]
        public void DontTrumpWinningPartnerWhenOpponentsGoAlone()
        {
            var players = new[]
            {
                new TestPlayer(140, "ACAHKH9HQD"),
                new TestPlayer(112),
                new TestPlayer(140),
                new TestPlayer(-3)
            };

            var bot = GetBot(Suit.Diamonds);
            var cardState = new TestCardState<EuchreOptions>(bot, players, "AS");
            Assert.AreEqual(cardState.legalCards.Count, 5, "All cards are legal");
            Assert.IsTrue(cardState.isPartnerTakingTrick, "Partner is taking trick");

            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("9H", suggestion.ToString(), $"Suggested {suggestion.StdNotation}; expected 9H");
        }

        [TestMethod]
        public void PlayOnlyAsHighAsNeededInLastSeat()
        {
            var players = new[]
            {
                new TestPlayer(102, "ASKSACAHQD"),
                new TestPlayer(140),
                new TestPlayer(140),
                new TestPlayer(140)
            };

            var bot = GetBot(Suit.Diamonds);
            var cardState = new TestCardState<EuchreOptions>(bot, players, "9STSQS");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("KS", suggestion.ToString(), $"Suggested {suggestion.StdNotation}; expected KS");
        }

        [TestMethod]
        public void ProtectTheLeftOnDefense()
        {
            var players = new[]
            {
                new TestPlayer(140, "ACTC9CJHQD"),
                new TestPlayer(102),
                new TestPlayer(140),
                new TestPlayer(140),
            };

            var bot = GetBot(Suit.Diamonds);
            var cardState = new TestCardState<EuchreOptions>(bot, players, "9SQS");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("9C", $"{suggestion}");
        }

        [TestMethod]
        public void DontProtectTheLeftWhenLastToPlay()
        {
            var players = new[]
            {
                new TestPlayer(140, "ACTC9CJHQD"),
                new TestPlayer(102),
                new TestPlayer(140),
                new TestPlayer(140),
            };

            var bot = GetBot(Suit.Diamonds);
            var cardState = new TestCardState<EuchreOptions>(bot, players, "9STSQS");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("QD", $"{suggestion}");
        }

        [TestMethod]
        public void DontProtectTheLeftWhenNextPlayerIsVoidInTrump()
        {
            var players = new[]
            {
                new TestPlayer(140, "AC9CJHQD"),
                new TestPlayer(140) { VoidSuits = new List<Suit> { Suit.Diamonds } },
                new TestPlayer(140),
                new TestPlayer(102),
            };

            var bot = GetBot(Suit.Diamonds);
            var cardState = new TestCardState<EuchreOptions>(bot, players, "9SQS");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("QD", $"{suggestion}");
        }

        [TestMethod]
        public void DontProtectTheLeftWith3PlusTrump()
        {
            var players = new[]
            {
                new TestPlayer(140, "ACTCJHQD9D"),
                new TestPlayer(102),
                new TestPlayer(140),
                new TestPlayer(140),
            };

            var bot = GetBot(Suit.Diamonds);
            var cardState = new TestCardState<EuchreOptions>(bot, players, "9SQS");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("9D", $"{suggestion}");
        }
        
        [TestMethod]
        public void DontProtectTheLeftIfHigh()
        {
            var players = new[]
            {
                new TestPlayer(140, "ACTC9CJHQD"),
                new TestPlayer(140),
                new TestPlayer(140),
                new TestPlayer(102, cardsTaken: "JDQHTH9H"),
            };

            var bot = GetBot(Suit.Diamonds);
            var cardState = new TestCardState<EuchreOptions>(bot, players, "9S");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("QD", $"{suggestion}");
        }

        [TestMethod]
        public void DontProtectTheLeftIfHoldingTheRight()
        {
            var players = new[]
            {
                new TestPlayer(140, "ACTC9CJHJD"),
                new TestPlayer(140),
                new TestPlayer(140),
                new TestPlayer(102, cardsTaken: "QDQHTH9H"),
            };

            var bot = GetBot(Suit.Diamonds);
            var cardState = new TestCardState<EuchreOptions>(bot, players, "9S");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("JH", $"{suggestion}");
        }

        [TestMethod]
        public void DontProtectTheLeftIfTookOneTrick()
        {
            var players = new[]
            {
                new TestPlayer(140, handScore: 1, hand: "ACTC9CJHQD"),
                new TestPlayer(140),
                new TestPlayer(140, handScore: 1, cardsTaken: "AHQHTH9H"),
                new TestPlayer(102),
            };

            var bot = GetBot(Suit.Diamonds);
            var cardState = new TestCardState<EuchreOptions>(bot, players, "TS9S");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("QD", $"{suggestion}");
        }

        [TestMethod]
        public void ProtectTheLeftIfTookTwoTricks()
        {
            var players = new[]
            {
                new TestPlayer(140, handScore: 2, hand: "ACTC9CJHQD"),
                new TestPlayer(140),
                new TestPlayer(140, handScore: 2, cardsTaken: "AHQHTH9HKCQCJCQS"),
                new TestPlayer(102),
            };

            var bot = GetBot(Suit.Diamonds);
            var cardState = new TestCardState<EuchreOptions>(bot, players, "9STS");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("9C", $"{suggestion}");
        }

        [TestMethod]
        public void LeadLeftToPartnerIfTheyCalled()
        {
            var players = new[]
            {
                new TestPlayer(140, "ACTC9CJHQD"),
                new TestPlayer(140),
                new TestPlayer(102),
                new TestPlayer(140),
            };

            var bot = GetBot(Suit.Diamonds);
            var cardState = new TestCardState<EuchreOptions>(bot, players);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("JH", $"{suggestion}");
        }

        [TestMethod]
        public void LeadLastTrumpIfAlreadyMadeBidAndPartnerIsVoid()
        {
            var players = new[]
            {
                new TestPlayer(102, "9CQD", 3),
                new TestPlayer(140),
                new TestPlayer(140) { VoidSuits = new List<Suit> { Suit.Diamonds }},
                new TestPlayer(140),
            };

            var bot = GetBot(Suit.Diamonds);
            var cardState = new TestCardState<EuchreOptions>(bot, players);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("QD", $"{suggestion}");
        }

        [TestMethod]
        [DataRow("Pass", " 9C 9S 9HAHJD", "QH", DisplayName="Should pass if three-suited and weak, even with three trump")]
        [DataRow("Pass", "9C 9D 9H JCJS", "9S", DisplayName="Should pass if four-suited and weak, even with both Jacks")]
        [DataRow("Pass", "AC AD AH9H AS", "9S", DisplayName="Should pass if four-suited with only one trump, even if Ace")]
        [DataRow("Pass", "ADKD AS AH JS", "9C", DisplayName="Should pass if four-suited with only one trump, even if off-Jack")]
        [DataRow("Pass", "ACKC AD AS JH", "9H", DisplayName="Should pass if four-suited with only one trump, even if high Jack")]
        [DataRow("Pass", "  TSJS 9DQDKD", "TD", DisplayName="Should pass if two-suited with weak trump and off-suit")]
        [DataRow("Pass", "  AHKH 9STSQS", "KS", DisplayName="Should pass if two-suited with very weak trump, even with high off-suit")]
        [DataRow("Pass", " AD AH 9CTCQC", "KC", DisplayName="Should pass if three-suited with very weak trump, even with high off-suit")]
        [DataRow("Pass", " AC 9S 9DTDQD", "KD", DisplayName="Should pass if three-suited with very weak trump and mixed off-suit")]
        [DataRow("Pass", " AC KS 9HTHQH", "KH", DisplayName="Should pass if three-suited with very weak trump and mixed off-suit")]
        [DataRow("Pass", " 9D 9H 9STSQS", "KS", DisplayName="Should pass if three-suited with very weak trump and weak off-suit")]
        public void TestAvoidBid(string bid, string hand, string upCard)
        {
            Assert.AreEqual(bid, GetSuggestedBid(hand, upCard));
        }

        [TestMethod]
        //  if partner has high jack, they can help make 5 by beating off jack
        [DataRow("♥", "9HTHQHKHAH", "JH", DisplayName = "Do not bid alone without either jack")]
        //  higher chance partner can cover off-suit card(s) to help make 5 tricks
        [DataRow("♣", "ADKD ACJSJC", "9C", DisplayName = "Do not bid alone with only three trump")]
        [DataRow("♦", "KH KDADJHJD", "9D", DisplayName = "Do not bid alone with non-boss off-suit")]
        [DataRow("♥", "9D KHAHJDJH", "9H", DisplayName = "Do not bid alone with weak off-suit")]
        public void TestAvoidBidAlone(string bid, string hand, string upCard)
        {
            Assert.AreEqual(bid, GetSuggestedBid(hand, upCard));
        }

        [TestMethod]
        [DataRow(   "♠", "  9HTH KSASJC", "9S", DisplayName="Should bid when two-suited with three trump of reasonable strength")]
        [DataRow(   "♣", " AD AH QCKCAC", "9C", DisplayName="Should bid three-suited missing both Jacks with strong off-suit")]
        [DataRow("Pass", " AD AH TCQCKC", "9C", DisplayName="Should pass three-suited missing both Jacks with strong off-suit")]
        [DataRow("Pass", " 9C 9S ADJHJD", "9D", DisplayName="Should pass with only three sure tricks, regardless of other cards")]
        [DataRow("Pass", "  9DTD QHKHAH", "9H", DisplayName="Should pass missing both Jacks with remaining high trump and two-suited")]
        [DataRow("Pass", " ACKC AH JCJS", "9S", DisplayName="Should pass with only Jacks with strong off-suit support")]
        [DataRow("Pass", " ASKS 9C JHJD", "9D", DisplayName="Should pass with only Jacks with mostly strong off-suit support")]
        [DataRow("Pass", " 9D 9H KCACJS", "TC", DisplayName="Should pass with weak off-suit and no high Jack if only three strong trump")]
        [DataRow("Pass", " 9C 9S QDADJH", "TD", DisplayName="Should pass with weak off-suit and no high Jack if only three strong trump")]
        [DataRow("Pass", "AC AD 9S JDJH", "TH", DisplayName="Should pass four-suited having both Jacks and two off-suit Aces")]
        [DataRow(   "♥", "AC AH 9S JDJH", "TH", DisplayName="Should take four-suited having both Jacks, an Ace and one off-suit Ace")]
        [DataRow("Pass", "  9DKD 9SKSJC", "TS", DisplayName="Should pass with three trump if two-suited")]
        [DataRow("Pass", "  9HQH 9CQCJC", "AC", DisplayName="Should pass with three trump if two-suited")]
        [DataRow("Pass", "  ASKS TDQDKD", "AD", DisplayName="Should pass with three weak trump if two-suited with high off-suit")]
        [DataRow("Pass", "  TC 9HTHQHJH", "AH", DisplayName="Should pass with four trump, regardless of off-suit")]
        [DataRow("Pass", "  9D 9STSQSKS", "AS", DisplayName="Should pass with four trump, regardless of off-suit")]
        public void TestTake4Bid(string bid, string hand, string upCard)
        {
            Assert.AreEqual(bid, GetSuggestedBid(hand, upCard, true));
        }

        [TestMethod]
        [DataRow("♠", "  9HTH KSASJC", "9S", DisplayName="Should bid when two-suited with three trump of reasonable strength")]
        [DataRow("♣", " AD AH QCKCAC", "9C", DisplayName="Should bid three-suited missing both Jacks with strong off-suit")]
        [DataRow("♣", " AD AH TCQCKC", "9C", DisplayName="Should bid three-suited missing both Jacks with strong off-suit")]
        [DataRow("♦", " 9C 9S ADJHJD", "9D", DisplayName="Should bid with three sure tricks, regardless of other cards")]
        [DataRow("♥", "  9DTD QHKHAH", "9H", DisplayName="Should bid missing both Jacks with remaining high trump and two-suited")]
        [DataRow("♠", " ACKC AH JCJS", "9S", DisplayName="Should bid with only Jacks with strong off-suit support")]
        [DataRow("♦", " ASKS 9C JHJD", "9D", DisplayName="Should bid with only Jacks with mostly strong off-suit support")]
        [DataRow("♣", " 9D 9H KCACJS", "TC", DisplayName="Should bid with weak off-suit and no high Jack if three strong trump")]
        [DataRow("♦", " 9C 9S QDADJH", "TD", DisplayName="Should bid with weak off-suit and no high Jack if three strong trump")]
        [DataRow("♥", "AC 9D 9S JDJH", "TH", DisplayName="Should bid four-suited having both Jacks and an off-suit Ace")]
        [DataRow("♠", "  9DKD 9SKSJC", "TS", DisplayName="Should bid with three trump if two-suited")]
        [DataRow("♣", "  9HQH 9CQCJC", "AC", DisplayName="Should bid with three trump if two-suited")]
        [DataRow("♦", "  ASKS TDQDKD", "AD", DisplayName="Should bid with three weak trump if two-suited with high off-suit")]
        [DataRow("♥", "  TC 9HTHQHJH", "AH", DisplayName="Should bid with four trump, regardless of off-suit")]
        [DataRow("♠", "  9D 9STSQSKS", "AS", DisplayName="Should bid with four trump, regardless of off-suit")]
        public void TestTakeBid(string bid, string hand, string upCard)
        {
            Assert.AreEqual(bid, GetSuggestedBid(hand, upCard));
        }

        [TestMethod]
        //  risk of both same suit as Ace being led on first trick AND getting trumped is small
        [DataRow("♥ alone", "AS KHAHJDJH", "9H", DisplayName = "Should bid alone with sure trump and off-suit Ace")]
        //  if partner doesn't have the off-Jack, you won't take all five tricks even together, may as well go for it
        [DataRow("♠ alone", "AC QSKSASJS", "9S", DisplayName = "Should bid alone with strength even if missing off-Jack")]
        public void TestTakeBidAlone(string bid, string hand, string upCard)
        {
            Assert.AreEqual(bid, GetSuggestedBid(hand, upCard));
        }

        [TestMethod]
        public void TrumpAceWhenGoingAlone()
        {
            var players = new[]
            {
                new TestPlayer(112, "ACKCAHKHQD"),
                new TestPlayer(140),
                new TestPlayer(-3),
                new TestPlayer(140)
            };

            var bot = GetBot(Suit.Diamonds);
            var cardState = new TestCardState<EuchreOptions>(bot, players, "AS9S");
            Assert.AreEqual(GetBidText(cardState.player.Bid), "♦ alone", "Diamonds alone bid is correct");
            Assert.AreEqual(cardState.legalCards.Count, 5, "All cards are legal");
            Assert.IsFalse(cardState.isPartnerTakingTrick, "Partner is not taking trick");

            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("QD", suggestion.ToString(), $"Suggested {suggestion.StdNotation}; expected QD");
        }

        private static string GetBidText(BidBase bid)
        {
            return GetBidText(bid.value);
        }

        private static string GetBidText(int bidValue)
        {
            if (bidValue == BidBase.Pass)
                return "Pass";

            if (bidValue > (int)EuchreBid.MakeAlone)
                return Card.SuitSymbol(bidValue - (int)EuchreBid.MakeAlone) + " alone";

            return Card.SuitSymbol(bidValue - (int)EuchreBid.Make);
        }

        private static EuchreBot GetBot(Suit trumpSuit)
        {
            return GetBot(trumpSuit, new EuchreOptions());
        }

        private static EuchreBot GetBot(Suit trumpSuit, EuchreOptions options)
        {
            return new EuchreBot(options, trumpSuit);
        }

        /// <summary>
        ///     Generate a suggested bid assuming default rules and first to bid to left of dealer.
        /// </summary>
        /// <param name="handString">First bidder's hand</param>
        /// <param name="upCardString">The card turned up by the dealer</param>
        /// <param name="take4for1">The value to use for EuchreOptions.take4for1</param>
        /// <returns>The suggested bid for the first bidder</returns>
        private static string GetSuggestedBid(string handString, string upCardString, bool take4for1 = false)
        {
            handString = handString.Replace(" ", "");

            var upCard = new Card(upCardString);
            var bot = GetBot(Suit.Unknown, new EuchreOptions { take4for1 = take4for1 });

            //  get the bid using the state-based suggest bid method
            var bidState = new SuggestBidState<EuchreOptions>
            {
                players = new[]
                {
                    new TestPlayer(hand: handString),
                    new TestPlayer(),
                    new TestPlayer(),
                    new TestPlayer()
                },
                dealerSeat = 3,
                hand = new Hand(handString),
                legalBids = new[]
                {
                    new BidBase((int)EuchreBid.Make + (int)upCard.suit),
                    new BidBase((int)EuchreBid.MakeAlone + (int)upCard.suit),
                    new BidBase(BidBase.Pass)
                },
                upCard = upCard,
                upCardSuit = upCard.suit
            };
            bidState.player = bidState.players[0];
            var suggestion = bot.SuggestBid(bidState);
            Assert.IsTrue(bidState.legalBids.Any(b => b.value == suggestion.value), "Bid is in legal bids");

            return GetBidText(suggestion);
        }
    }
}