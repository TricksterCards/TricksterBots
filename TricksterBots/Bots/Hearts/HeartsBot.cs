using System;
using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    public class HeartsBot : BaseBot<HeartsOptions>
    {
        public HeartsBot(HeartsOptions options, Suit trumpSuit) : base(options, trumpSuit)
        {
        }

        public override DeckType DeckType
        {
            get
            {
                switch (options.players)
                {
                    case 3:
                        return DeckType.No2D_51Card;
                    case 5:
                        return DeckType.No2C2D_50Card;
                    case 6:
                        return DeckType.No2C2D2S3C_48Card;
                    default:
                        return DeckType.Std52Card;
                }
            }
        }

        private int JackOfDiamondsValue => options.jackOfDiamonds ? -10 : options.jackOfDiamondsForMinus5 ? -5 : 0;

        public override BidBase SuggestBid(SuggestBidState<HeartsOptions> state)
        {
            throw new NotImplementedException();
        }

        public override List<Card> SuggestDiscard(SuggestDiscardState<HeartsOptions> state)
        {
            throw new NotImplementedException();
        }

        public override Card SuggestNextCard(SuggestCardState<HeartsOptions> state)
        {
#if SAVESTATELOCAL
            if (state.cloudCard == null)
            {
                File.WriteAllText($@"C:\Users\tedjo\LastCardState_{state.player.Seat}.json", JsonSerializer.Serialize(state));
            }
#endif

            var (players, trick, legalCards, cardsPlayed, player, isPartnerTakingTrick, cardTakingTrick) = (new PlayersCollectionBase(this, state.players), state.trick, state.legalCards, state.cardsPlayed,
                state.player, state.isPartnerTakingTrick, state.cardTakingTrick);
            var nPlayers = players.Count;
            var multiplePlayersHavePoints = players.Count(p => p.HandScore == 0 || p.HandScore == JackOfDiamondsValue) < nPlayers - 1;

            //  keep track of whether we have the Q♠ or J♦ (these will be null the associated game options are off)
            var hand = new Hand(player.Hand);
            var holdingQS = hand.Any(IsBlackLady);
            var queenSpades = legalCards.SingleOrDefault(IsBlackLady);
            var jackOfDiamonds = legalCards.SingleOrDefault(IsJackOfDiamonds);

            if (trick.Count == 0)
            {
                //  lead the J♦ if playing that option and we know it's good
                if (jackOfDiamonds != null && IsCardHigh(jackOfDiamonds, cardsPlayed))
                    return jackOfDiamonds;

                //  lead a low Spade if the QS is still at large and we're not holding the QS, KS, or AS
                var lowSpades = legalCards.Where(c => EffectiveSuit(c) == Suit.Spades && c.rank < Rank.Queen).ToList();
                if (lowSpades.Any() && !options.noBlackLady && !cardsPlayed.Any(IsBlackLady) &&
                    !hand.Any(c => EffectiveSuit(c) == Suit.Spades && c.rank >= Rank.Queen))
                    return lowSpades.OrderBy(RankSort).First();

                //  we know we have 2 or more legal cards, so we know we have at least one which isn't the Q♠.
                //  the other card might the J♦ and will be if the expression below returns null, so lead J♦ if we must.
                return legalCards.Where(c => !IsBlackLady(c) && !IsJackOfDiamonds(c)).OrderBy(c => c.rank).FirstOrDefault() ?? jackOfDiamonds;
            }

            //  this is the first trick of the game where points can't be played
            if (options.dumpPoints == HeartsDumpPoints.AfterFirstTrick && IsFirstTrick(players))
            {
                //  if we're playing with the J♦
                if (JackOfDiamondsValue < 0)
                {
                    //  play our highest legal card avoiding the J♦ and above
                    var tryPlay = legalCards.Where(c => c.suit != Suit.Diamonds || c.rank < Rank.Jack).OrderByDescending(RankSort).FirstOrDefault();
                    if (tryPlay != null)
                        return tryPlay;
                }

                //  if we can't or don't need to avoid the J♦ and above, just return our highest legal card
                return legalCards.OrderByDescending(RankSort).First();
            }

            //  give the jack of diamonds to our partner if they are taking the trick with the highest outstanding card of that suit
            if (jackOfDiamonds != null && isPartnerTakingTrick && IsCardHigh(cardTakingTrick, cardsPlayed))
                return jackOfDiamonds;

            //  if we're able to follow suit, all our legal cards will be of that suit
            if (legalCards.All(c => c.suit == trick[0].suit))
            {
                var trickScore = ScoreTrick(trick);

                if (trickScore < 0)
                    //  taking this trick will reduce our score, let's try to take it
                    return legalCards.OrderByDescending(RankSort).First();

                //  we're the last to play
                if (trick.Count == nPlayers - 1)
                {
                    //  play the Q♠ if we have it, our partner isn't taking the trick, and it won't take the trick
                    if (queenSpades != null && !isPartnerTakingTrick && trick[0].suit == Suit.Spades && cardTakingTrick.rank > Rank.Queen)
                        return queenSpades;

                    if (JackOfDiamondsValue < 0 && trick[0].suit == Suit.Diamonds && !cardsPlayed.Any(IsJackOfDiamonds))
                    {
                        //  if we the J♦ option is enabled and we have J♦ and either our partner is taking the trick
                        //  OR there are at most 2 points in the trick and the J♦ will take the trick, play it!
                        if (jackOfDiamonds != null && (isPartnerTakingTrick || trickScore < 3 && cardTakingTrick.rank < Rank.Jack))
                            return jackOfDiamonds;

                        //  otherwise, play the highest diamond below the jack, the lowest diamond above the jack, or the jack (we will have one of these)
                        return legalCards.Where(c => c.rank < Rank.Jack).OrderByDescending(RankSort).FirstOrDefault() ??
                               legalCards.Where(c => c.rank > Rank.Jack).OrderBy(RankSort).FirstOrDefault() ?? jackOfDiamonds;
                    }

                    //  if taking this trick won't hurt us, ditch a high card avoiding the Q♠
                    if (trickScore <= 0)
                        return legalCards.Where(c => !IsBlackLady(c)).OrderByDescending(RankSort).First();
                }

                //  we can follow suit so just play the highest card we can below the highest already played
                var cardsBelowHighestPlayed = legalCards.Where(c => c.rank < cardTakingTrick.rank).OrderBy(RankSort).ToList();

                if (JackOfDiamondsValue < 0 && trick[0].suit == Suit.Diamonds && !trick.Any(IsJackOfDiamonds) && !cardsPlayed.Any(IsJackOfDiamonds))
                    //  if we might be able to take the J♦ later, don't throw high diamonds now
                    cardsBelowHighestPlayed = cardsBelowHighestPlayed.Where(c => c.rank < Rank.Jack).ToList();

                if (cardsBelowHighestPlayed.Any())
                {
                    var highestRankBelowHighestPlayed = cardsBelowHighestPlayed.Max(c => c.rank);

                    //  if the suit is Hearts and we have more than one and the highest heart is 10 or above, save it 'til later unless multiple players have points
                    if (!multiplePlayersHavePoints && trick[0].suit == Suit.Hearts && cardsBelowHighestPlayed.Count > 1 &&
                        highestRankBelowHighestPlayed >= Rank.Ten)
                        return cardsBelowHighestPlayed.Last(c => c.rank < highestRankBelowHighestPlayed);

                    //  else return the highest rank card
                    return cardsBelowHighestPlayed.First(c => c.rank == highestRankBelowHighestPlayed);
                }

                //  if we're the second to last to play and we couldn't get below the highest in the trick and this isn't going to hurt (yet),
                //  play our highest card in suit (if ♠ and Q♠ hasn't been played, just J or lower if we can)
                if (trick.Count == nPlayers - 2 && trickScore <= 0)
                {
                    if (!options.noBlackLady && trick[0].suit == Suit.Spades && !cardsPlayed.Any(IsBlackLady))
                    {
                        var belowQueen = legalCards.Where(c => c.rank < Rank.Queen).ToList();
                        if (belowQueen.Any())
                        {
                            var highLessThanQueen = belowQueen.Max(c => c.rank);
                            return belowQueen.Last(c => c.rank == highLessThanQueen);
                        }
                    }

                    var highRank = legalCards.Max(c => c.rank);
                    return legalCards.Last(c => c.rank == highRank);
                }

                //  if we're the last to play, there are points in the trick, and we couldn't get below: play the highest we have avoiding Q♠
                if (trick.Count == nPlayers - 1)
                {
                    if (JackOfDiamondsValue < 0 && trick[0].suit == Suit.Diamonds && !trick.Any(IsJackOfDiamonds) && !cardsPlayed.Any(IsJackOfDiamonds))
                    {
                        //  if we might be able to take the J♦ later, don't throw high diamonds now
                        var play = legalCards.Where(c => c.rank <= Rank.Jack).OrderByDescending(RankSort).FirstOrDefault();
                        if (play != null)
                            return play;
                    }
                    else
                    {
                        //  otherwise play the highest card we have avoiding Q♠
                        return legalCards.Where(c => !IsBlackLady(c)).OrderByDescending(RankSort).First();
                    }
                }

                //  just play the lowest card we have avoiding Q♠ and J♦ (they won't both in our legalCards here because we're following suit)
                return legalCards.Where(c => !IsBlackLady(c) && !IsJackOfDiamonds(c)).OrderBy(RankSort).First();
            }

            //  we can't follow suit so dump the Q♠ if we have it and our partner is not taking the trick
            if (queenSpades != null && !isPartnerTakingTrick)
                return queenSpades;

            //  otherwise try to dump the A♠ or K♠ if the Q♠ is still out there and not in our hand
            //  we must look and player.Hand because the Q♠ may not be legal if options.qsAfterHearts = true
            if (!options.noBlackLady && !holdingQS && !cardsPlayed.Any(IsBlackLady))
            {
                var aboveQueen = legalCards.Where(c => c.suit == Suit.Spades && c.rank > Rank.Queen).OrderBy(RankSort).ToList();
                if (aboveQueen.Count > 0)
                    return aboveQueen.Last();
            }

            //  else dump the highest heart, but save a heart until we have three or fewer cards left unless multiple players have points (shooting prevention)
            var hearts = legalCards.Where(c => c.suit == Suit.Hearts).OrderBy(c => c.rank).ToList();
            if (!isPartnerTakingTrick && (hearts.Count > 1 || hearts.Count > 0 && (multiplePlayersHavePoints || legalCards.Count <= 3)))
            {
                var highestHeart = hearts.Max(c => c.rank);

                //  if we have more than one heart and the highest is 10 or above, save it 'til later unless multiple players have points
                if (!multiplePlayersHavePoints && hearts.Count > 1 && highestHeart >= Rank.Ten)
                    return hearts.Last(c => c.rank < highestHeart);

                return hearts.Last(c => c.rank == highestHeart);
            }

            //  favor dumping non-hearts if we didn't dump one above
            var nonHearts = legalCards.Where(c => c.suit != Suit.Hearts).ToList();

            if (JackOfDiamondsValue < 0 && !trick.Any(IsJackOfDiamonds) && !cardsPlayed.Any(IsJackOfDiamonds))
                //  if we might be able to take the J♦ later, don't throw high diamonds now
                nonHearts = nonHearts.Where(c => c.suit != Suit.Diamonds || c.rank < Rank.Jack).ToList();

            if (holdingQS)
                //  if we're holding the QS, avoid dumping Spades since we might need them for protection later
                nonHearts = nonHearts.Where(c => c.suit != Suit.Spades).ToList();

            if (nonHearts.Count > 0)
                return nonHearts.OrderByDescending(RankSort).First();

            //  fallback is to dump the highest rank card we have
            return legalCards.OrderByDescending(RankSort).First();
        }

        public override List<Card> SuggestPass(SuggestPassState<HeartsOptions> state)
        {
            var (passCount, hand) = (state.passCount, state.hand);
            var pass = new Hand();

            if (passCount == 0)
                return pass;

            if (!options.noBlackLady)
            {
                //  if we have the Q, K, or A of Spades, pass them
                var nHighSpades = hand.Count(c => c.suit == Suit.Spades && c.rank >= Rank.Queen);
                var nNotHighSpades = hand.Count(c => c.suit == Suit.Spades && c.rank < Rank.Queen);
                if (nHighSpades > 0 && nNotHighSpades < 3)
                    pass.AddRange(hand.Where(c => c.suit == Suit.Spades && c.rank >= Rank.Queen).OrderByDescending(c => c.rank).Take(passCount - pass.Count));
            }

            var lowHearts = hand.Where(c => EffectiveSuit(c) == Suit.Hearts && c.rank < Rank.Eight).OrderBy(c => c.rank).ToList();
            var nLowHearts = lowHearts.Count;

            if (pass.Count < passCount && nLowHearts > 0)
                //  pass our lowest Heart below the eight to make it more difficult for the receiving player to shoot the moon
                pass.Add(lowHearts[0]);

            if (pass.Count < passCount)
            {
                //  look at passing high Hearts, but keep at least one as it may be useful to block a moon-shooting attempt
                var nHighHearts = hand.Count(c => c.suit == Suit.Hearts && Rank.Jack <= c.rank);

                if (nHighHearts > 1 && nLowHearts < 3)
                {
                    var highestHeart = hand.Where(c => c.suit == Suit.Hearts).Max(c => c.rank);
                    pass.AddRange(hand.Where(c => c.suit == Suit.Hearts && Rank.Jack <= c.rank && c.rank != highestHeart).OrderByDescending(c => c.rank)
                        .Take(passCount - pass.Count));
                }
            }

            if (pass.Count < passCount)
                //  pass high cards that won't help us protect the J♦ or Q♠ (depending on options)
            {
                pass.AddRange(hand
                    .Where(c => c.suit == Suit.Clubs || JackOfDiamondsValue == 0 && c.suit == Suit.Diamonds ||
                                options.noBlackLady && c.suit == Suit.Spades).OrderByDescending(c => c.rank)
                    .Take(passCount - pass.Count));
            }

            if (pass.Count < passCount && JackOfDiamondsValue < 0)
            {
                pass.AddRange(hand.Where(c => c.suit == Suit.Diamonds && c.rank < Rank.Jack).OrderByDescending(RankSort).Where(c => !pass.Contains(c))
                    .Take(passCount - pass.Count));
            }

            if (pass.Count < passCount)
                pass.AddRange(hand.OrderBy(PassSuitSort).ThenByDescending(RankSort).Where(c => !pass.Contains(c)).Take(passCount - pass.Count));

            return pass;
        }

        private static bool IsFirstTrick(IEnumerable<PlayerBase> players)
        {
            return players.All(p => string.IsNullOrEmpty(p.CardsTaken));
        }

        private bool IsBlackLady(Card c)
        {
            return !options.noBlackLady && c.suit == Suit.Spades && c.rank == Rank.Queen;
        }

        private bool IsJackOfDiamonds(Card c)
        {
            return JackOfDiamondsValue != 0 && c.suit == Suit.Diamonds && c.rank == Rank.Jack;
        }

        private int PassSuitSort(Card c)
        {
            switch (c.suit)
            {
                case Suit.Clubs:
                    return 0;
                case Suit.Diamonds:
                    return JackOfDiamondsValue < 0 ? 2 : 0;
                case Suit.Hearts:
                    return 1;
                case Suit.Spades:
                    return options.noBlackLady ? 0 : 2;
            }

            return 0;
        }

        private int ScoreTrick(IEnumerable<Card> trick)
        {
            var score = 0;
            foreach (var card in trick)
            {
                if (IsBlackLady(card))
                    score += 13;
                else if (IsJackOfDiamonds(card))
                    score += JackOfDiamondsValue;
                else if (card.suit == Suit.Hearts)
                    score += 1;
            }

            return score;
        }
    }
}