using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    public class EuchreBot : BaseBot<EuchreOptions>
    {
        private static readonly Dictionary<Suit, int> suitOrder = new Dictionary<Suit, int>
        {
            { Suit.Unknown, 0 },
            { Suit.Diamonds, 1 },
            { Suit.Clubs, 2 },
            { Suit.Hearts, 3 },
            { Suit.Spades, 4 },
            { Suit.Joker, 5 }
        };

        public EuchreBot(EuchreOptions options, Suit trumpSuit) : base(options, trumpSuit)
        {
        }

        public override DeckType DeckType
        {
            get
            {
                switch (options.deckSize)
                {
                    case 24:
                        return options.withJoker ? DeckType.NineToAceAndJoker : DeckType.NineToAce;
                    case 28:
                        return options.withJoker ? DeckType.EightToAceAndJoker : DeckType.EightToAce;
                    case 32:
                        return options.withJoker ? DeckType.SevenToAceAndJoker : DeckType.SevenToAce;
                }

                return DeckType.NineToAce;
            }
        }

        public double EstimatedTricks(Hand hand, Suit maybeTrump, bool withJoker)
        {
            //  ensure all suits have at least some value so we never pick Suit.Unknown
            var est = 0.1;

            var trumpCards = hand.Where(c => EffectiveSuit(c, maybeTrump) == maybeTrump).ToList();
            foreach (var card in trumpCards)
            {
                var nCardsAbove = trumpCards.Count(c => RankSort(c, maybeTrump) > RankSort(card, maybeTrump));
                var nCardsBelow = trumpCards.Count(c => RankSort(c, maybeTrump) < RankSort(card, maybeTrump));
                var nRanksAbove = RankSort(withJoker ? new Card(Suit.Joker, Rank.High) : new Card(maybeTrump, Rank.Jack), maybeTrump) -
                                  RankSort(card, maybeTrump);
                var nGapsAbove = nRanksAbove - nCardsAbove;

                if (card.rank < Rank.Jack)
                    //  adjust for the "artificial" gap left by promoting the Jack in the trump suit
                    nGapsAbove--;

                if (nCardsBelow >= nGapsAbove)
                    est += 1;
                else if (nCardsBelow == nGapsAbove - 1)
                    est += 0.75;
                else if (nCardsBelow == nGapsAbove - 2)
                    est += 0.5;
            }

            var offSuitCards = hand.Where(c => EffectiveSuit(c, maybeTrump) != maybeTrump).ToList();
            var offSuitAces = offSuitCards.Where(c => c.rank == Rank.Ace);
            foreach (var nCardsOfAceSuit in offSuitAces.Select(ace => offSuitCards.Count(c => EffectiveSuit(c, maybeTrump) == ace.suit)))
            {
                if (nCardsOfAceSuit == 1 && trumpCards.Count > 1)
                    est += 0.5;
                else if (nCardsOfAceSuit <= 2)
                    est += 0.25;
            }

            if (trumpCards.Count == 3 &&
                offSuitCards.Any(c1 => offSuitCards.Any(c2 => c1 != c2 && EffectiveSuit(c2, maybeTrump) == EffectiveSuit(c1, maybeTrump))))
                //  if we're two-suited with three trump, add most of a trick
                est += 0.75;

            return est;
        }

        public override BidBase SuggestBid(SuggestBidState<EuchreOptions> state)
        {
            var (players, dealerSeat, hand, legalBids, player, upCard, upCardSuit) =
                (new PlayersCollectionBase(this, state.players), state.dealerSeat, state.hand, state.legalBids, state.player, state.upCard, state.upCardSuit);

            if (IsAskingDefendAlone(players))
                return new BidBase(BidBase.Pass);

            var isDealer = player.Seat == dealerSeat;

            if (options.goUnder && legalBids.Any(b => b.value == (int)EuchreBid.GoUnder))
            {
                //  go under if we can and don't have any suit we think we want to bid
                var bestBid = SuggestBid(hand, upCard, upCardSuit, isDealer);

                if (bestBid.value == BidBase.Pass && upCard != null)
                    bestBid = SuggestBid(hand, null, upCardSuit, isDealer);

                return legalBids.Single(b => b.value == (bestBid.value == BidBase.Pass ? (int)EuchreBid.GoUnder : (int)EuchreBid.AfterGoUnder));
            }

            var suggestion = SuggestBid(hand, upCard, upCardSuit, isDealer);
            var canBidSuggestion = legalBids.Any(b => b.value == suggestion.value);

            return canBidSuggestion ? suggestion : new BidBase(BidBase.Pass);
        }

        //  overload called above and for unit tests
        public BidBase SuggestBid(Hand hand, Card upCard, Suit upCardSuit, bool isDealer)
        {
            var highSuit = Suit.Unknown;
            var highEstimate = 0.0;

            if (upCard != null)
            {
                var effectiveHand = new Hand();
                effectiveHand.AddRange(hand);

                if (isDealer)
                    effectiveHand.Add(upCard);

                highEstimate = EstimatedTricks(effectiveHand, EffectiveUpCardSuit(upCard), options.withJoker);
                highSuit = EffectiveUpCardSuit(upCard);
            }
            else
            {
                foreach (var suit in SuitRank.stdSuits.Where(s => s != upCardSuit))
                {
                    var est = EstimatedTricks(hand, suit, options.withJoker);
                    if (est > highEstimate)
                    {
                        highEstimate = est;
                        highSuit = suit;
                    }
                }

                if (options.allowNotrump)
                {
                    var est = EstimatedNotrumpTricks(hand);
                    if (est > highEstimate)
                    {
                        highEstimate = est;
                        highSuit = Suit.Unknown;
                    }
                }
            }

            //  bid alone if we think we'll take more than 4-tricks, so long as we hold the appropriate high cards
            if (highEstimate >= 4.25)
            {
                //  when aloneTake5 is true, we need to hold the Joker (if present) or the high Jack to bid alone
                if (options.aloneTake5 && hand.Any(c => options.withJoker ? c.rank == Rank.High : c.suit == highSuit && c.rank == Rank.Jack))
                    return new BidBase((int)EuchreBid.MakeAlone + (int)highSuit);

                //  when aloneTake5 is false, we need to hold one of the top cards (Joker or either Jack) to bid alone
                if (!options.aloneTake5 && hand.Any(c => EffectiveSuit(c, highSuit) == highSuit && (c.rank == Rank.Jack || c.rank == Rank.High)))
                    return new BidBase((int)EuchreBid.MakeAlone + (int)highSuit);
            }

            if (highEstimate >= (options.take4for1 ? 3.25 : 2.25))
                return new BidBase((int)EuchreBid.Make + (int)highSuit);

            //  if we're the dealer and this is the second round with stick-the-dealer enabled, we must bid
            if (isDealer && options.stickTheDealer && upCard == null)
                return new BidBase((int)EuchreBid.Make + (int)highSuit);

            return new BidBase(BidBase.Pass);
        }

        public override List<Card> SuggestDiscard(SuggestDiscardState<EuchreOptions> state)
        {
            var (player, hand) = (state.player, state.hand);

            if (player.Bid == (int)EuchreBid.GoUnder)
                return hand.OrderBy(IsTrump).ThenBy(RankSort).Take(3).ToList();

            //  discard the lowest card of the offsuit with the fewest cards (or the lowest card of trump)
            var offSuits = hand.Select(EffectiveSuit).Where(s => s != trump).Distinct().ToList();
            var lowSuitCount =
                offSuits.Select(s => new
                        { suit = s, count = hand.Count(c => EffectiveSuit(c) == s), lowRank = hand.Where(c => EffectiveSuit(c) == s).Min(RankSort) })
                    .OrderBy(sc => sc.count)
                    .ThenBy(sc => sc.lowRank)
                    .FirstOrDefault();
            var lowSuit = lowSuitCount?.suit ?? trump;
            var lowCard = hand.Where(c => EffectiveSuit(c) == lowSuit).OrderBy(RankSort).First();
            return new List<Card> { lowCard };
        }

        public override Card SuggestNextCard(SuggestCardState<EuchreOptions> state)
        {
            var (players, trick, legalCards, cardsPlayed, player, isPartnerTakingTrick, cardTakingTrick) = (new PlayersCollectionBase(this, state.players),
                state.trick, state.legalCards, state.cardsPlayed,
                state.player, state.isPartnerTakingTrick, state.cardTakingTrick);

            var playerBid = BidBid(player);
            var partnerIsMaker = players.PartnersOf(player).Any(p => BidBid(p) == EuchreBid.Make);
            var weAreMaker = playerBid == EuchreBid.Make || playerBid == EuchreBid.MakeAlone || partnerIsMaker;

            var lowestCard = legalCards.OrderBy(c => IsTrump(c) ? 1 : 0).ThenBy(RankSort).First();

            if (trick.Count == 0)
            {
                //  we're leading
                var sortedTrump = legalCards.Where(IsTrump).OrderBy(RankSort).ToList();

                //  Lead trump early in the game if your partner called it
                if (sortedTrump.Count > 0 && partnerIsMaker && cardsPlayed.Count(IsTrump) < 3)
                {
                    //  we have a trump to play, our partner is the maker, and not much trump has been played thus far:
                    //  lead our high trump if it's good or our low trump if not
                    var highTrump = sortedTrump.Last();
                    return IsCardHigh(highTrump, cardsPlayed) ? highTrump : sortedTrump.First();
                }

                //  Lead trump if you called it and have three or more trump
                if (sortedTrump.Count >= 3 && (playerBid == EuchreBid.Make || playerBid == EuchreBid.MakeAlone))
                {
                    //  we have three or more trump and we're the maker: lead our high trump if it's good or our low trump if not
                    var highTrump = sortedTrump.Last();
                    return IsCardHigh(highTrump, cardsPlayed) ? highTrump : sortedTrump.First();
                }

                //  Never lead your last trump unless you have the high remaining card in an off suit
                var nonTrump = legalCards.Where(c => !IsTrump(c)).ToList();
                if (sortedTrump.Count == 1 && nonTrump.All(c => !IsCardHigh(c, cardsPlayed)))
                {
                    //  get the number of cards in each non-trump suit
                    var countsBySuit = nonTrump.GroupBy(EffectiveSuit).ToDictionary(g => g.Key, g => g.Count());

                    //  play the lowest card in non-trump suit with fewest cards
                    return nonTrump.OrderBy(c => countsBySuit[EffectiveSuit(c)]).ThenBy(RankSort).First();
                }

                //  We want to lead a high (best in suit) card with conditions:
                //  if we have only one high card, play it only if it's not trump or we are the maker and have more than one trump remaining
                var highCards = legalCards.Where(c => IsCardHigh(c, cardsPlayed)).ToList();
                if (highCards.Count == 1 && (!IsTrump(highCards[0]) || weAreMaker && sortedTrump.Count > 1))
                    return highCards[0];

                //  if we have more than one high card, if we're the maker and one of the high cards is trump, lead it
                //  otherwise, play the non-trump high card from the suit with the fewest cards
                if (highCards.Count > 1)
                {
                    if (weAreMaker && highCards.Any(IsTrump))
                        return highCards.Single(IsTrump);

                    //  play the high non-trump card from the suit with fewest cards
                    var nonTrumpHighCards = highCards.Where(c => !IsTrump(c)).ToList();
                    var theSuit = nonTrumpHighCards.Select(EffectiveSuit).OrderBy(s => legalCards.Count(c => EffectiveSuit(c) == s)).ThenBy(s => suitOrder[s]).First();
                    return nonTrumpHighCards.Single(c => EffectiveSuit(c) == theSuit);
                }

                //  return the lowest card we have favoring non-trump
                return lowestCard;
            }

            var trickSuit = EffectiveSuit(trick[0]);
            var isLastToPlay = trick.Count + 1 == players.Count(p => p.IsActivelyPlaying);

            if (legalCards.Any(c => EffectiveSuit(c) == trickSuit))
            {
                //  we can follow suit
                if (trickSuit == trump || trick.All(c => EffectiveSuit(c) != trump))
                {
                    //  tricksuit is trump or trick contains no trump; does our best follow card win?
                    var highFollow = legalCards.Where(c => EffectiveSuit(c) == trickSuit).OrderBy(RankSort).Last();
                    if (isPartnerTakingTrick)
                    {
                        //  our partner is taking the trick
                        if (isLastToPlay)
                            //  we're last to play; let our partner have it
                            return lowestCard;

                        if (!IsCardHigh(cardTakingTrick, cardsPlayed.Concat(new Hand(player.Hand))) && IsCardHigh(highFollow, cardsPlayed))
                            //  partner might lose the trick, but we have the highest card; play it
                            return highFollow;
                    }
                    else if (!trick.Any(c => EffectiveSuit(c) == trickSuit && RankSort(c) > RankSort(highFollow)))
                    {
                        //  the other team is taking the trick, but our highest card will win

                        if (isLastToPlay)
                            //  we're last to play; play only as high as we need to
                            return legalCards.OrderBy(RankSort).First(c => EffectiveSuit(c) == trickSuit && RankSort(c) > RankSort(cardTakingTrick));

                        //  otherwise play our highest card
                        return highFollow;
                    }
                }

                //  we can't win, so play low
                return lowestCard;
            }

            //  we can't follow suit but we have trump
            if (legalCards.Any(IsTrump))
            {
                //  the trick already contains trump
                if (trick.Any(IsTrump))
                {
                    //  if partner trumped in; let them have it
                    if (isPartnerTakingTrick)
                        return lowestCard;

                    //  otherwise play just good enough to take the trick, if possible
                    var goodEnoughTrump = legalCards.OrderBy(RankSort).FirstOrDefault(c => IsTrump(c) && RankSort(c) > RankSort(cardTakingTrick));
                    if (goodEnoughTrump != null)
                        return goodEnoughTrump;
                }
                else if (!isPartnerTakingTrick || !isLastToPlay && !IsCardHigh(cardTakingTrick, cardsPlayed))
                {
                    //  trick does not contain trump; play our lowest trump if partner is not already winning with the high card
                    return legalCards.Where(IsTrump).OrderBy(RankSort).First();
                }
            }

            //  return the lowest card we have favoring non-trump
            return lowestCard;
        }

        //  used for call for best
        public override List<Card> SuggestPass(SuggestPassState<EuchreOptions> state)
        {
            var player = state.player;

            var hand = new Hand(player.Hand);
            if (IsMakeAlone(player))
            {
                //  if we bid alone, treat this like an extra discard
                return SuggestDiscard(new SuggestDiscardState<EuchreOptions> { player = player, hand = hand });
            }

            //  if partner bid, try to give our partner trump if we can
            var list = hand.Where(IsTrump).OrderByDescending(RankSort).Take(1).ToList();
            if (list.Count > 0)
                return list;

            //  otherwise give them our highest-ranked off-suit card, tie-breaking using the shortest suit
            var suitCounts = hand.GroupBy(EffectiveSuit).ToDictionary(g => g.Key, g => g.Count());
            return hand.OrderByDescending(RankSort).ThenBy(c => suitCounts[EffectiveSuit(c)]).Take(1).ToList();
        }

        public override int SuitSort(Card c)
        {
            var suitSort = suitOrder[c.suit];

            if (trump == Suit.Unknown)
                return suitSort;

            var trumpSort = suitOrder[trump];
            if (c.suit == Suit.Joker || c.rank == Rank.Jack && c.Color == Card.ColorOfSuit(trump))
                return trumpSort;

            return suitSort > trumpSort ? suitSort - 4 : suitSort;
        }

        protected override Suit EffectiveSuit(Card c, Suit trumpSuit)
        {
            return c.suit == Suit.Joker || c.rank == Rank.Jack && c.Color == Card.ColorOfSuit(trumpSuit) ? trumpSuit : c.suit;
        }

        protected override int RankSort(Card c, Suit trumpSuit)
        {
            if (c.suit == Suit.Joker)
                return (int)Rank.Ace + 3;

            if (c.rank == Rank.Jack && c.suit == trumpSuit)
                return (int)Rank.Ace + 2;

            if (c.rank == Rank.Jack && c.Color == Card.ColorOfSuit(trumpSuit))
                return (int)Rank.Ace + 1;

            return (int)c.rank;
        }

        private static EuchreBid BidBid(PlayerBase player)
        {
            return player == null ? (EuchreBid)BidBase.NotPlaying : BidBid(player.Bid);
        }

        private static EuchreBid BidBid(int bidValue)
        {
            return (EuchreBid)(bidValue - bidValue % 10);
        }

        private static Suit EffectiveUpCardSuit(SuitRank upCard)
        {
            return upCard.suit == Suit.Joker ? Suit.Spades : upCard.suit;
        }

        private static double EstimatedNotrumpTricks(Hand hand)
        {
            var est = 0.0;

            var countsBySuit = hand.GroupBy(c => c.suit).ToDictionary(g => g.Key, g => g.Count());

            foreach (var card in hand)
            {
                var count = countsBySuit[card.suit];

                if (card.rank == Rank.Ace && count <= 3)
                    est += 1;
                else if (card.rank == Rank.King && count == 2)
                    est += hand.Any(c => c.rank == Rank.Ace && c.suit == card.suit) ? 1 : 0.75;
            }

            return est;
        }

        private static bool IsMakeAlone(PlayerBase player)
        {
            return BidBid(player.Bid) == EuchreBid.MakeAlone;
        }

        private bool IsAskingDefendAlone(PlayersCollectionBase players)
        {
            return options.defendAlone && players.Any(p => p.Bid == BidBase.NotPlaying);
        }
    }
}