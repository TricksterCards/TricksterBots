﻿using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestPitchBot
    {
        [TestMethod]
        public void TestBids()
        {
            var options = new PitchOptions()
            {
                variation = PitchVariation.FourPoint,
                drawOption = PitchDrawOption.NonTrump,
                gameOverScore = 15,
                isPartnership = true,
                lowGoesToTaker = true,
                minBid = 2,
                offerShootBid = false,
                pitcherLeadsTrump = true,
                playTrump = PitchPlayTrump.Anytime,
                stickTheDealer = true,
            };

            Assert.AreEqual((int)PitchBid.Base + 3, GetSuggestedBid("ACKC3S6H7HQH", out var hand, options), $"Expect bid of 3 for hand {Util.PrettyHand(hand)}");
            Assert.AreEqual((int)PitchBid.Base + 4, GetSuggestedBid("ACKCQCJC2CAH", out hand, options), $"Expect bid of 4 for hand {Util.PrettyHand(hand)}");
            Assert.AreEqual(BidBase.Pass, GetSuggestedBid("6C5C5S4S3D2D", out hand, options), $"Expect bid of Pass for hand {Util.PrettyHand(hand)}");

            options.offerShootBid = true;
            Assert.AreEqual((int)PitchBid.ShootMoonBid, GetSuggestedBid("ACKCQCJC2CAH", out hand, options), $"Expect bid of Shoot for hand {Util.PrettyHand(hand)}");
        }

        [TestMethod]
        public void TestHighBids()
        {
            var options = new PitchOptions()
            {
                variation = PitchVariation.FourPoint,
                drawOption = PitchDrawOption.NonTrump,
                gameOverScore = 15,
                isPartnership = true,
                kitty = 3,
                lowGoesToTaker = true,
                minBid = 2,
                offerShootBid = false,
                pitcherLeadsTrump = true,
                playTrump = PitchPlayTrump.Anytime,
                stickTheDealer = true,
            };

            Assert.AreEqual((int)PitchBid.Base + 3, GetSuggestedBid("AHQH6H5DJC5C", out var hand, options), $"Expect bid of 3 for hand {Util.PrettyHand(hand)}");
        }

        private static PitchBot GetBot(PitchVariation variation)
        {
            return GetBot(new PitchOptions() { variation = variation });
        }

        private static PitchBot GetBot(PitchOptions options)
        {
            return new PitchBot(options, Suit.Unknown);
        }

        private static int GetSuggestedBid(string handString, out Hand hand, PitchOptions options)
        {
            var players = new[]
            {
                new TestPlayer(seat: 0, hand: handString.Replace(" ", string.Empty)),
                new TestPlayer(seat: 1),
                new TestPlayer(seat: 2),
                new TestPlayer(seat: 3)
            };

            var maxBid = 4;
            switch (options.variation)
            {
                case PitchVariation.FourPoint:
                    maxBid = 4;
                    break;
                case PitchVariation.FivePoint:
                    maxBid = 5;
                    break;
                case PitchVariation.SixPoint:
                    maxBid = 6;
                    break;
                case PitchVariation.SevenPoint:
                    maxBid = 7;
                    break;
                case PitchVariation.TenPoint:
                    maxBid = 10;
                    break;
                case PitchVariation.ElevenPoint:
                    maxBid = 11;
                    break;
                case PitchVariation.ThirteenPoint:
                    maxBid = 13;
                    break;
                case PitchVariation.NinePoint:
                    maxBid = 9;
                    break;
            }

            var legalBids = new List<BidBase>();
            for (var v = options.minBid; v <= maxBid; ++v) legalBids.Add(new BidBase((int)PitchBid.Base + v));
            if (options.offerShootBid)
            {
                legalBids.Add(new BidBase((int)PitchBid.ShootMoonBid));
            }
            legalBids.Add(new BidBase(BidBase.Pass));

            hand = new Hand(players[0].Hand);
            var bidState = new SuggestBidState<PitchOptions>
            {
                dealerSeat = 3,
                player = players[0],
                players = players,
                legalBids = legalBids,
                hand = hand
            };

            return GetBot(options).SuggestBid(bidState).value;
        }
    }
}