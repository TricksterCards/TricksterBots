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
                    case 20:
                        return options.withJoker ? DeckType.TenToAceAndJoker : DeckType.TenToAce;
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

            if (trumpCards.Count >= 3 &&
                offSuitCards.Any(c1 => offSuitCards.Count < 2 || offSuitCards.Any(c2 => c1 != c2 && EffectiveSuit(c2, maybeTrump) == EffectiveSuit(c1, maybeTrump))))
                //  if we're one/two-suited with three or more trump, add most of a trick
                est += 0.75;

            return est;
        }

        public override BidBase SuggestBid(SuggestBidState<EuchreOptions> state)
        {
            var (players, dealerSeat, hand, legalBids, player, upCard, upCardSuit) =
                (new PlayersCollectionBase(this, state.players), state.dealerSeat, state.hand, state.legalBids, state.player, state.upCard, state.upCardSuit);

            if (hand.All(c => c.suit == Suit.Unknown))
                return legalBids.FirstOrDefault(b => b.value == BidBase.Pass) ?? legalBids.First();

            if (IsAskingDefendAlone(players))
                return new BidBase(BidBase.Pass);

            var isDealer = player.Seat == dealerSeat;
            var isTeamDealing = isDealer || players.PartnersOf(player).Any(p => p.Seat == dealerSeat);

            //  always call a misdeal if offered
            if (legalBids.Any(b => b.value == (int)EuchreBid.CallMisdeal))
                return legalBids.Single(b => b.value == (int)EuchreBid.CallMisdeal);

            if (legalBids.Any(b => b.value == (int)EuchreBid.GoUnder))
            {
                //  go under if we can and don't have any suit we think we want to bid
                var bestBid = SuggestBid(hand, upCard, upCardSuit, isDealer, isTeamDealing, canPass: true);

                if (bestBid.value == BidBase.Pass && upCard != null)
                    bestBid = SuggestBid(hand, null, upCardSuit, isDealer, isTeamDealing, canPass: true);

                return legalBids.Single(b => b.value == (bestBid.value == BidBase.Pass ? (int)EuchreBid.GoUnder : (int)EuchreBid.AfterGoUnder));
            }

            var suggestion = SuggestBid(hand, upCard, upCardSuit, isDealer, isTeamDealing, legalBids.Any(b => b.value == BidBase.Pass));
            var canBidSuggestion = legalBids.Any(b => b.value == suggestion.value);

            if (!canBidSuggestion && IsAloneBid(suggestion))
            {
                suggestion = DowngradeAloneBid(suggestion);
                canBidSuggestion = legalBids.Any(b => b.value == suggestion.value);
            }

            return canBidSuggestion ? suggestion : legalBids.FirstOrDefault(b => b.value == BidBase.Pass) ?? legalBids.First();
        }

        private static bool IsAloneBid(BidBase bid)
        {
            return bid.value >= (int)EuchreBid.MakeAlone && bid.value < (int)EuchreBid.Defend;
        }

        private static BidBase DowngradeAloneBid(BidBase bid)
        {
            return !IsAloneBid(bid) ? bid : new BidBase(bid.value - (int)EuchreBid.MakeAlone + (int)EuchreBid.Make);
        }

        //  overload called above
        private BidBase SuggestBid(Hand hand, Card upCard, Suit upCardSuit, bool isDealer, bool isTeamDealing, bool canPass)
        {
            var highSuit = Suit.Unknown;
            var highEstimate = 0.0;

            if (upCard != null)
            {
                var effectiveHand = new Hand(hand);

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

            //  bid alone if we think we'll take at least 4-tricks, so long as we hold the appropriate high cards
            //  only require just under 3-tricks if call-for-best is on (as we'll likely get another trump from partner)
            //  and make sure to not go alone if the up-card is high and will be picked up by the opponents
            var upCardIsHigh = upCard != null && (options.withJoker ? upCard.rank == Rank.High : upCard.rank == Rank.Jack);
            var opponentsHaveHigh = upCardIsHigh && !isTeamDealing;
            if (!opponentsHaveHigh && highEstimate >= (options.callForBest && !options.aloneTake5 && !options.take4for1 ? 2.75 : 4.0))
            {
                //  when aloneTake5 is true, we need to hold the Joker (if present) or the high Jack to bid alone (plus high in each off-suit)
                if (options.aloneTake5 && hand.Any(c => options.withJoker ? c.rank == Rank.High : c.suit == highSuit && c.rank == Rank.Jack) && hand.All(c => EffectiveSuit(c, highSuit) == highSuit || hand.Any(h => h.rank == Rank.Ace && h.suit == c.suit)))
                    return new BidBase((int)EuchreBid.MakeAlone + (int)highSuit);

                //  when aloneTake5 is false, we need to be call-for-best or hold one of the top cards (Joker or either Jack) to bid alone
                if (!options.aloneTake5 && (options.callForBest || hand.Any(c => EffectiveSuit(c, highSuit) == highSuit && (c.rank == Rank.Jack || c.rank == Rank.High))))
                    return new BidBase((int)EuchreBid.MakeAlone + (int)highSuit);
            }

            if (highEstimate >= (options.take4for1 ? 3.25 : 2.25))
                return new BidBase((int)EuchreBid.Make + (int)highSuit);

            //  we don't have anything we like, so pass if we can or bid the best we've got
            return canPass ? new BidBase(BidBase.Pass) : new BidBase((int)EuchreBid.Make + (int)highSuit);
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
            var partners = players.PartnersOf(player);
            var partnerIsMaker = partners.Any(p => BidBid(p) == EuchreBid.Make);
            var weAreMaker = playerBid == EuchreBid.Make || playerBid == EuchreBid.MakeAlone || partnerIsMaker;
            var isDefending = !weAreMaker && !partnerIsMaker;
            var cardsPlayedPlusHand = cardsPlayed.Concat(new Hand(player.Hand));

            var lowestCard = legalCards.OrderBy(c => IsTrump(c) ? 1 : 0).ThenBy(RankSort).First();

            if (trick.Count == 0)
            {
                //  we're leading
                var sortedTrump = legalCards.Where(IsTrump).OrderBy(RankSort).ToList();

                //  Lead trump early in the game if your partner called it
                if (sortedTrump.Count > 0 && partnerIsMaker && cardsPlayed.Count(IsTrump) < 3)
                {
                    //  we have a trump to play, our partner is the maker, and not much trump has been played thus far:
                    //  lead our highest trump to help partner
                    return sortedTrump.Last();
                }

                //  Lead high trump with 2+ trump if alone and opponents have not taken any tricks yet
                if (sortedTrump.Count > 1 && playerBid == EuchreBid.MakeAlone &&
                    players.Opponents(player).All(p => p.HandScore == 0))
                {
                    return sortedTrump.Last();
                }

                //  Lead trump if you called it and have three or more trump
                if (sortedTrump.Count >= 3 && (playerBid == EuchreBid.Make || playerBid == EuchreBid.MakeAlone))
                {
                    //  we have three or more trump and we're the maker: lead our high trump if it's good or our low trump if not
                    var highTrump = sortedTrump.Last();
                    return IsCardHigh(highTrump, cardsPlayed) ? highTrump : sortedTrump.First();
                }

                //  Lead last trump if you called it, have already taken 3 tricks, and partner is void or not playing.
                //  Increases chances of taking all 5 tricks by forcing opponents to discard a high off-suit card.
                var alreadyMadeBid = weAreMaker && 3 <= player.HandScore + partners.Sum(p => p.HandScore);
                var partnersAreNotPlayingOrVoid = partners.All(p => p.Bid == BidBase.NotPlaying || players.TargetIsVoidInSuit(player, p, trump, cardsPlayed));
                if (sortedTrump.Count == 1 && alreadyMadeBid && partnersAreNotPlayingOrVoid)
                {
                    return sortedTrump.Last();
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

                //  lead our highest off-suit if we're alone, out of trump and opponents haven't taken a trick yet
                if (sortedTrump.Count == 0 && playerBid == EuchreBid.MakeAlone &&
                    players.Opponents(player).All(p => p.HandScore == 0))
                    return legalCards.OrderByDescending(RankSort).First();

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

                        if (!IsCardHigh(cardTakingTrick, cardsPlayedPlusHand) && IsCardHigh(highFollow, cardsPlayed))
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

            bool NeedToProtectOffJack()
            {
                if (!isDefending || isLastToPlay)
                    return false;

                // no need to protect if we don't have exactly two trump (more and we can still protect, less and we can't protect anyway)
                if (legalCards.Count(IsTrump) != 2)
                    return false;

                // it's only worth protecting the left if a guaranteed trick would be a stopper or the last trick to Euchre
                if (player.HandScore != 0 && player.HandScore != 2)
                    return false;

                // if we don't have the left or it's already high, there's nothing to protect
                var offJack = legalCards.FirstOrDefault(c => IsTrump(c) && c.rank == Rank.Jack && c.suit != trump);
                if (offJack == null || IsCardHigh(offJack, cardsPlayedPlusHand))
                    return false;

                // protect the left unless we know LHO is void in trump (so they can't over-trump us)
                if (players.LhoIsVoidInSuit(player, trump, cardsPlayed))
                    return false;

                return true;
            }

            //  we can't follow suit but we have trump (and don't need to protect the off jack)
            if (legalCards.Any(IsTrump) && !NeedToProtectOffJack())
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