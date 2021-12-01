using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    public interface IBaseBot
    {
        DeckType DeckType { get; }
        bool IsPartnership { get; }
        bool IsTwoTeams { get; }
        bool CanSeeHand(PlayersCollectionBase players, PlayerBase player, PlayerBase target);
        Suit EffectiveSuit(Card c);
    }

    public abstract class BaseBot<T> : IBaseBot where T : GameOptions
    {
        protected BaseBot(T options, Suit trumpSuit)
        {
            this.options = options;
            trump = trumpSuit;
        }

        protected T options { get; }

        protected Suit trump { get; }

        public virtual bool CanSeeHand(PlayersCollectionBase players, PlayerBase player, PlayerBase target)
        {
            return player.Seat == target.Seat;
        }

        public abstract DeckType DeckType { get; }

        public Suit EffectiveSuit(Card c)
        {
            return EffectiveSuit(c, trump);
        }

        /// <summary>
        ///     Indicates whether this game is played as a partner. Could be a 2- or 3- player partnership.
        /// </summary>
        public bool IsPartnership => options.isPartnership;

        /// <summary>
        ///     Indicates if a 6-player game is played as 2 teams in 3v3 configuration.
        /// </summary>
        public bool IsTwoTeams => options.isTwoTeams;

        public abstract BidBase SuggestBid(SuggestBidState<T> state);

        public abstract List<Card> SuggestDiscard(SuggestDiscardState<T> state);

        public abstract Card SuggestNextCard(SuggestCardState<T> state);

        public abstract List<Card> SuggestPass(SuggestPassState<T> state);

        protected virtual Suit EffectiveSuit(Card c, Suit trumpSuit)
        {
            return c.suit == Suit.Joker && trumpSuit != Suit.Unknown ? trumpSuit : c.suit;
        }

        protected int HighRankInSuit(Card card)
        {
            return HighRankInSuit(EffectiveSuit(card));
        }

        protected int HighRankInSuit(Suit suit)
        {
            return options.highRankBySuit.TryGetValue(suit, out var s) ? s : (int)Rank.Ace;
        }

        protected bool IsCardHigh(Card highestCard, IEnumerable<Card> cardsPlayed)
        {
            if (!IsOfValue(highestCard))
                return false;

            var highRank = HighRankInSuit(highestCard);
            return RankSort(highestCard) == highRank ||
                   cardsPlayed.Count(c => EffectiveSuit(c) == EffectiveSuit(highestCard) && RankSort(c) > RankSort(highestCard)) ==
                   highRank - RankSort(highestCard);
        }

        protected virtual bool IsOfValue(Card c)
        {
            return true;
        }

        protected bool IsTrump(Card card)
        {
            return IsOfValue(card) && EffectiveSuit(card) == trump;
        }

        protected int RankSort(Card c)
        {
            return RankSort(c, trump);
        }

        protected virtual int RankSort(Card c, Suit trumpSuit)
        {
            return (int)c.rank;
        }

        protected virtual int SuitOrder(Suit s)
        {
            return (int)s;
        }
    }
}