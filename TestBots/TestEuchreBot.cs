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
        public void DontTrumpWinningPartnerWhenOpppontsGoAlone()
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
        public void TestAvoidBid()
        {
            Assert.AreEqual("Pass", GetSuggestedBid(" 9C 9S 9HAHJD", "QH"), "Should pass if three-suited and weak, even with three trump");
            Assert.AreEqual("Pass", GetSuggestedBid("9C 9D 9H JCJS", "9S"), "Should pass if four-suited and weak, even with both Jacks");
            Assert.AreEqual("Pass", GetSuggestedBid("AC AD AH9H AS", "9S"), "Should pass if four-suited with only one trump, even if Ace");
            Assert.AreEqual("Pass", GetSuggestedBid("ADKD AS AH JS", "9C"), "Should pass if four-suited with only one trump, even if off-Jack");
            Assert.AreEqual("Pass", GetSuggestedBid("ACKC AD AS JH", "9H"), "Should pass if four-suited with only one trump, even if high Jack");
            Assert.AreEqual("Pass", GetSuggestedBid("  TSJS 9DQDKD", "TD"), "Should pass if two-suited with weak trump and off-suit");
            Assert.AreEqual("Pass", GetSuggestedBid("  AHKH 9STSQS", "KS"), "Should pass if two-suited with very weak trump, even with high off-suit");
            Assert.AreEqual("Pass", GetSuggestedBid(" AD AH 9CTCQC", "KC"), "Should pass if three-suited with very weak trump, even with high off-suit");
            Assert.AreEqual("Pass", GetSuggestedBid(" AC 9S 9DTDQD", "KD"), "Should pass if three-suited with very weak trump and mixed off-suit");
            Assert.AreEqual("Pass", GetSuggestedBid(" AC KS 9HTHQH", "KH"), "Should pass if three-suited with very weak trump and mixed off-suit");
            Assert.AreEqual("Pass", GetSuggestedBid(" 9D 9H 9STSQS", "KS"), "Should pass if three-suited with very weak trump and weak off-suit");
        }

        [TestMethod]
        public void TestAvoidBidAlone()
        {
            //  if partner has high jack, they can help make 5 by beating off jack
            Assert.AreEqual("♥", GetSuggestedBid("9HTHQHKHAH", "JH"), "Do not bid alone without either jack");
            //  higher chance partner can cover off-suit card(s) to help make 5 tricks
            Assert.AreEqual("♣", GetSuggestedBid("ADKD ACJSJC", "9C"), "Do not bid alone with only three trump");
            Assert.AreEqual("♦", GetSuggestedBid("KH KDADJHJD", "9D"), "Do not bid alone with non-boss off-suit");
            Assert.AreEqual("♥", GetSuggestedBid("9D KHAHJDJH", "9H"), "Do not bid alone with weak off-suit");
        }

        [TestMethod]
        public void TestTake4Bid()
        {
            Assert.AreEqual("♠", GetSuggestedBid("  9HTH KSASJC", "9S", true), "Should bid when two-suited with three trump of reasonable strength");
            Assert.AreEqual("♣", GetSuggestedBid(" AD AH QCKCAC", "9C", true), "Should bid three-suited missing both Jacks with strong off-suit");
            Assert.AreEqual("Pass", GetSuggestedBid(" AD AH TCQCKC", "9C", true), "Should pass three-suited missing both Jacks with strong off-suit");
            Assert.AreEqual("Pass", GetSuggestedBid(" 9C 9S ADJHJD", "9D", true), "Should pass with only three sure tricks, regardless of other cards");
            Assert.AreEqual("Pass", GetSuggestedBid("  9DTD QHKHAH", "9H", true), "Should pass missing both Jacks with remaining high trump and two-suited");
            Assert.AreEqual("Pass", GetSuggestedBid(" ACKC AH JCJS", "9S", true), "Should pass with only Jacks with strong off-suit support");
            Assert.AreEqual("Pass", GetSuggestedBid(" ASKS 9C JHJD", "9D", true), "Should pass with only Jacks with mostly strong off-suit support");
            Assert.AreEqual("Pass", GetSuggestedBid(" 9D 9H KCACJS", "TC", true), "Should pass with weak off-suit and no high Jack if only three strong trump");
            Assert.AreEqual("Pass", GetSuggestedBid(" 9C 9S QDADJH", "TD", true), "Should pass with weak off-suit and no high Jack if only three strong trump");
            Assert.AreEqual("Pass", GetSuggestedBid("AC AD 9S JDJH", "TH", true), "Should pass four-suited having both Jacks and two off-suit Aces");
            Assert.AreEqual("♥", GetSuggestedBid("AC AH 9S JDJH", "TH", true), "Should take four-suited having both Jacks, an Ace and one off-suit Ace");
            Assert.AreEqual("Pass", GetSuggestedBid("  9DKD 9SKSJC", "TS", true), "Should pass with three trump if two-suited");
            Assert.AreEqual("Pass", GetSuggestedBid("  9HQH 9CQCJC", "AC", true), "Should pass with three trump if two-suited");
            Assert.AreEqual("Pass", GetSuggestedBid("  ASKS TDQDKD", "AD", true), "Should pass with three weak trump if two-suited with high off-suit");
            Assert.AreEqual("Pass", GetSuggestedBid("  TC 9HTHQHJH", "AH", true), "Should pass with four trump, regardless of off-suit");
            Assert.AreEqual("Pass", GetSuggestedBid("  9D 9STSQSKS", "AS", true), "Should pass with four trump, regardless of off-suit");
        }

        [TestMethod]
        public void TestTakeBid()
        {
            Assert.AreEqual("♠", GetSuggestedBid("  9HTH KSASJC", "9S"), "Should bid when two-suited with three trump of reasonable strength");
            Assert.AreEqual("♣", GetSuggestedBid(" AD AH QCKCAC", "9C"), "Should bid three-suited missing both Jacks with strong off-suit");
            Assert.AreEqual("♣", GetSuggestedBid(" AD AH TCQCKC", "9C"), "Should bid three-suited missing both Jacks with strong off-suit");
            Assert.AreEqual("♦", GetSuggestedBid(" 9C 9S ADJHJD", "9D"), "Should bid with three sure tricks, regardless of other cards");
            Assert.AreEqual("♥", GetSuggestedBid("  9DTD QHKHAH", "9H"), "Should bid missing both Jacks with remaining high trump and two-suited");
            Assert.AreEqual("♠", GetSuggestedBid(" ACKC AH JCJS", "9S"), "Should bid with only Jacks with strong off-suit support");
            Assert.AreEqual("♦", GetSuggestedBid(" ASKS 9C JHJD", "9D"), "Should bid with only Jacks with mostly strong off-suit support");
            Assert.AreEqual("♣", GetSuggestedBid(" 9D 9H KCACJS", "TC"), "Should bid with weak off-suit and no high Jack if three strong trump");
            Assert.AreEqual("♦", GetSuggestedBid(" 9C 9S QDADJH", "TD"), "Should bid with weak off-suit and no high Jack if three strong trump");
            Assert.AreEqual("♥", GetSuggestedBid("AC 9D 9S JDJH", "TH"), "Should bid four-suited having both Jacks and an off-suit Ace");
            Assert.AreEqual("♠", GetSuggestedBid("  9DKD 9SKSJC", "TS"), "Should bid with three trump if two-suited");
            Assert.AreEqual("♣", GetSuggestedBid("  9HQH 9CQCJC", "AC"), "Should bid with three trump if two-suited");
            Assert.AreEqual("♦", GetSuggestedBid("  ASKS TDQDKD", "AD"), "Should bid with three weak trump if two-suited with high off-suit");
            Assert.AreEqual("♥", GetSuggestedBid("  TC 9HTHQHJH", "AH"), "Should bid with four trump, regardless of off-suit");
            Assert.AreEqual("♠", GetSuggestedBid("  9D 9STSQSKS", "AS"), "Should bid with four trump, regardless of off-suit");
        }

        [TestMethod]
        public void TestTakeBidAlone()
        {
            //  risk of both same suit as Ace being led on first trick AND getting trumped is small
            Assert.AreEqual("♥ alone", GetSuggestedBid("AS KHAHJDJH", "9H"), "Should bid alone with sure trump and off-suit Ace");
            //  if partner doesn't have the off-Jack, you won't take all five tricks even together, may as well go for it
            Assert.AreEqual("♠ alone", GetSuggestedBid("AC QSKSASJS", "9S"), "Should bid alone with strength even if missing off-Jack");
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