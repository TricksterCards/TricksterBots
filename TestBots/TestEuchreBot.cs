using System.Collections.Generic;
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
                    new TestPlayer(seat: 0, hand: handString),
                    new TestPlayer(seat: 1),
                    new TestPlayer(seat: 2),
                    new TestPlayer(seat: 3)
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
        [DataRow("QS",  "♦ alone", "JDJHTC9CQS", DisplayName = "Ditch non-boss off-suit singleton")]
        [DataRow("9C",  "♦ alone", "JDJHTC9CAS", DisplayName = "Avoid ditching off-suit Ace")]
        [DataRow("AH",  "♦ alone", "JDJHADKDAH", DisplayName = "Ditch off-suit Ace if necessary")]
        [DataRow("9D",  "♦ alone", "JDJHADKD9D", DisplayName = "Ditch low from trump if necessary")]
        [DataRow("9D", "NT alone", "AHJD9DACKC", DisplayName = "Ditch low from suit without boss in NT")]
        [DataRow("TD", "NT alone", "AHJDTDKC9C", DisplayName = "Ditch low from suit without possible boss in NT")]
        public void CallForBestMaker(string expected, string bidStr, string hand)
        {
            var bid = GetBid(bidStr, out var bidSuit);
            var bot = GetBot(bidSuit, new EuchreOptions { allowNotrump = true, callForBest = true });

            //  first suggestion is maker passing to non-playing partner
            var passState = new SuggestPassState<EuchreOptions>
            {
                player = new TestPlayer(bid.value, hand),
                hand = new Hand(hand),
                passCount = 1
            };
            var suggestion = bot.SuggestPass(passState);
            Assert.AreEqual(expected, suggestion[0].ToString());
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
        [DataRow("♥", " 9HTHQHKHAH", "JH", DisplayName = "Do not bid alone without either jack")]
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
        [DataRow("♠ alone", "  AC 9SKSASJS", "TS", false, false, DisplayName = "Should bid alone with four trump and off-suit Ace")]
        [DataRow("♠ alone", "  AC 9SKSJCJS", "TS", false, false, DisplayName = "Should bid alone with four trump and off-suit Ace (case 2)")]
        //  risk of both same suit as Ace being led on first trick AND getting trumped is small
        [DataRow("♥ alone", "  AS KHAHJDJH", "9H", false, false, DisplayName = "Should bid alone with sure trump and off-suit Ace")]
        //  if partner doesn't have the off-Jack, you won't take all five tricks even together, may as well go for it
        [DataRow("♠ alone", "  AC QSKSASJS", "9S", false, false, DisplayName = "Should bid alone with strength even if missing off-Jack")]
        //  two-suited with an off-suit ace and top three trump should go alone
        [DataRow("♥ alone", "  ASQS AHJDJH", "9H", false, false, DisplayName = "Should bid alone if two-suited with off-suit Ace and top three trump")]
        //  if we're close to going alone with call for best enabled, we should count on an extra trump
        [DataRow("♠ alone", " ACKC AH JCJS", "9S",  true, false, DisplayName = "Should bid alone if call-for-best with only Jacks with strong off-suit support")]
        //  but don't count on anything extra from partner if alone must take 5 is enabled
        [DataRow(      "♠", " ACKC AH JCJS", "9S",  true,  true, DisplayName = "Should not bid alone with only Jacks if alone-must-take-5 even with call-for-best")]
        [DataRow("♠ alone", "  AH QSKSASJC", "9S",  true, false, DisplayName = "Should go alone without high Jack if strong enough")]
        [DataRow(      "♠", "  AH QSKSASJC", "JS",  true, false, DisplayName = "Should not go alone if opponents will pick up high Jack")]
        [DataRow("♥ alone", "  AD THQHKHAH", "9H",  true, false, DisplayName = "Bid alone in call-for-best without either Jack with 4 trump and strong off-suit")]
        [DataRow("♥ alone", "  9D THQHKHAH", "9H",  true, false, DisplayName = "Bid alone in call-for-best without either Jack with 4 trump and weak off-suit")]
        [DataRow(      "♥", "  ADKD QHKHAH", "9H",  true, false, DisplayName = "Should not go alone in call-for-best without either Jack with next 3 trump and strong off-suit")]
        [DataRow(      "♥", "  KDQD QHKHAH", "9H",  true, false, DisplayName = "Should not go alone in call-for-best without either Jack, only 3 trump, and no off-suit Ace")]
        [DataRow(      "♥", "  ADAC THQHKH", "AH",  true, false, DisplayName = "Should not go alone in call-for-best without top 3 trump")]
        //  odds are good we can take these alone - if partner has A in our offsuit, we'll likely be good even if they sit out
        [DataRow("♣ alone", "  ADKD ACJSJC", "9C", false, false, DisplayName = "Bid alone with top three trump, two-suited, and top off-suit")]
        [DataRow("♣ alone", " AD AH ACJSJC", "9C", false, false, DisplayName = "Bid alone with top three trump and top off-suit")]
        [DataRow("♦ alone", "  KH KDADJHJD", "9D", false, false, DisplayName = "Bid alone with top four trump and high off-suit")]
        [DataRow("♥ alone", "  9D KHAHJDJH", "9H", false, false, DisplayName = "Bid alone with top four trump and any off-suit")]
        //  but if alone must take 5 is on we should be more cautious
        [DataRow("♣ alone", "  ADKD ACJSJC", "9C", false,  true, DisplayName = "Bid alone if alone-must-take-5 with top three trump, two-suited, and top off-suit")]
        [DataRow("♣ alone", " AD AH ACJSJC", "9C", false,  true, DisplayName = "Bid alone if alone-must-take-5 with top three trump and top off-suit")]
        [DataRow(      "♦", "  KH KDADJHJD", "9D", false,  true, DisplayName = "Don't bid alone if alone-must-take-5 with top four trump and high off-suit")]
        [DataRow(      "♥", "  9D KHAHJDJH", "9H", false,  true, DisplayName = "Don't bid alone if alone-must-take-5 with top four trump and any off-suit")]
        public void TestTakeBidAlone(string bid, string hand, string upCard, bool callForBest, bool aloneTake5)
        {
            Assert.AreEqual(bid, GetSuggestedBid(hand, upCard, aloneTake5: aloneTake5, callForBest: callForBest));
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

        [TestMethod]
        [DataRow("JD", "JDJHTDASKS", 0, DisplayName = "Lead boss trump if we have it")]
        [DataRow("JH", "JHTDAS", 0, DisplayName = "Lead high trump if opponents haven't taken a trick")]
        [DataRow("AS", "JHTDAS", 1, DisplayName = "Lead off-suit if opponents have a trick")]
        [DataRow("KS", "KS9S", 0, DisplayName = "Lead high off-suit if opponents haven't taken a trick")]
        [DataRow("9S", "KS9S", 1, DisplayName = "Lead low off-suit if opponents have a trick")]
        public void LeadWhenAlone(string card, string hand, int oppTricks)
        {
            var players = new[]
            {
                new TestPlayer(112, hand, handScore: 5 - hand.Length/2 - oppTricks),
                new TestPlayer(140, handScore: oppTricks),
                new TestPlayer(-3),
                new TestPlayer(140)
            };

            var bot = GetBot(Suit.Diamonds);
            var cardState = new TestCardState<EuchreOptions>(bot, players);

            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual(card, suggestion.ToString());
        }

        private static BidBase GetBid(string bidText, out Suit suit)
        {
            var parts = bidText.Split(' ');
            suit = Suit.Unknown;
            switch (parts[0])
            {
                case "♣":
                    suit = Suit.Clubs;
                    break;
                case "♦":
                    suit = Suit.Diamonds;
                    break;
                case "♠":
                    suit = Suit.Spades;
                    break;
                case "♥":
                    suit = Suit.Hearts;
                    break;
            }

            var isAlone = parts.Length > 1;
            return new BidBase((isAlone ? (int)EuchreBid.MakeAlone : (int)EuchreBid.Make) + (int)suit);
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
        /// <param name="callForBest">The value to use for EuchreOptions.callForBest</param>
        /// <returns>The suggested bid for the first bidder</returns>
        private static string GetSuggestedBid(string handString, string upCardString, bool take4for1 = false, bool callForBest = false, bool aloneTake5 = false)
        {
            handString = handString.Replace(" ", "");

            var upCard = new Card(upCardString);
            var bot = GetBot(Suit.Unknown, new EuchreOptions { aloneTake5 = aloneTake5, callForBest = callForBest, take4for1 = take4for1 });

            //  get the bid using the state-based suggest bid method
            var bidState = new SuggestBidState<EuchreOptions>
            {
                players = new[]
                {
                    new TestPlayer(seat: 0, hand: handString),
                    new TestPlayer(seat: 1),
                    new TestPlayer(seat: 2),
                    new TestPlayer(seat: 3)
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