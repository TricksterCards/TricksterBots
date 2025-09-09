using System;
using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    public class SpadesBot : BaseBot<SpadesOptions>
    {
        private static readonly Dictionary<Suit, int> suitOrder = new Dictionary<Suit, int>
        {
            { Suit.Unknown, 0 },
            { Suit.Diamonds, 1 },
            { Suit.Clubs, 2 },
            { Suit.Hearts, 3 },
            { Suit.Spades, 4 }
        };

        public SpadesBot(SpadesOptions options, Suit trumpSuit) : base(options, trumpSuit)
        {
        }

        private double EstimatedTricks(Hand hand)
        {
            var est = 0.0;
            var noJokers = DeckType != DeckType.SpadesWithJokers;

            var cardsOfTrump = hand.Where(IsTrump).OrderByDescending(RankSort).ToList();
            var offSuitCards = hand.Where(c => !IsTrump(c)).ToList();

            //  process voids, singletons, and doubletons
            double nTrump = cardsOfTrump.Count;
            foreach (var suitCount in SuitRank.stdSuits.Select(s => new { suit = s, count = offSuitCards.Count(c => EffectiveSuit(c) == s) }))
            {
                var maxLengthInSuit = 2 + (options.players == 3 ? 1 : 0);
                if (suitCount.suit != Suit.Spades && suitCount.count <= maxLengthInSuit)
                {
                    //  count 2 for each void, 1 for each singleton, and 0.25 for each doubleton (add 1 more if only 3 players)
                    var use = (suitCount.count == maxLengthInSuit ? 0.25 : maxLengthInSuit - suitCount.count);
                    var trumpIn = Math.Min(nTrump, use);
                    nTrump -= trumpIn;
                    est += trumpIn;
                }
            }

            //  keep the highest cards in trump not used to trump in (this assumes we get away with trumping-in low trump)
            cardsOfTrump = cardsOfTrump.Take((int)Math.Round(nTrump)).ToList();

            foreach (var card in cardsOfTrump)
            {
                var nCardsAbove = cardsOfTrump.Count(c => RankSort(c) > RankSort(card));
                var nCardsBelow = cardsOfTrump.Count(c => RankSort(c) < RankSort(card));
                var nRanksAbove = HighRankInSuit(card) - RankSort(card);
                var nGapsAbove = nRanksAbove - nCardsAbove;

                if (nCardsBelow >= nGapsAbove)
                    est += 1;
                else if (nCardsBelow == nGapsAbove - 1)
                    est += 0.75;
                else if (nCardsBelow == nGapsAbove - 2)
                    est += 0.25;
            }

            var offSuitAces = offSuitCards.Where(c => c.rank == Rank.Ace);
            foreach (var nCardsOfAceSuit in offSuitAces.Select(ace => offSuitCards.Count(c => EffectiveSuit(c) == ace.suit)))
            {
                var maxLengthInSuit = 5 + (noJokers ? 1 : 0) + (options.players == 3 ? 1 : 0);
                if (nCardsOfAceSuit <= maxLengthInSuit)
                    est += 1;
                else
                    est += 0.5;
            }

            var offSuitKings = offSuitCards.Where(c => c.rank == Rank.King);
            foreach (var nCardsOfKingSuit in offSuitKings.Select(king => offSuitCards.Count(c => EffectiveSuit(c) == king.suit)))
            {
                if (nCardsOfKingSuit > 1)
                {
                    var maxLengthInSuit = 4 + (noJokers ? 1 : 0) + (options.players == 3 ? 1 : 0);
                    if (nCardsOfKingSuit <= maxLengthInSuit)
                        est += 1;
                    else
                        est += 0.25;
                }
            }

            if (noJokers || options.players == 3)
            {
                //  add some value for Queens if we're not playing with Jokers (or we only have three players)
                var offSuitQueens = offSuitCards.Where(c => c.rank == Rank.Queen);
                foreach (var nCardsOfQueenSuit in offSuitQueens.Select(queen => offSuitCards.Count(c => EffectiveSuit(c) == queen.suit)))
                {
                    var maxLengthInSuit = 4 + (noJokers && options.players == 3 ? 1 : 0);
                    if (nCardsOfQueenSuit > 2 && nCardsOfQueenSuit <= maxLengthInSuit)
                        est += (noJokers && options.players == 3) ? 1 : 0.5;
                }
            }

            if (noJokers && options.players == 3)
            {
                //  add some value for Jacks if we only have three players and we're not playing with Jokers
                var offSuitJacks = offSuitCards.Where(c => c.rank == Rank.Jack);
                foreach (var nCardsOfJackSuit in offSuitJacks.Select(queen => offSuitCards.Count(c => EffectiveSuit(c) == queen.suit)))
                {
                    if (nCardsOfJackSuit > 3 && nCardsOfJackSuit <= 5)
                        est += 0.5;
                }
            }

            return est;
        }

        private bool TryNilBid(List<Card> hand, double est, out BidBase bid)
        {
            bid = null;

            //  bid nil if we have a near-zero estimate, we don't have the highest card any suit
            var maxEstForNil = (options.variation == SpadesVariation.Suicide ? 7.0 : 1.0) + options.nilPass;
            var passable = options.nilPass;

            var highestCards = hand.Where(c => RankSort(c) == HighRankInSuit(c)).ToList();
            if (highestCards.Count > passable)
                return false;

            passable -= highestCards.Count;
            hand = hand.Where(c => !highestCards.Contains(c)).ToList();

            if (hand.Count(IsTrump) - passable >= 4)
                return false;

            var nTrumpToPass = hand.Count(IsTrump) - 3;
            if (nTrumpToPass > passable)
                return false;

            if (nTrumpToPass > 0)
            {
                var trumpToPass = hand.Where(IsTrump).OrderByDescending(RankSort).Take(nTrumpToPass);
                passable -= nTrumpToPass;
                hand = hand.Where(c => !trumpToPass.Contains(c)).ToList();
            }

            if (est < maxEstForNil)
            {
                //  and we don't have any unprotected high cards in any suit (e.g. K23 is good, but not K or K2)
                if (SuitRank.stdSuits.All(s =>
                {
                    var cardsInSuit = hand.Where(c => EffectiveSuit(c) == s).ToList();

                    if (cardsInSuit.Count == 0)
                        return true;

                    var nLowCards = cardsInSuit.Count(c => RankSort(c) < RankSort(new Card(c.suit, Rank.Queen)));
                    var nHighCards = cardsInSuit.Count(c => RankSort(c) >= RankSort(new Card(c.suit, Rank.Queen)));

                    if (nLowCards < nHighCards && passable >= nHighCards - nLowCards)
                    {
                        passable -= nHighCards - nLowCards;
                        return true;
                    }

                    return nLowCards >= nHighCards;
                }))
                {
                    bid = new SpadesBid(0, false, options.nilOrZero == SpadesNilOrZero.Zero);
                    return true;
                }
            }

            return false;
        }

        private int MaxTricks => DeckBuilder.DeckSize(DeckType) / options.players;

        public override BidBase SuggestBid(SuggestBidState<SpadesOptions> state)
        {
            var (players, player, hand) = (new PlayersCollectionBase(this, state.players), state.player, state.hand);

            var biddableBids = state.legalBids.Where(b => b.value != BidBase.NoBid).OrderBy(b => new SpadesBid(b).Tricks).ToList();

            if (biddableBids.Count < 1)
                throw new Exception("No biddable bids");

            if (biddableBids.Count == 1)
                return biddableBids.First();

            //  if we're offered a show bid, take it (never bid blind nil)
            if (biddableBids.Any(b => new SpadesBid(b).IsShowHand))
                return biddableBids.First(b => new SpadesBid(b).IsShowHand);

            //  if we're offered a pass bid, take it (never bid blind anything)
            if (biddableBids.Any(b => b.value == BidBase.Pass))
                return biddableBids.First(b => b.value == BidBase.Pass);

            var est = EstimatedTricks(hand);

            //  try to bid Nil if the biddable bids include Nil, partner didn't already bid nil, and noone has bid Blind Nil
            var partner = players.PartnerOf(player);
            var partnerBidNil = partner != null && partner.Bid != BidBase.NoBid && new SpadesBid(partner.Bid).IsNil;
            if (biddableBids.Any(b => new SpadesBid(b).IsNil))
            {
                var someoneBidBlindNil = players.Any(p => p.Bid != BidBase.NoBid && new SpadesBid(p.Bid).IsBlindNil);
                if (!partnerBidNil && !someoneBidBlindNil && TryNilBid(hand, est, out var bid))
                    return bid;
            }

            if ((partnerBidNil || partner?.Bid == BidBase.NoBid) && options.nilPass > 0)
                est += options.nilPass / 2.0;

            if (options.variation == SpadesVariation.Whiz)
            {
                //  in Whiz, if we didn't suggest Nil above, suggest the one non-nil bid or Nil if that's all we have
                return biddableBids.FirstOrDefault(b => new SpadesBid(b).IsNotNil) ?? biddableBids.First();
            }

            if (IsPartnership && options.variation == SpadesVariation.Suicide)
            {
                if (est < 7)
                    est = Math.Ceiling(est + 1);  // add 1 and round up in partnership suicide because with 2 Nil bidders, we're going to take more
            }

            var maxBid = options.tenForTwoHundred ? Math.Min(10, MaxTricks) : MaxTricks;
            if (IsPartnership)
            {
                if (partner != null && partner.Bid != BidBase.NoBid)
                    maxBid -= (new SpadesBid(partner.Bid).Tricks);
            }

            var bidValue = Math.Max(1, Math.Min(maxBid, (int)Math.Floor(est)));

            //  find the legal bid object matching our bid value (or one less than our bid value)
            var theBid = biddableBids.SingleOrDefault(b => new SpadesBid(b).Tricks == bidValue)
                         ?? biddableBids.SingleOrDefault(b => new SpadesBid(b).Tricks == (bidValue == 1 ? bidValue + 1 : bidValue - 1));

            //  ensure we return a legal bid
            return theBid ?? biddableBids.FirstOrDefault(b => new SpadesBid(b).IsNotNil) ?? biddableBids.First();
        }

        public override List<Card> SuggestDiscard(SuggestDiscardState<SpadesOptions> state)
        {
            throw new NotImplementedException();
        }

        private bool RankBelowQueen(Card c)
        {
            return RankSort(c) < RankSort(new Card(c.suit, Rank.Queen));
        }

        private int TargetHighRankInSuit(PlayerBase target, Suit suit, IEnumerable<Card> knownCards)
        {
            //  protected cards are those played below a card we now know to be "boss" - find the lowest one
            var lowProtectedCard = target.PlayedCards?.Where(pc => suit == EffectiveSuit(pc.CardPlayed) && (EffectiveSuit(pc.HighInTrick) != EffectiveSuit(pc.CardPlayed) || IsCardHigh(pc.HighInTrick, knownCards)))
                .Select(pc => pc.CardPlayed)
                .OrderBy(RankSort)
                .FirstOrDefault();

            var lowProtectedCardRank = lowProtectedCard == null ? 1000 : RankSort(lowProtectedCard);

            //  thus the highest remaining rank must be below our lowest protected card
            var highRemainingCard = DeckBuilder.BuildDeck(DeckType).Where(c => EffectiveSuit(c) == suit && RankSort(c) < lowProtectedCardRank && !knownCards.Any(kc => EffectiveSuit(kc) == suit && RankSort(c) == RankSort(kc)))
                    .OrderByDescending(RankSort)
                    .FirstOrDefault();

            return highRemainingCard == null ? 0 : RankSort(highRemainingCard);
        }

        private int CountGapsBetweenCards(Card high, Card low, IEnumerable<Card> knownCards)
        {
            var suit = EffectiveSuit(high);
            var highRank = RankSort(high);
            var lowRank = RankSort(low);

            if (highRank < lowRank)
                return 0;

            return highRank - lowRank - 1 - knownCards.Count(c => EffectiveSuit(c) == suit && highRank > RankSort(c) && lowRank < RankSort(c));
        }

        private Card TryProtectNil(PlayerBase player, IReadOnlyList<Card> trick, IReadOnlyList<Card> legalCards, PlayersCollectionBase players, IReadOnlyList<Card> cardsPlayed)
        {
            Card suggestion = null;
            var partner = players.PartnerOf(player);
            var nPlayers = players.Count;

            if (trick.Count < nPlayers / 2)
            {
                //  i'm playing before my partner
                if (trick.Count == 0)
                {
                    var partnerVoidSuits = new Dictionary<Suit, bool>();

                    foreach (var suit in SuitRank.stdSuits)
                    {
                        partnerVoidSuits[suit] = players.PartnerIsVoidInSuit(player, new Card {rank = Rank.Ace, suit = suit}, cardsPlayed);
                    }

                    //  prefer leading our highest spade if possible and partner is not void, then a boss off-suit card, then low in a suit partner is void in
                    suggestion = legalCards.Where(c => IsTrump(c) && !partnerVoidSuits[EffectiveSuit(c)]).OrderByDescending(RankSort).FirstOrDefault()
                                 ?? legalCards.Where(c => IsCardHigh(c, cardsPlayed)).OrderByDescending(RankSort).FirstOrDefault()
                                 ?? legalCards.Where(c => partnerVoidSuits[EffectiveSuit(c)]).OrderBy(RankSort).FirstOrDefault();

                    if (suggestion == null)
                    {
                        //  no suggestion yet, look for a card we know to be higher than partner's highest
                        var knownCards = new Hand(player.Hand).Concat(cardsPlayed).ToList();
                        var partnerHighRankBySuit = new Dictionary<Suit, int>();

                        foreach (var suit in SuitRank.stdSuits)
                        {
                            partnerHighRankBySuit[suit] = TargetHighRankInSuit(partner, suit, knownCards);
                        }

                        //  lead the lowest card we know is higher than partner's highest in a suit before falling back to just the highest legal card we have
                        suggestion = legalCards.Where(c => RankSort(c) > partnerHighRankBySuit[EffectiveSuit(c)]).OrderBy(RankSort).FirstOrDefault()
                                     ?? legalCards.OrderByDescending(RankSort).First();
                    }
                }
                else if (EffectiveSuit(legalCards[0]) == EffectiveSuit(trick[0]))
                {
                    //  we can follow suit: play our highest card if we can still win (unless there are no gaps between winning card and our highest or partner is known to be below)
                    if (IsTrump(trick[0]) || (trick.All(c => !IsTrump(c)) && !players.PartnerIsVoidInSuit(player, trick[0], cardsPlayed)))
                    {
                        var hand = new Hand(player.Hand);
                        var knownCards = hand.Concat(cardsPlayed).ToList();

                        var highCardInHand = hand.Where(c => EffectiveSuit(c) == EffectiveSuit(trick[0])).OrderByDescending(RankSort).First();
                        var highCardInTrick = trick.Where(c => EffectiveSuit(c) == EffectiveSuit(trick[0])).OrderByDescending(RankSort).First();

                        //  verify there are no gaps between the winning card and our highest before trying to play above it
                        if (CountGapsBetweenCards(highCardInHand, highCardInTrick, knownCards) > 0)
                        {
                            //  don't play high if we know partner can already get under the highest card in the trick
                            if (RankSort(highCardInTrick) < TargetHighRankInSuit(partner, EffectiveSuit(trick[0]), knownCards))
                            {
                                suggestion = highCardInHand;
                            }
                        }
                    }
                }
                else if (legalCards.Any(IsTrump))
                {
                    //  trump in if partner hasn't played, the highest card in the trick is not boss, and partner is not void in this suit or known to be below the high card
                    if (trick.All(c => !IsTrump(c)) && !players.PartnerIsVoidInSuit(player, trick[0], cardsPlayed) && !trick.Any(c => IsCardHigh(c, cardsPlayed)))
                    {
                        var hand = new Hand(player.Hand);
                        var knownCards = hand.Concat(cardsPlayed).ToList();

                        //  don't trump in if we know the highest card in the trick is above partner's highest card
                        var highCardInTrick = trick.Where(c => EffectiveSuit(c) == EffectiveSuit(trick[0])).OrderByDescending(RankSort).First();
                        if (RankSort(highCardInTrick) < TargetHighRankInSuit(partner, EffectiveSuit(trick[0]), knownCards))
                        {
                            suggestion = legalCards.Where(IsTrump).OrderBy(RankSort).First();
                        }
                    }
                }
            }
            else if (TrickHighCardIndex(trick) == trick.Count - nPlayers / 2)
            {
                //  partner taking the trick but he/she bid nil - we better try and take it
                var partnersCard = trick[trick.Count - nPlayers / 2];
                suggestion = legalCards.Where(c => EffectiveSuit(c) == EffectiveSuit(partnersCard) && RankSort(c) > RankSort(partnersCard)).OrderBy(RankSort).FirstOrDefault() ?? legalCards.Where(IsTrump).OrderBy(RankSort).FirstOrDefault();
            }

            //  return either the suggestion, the lowest non-trump we can, or the lowest trump we can
            return suggestion ?? legalCards.Where(c => !IsTrump(c)).OrderBy(RankSort).FirstOrDefault() ?? legalCards.OrderBy(RankSort).First();
        }


        //  we're trying not to take the trick
        private Card TryDumpEm(IReadOnlyList<Card> trick, IReadOnlyList<Card> legalCards, int nPlayers, bool stillNeedToMakeBid = false, bool playSafe = true)
        {
            Card suggestion;

            if (trick.Count == 0)
            {
                //  lead the absolute lowest card we can
                suggestion = legalCards.OrderBy(RankSort).First();
            }
            else if (EffectiveSuit(legalCards[0]) == EffectiveSuit(trick[0]))
            {
                //  we can follow suit: dump the highest card that won't take the trick
                if (trick.Any(IsTrump) && !IsTrump(trick[0]))
                {
                    //  if the lead suit was not Spades and there are Spades in the trick, just dump our highest card
                    //  (unless we're still trying to take tricks in which case keep queen and above)
                    suggestion = !stillNeedToMakeBid ? legalCards.OrderByDescending(RankSort).First() : legalCards.Where(RankBelowQueen).OrderByDescending(RankSort).FirstOrDefault();
                }
                else
                {
                    //  otherwise dump the highest card below the card that is currently taking the trick 
                    var trickTakerRank = trick.Where(c => EffectiveSuit(c) == EffectiveSuit(trick[0])).Max(RankSort);
                    suggestion = legalCards.Where(c => RankSort(c) < trickTakerRank).OrderByDescending(RankSort).FirstOrDefault();

                    //  else if we can't get below the highest card and we're playing last, just play our highest card
                    if (suggestion == null && (!playSafe || trick.Count == nPlayers - 1))
                    {
                        suggestion = legalCards.OrderByDescending(RankSort).First();
                    }
                }
            }
            else if (stillNeedToMakeBid)
            {
                suggestion = legalCards.Where(c => !IsTrump(c) && RankBelowQueen(c)).OrderByDescending(RankSort).FirstOrDefault() ?? legalCards.Where(c => !IsTrump(c)).OrderBy(RankSort).FirstOrDefault();
            }
            else if (trick.Any(IsTrump))
            {
                //  we can't follow suit but the trick contains trump: dump the highest trump that won't take the trick or the highest non-trump we have
                var maxTrumpInTrick = trick.Where(IsTrump).Max(RankSort);
                suggestion = legalCards.Where(c => IsTrump(c) && RankSort(c) < maxTrumpInTrick).OrderByDescending(RankSort).FirstOrDefault() ?? legalCards.Where(c => !IsTrump(c)).OrderByDescending(RankSort).FirstOrDefault();
            }
            else
            {
                //  we can't follow suit and the trick does not contain trump: dump the highest non-trump we have or the highest trump if that's all we have left
                suggestion = legalCards.Where(c => !IsTrump(c)).OrderByDescending(RankSort).FirstOrDefault() ?? legalCards.OrderByDescending(RankSort).FirstOrDefault();
            }

            //  return either the suggestion, the lowest non-trump we can, or the lowest trump we can
            return suggestion ?? legalCards.Where(c => !IsTrump(c)).OrderBy(RankSort).FirstOrDefault() ?? legalCards.OrderBy(RankSort).First();
        }

        //  we're trying to take a trick
        private Card TryTakeEm(PlayerBase player, IReadOnlyList<Card> trick, IReadOnlyList<Card> legalCards, IReadOnlyList<Card> cardsPlayed, PlayersCollectionBase players, bool isPartnerTakingTrick, Card cardTakingTrick, PlayerBase trickTaker)
        {
            Card suggestion = null;

            if (trick.Count == 0)
            {
                //  we're leading

                var isLhoVoidInSuit = new Dictionary<Suit, bool>();
                var isRhoVoidInSuit = new Dictionary<Suit, bool>();
                var isPartnerVoidInSuit = new Dictionary<Suit, bool>();

                foreach (var suit in SuitRank.stdSuits)
                {
                    var card = new Card(suit, Rank.Ace);
                    isLhoVoidInSuit[suit] = players.LhoIsVoidInSuit(player, card, cardsPlayed);
                    isRhoVoidInSuit[suit] = players.RhoIsVoidInSuit(player, card, cardsPlayed);
                    isPartnerVoidInSuit[suit] = players.PartnerIsVoidInSuit(player, card, cardsPlayed);
                }

                if (!isRhoVoidInSuit[trump])
                {
                    //  RHO may still have trump: try to avoid suits where RHO is known to be void
                    var avoidSuits = SuitRank.stdSuits.Where(s => isRhoVoidInSuit[s]).ToList();
                    var preferredLegalCards = legalCards.Where(c => !avoidSuits.Contains(EffectiveSuit(c))).ToList();
                    if (preferredLegalCards.Count > 0)
                        legalCards = preferredLegalCards;
                }

                if (!isLhoVoidInSuit[trump])
                {
                    //  LHO may still have trump: try to avoid suits where LHO is known to be void unless partner is also void and may have spades
                    var avoidSuits = SuitRank.stdSuits.Where(s => isLhoVoidInSuit[s] && (isPartnerVoidInSuit[trump] || !isPartnerVoidInSuit[s])).ToList();
                    var preferredLegalCards = legalCards.Where(c => !avoidSuits.Contains(EffectiveSuit(c))).ToList();
                    if (preferredLegalCards.Count > 0)
                        legalCards = preferredLegalCards;
                }

                var cards = legalCards;
                var bossCards = legalCards.Where(c => IsCardHigh(c, cardsPlayed)).OrderByDescending(c => cards.Count(c1 => EffectiveSuit(c1) == EffectiveSuit(c))).ToList();
                if (bossCards.Count > 0)
                {
                    //  consider leading our "boss" cards favoring boss in our longest suit
                    suggestion = bossCards.First();
                }
                else if (!isPartnerVoidInSuit[trump])
                {
                    //  partner may still have trump: try to lead a suit where partner is known to be void
                    var preferSuits = SuitRank.stdSuits.Where(s => isPartnerVoidInSuit[s]).ToList();
                    var preferredLegalCards = legalCards.Where(c => preferSuits.Contains(EffectiveSuit(c))).ToList();
                    if (preferredLegalCards.Count > 0)
                        legalCards = preferredLegalCards;
                }
            }
            else if (trick.Count == players.Count - 1 && !isPartnerTakingTrick && new SpadesBid(trickTaker.Bid).IsNil && trickTaker.HandScore == 0)
            {
                //  we're last to play and an opponent nil bidder is taking the trick: fall-through to play low and try to get under them
                //  this is opportunistic nil-busting as we only get here when focusing more on our own bid than the nil
            }
            else if (EffectiveSuit(legalCards[0]) == EffectiveSuit(trick[0]))
            {
                //  we can follow suit

                if (IsTrump(trick[0]) || trick.All(c => !IsTrump(c)))
                {
                    //  either it's a trump-led trick or there's no trump in the trick

                    if (isPartnerTakingTrick && (trick.Count == players.Count - 1 || IsCardHigh(cardTakingTrick, cardsPlayed.Concat(new Hand(player.Hand)))))
                    {
                        //  our partner is taking the trick and we're either the last to play or they are taking it with "boss" - don't try to take it
                    }
                    else if (trick.Count == players.Count - 1)
                    {
                        //  we're the last to play: just play good enough to take the trick
                        var highestInTrick = trick.Where(c => EffectiveSuit(c) == EffectiveSuit(trick[0])).Max(RankSort);
                        suggestion = legalCards.Where(c => RankSort(c) > highestInTrick).OrderBy(RankSort).FirstOrDefault();
                    }
                    else
                    {
                        //  we're not the last to play: if our best card is "boss," play it
                        var highCard = legalCards.OrderBy(RankSort).Last();

                        if (IsCardHigh(highCard, cardsPlayed))
                            suggestion = highCard;

                        //  in next-to-last seat in a partnership game, play our highest card (but lowest equivalent) if better than what's currently winning
                        //  (and not effectively the same as partner's card)
                        else if (IsPartnership && trick.Count == players.Count - 2 && RankSort(highCard) > RankSort(cardTakingTrick))
                        {
                            var knownCards = cardsPlayed.Concat(legalCards).ToList();
                            if (!isPartnerTakingTrick || !IsCardEffectivelyTheSame(highCard, cardTakingTrick, knownCards))
                                suggestion = legalCards.Where(c => IsCardEffectivelyTheSame(c, highCard, knownCards)).OrderBy(RankSort).First();
                        }
                    }
                }
            }
            else if (legalCards.Any(IsTrump))
            {
                //  we can't follow suit but we have trump

                if (IsPartnership && trick.Count == 1 && players.LhoIsVoidInSuit(player, trick[0], cardsPlayed))
                {
                    //  second to play and left hand opponent is void in the led suit - don't trump-in; leave it to our partner
                }
                else if (isPartnerTakingTrick && (trick.Count == players.Count - 1 || IsCardHigh(cardTakingTrick, cardsPlayed.Concat(new Hand(player.Hand)))))
                {
                    //  our partner is taking the trick and we're either the last to play or they are taking it with "boss" - don't trump in
                }
                else if (trick.All(c => !IsTrump(c)))
                {
                    //  no trump currently in the trick: play our lowest trump
                    suggestion = legalCards.Where(IsTrump).OrderBy(RankSort).First();
                }
                else
                {
                    //  trump currently in the trick: play high enough to beat what's in the trick already (if we can)
                    var highestTrumpInTrick = trick.Where(IsTrump).Max(RankSort);
                    suggestion = legalCards.Where(c => IsTrump(c) && RankSort(c) > highestTrumpInTrick).OrderBy(RankSort).FirstOrDefault();
                }
            }

            //  if we generated a suggestion, return it
            if (suggestion != null)
                return suggestion;

            //  else, if we only have trump, return the lowest
            if (legalCards.All(IsTrump))
                return legalCards.OrderBy(RankSort).First();

            //  else, dump the lowest card from the weakest suit
            return LowestCardFromWeakestSuit(legalCards, cardsPlayed);
        }

        private Card LowestCardFromWeakestSuit(IReadOnlyList<Card> legalCards, IReadOnlyList<Card> cardsPlayed)
        {
            var nonTrumpCards = legalCards.Where(c => !IsTrump(c)).ToList();

            //  if we only have trump, dump the lowest trump
            if (nonTrumpCards.Count == 0)
                return legalCards.OrderBy(RankSort).First();

            var suitCounts = nonTrumpCards.GroupBy(EffectiveSuit).Select(g => new { suit = g.Key, count = g.Count() }).ToList();

            //  try to ditch a singleton that's not "boss" and whose suit has the most outstanding cards
            var bestSingletonSuitCount = suitCounts.Where(sc => sc.count == 1).Where(sc => !IsCardHigh(nonTrumpCards.Single(c => EffectiveSuit(c) == sc.suit), cardsPlayed)).OrderBy(sc => cardsPlayed.Count(c => EffectiveSuit(c) == sc.suit)).FirstOrDefault();

            if (bestSingletonSuitCount != null)
                return nonTrumpCards.Single(c => EffectiveSuit(c) == bestSingletonSuitCount.suit);

            //  now we look at doubletons in the order of the number of remaining cards in the suit
            var doubletonSuitCounts = suitCounts.Where(sc => sc.count == 2).OrderBy(sc => cardsPlayed.Count(c => EffectiveSuit(c) == sc.suit)).ToList();

            foreach (var sc in doubletonSuitCounts)
            {
                var cards = nonTrumpCards.Where(c => EffectiveSuit(c) == sc.suit).OrderBy(RankSort).ToList();

                if (IsCardHigh(cards[1], cardsPlayed) && cards[0].rank < cards[1].rank - 1)
                {
                    //  okay to ditch from this doubleton where the high card is "boss" and the card below it isn't adjacent
                    return cards[0];
                }

                if (cards.All(c => c.rank != Rank.King))
                {
                    //  okay to ditch from this doubleton that doesn't contain a king we might be able to make "boss"
                    return cards[0];
                }
            }

            //  return the lowest card from the longest non-trump suit
            return nonTrumpCards.OrderByDescending(c => nonTrumpCards.Count(c1 => EffectiveSuit(c1) == c.suit)).ThenBy(RankSort).First();
        }

        private int CountSureTricks(Hand hand, IReadOnlyList<Card> cardsPlayed)
        {
            //  Our "sure tricks" are trump we'll take no matter how we play our cards.
            //  When multiple cards combine to form a single "sure trick" we count the lowest one,
            //  but during play, any uncounted higher-ranked card may actually take the "sure trick"

            //  Examples:
            //  A, K, _, J, _, _, _ -> 2 sure tricks (A and K)
            //  A, _, Q, J, _, _, _ -> 2 sure tricks (A and J)
            //  A, _, Q, _, T, 9, _ -> 2 sure tricks (A and 9)
            //  A, _, Q, _, T, 9, 8 -> 3 sure tricks (A, 9, and 8)

            var myTrump = hand.Where(IsTrump).ToList();
            var knownTrump = cardsPlayed.Where(IsTrump).Concat(myTrump).ToList();

            //  we start by expecting to find the highest ranked trump
            var nextRank = HighRankInSuit(trump);
            var lostTricks = 0;
            var sureTricks = 0;

            //  then we walk known trump from high to low
            foreach (var t in knownTrump.OrderByDescending(RankSort))
            {
                //  gaps between known trump are tricks we'll lose
                var rank = RankSort(t);
                lostTricks += nextRank - rank;

                //  if the current known trump is in our hand
                if (myTrump.Any(c => RankSort(c) == rank))
                {
                    if (lostTricks <= 0)
                        sureTricks++; //  then it's a "sure trick" if we're out of lost tricks
                    else
                        lostTricks--; //  otherwise "spend" it against a lost trick (if any)
                }

                //  then move to the next known trump looking for the next expected rank
                nextRank = rank - 1;
            }

            return sureTricks;
        }

        private static bool ShouldTryToBustNil(PlayersCollectionBase players, PlayerBase player)
        {
            if (!players.Opponents(player).Any(p => new SpadesBid(p.Bid).IsNil && p.HandScore == 0))
                return false;

            var partner = players.PartnerOf(player);
            var teamBid = new SpadesBid(player.Bid).Tricks + (partner != null ? new SpadesBid(partner.Bid).Tricks : 0);
            var totalBid = players.Sum(p => new SpadesBid(p.Bid).Tricks);

            return teamBid < 9 && (totalBid < 12 || teamBid < 6);
        }

        public override Card SuggestNextCard(SuggestCardState<SpadesOptions> state)
        {
            return SuggestNextCard(state, false);
        }

        private Card SuggestNextCard(SuggestCardState<SpadesOptions> state, bool recallingForPartnerNil)
        {
            var (players, trick, legalCards, cardsPlayed, player, isPartnerTakingTrick, cardTakingTrick, trickTaker) = (new PlayersCollectionBase(this, state.players),
                state.trick, state.legalCards, state.cardsPlayed, state.player, state.isPartnerTakingTrick && !recallingForPartnerNil, state.cardTakingTrick, state.trickTaker);

            var nPlayers = players.Count;
            var partner = recallingForPartnerNil ? null : players.PartnerOf(player);

            if (legalCards.Count == 0)
                return null;

            //  if there's only one card, play it
            if (legalCards.Count == 1)
                return legalCards[0];

            //  look for our team's nil bids
            var playerBid = new SpadesBid(player.Bid);

            //  always try to take tricks if "first hand bids itself"
            if (playerBid.IsNoBid)
                return TryTakeEm(player, trick, legalCards, cardsPlayed, players, isPartnerTakingTrick, cardTakingTrick, trickTaker);

            if (partner != null)
            {
                //  try to make my own nil bid first
                if (playerBid.IsNil && player.HandScore == 0)
                    return TryDumpEm(trick, legalCards, nPlayers);

                //  then try to protect my partner's nil bid
                var partnerBid = new SpadesBid(partner.Bid);
                if (partnerBid.IsNil && partner.HandScore == 0)
                    return TryProtectNil(player, trick, legalCards, players, cardsPlayed);

                //  but play for myself if my partner blew their nil bid
                if (partnerBid.IsNil)
                    return SuggestNextCard(state, true);
            }

            var hand = new Hand(player.Hand);
            var sureTricks = CountSureTricks(hand, cardsPlayed);
            var tricksLeft = hand.Count;
            var shouldTryToMakeBid = ShouldTryToMakeBid(player, partner, sureTricks, tricksLeft);

            //  an opponent bid nil and we should try to set them: do it
            if (ShouldTryToBustNil(players, player))
                return TryBustNil(player, trick, legalCards, cardsPlayed, players, cardTakingTrick, shouldTryToMakeBid, isPartnerTakingTrick);

            //  we bid nil: minimize our own tricks, even if we've already blown it
            if (playerBid.IsNil)
                return TryDumpEm(trick, legalCards, nPlayers);

            //  noone bid nil or all nils have been blown: play based on whether we need tricks
            return (shouldTryToMakeBid || ShouldTryToSetOpponents(player, partner, players, sureTricks, tricksLeft)) ? TryTakeEm(player, trick, legalCards, cardsPlayed, players, isPartnerTakingTrick, cardTakingTrick, trickTaker) : TryDumpEm(trick, legalCards, players.Count);

        }

        private bool ShouldTryToMakeBid(PlayerBase player, PlayerBase partner, int sureTricks, int tricksLeft)
        {
            //  figure out how many tricks we need to make our bid
            var tricksBid = new SpadesBid(player.Bid).Tricks + (partner != null ? new SpadesBid(partner.Bid).Tricks : 0);
            var taken = player.HandScore + (partner?.HandScore ?? 0);
            var need = tricksBid - taken;

            //  TODO: be willing to lose all remaining tricks if opponents win the game by making their bid (and we think we can get them to roll over bags)

            //  try to make our bid if we haven't already made it and there are still enough tricks left
            //  treat "sure tricks" like we already have them since we know we'll get them anyway
            return need - sureTricks > 0 && tricksLeft >= need;
        }

        private bool ShouldTryToSetOpponents(PlayerBase player, PlayerBase partner, PlayersCollectionBase players, int sureTricks, int tricksLeft)
        {
            //  TODO: try setting opponents in a solo/cutthroat game (probably in more limited scenarios)
            if (!IsPartnership)
                return false;

            var playerBid = new SpadesBid(player.Bid);
            var partnerBid = partner != null ? new SpadesBid(partner.Bid) : null;

            var lho = players.Lho(player);
            var lhoBid = new SpadesBid(lho.Bid);
            var rho = players.Rho(player);
            var rhoBid = new SpadesBid(rho.Bid);

            if (options.playTo == PlayTo.Score)
            {
                //  don't try to set the opponents if we'll win just by making our bid
                //  includes adjusting for potential points won/lost from Nil bids
                var ownPoints = playerBid.IsNotNil ? 10 * playerBid.Tricks : -100;
                var partnerPoints = partner == null ? -100 : (partnerBid.IsNotNil ? 10 * partnerBid.Tricks : (partner.HandScore > 0 ? -100 : 100));
                var potentialTeamScore = player.GameScore + ownPoints + partnerPoints;
                var lhoPoints = lhoBid.IsNotNil ? 10 * lhoBid.Tricks : (lho.HandScore > 0 ? -100 : 100);
                var rhoPoints = rhoBid.IsNotNil ? 10 * rhoBid.Tricks : (rho.HandScore > 0 ? -100 : 100);
                var potentialOpponentScore = lho.GameScore + lhoPoints + rhoPoints;
                if (potentialTeamScore >= options.gameOverScore && potentialOpponentScore < potentialTeamScore)
                    return false;
            }

            //  TODO: be willing to take any number of bags if opponents win the game by making their bid

            //  look only at non-nil opponents (unless nil tricks help team bid)
            var theirBid = lhoBid.Tricks + rhoBid.Tricks;
            var theirTaken = (lhoBid.IsNotNil || options.failedNilBags == FailedNilBags.HelpsTeamBid ? lho.HandScore : 0) + (rhoBid.IsNotNil || options.failedNilBags == FailedNilBags.HelpsTeamBid ? rho.HandScore : 0);
            var theyNeed = theirBid - theirTaken;

            //  keep in mind how many bags we'll take
            //  we're willing to take more bags the higher opponents bid since setting them is worth more
            var canAffordBags = options.BagsThreshold == 0;

            if (!canAffordBags)
            {
                var ourBid = playerBid.Tricks + (partnerBid?.Tricks ?? 0);
                var threshold = options.BagsThreshold == 1 ? 2 : options.BagsThreshold - 1;
                var allowedBags = Math.Min(theirBid / 3, threshold - GetBags(player));
                var requiredBags = Math.Max(MaxTricks - theirBid - ourBid + 1, 0);
                canAffordBags = requiredBags <= allowedBags;
            }

            //  try to set opponents if they haven't made it, enough tricks are left, and we won't take too many bags
            return tricksLeft - sureTricks >= theyNeed && canAffordBags;
        }

        private int GetBags(PlayerBase player)
        {
            return GetBags(player.GameScore);
        }

        private int GetBags(long gameScore)
        {
            if (options.BagsThreshold == 0)
                return 0;

            var bags = gameScore % options.BagsThreshold;
            if (bags < 0)
                bags += options.BagsThreshold;
            return (int)bags;
        }

        private Card TryBustNil(PlayerBase player, IReadOnlyList<Card> trick, IReadOnlyList<Card> legalCards, IReadOnlyList<Card> cardsPlayed, PlayersCollectionBase players, Card cardTakingTrick, bool stillNeedToMakeBid, bool isPartnerTakingTrick)
        {
            //  we'll target the first nil-bidder counter-clockwise from us (so RHO, then across, then LHO)
            var targetNilBidder = players.Opponents(player).Where(p => new SpadesBid(p.Bid).IsNil && p.HandScore == 0).OrderBy(p => player.Seat - p.Seat).First();
            var targetsPartner = players.PartnerOf(targetNilBidder); // will be null in non-partnership games

            if (trick.Count == 0)
            {
                //  we're leading: try to pick something intelligent
                var avoidSuits = new List<Suit>();

                var isTargetsPartnerVoidInSpades = targetsPartner == null || players.TargetIsVoidInSuit(player, targetsPartner, new Card(Suit.Spades, Rank.Ace), cardsPlayed);

                foreach (var suit in SuitRank.stdSuits)
                {
                    //  avoid leading a suit the nil bidder is void in
                    if (players.TargetIsVoidInSuit(player, targetNilBidder, new Card(suit, Rank.Ace), cardsPlayed))
                        avoidSuits.Add(suit);

                    //  also avoid suits the nil bidder's partner is void in unless the partner is also void in trump
                    if (targetsPartner != null && !isTargetsPartnerVoidInSpades && players.TargetIsVoidInSuit(player, targetsPartner, new Card(suit, Rank.Ace), cardsPlayed))
                        avoidSuits.Add(suit);
                }

                var preferredLegalCards = legalCards.Where(c => !avoidSuits.Contains(EffectiveSuit(c))).ToList();

                return TryDumpEm(trick, preferredLegalCards.Count > 0 ? preferredLegalCards : legalCards, players.Count, stillNeedToMakeBid);
            }

            //  we're not leading: check the trick to determine what to do
            var targetOffset = (player.Seat - targetNilBidder.Seat + 4) % 4;
            if (trick.Count > targetOffset)
            {
                //  nil bidder has played and is taking the trick:
                //  try to get under them, but go high if we can't (this is the "risky" version of TryDumpEm)
                if (trick[trick.Count - targetOffset].SameAs(cardTakingTrick))
                    return TryDumpEm(trick, legalCards, players.Count, stillNeedToMakeBid, false);

                //  nil bidder has played, but is not taking the trick:
                //  still need to make our bid - play high but not too high
                if (stillNeedToMakeBid)
                {
                    if (isPartnerTakingTrick)
                    {
                        return TryDumpEm(trick, legalCards, players.Count, true);
                    }

                    //  nil bidder isn't taking the trick and our partner isn't taking the trick and we still want tricks, can we take this one?
                    var winners = legalCards.Where(c => EffectiveSuit(c) == EffectiveSuit(cardTakingTrick) && RankSort(c) > RankSort(cardTakingTrick)).ToList();
                    if (winners.Count > 0)
                        return winners.Where(RankBelowQueen).OrderByDescending(RankSort).FirstOrDefault() ?? winners.OrderBy(RankSort).First();

                    //  we can't win by following suit or beating the trump taking the suit but can we trump-in?
                    if (!IsTrump(cardTakingTrick) && legalCards.Any(IsTrump))
                    {
                        return legalCards.Where(IsTrump).Where(RankBelowQueen).OrderByDescending(RankSort).FirstOrDefault() ?? legalCards.Where(IsTrump).OrderBy(RankSort).First();
                    }

                    //  else let's throw the highest non-trump below queen, or lowest non-trump queen or above, or, finally, the lowest card we have (which will be trump)
                    return legalCards.Where(c => !IsTrump(c) && RankBelowQueen(c)).OrderByDescending(RankSort).FirstOrDefault() ?? legalCards.Where(c => !IsTrump(c)).OrderBy(RankSort).FirstOrDefault() ?? legalCards.OrderBy(RankSort).First();
                }

                //  play our highest card, preferring trump; this improves our ability to duck under the nil bidder later
                return legalCards.Where(IsTrump).OrderByDescending(RankSort).FirstOrDefault() ?? legalCards.OrderByDescending(RankSort).First();
            }

            //  nil bidder has not played: try to end up under them
            return TryDumpEm(trick, legalCards, players.Count, stillNeedToMakeBid);
        }

        public override List<Card> SuggestPass(SuggestPassState<SpadesOptions> state)
        {
            var (player, nCards) = (state.player, state.passCount);

            var cards = new List<Card>();
            var hand = new Hand(player.Hand);
            var playerBid = new SpadesBid(player.Bid);

            if (playerBid.IsNil)
            {
                var nTrump = hand.Count(IsTrump);

                //  if we're the nil bidder and have more than 3 trump, pass our highest trump (keeping 3)
                if (nTrump > 3)
                {
                    cards.AddRange(hand.Where(IsTrump).OrderByDescending(RankSort).Take(Math.Min(nTrump - 3, nCards)));
                }

                //  if we still need cards to pass, pass trump cards of the top 3 ranks
                if (cards.Count < nCards)
                {
                    var highTrump = hand.RemoveCards(cards).Where(c => IsTrump(c) && RankSort(c) > highRankBySuit[trump] - 3).ToList();

                    if (highTrump.Count > 0)
                    {
                        cards.AddRange(highTrump.OrderByDescending(RankSort).Take(nCards - cards.Count));
                    }
                }

                //  if we still need cards to pass, choose the highest ranks favoring the shortest suits
                if (cards.Count < nCards)
                {
                    hand = hand.RemoveCards(cards);
                    var countsBySuit = hand.GroupBy(EffectiveSuit).ToDictionary(g => g.Key, g => g.Count());
                    cards.AddRange(hand.OrderByDescending(RankSort).ThenBy(c => countsBySuit[EffectiveSuit(c)]).Take(nCards - cards.Count));
                }
            }
            else
            {
                var countsBySuit = hand.GroupBy(EffectiveSuit).ToDictionary(g => g.Key, g => g.Count());

                //  if we're not the nil bidder, pass the lowest ranks, avoiding trump and favoring our longest suits
                cards.AddRange(hand.Where(c => !IsTrump(c)).OrderBy(RankSort).ThenByDescending(c => countsBySuit[EffectiveSuit(c)]).Take(nCards));

                //  if we still need cards, pass the lowest ranks of trump
                if (cards.Count < nCards)
                    cards.AddRange(hand.Where(IsTrump).OrderBy(RankSort).Take(nCards - cards.Count));
            }

            return cards;
        }

        public override int SuitOrder(Suit s)
        {
            return suitOrder[s];
        }
    }
}
