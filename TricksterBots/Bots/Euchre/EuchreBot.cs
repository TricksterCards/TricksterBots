﻿using System;
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

        private double EstimatedTricks(Hand hand, Suit maybeTrump, bool withJoker)
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
            if (state.legalBids.Count == 1)
                return state.legalBids.First();

            if (state.options.variation == EuchreVariation.BidEuchre)
                return SuggestBidEuchreBid(state);

            var (players, dealerSeat, hand, legalBids, player, upCard, upCardSuit) =
                (new PlayersCollectionBase(this, state.players), state.dealerSeat, state.hand, state.legalBids, state.player, state.upCard, state.upCardSuit);

            if (hand.All(c => c.suit == Suit.Unknown))
                return legalBids.FirstOrDefault(b => b.value == BidBase.Pass) ?? legalBids.First();

            if (IsAskingDefendAlone(players))
                return new BidBase(BidBase.Pass);

            var isDealer = player.Seat == dealerSeat;
            var isTeamDealing = isDealer || players.PartnersOf(player).Any(p => p.Seat == dealerSeat);
            var willLeadFirst = MakerWillLeadFirst(state);

            //  always call a misdeal if offered
            if (legalBids.Any(b => b.value == (int)EuchreBid.CallMisdeal))
                return legalBids.Single(b => b.value == (int)EuchreBid.CallMisdeal);

            if (legalBids.Any(b => b.value == (int)EuchreBid.GoUnder))
            {
                //  go under if we can and don't have any suit we think we want to bid
                var bestBid = SuggestBid(hand, upCard, upCardSuit, isDealer, isTeamDealing, canPass: true, willLeadFirst);

                if (bestBid.value == BidBase.Pass && upCard != null)
                    bestBid = SuggestBid(hand, null, upCardSuit, isDealer, isTeamDealing, canPass: true, willLeadFirst);

                return legalBids.Single(b => b.value == (bestBid.value == BidBase.Pass ? (int)EuchreBid.GoUnder : (int)EuchreBid.AfterGoUnder));
            }

            var suggestion = SuggestBid(hand, upCard, upCardSuit, isDealer, isTeamDealing, legalBids.Any(b => b.value == BidBase.Pass), willLeadFirst);
            var canBidSuggestion = legalBids.Any(b => b.value == suggestion.value);

            if (!canBidSuggestion && IsAloneBid(suggestion))
            {
                suggestion = DowngradeAloneBid(suggestion);
                canBidSuggestion = legalBids.Any(b => b.value == suggestion.value);
            }

            return canBidSuggestion ? suggestion : legalBids.FirstOrDefault(b => b.value == BidBase.Pass) ?? legalBids.First();
        }

        private BidBase SuggestBidEuchreBid(SuggestBidState<EuchreOptions> state)
        {
            var legalBids = state.legalBids.Where(b => b.value != BidBase.NoBid).Select(b => new BidEuchreBid(b.value)).ToList();
            var legalLevelBids = legalBids.Where(b => b.IsLevelBid).ToList();
            var maxTricks = 0.0;
            var maxSuit = Suit.Unknown;
            var ntDown = false;
            var players = new PlayersCollectionBase(this, state.players);
            var partners = players.PartnersOf(state.player);
            var possibleTricks = (double)state.hand.Count;
            var willLeadFirst = MakerWillLeadFirst(state);
            var withJoker = state.options.withJoker;

            foreach (var suit in SuitRank.stdSuits)
            {
                var tricks = EstimatedTricks(state.hand, suit, withJoker);
                if (tricks > maxTricks)
                {
                    maxTricks = tricks;
                    maxSuit = suit;
                }
            }

            if (state.options.allowNotrump)
            {
                var ntTricks = EstimatedNotrumpTricks(state.hand, willLeadFirst, false);
                if (ntTricks > maxTricks)
                {
                    maxTricks = ntTricks;
                    maxSuit = Suit.Unknown;
                }

                if (options.allowLowNotrump)
                {
                    var lowNtTricks = EstimatedNotrumpTricks(state.hand, willLeadFirst, true);
                    if (lowNtTricks > maxTricks)
                    {
                        maxTricks = lowNtTricks;
                        maxSuit = Suit.Unknown;
                        ntDown = true;
                    }
                }
            }

            var bestTrumpInDeck = deck.Where(c => EffectiveSuit(c, maxSuit) == maxSuit).OrderByDescending(c => RankSort(c, maxSuit)).FirstOrDefault();
            var bestTrumpInHand = state.hand.Where(c => EffectiveSuit(c, maxSuit) == maxSuit).OrderByDescending(c => RankSort(c, maxSuit)).FirstOrDefault();
            var hasHighestTrump = bestTrumpInDeck != null && bestTrumpInHand != null && RankSort(bestTrumpInDeck, maxSuit) == RankSort(bestTrumpInHand, maxSuit);
            var shouldConsiderAlone = maxSuit == Suit.Unknown || hasHighestTrump;

            //  if we're bidding level-only, look at the alone bids before adjusting for partner and kitty
            if (legalLevelBids.Any())
            {
                var aloneLevelBid = legalLevelBids.FirstOrDefault(b => b.IsAloneCall0);
                if (shouldConsiderAlone && aloneLevelBid != null && maxTricks >= possibleTricks)
                    return new BidBase(aloneLevelBid);

                var aloneCall1Bid = legalLevelBids.FirstOrDefault(b => b.IsAloneCall1);
                if (shouldConsiderAlone && aloneCall1Bid != null && maxTricks >= possibleTricks - 1)
                    return new BidBase(aloneCall1Bid);

                var aloneCall2Bid = legalLevelBids.FirstOrDefault(b => b.IsAloneCall2);
                if (shouldConsiderAlone && aloneCall2Bid != null && maxTricks >= possibleTricks - 2)
                    return new BidBase(aloneCall2Bid);
            }

            //  save this for use later
            var unadjustedMaxTricks = maxTricks;

            // Estimate extra tricks from partner and/or kitty unless we're already estimating 3/5 or more of the possible tricks
            if (maxTricks < 4.0/5.0 * possibleTricks)
            {
                var kittySize = deck.Count - state.options.players * possibleTricks;

                //  assume kitty is good for kittySize/totalTricks remaining tricks
                if (state.options.withKitty)
                    maxTricks = Math.Min(possibleTricks, maxTricks + kittySize / possibleTricks);

                //  assume each partner is good for 1/n remaining tricks (where n is the number of other players)
                if (partners.Length > 0)
                {
                    // ReSharper disable once PossibleLossOfFraction
                    maxTricks = Math.Min(possibleTricks, maxTricks + partners.Length / (state.options.players - 1) * (possibleTricks - maxTricks));
                }
            }

            if (options.bidType == EuchreBidType.LevelAndSuit)
            {
                //  we've got to bid both level and suit

                //  handle NT up/down
                int intSuit;
                if (maxSuit == Suit.Unknown)
                    if (ntDown)
                        intSuit = BidEuchreBid.Suit_NTdown;
                    else if (options.allowLowNotrump)
                        intSuit = BidEuchreBid.Suit_NTup;
                    else
                        intSuit = (int)Suit.Unknown;
                else
                    intSuit = (int)maxSuit;

                //  get the lowest bid available
                var lowestBid = new BidEuchreBid(state.legalBids.First().value);

                //  get out best bid using the intSuit
                BidEuchreBid bestBid;
                if (unadjustedMaxTricks >= possibleTricks)
                    bestBid = BidEuchreBid.FromIntSuitAndLevel(intSuit, BidEuchreBid.Level_AloneCall0);
                else if (unadjustedMaxTricks >= possibleTricks - 1)
                    bestBid = BidEuchreBid.FromIntSuitAndLevel(intSuit, BidEuchreBid.Level_AloneCall1);
                else if (unadjustedMaxTricks >= possibleTricks - 2)
                    bestBid = BidEuchreBid.FromIntSuitAndLevel(intSuit, BidEuchreBid.Level_AloneCall2);
                else if (options.biddingStyle == EuchreBidStyle.Auction && !lowestBid.IsAnyAlone)
                {
                    //  if we're using auction-style bidding, we'll have another opportunity to bid, so bid as low as we can
                    bestBid = BidEuchreBid.FromIntSuitAndLevel(intSuit, Math.Min(lowestBid.BidLevel, (int)maxTricks));
                }
                else
                    bestBid = BidEuchreBid.FromIntSuitAndLevel(intSuit, (int)maxTricks);

                if (legalBids.Any(lb => lb.Equals(bestBid)))
                    return new BidBase(bestBid);

                //  handle condition when we can't pass
                if (state.legalBids.All(b => b.value != BidBase.Pass))
                {
                    return new BidBase(BidEuchreBid.FromIntSuitAndLevel(intSuit, lowestBid.BidLevel));
                }
            }
            else if (legalLevelBids.Any())
            {
                //  handle non-alone levels when we're bidding level-only
                var canPass = state.legalBids.Any(b => b.value == BidBase.Pass);
                var highLevel = players.Select(p => new BidEuchreBid(p.Bid).BidLevel).Max();
                var isLastToBid = state.player.Seat == state.dealerSeat;
                var isPartnerWinningBid = highLevel > 0 && partners.Any(p => new BidEuchreBid(p.Bid).BidLevel == highLevel);

                //  pass if last to bid and partner has the high bid
                if (canPass && isLastToBid && isPartnerWinningBid)
                    return new BidBase(BidBase.Pass);

                var minLegalLevel = legalLevelBids.Select(b => b.BidLevel).Min();

                //  choose level (only bidding as high as necessary if last to bid or this is auction-style bidding)
                if (maxTricks >= minLegalLevel)
                    return new BidBase(BidEuchreBid.FromLevel(isLastToBid || options.biddingStyle == EuchreBidStyle.Auction ? minLegalLevel : (int)maxTricks));

                //  handle stick-the-dealer
                if (!canPass)
                    return new BidBase(BidEuchreBid.FromLevel(minLegalLevel));
            }
            else
            {
                //  choose suit
                BidEuchreBid match;

                //  there are two bids where BidSuit returns Suit.Unknown so handle them specially
                if (maxSuit == Suit.Unknown)
                {
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    match = ntDown ? legalBids.SingleOrDefault(b => b.IsNTdown) : legalBids.SingleOrDefault(b => b.IsNT || b.IsNTup);
                }
                else
                {
                    match = legalBids.SingleOrDefault(b => b.BidSuit == maxSuit);
                }

                if (match != null)
                    return new BidBase(match);
            }

            return new BidBase(BidBase.Pass);
        }

        private bool MakerWillLeadFirst(SuggestBidState<EuchreOptions> state)
        {
            return options.makerLeadsFirst || (state.dealerSeat + 1) % state.options.players == state.player.Seat;
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
        private BidBase SuggestBid(Hand hand, Card upCard, Suit upCardSuit, bool isDealer, bool isTeamDealing, bool canPass, bool willLeadFirst)
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
                    var est = EstimatedNotrumpTricks(hand, willLeadFirst, lowIsHigh: false);
                    if (est > highEstimate)
                    {
                        highEstimate = est;
                        highSuit = Suit.Unknown;
                    }

                    if (options.allowLowNotrump)
                    {
                        var lowEst = EstimatedNotrumpTricks(hand, willLeadFirst, lowIsHigh: true);
                        if (lowEst > highEstimate)
                        {
                            highEstimate = lowEst;
                            highSuit = Suit.Joker;
                        }
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

                //  when aloneTake5 is false, we need to hold one of the top cards (Joker or either Jack) to bid alone (or be call-for-best with 4+ trump)
                if (!options.aloneTake5 && ((options.callForBest && hand.Count(c => EffectiveSuit(c, highSuit) == highSuit) >= 4) || hand.Any(c => EffectiveSuit(c, highSuit) == highSuit && (c.rank == Rank.Jack || c.rank == Rank.High))))
                    return new BidBase((int)EuchreBid.MakeAlone + (int)highSuit);
            }

            if (highEstimate >= (options.take4for1 ? 3.25 : 2.25))
                return new BidBase((int)EuchreBid.Make + (int)highSuit);

            //  we don't have anything we like, so pass if we can or bid the best we've got
            return canPass ? new BidBase(BidBase.Pass) : new BidBase((int)EuchreBid.Make + (int)highSuit);
        }

        public override List<Card> SuggestDiscard(SuggestDiscardState<EuchreOptions> state)
        {
            if (options.variation == EuchreVariation.BidEuchre)
            {
                //  we're being asked to discard because we added the kitty
                return options.withKitty ? state.hand.OrderBy(IsTrump).ThenBy(RankSort).Take(options.KittySize).ToList() : new List<Card>();
            }

            var (player, hand) = (state.player, state.hand);

            if (player.Bid == (int)EuchreBid.GoUnder)
                return hand.OrderBy(IsTrump).ThenBy(RankSort).Take(3).ToList();

            //  discard the lowest card of the off-suit with the fewest cards (or the lowest card of trump)
            var offSuits = hand.Select(EffectiveSuit).Where(s => s != trump).Distinct().ToList();
            var lowSuitCount =
                offSuits.Select(s => new
                    {
                        suit = s,
                        count = hand.Count(c => EffectiveSuit(c) == s),
                        lowRank = hand.Where(c => EffectiveSuit(c) == s).Min(RankSort),
                        highRank = hand.Where(c => EffectiveSuit(c) == s).Max(RankSort)
                    })
                    .OrderBy(sc => sc.lowRank == RankSort(new Card(sc.suit, Rank.Ace))) //  don't get rid of off-suit Aces unless we have to
                    .ThenBy(sc => sc.count == 2 && sc.highRank == RankSort(new Card(sc.suit, Rank.King))) //  don't get rid of protection for an off-suit King unless we have to
                    .ThenBy(sc => sc.count)
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

            var playerIsMaker = IsMaker(player);
            var partners = players.PartnersOf(player);
            var partnerIsMaker = partners.Any(IsMaker);
            var teamIsMaker = playerIsMaker || partnerIsMaker;
            var teamHandScore = player.HandScore + partners.Sum(p => p.HandScore);
            var isDefending = !teamIsMaker;
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
                if (sortedTrump.Count > 1 && IsMakeAlone(player) &&
                    players.Opponents(player).All(p => p.HandScore == 0))
                {
                    return sortedTrump.Last();
                }

                //  Lead trump if you called it and have three or more trump
                if (sortedTrump.Count >= 3 && playerIsMaker)
                {
                    //  we have three or more trump and we're the maker: lead our high trump if it's good or our low trump if not
                    var highTrump = sortedTrump.Last();
                    return IsCardHigh(highTrump, cardsPlayed) ? highTrump : sortedTrump.First();
                }

                //  Lead last trump if you called it, have already taken 3 tricks, and partner is void or not playing.
                //  Increases chances of taking all 5 tricks by forcing opponents to discard a high off-suit card.
                if (!IsBidEuchre && sortedTrump.Count == 1)
                {
                    var alreadyMadeBid = teamIsMaker && 3 <= player.HandScore + partners.Sum(p => p.HandScore);
                    var partnersAreNotPlayingOrVoid = partners.All(p => p.Bid == BidBase.NotPlaying || players.TargetIsVoidInSuit(player, p, trump, cardsPlayed));
                    if (alreadyMadeBid && partnersAreNotPlayingOrVoid)
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
                if (highCards.Count == 1 && (!IsTrump(highCards[0]) || teamIsMaker && sortedTrump.Count > 1))
                    return highCards[0];

                //  if we have more than one high card, if we're the maker and one of the high cards is trump, lead it
                //  otherwise, play the non-trump high card from the suit with the fewest cards
                if (highCards.Count > 1)
                {
                    if (teamIsMaker && highCards.Any(IsTrump))
                        return highCards.First(IsTrump);

                    //  play the high non-trump card from the suit with fewest cards
                    var nonTrumpHighCards = highCards.Where(c => !IsTrump(c)).ToList();
                    if (nonTrumpHighCards.Any())
                    {
                        var theSuit = nonTrumpHighCards.Select(EffectiveSuit).OrderBy(s => legalCards.Count(c => EffectiveSuit(c) == s)).ThenBy(s => suitOrder[s]).First();
                        return nonTrumpHighCards.First(c => EffectiveSuit(c) == theSuit);
                    }
                }

                //  lead our highest off-suit if we're alone, out of trump and opponents haven't taken a trick yet
                if (sortedTrump.Count == 0 && IsMakeAlone(player) &&
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
                    else if (!trick.Any(c => EffectiveSuit(c) == trickSuit && RankSort(c) >= RankSort(highFollow)))
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
                if (IsBidEuchre || !isDefending || isLastToPlay)
                    return false;

                // no need to protect if we don't have exactly two trump (more and we can still protect, less and we can't protect anyway)
                if (legalCards.Count(IsTrump) != 2)
                    return false;

                // it's only worth protecting the left if a guaranteed trick would be a stopper or the last trick to Euchre
                if (teamHandScore != 0 && teamHandScore != 2)
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
                //  if we bid alone in classic euchre, treat this like an extra discard; in Bid Euchre just pass the requested number of the lowest cards favoring non-trump
                return IsBidEuchre
                    ? hand.OrderBy(IsTrump).ThenBy(RankSort).Take(state.passCount).ToList()
                    : SuggestDiscard(new SuggestDiscardState<EuchreOptions> { player = player, hand = hand });
            }

            //  if partner bid, try to give our partner trump if we can
            var list = hand.Where(IsTrump).OrderByDescending(RankSort).Take(state.passCount).ToList();
            if (list.Count == state.passCount)
                return list;

            //  if we don't have enought, add our highest-ranked off-suit card, tie-breaking using the shortest suit
            var suitCounts = hand.Where(c => !IsTrump(c)).GroupBy(EffectiveSuit).ToDictionary(g => g.Key, g => g.Count());
            list.AddRange(hand.Where(c => !IsTrump(c)).OrderByDescending(RankSort).ThenBy(c => suitCounts[EffectiveSuit(c)]).Take(state.passCount - list.Count));

            return list;
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

        private double EstimatedNotrumpTricks(Hand hand, bool willLeadFirst, bool lowIsHigh)
        {
            //  temporarily override NT direction on options (so RankSort, IsCardHigh, etc. works as expected)
            var bidIsNtDown = options._bidIsNtDown;
            options._bidIsNtDown = lowIsHigh;

            var est = 0.0;

            try
            {
                var cardsPlayed = new List<Card>();
                var sortedHand = hand.OrderByDescending(RankSort).ToList();
                var nHighSuits = sortedHand.Where(c => c.suit != Suit.Joker)
                    .GroupBy(c => c.suit)
                    .ToDictionary(g => g.Key, g => g.First())
                    .Count(g => IsCardHigh(g.Value, cardsPlayed));

                //  If we lead first or have a high card in every suit, we can run our high cards so long as noone else holds a Joker
                if ((willLeadFirst || nHighSuits == 4) && (!options.withJoker || hand.Any(c => c.suit == Suit.Joker)))
                {
                    foreach (var card in sortedHand)
                    {
                        if (IsCardHigh(card, cardsPlayed))
                            est += 1;

                        // TODO: calculate if remaining low cards are likely good too
                        // This happens when we have enough high cards to make other players void

                        cardsPlayed.Add(card);
                    }
                }
            }
            finally
            {
                //  restory correct NT direction on options
                options._bidIsNtDown = bidIsNtDown;
            }

            return est;
        }

        private bool IsBidEuchre => options.variation == EuchreVariation.BidEuchre;

        private bool IsMaker(PlayerBase player)
        {
            if (IsBidEuchre)
                return new BidEuchreBid(player.Bid).IsMake;

            var bid = BidBid(player);
            return bid == EuchreBid.Make || bid == EuchreBid.MakeAlone;
        }

        private bool IsMakeAlone(PlayerBase player)
        {
            return IsBidEuchre ? new BidEuchreBid(player.Bid).IsAnyAlone : BidBid(player.Bid) == EuchreBid.MakeAlone;
        }

        private bool IsAskingDefendAlone(PlayersCollectionBase players)
        {
            return options.defendAlone && players.Any(p => p.Bid == BidBase.NotPlaying);
        }
    }
}