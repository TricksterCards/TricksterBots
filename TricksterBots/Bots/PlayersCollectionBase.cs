using System;
using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    /// <summary>
    ///     Collection of player﻿s with game-options-specific helpers methods.
    /// </summary>
    public class PlayersCollectionBase : List<PlayerBase>
    {
        private readonly IBaseBot gameBot;

        /// <summary>
        ///     Constructs an PlayersCollection from a game bot and a collection of PlayerBase﻿s.
        /// </summary>
        public PlayersCollectionBase(IBaseBot bot, IEnumerable<PlayerBase> players)
            : base(players)
        {
            gameBot = bot;
        }
        protected virtual bool IsPartnership => gameBot.IsPartnership;

        public PlayerBase Lho(PlayerBase player)
        {
            return this.First(p => p.Seat == (player.Seat + 1) % Count);
        }

        public bool LhoIsVoidInSuit(PlayerBase player, Card card, IEnumerable<Card> cardsPlayed)
        {
            return TargetIsVoidInSuit(player, Lho(player), card, cardsPlayed);
        }

        public List<PlayerBase> Opponents(PlayerBase player)
        {
            var partnersSeats = PartnersSeatsOf(player);
            return this.Where(p => p.Seat != player.Seat && !partnersSeats.Contains(p.Seat)).ToList();
        }

        public Dictionary<Suit, bool> OpponentsVoidSuits(PlayerBase player)
        {
            var opponents = Opponents(player);
            return SuitRank.allSuits.ToDictionary(s => s, s => opponents.All(p => p.VoidSuits.Contains(s)));
        }

        public bool PartnerIsVoidInSuit(PlayerBase player, Card card, IReadOnlyList<Card> cardsPlayed)
        {
            return IsPartnership && PartnersOf(player).All(target => TargetIsVoidInSuit(player, target, card, cardsPlayed));
        }

        public PlayerBase PartnerOf(PlayerBase player)
        {
            return IsPartnership ? OppositeOf(player) : null;
        }

        /// <summary>
        ///     Gets the partners of a player.
        /// </summary>
        /// <returns>Returns an array of 0, 1 or 2 OtherPlayer﻿s depending on the game options.</returns>
        public virtual PlayerBase[] PartnersOf(PlayerBase player)
        {
            if (gameBot.IsTwoTeams && Count == 6)
                return new[] { this.First(p => p.Seat == (player.Seat + 2) % Count), this.First(p => p.Seat == (player.Seat + 4) % Count) };

            return IsPartnership ? new[] { OppositeOf(player) } : Array.Empty<PlayerBase>();
        }

        public int[] PartnersSeatsOf(PlayerBase player)
        {
            return PartnersOf(player).Select(p => p.Seat).ToArray();
        }

        public PlayerBase Rho(PlayerBase player)
        {
            return this.First(p => p.Seat == (player.Seat + Count - 1) % Count);
        }

        public bool RhoIsVoidInSuit(PlayerBase player, Card card, IEnumerable<Card> cardsPlayed)
        {
            return TargetIsVoidInSuit(player, Rho(player), card, cardsPlayed);
        }

        public bool TargetIsVoidInSuit(PlayerBase player, PlayerBase target, Card card, IEnumerable<Card> cardsPlayed)
        {
            if (gameBot.CanSeeHand(this, player, target))
                return new Hand(target.Hand).All(c => gameBot.EffectiveSuit(c) != gameBot.EffectiveSuit(card));

            return target.VoidSuits.Contains(gameBot.EffectiveSuit(card)) || PlayerHasAllRemainingInSuit(player, card, cardsPlayed);
        }

        private PlayerBase OppositeOf(PlayerBase player)
        {
            return this.First(p => p.Seat == (player.Seat + Count / 2) % Count);
        }

        private bool PlayerHasAllRemainingInSuit(PlayerBase player, Card card, IEnumerable<Card> cardsPlayed)
        {
            var suit = gameBot.EffectiveSuit(card);
            var totalCardsInSuit = DeckBuilder.BuildDeck(gameBot.DeckType).Count(c => gameBot.EffectiveSuit(c) == suit);
            return totalCardsInSuit ==
                   this.Where(p => gameBot.CanSeeHand(this, player, p)).Sum(p => new Hand(p.Hand).Count(c => gameBot.EffectiveSuit(c) == suit))
                   + cardsPlayed.Count(c => gameBot.EffectiveSuit(c) == suit);
        }
    }
}