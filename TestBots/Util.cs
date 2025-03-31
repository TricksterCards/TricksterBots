using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    internal static class Util
    {
        public static string PrettyCards(IEnumerable<Card> cards)
        {
            return PrettyHand(new Hand(cards));
        }

        public static string PrettyHand(string handString)
        {
            return PrettyHand(new Hand(handString));
        }

        public static string PrettyHand(Hand hand)
        {
            return string.Join(" ", hand.Select(c => c.StdNotation));
        }
    }

    public class TestPlayer : PlayerBase
    {
        public TestPlayer(
            int bid = BidBase.NoBid,
            string hand = "",
            int handScore = 0,
            int gameScore = 0,
            string cardsTaken = "",
            int seat = 0
        )
        {
            Bid = bid;
            BidHistory = bid == BidBase.NoBid ? new List<int>() : new List<int> { bid };
            CardsTaken = cardsTaken;
            GameScore = gameScore;
            Hand = hand;
            HandScore = handScore;
            PlayedCards = new List<PlayedCard>();
            Seat = seat;
            VoidSuits = new List<Suit>();
        }
    }

    public class TestCardState<T> : SuggestCardState<T> where T : GameOptions, new()
    {
        public TestCardState(
            IBaseBot bot,
            IEnumerable<TestPlayer> players,
            string trick = "",
            string notLegal = "",
            Suit notLegalSuit = Suit.Unknown,
            Suit trumpSuit = Suit.Unknown,
            bool trumpAnytime = false
        )
        {
            this.trumpSuit = trumpSuit;

            //  create a players collection for the valuable helpers
            var playersCollection = new PlayersCollectionBase(bot, players);

            //  ensure we have the correct number of players for the game rules
            if (playersCollection.Count != bot.Options.players)
                throw new Exception("Invalid number of players for game options");

            //  save the underlying list into our state
            this.players = playersCollection;

            //  set the seats of the players if not already set
            if (this.players.All(p => p.Seat == 0))
                for (var seat = 0; seat < this.players.Count; ++seat)
                    this.players[seat].Seat = seat;

            //  the "playing player" is assumed to be the first
            player = this.players[0];

            //  set the legal cards to be all the cards in the player's hand (adjusted later if cards in trick)
            legalCards = new Hand(player.Hand);

            //  set the cards played to the taken cards of all the players
            cardsPlayed = new Hand(string.Join("", this.players.Select(p => p.CardsTaken)));

            //  save the trick
            this.trick = new Hand(trick);

            //  if we have cards in the trick, set stuff about the trick and adjust legal cards 
            if (this.trick.Count > 0)
            {
                Assert.IsTrue(this.trick.Count < playersCollection.Count, $"{this.trick.Count} cards in trick is too many");

                var highCardIndex = bot.TrickHighCardIndex(this.trick);
                cardTakingTrick = this.trick[highCardIndex];
                var seatTakingTrick = SeatTakingTrick(bot, playersCollection, player);
                isPartnerTakingTrick = playersCollection.PartnersOf(playersCollection[0]).Any(p => p.Seat == seatTakingTrick);
                trickTaker = playersCollection.Single(p => p.Seat == seatTakingTrick);

                //  if we can follow suit, we must (unless we have trump and can trump anytime)
                var ledSuit = bot.EffectiveSuit(this.trick[0]);
                var legalCardsFollowingSuit = legalCards.Where(c =>
                    bot.EffectiveSuit(c) == ledSuit || (trumpAnytime && bot.EffectiveSuit(c) == trumpSuit)).ToList();
                if (legalCardsFollowingSuit.Any())
                    legalCards = legalCardsFollowingSuit;
            }
            else
            {
                cardTakingTrick = null;
                isPartnerTakingTrick = false;
                trickTaker = null;
            }

            if (!string.IsNullOrEmpty(notLegal))
                legalCards = new Hand(legalCards).RemoveCards(new Hand(notLegal));

            if (notLegalSuit != Suit.Unknown)
                legalCards = legalCards.Where(c => bot.EffectiveSuit(c) != notLegalSuit).ToList();
        }

        private int SeatTakingTrick(IBaseBot bot, PlayersCollectionBase players, PlayerBase nextOrLastPlayer)
        {
            var highCardIndex = bot.TrickHighCardIndex(trick);
            var activePlayers = players.Where(p => p.Bid != BidBase.NotPlaying && !p.Folded).OrderBy(p => p.Seat).ToList();
            var nextOrLastPlayerIndex = activePlayers.IndexOf(nextOrLastPlayer);
            var shift = trick.Count == activePlayers.Count ? 1 : 0; // shift by one if this is the last player
            var firstPlayerIndex = (nextOrLastPlayerIndex + activePlayers.Count - trick.Count + shift) % activePlayers.Count;
            return activePlayers[(firstPlayerIndex + highCardIndex) % activePlayers.Count].Seat;
        }
    }
}