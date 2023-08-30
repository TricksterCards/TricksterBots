using System;
using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    public enum PitchBid
    {
        ShootMoonValue = 15,
        NotPitching = BidSpace.Pitch,
        Pitching = NotPitching + 10,
        Base = BidSpace.Pitch + 180,
        ShootMoonBid = Base + ShootMoonValue
    }

    public class PitchBot : BaseBot<PitchOptions>
    {
        //  this is for the auto discard and refill for 7, 10, 11 & 13
        private static readonly PitchVariation[] autoDiscardVariations =
        {
            PitchVariation.SevenPoint, PitchVariation.TenPoint, PitchVariation.ElevenPoint, PitchVariation.ThirteenPoint
        };

        private const int ShootMoonPitchBid = (int)PitchBid.ShootMoonBid;

        private readonly Dictionary<Rank, int> gamePointsByRank = new Dictionary<Rank, int>
        {
            { Rank.Ace, 4 * 2 },
            { Rank.King, 3 * 2 },
            { Rank.Queen, 2 * 2 },
            { Rank.Jack, 1 * 2 },
            { Rank.High, 1 },
            { Rank.Low, 1 },
            { Rank.Ten, 10 * 2 }
        };

        public PitchBot(PitchOptions options, Suit trumpSuit) : base(options, trumpSuit)
        {
        }

        public override DeckType DeckType
        {
            get
            {
                switch (options.variation)
                {
                    case PitchVariation.FourPoint:
                    case PitchVariation.FivePoint:
                        return options.deck;
                    case PitchVariation.NinePoint:
                        return DeckType.Std52Card;
                    case PitchVariation.SixPoint:
                        switch (options.deck)
                        {
                            case DeckType.Std52Card:
                                return DeckType.Std53Card;
                            case DeckType.TwoAndNineToAce:
                                return DeckType.TwoAndNineToAceAndJoker;
                            case DeckType.FiveToAce:
                                return DeckType.FiveToAceAndJoker;
                            case DeckType.NineToAce:
                                return DeckType.NineToAceAndJoker;
                            default:
                                throw new Exception($"Unrecognized deck type: {options.deck}");
                        }
                    case PitchVariation.SevenPoint:
                    case PitchVariation.TenPoint:
                    case PitchVariation.ElevenPoint:
                    case PitchVariation.ThirteenPoint:
                        if (options.players == 5 && options.kitty == 0)
                            return DeckType.No4s_50Card; //  5-player call your partner w/o a kitty (10 cards to each player)
                        else
                            return DeckType.Std54Card;
                    default:
                        throw new Exception($"Unrecognized Pitch variation: {options.variation}");
                }
            }
        }

        private bool HasPostBidDiscard => autoDiscardVariations.Contains(options.variation);

        private bool IsCallPartner => options.players > 4 && !options.isPartnership;

        private bool IsOffAceTrump => options.variation == PitchVariation.ElevenPoint;

        private bool IsOffJackTrump => options.variation != PitchVariation.FourPoint && options.variation != PitchVariation.NinePoint;

        private bool IsOffThreeTrump => PitchOptions.availablePoints[options.variation] >= 13;

        private bool IsRankFiveWorth5 => options.variation == PitchVariation.NinePoint;

        private bool IsRankThreeWorth3 => PitchOptions.availablePoints[options.variation] >= 10;

        private int MinPitchBid => (int)PitchBid.Base + options.minBid;

        private int NumberOfCardsDealtPerPlayer
        {
            get
            {
                switch (options.variation)
                {
                    case PitchVariation.FourPoint:
                    case PitchVariation.FivePoint:
                    case PitchVariation.SixPoint:
                        return 6;
                    case PitchVariation.NinePoint:
                        return 9;
                    case PitchVariation.SevenPoint:
                    case PitchVariation.TenPoint:
                    case PitchVariation.ElevenPoint:
                    case PitchVariation.ThirteenPoint:
                        return options.players == 5 ? 10 : options.players == 6 && options.kitty > 0 ? 8 : 9;
                    default:
                        throw new Exception($"Unrecognized Pitch variation: {options.variation}");
                }
            }
        }

        public override BidBase SuggestBid(SuggestBidState<PitchOptions> state)
        {
            var (player, hand, players, dealerSeat) = (state.player, state.hand, new PlayersCollectionBase(this, state.players), state.dealerSeat);

            var highEstimate = 0.0;
            var highSuit = Suit.Unknown;

            //  estimate each suit to determine which is our "best"
            foreach (var suit in SuitRank.stdSuits)
            {
                var est = EstimatedPoints(hand, suit);
                if (est > highEstimate)
                {
                    highEstimate = est;
                    highSuit = suit;
                }
            }

            //  phase three bidding: call your partner
            if (IsCallPartner && trump != Suit.Unknown)
            {
                //  call for the highest trump we don't have
                //  TODO: make this smarter; e.g. call for three if holding ace
                var card = DeckBuilder.BuildDeck(DeckType).Where(IsTrump).Where(t => !hand.Any(c => c.SameAs(t))).OrderBy(RankSort).Last();
                return new BidBase(CallForCard(card));
            }

            //  phase two bidding: select our "best" suit
            //  TODO: Verify that this new check works
            if (player.BidHistory.LastOrDefault() >= (int)PitchBid.Pitching)
            {
                var pitcherBidValue = player.BidHistory.Last() - (int)PitchBid.Base;
                return new BidBase((int)PitchBid.Pitching + pitcherBidValue * 10 + (int)highSuit);
            }

            //  phase one bidding: select points

            //  floor our high estimate to an int for comparison against bids
            var flooredEstimate = Math.Min((int)Math.Floor(highEstimate), PitchOptions.availablePoints[options.variation]);

            //  then figure out the high bid (using one less than the minBid if noone has bid yet)
            var highBid = Math.Max(MinPitchBid - 1, players.Max(p => p.Bid));

            //  if shoot-the-moon is available and our estimate is to take ALL points
            if (options.offerShootBid && !options.shootAllTricks && highBid != ShootMoonPitchBid && flooredEstimate == PitchOptions.availablePoints[options.variation])
            {
                //  then shoot the moon
                return new BidBase(ShootMoonPitchBid);
            }

            //  if we're the last to bid, noone else has bid, and we're playing "stick the dealer"
            if (player.Seat == dealerSeat && highBid < MinPitchBid && options.stickTheDealer)
            {
                //  then bid the minimum
                return new BidBase(MinPitchBid);
            }

            //  if we're the last to bid, someone has bid, and our partner has the high bid
            var partner = players.PartnerOf(player);
            if (partner != null && player.Seat == dealerSeat && highBid >= MinPitchBid && partner.Bid == highBid)
            {
                //  then don't outbid our partner
                return new BidBase(BidBase.Pass);
            }

            //  if we haven't bid yet and our estimate is greater than the high bid
            var estimatedBid = flooredEstimate + (int)PitchBid.Base;
            if (estimatedBid > highBid)
            {
                //  bid one greater than the current high bid if we're the last to bid, otherwise bid our estimate
                return new BidBase(player.Seat == dealerSeat ? highBid + 1 : estimatedBid);
            }

            //  pass if no other bid was selected
            return new BidBase(BidBase.Pass);
        }

        public override List<Card> SuggestDiscard(SuggestDiscardState<PitchOptions> state)
        {
            var (player, hand) = (state.player, state.hand);

            var maxDiscardCount = GetMaxDiscardCount(player, DetermineLegalDiscards(hand));
            var toss = hand.Where(c => !IsTrump(c) && c.rank != Rank.Ace).OrderBy(DiscardSort).Take(maxDiscardCount).ToList();

            if (hand.Count - toss.Count > 6)
                toss.AddRange(hand.Where(c => IsTrump(c) || c.rank == Rank.Ace).OrderBy(DiscardSort).ThenBy(RankSort).Take(hand.Count - toss.Count - 6));

            return toss;
        }

        public override Card SuggestNextCard(SuggestCardState<PitchOptions> state)
        {
            var (players, trick, legalCards, cardsPlayed, player, isPartnerTakingTrick, cardTakingTrick, trickTaker) =
                (new PlayersCollectionBase(this, state.players), state.trick, state.legalCards, state.cardsPlayed, state.player, state.isPartnerTakingTrick, state.cardTakingTrick, state.trickTaker);

            //  set isPartnerTaking trick for call your partner
            if (IsCallPartner && trickTaker != null)
                isPartnerTakingTrick = OnSameTeam(players, player, trickTaker);

            //  if there's only one card, play it
            if (legalCards.Count == 1)
                return legalCards[0];

            //  if this is the first trick and we called for a partner, lead our lowest, most valuable card
            //  this works because we called for a boss card from partner that they MUST play on the first trick
            if (IsCallPartner && trick.Count == 0 && new Hand(player.Hand).Count == 6)
            {
                var lowestValuableCard = legalCards
                    .Where(c => !IsCardHigh(c, legalCards))
                    .OrderByDescending(c => !options.lowGoesToTaker && c.rank == Rank.Two ? 0 : CardPoints(c))
                    .ThenBy(RankSort)
                    .FirstOrDefault();

                //  if all of our cards are boss, just fall through to normal logic (doesn't matter what we play)
                if (lowestValuableCard != null)
                    return lowestValuableCard;
            }

            //  if this is the first trick and we're defending in call-for-partner, play our lowest, least valuable card
            if (IsCallPartner && !IsPitching(player) && !IsCalledPartner(player) && new Hand(player.Hand).Count == 6)
            {
                var lowestLeastValuableCard = legalCards
                    .Where(c => !IsCardHigh(c, legalCards))
                    .OrderBy(c => !options.lowGoesToTaker && c.rank == Rank.Two ? 0 : CardPoints(c))
                    .ThenBy(RankSort)
                    .FirstOrDefault();

                //  if all of our cards are boss, just fall through to normal logic (doesn't matter what we play)
                if (lowestLeastValuableCard != null)
                    return lowestLeastValuableCard;
            }

            //  leading
            if (trick.Count == 0)
                return SuggestLead(state);

            var activePlayersCount = players.Count(p => p.IsActivelyPlaying);

            //  last to play
            if (trick.Count == activePlayersCount - 1)
                return SuggestCardFromLastSeat(state);

            var knownCards = new Hand(player.Hand);
            knownCards.AddRange(cardsPlayed);

            //  update the isPartnerTakingTrick flag to only be true if we know the partner is going to take the trick
            if (isPartnerTakingTrick)
            {
                if (trick.Count < activePlayersCount - 1)
                {
                    //  we're not the last to play: is the card the partner is taking the trick with good among all the known cards including ours?
                    if (!IsCardHigh(cardTakingTrick, knownCards) || (!IsTrump(cardTakingTrick) && !players.LhoIsVoidInSuit(player, trump, knownCards)))
                    {
                        //  we don't know for sure that our partner is taking the trick so don't throw points his way, but consider taking the trick if we want
                        isPartnerTakingTrick = false;
                    }
                }
            }

            var trickSuit = EffectiveSuit(trick[0]);

            //  we can follow suit
            if (legalCards.Any(c => EffectiveSuit(c) == trickSuit))
            {
                if ((trickSuit == trump || trick.All(c => EffectiveSuit(c) != trump)) && !isPartnerTakingTrick)
                {
                    //  tricksuit is trump or trick contains no trump; does our best follow card win?
                    var highFollow = legalCards.Where(c => EffectiveSuit(c) == trickSuit).OrderBy(RankSort).Last();
                    if (!trick.Any(c => EffectiveSuit(c) == trickSuit && RankSort(c) > RankSort(highFollow)))
                        return highFollow;
                }

                if (isPartnerTakingTrick)
                {
                    //  try giving our partner a point card without giving away an already known good card
                    var candidate = GoodCardToGiveToPartner(legalCards, knownCards);
                    if (candidate != null)
                        return candidate;
                }

                //  we can't win by following suit, so check if we can/should trump in
                if (isPartnerTakingTrick || options.playTrump != PitchPlayTrump.Anytime || trickSuit == trump ||
                    !legalCards.Any(IsTrump))
                    //  play low
                    return LowestCardWorthFewestPoints(state);
            }

            //  we can't follow suit (or don't need to), we have trump, our partner isn't guaranteed to take the trick, and the trick is worth taking
            if (legalCards.Any(c => EffectiveSuit(c) == trump) && !isPartnerTakingTrick && IsTrickWorthTaking(trick))
            {
                if (trick.Any(c => EffectiveSuit(c) == trump))
                {
                    //  trick already contains trump; does our best trump win?
                    var highTrump = legalCards.Where(c => EffectiveSuit(c) == trump).OrderBy(RankSort).Last();
                    if (!trick.Any(c => EffectiveSuit(c) == trump && RankSort(c) > RankSort(highTrump)))
                        return highTrump;
                }
                else
                {
                    //  trick does not contain trump and we're not in last seat
                    //  try to take it with a low trump that's not worth points (as we could be over-trumped yet)
                    var lowNonPointerTrump = legalCards.Where(c => IsTrump(c) && CardPoints(c) == 0).OrderBy(RankSort)
                        .FirstOrDefault();
                    if (lowNonPointerTrump != null)
                        return lowNonPointerTrump;
                }
            }

            if (isPartnerTakingTrick)
            {
                //  try giving our partner a point card without giving away an already known good card
                var candidate = GoodCardToGiveToPartner(legalCards, knownCards);
                if (candidate != null)
                    return candidate;
            }

            //  return the lowest card we have favoring non-trump
            return LowestCardWorthFewestPoints(state);
        }

        private Card SuggestLead(SuggestCardState<PitchOptions> state)
        {
            var legalCards = state.legalCards;
            var cardsPlayed = state.cardsPlayed;
            var sortedSuits = legalCards.Select(EffectiveSuit).OrderBy(s => legalCards.Count(c => EffectiveSuit(c) == s));

            //  try to lead a known good card from the suit with the fewest cards
            foreach (var suit in sortedSuits)
            {
                var s = suit;
                var highCard = legalCards.Where(c => EffectiveSuit(c) == s).OrderBy(RankSort).Last();
                if (IsCardHigh(highCard, cardsPlayed))
                    return highCard;
            }

            //  return the lowest card we have favoring non-trump
            return LowestCardWorthFewestPoints(state);
        }

        private Card SuggestCardFromLastSeat(SuggestCardState<PitchOptions> state)
        {
            var legalCards = state.legalCards;
            var knownCards = new Hand(state.player.Hand).AddCards(state.cardsPlayed);
            var trickSuit = EffectiveSuit(state.trick[0]);
            var cardTakingTrick = state.cardTakingTrick;

            //  try giving our partner a point card without giving away an already known good card
            if (state.isPartnerTakingTrick)
                return GoodCardToGiveToPartner(legalCards, knownCards)
                       ?? LowestCardWorthFewestPoints(state);

            //  trump in with a low pointer if possible
            var lowestTrumpPointer = LowestMostValuablePointer(legalCards, knownCards, cardTakingTrick);
            if (lowestTrumpPointer != null)
                return lowestTrumpPointer;

            var lowestWinner = legalCards.Where(c => EffectiveSuit(c) == trickSuit && RankSort(c) > RankSort(cardTakingTrick)).OrderBy(RankSort).FirstOrDefault();
            if (lowestWinner != null)
                return lowestWinner;

            //  return the lowest card we have favoring non-trump
            return LowestCardWorthFewestPoints(state);
        }

        private Card LowestCardWorthFewestPoints(SuggestCardState<PitchOptions> state)
        {
            //  return the lowest card we have favoring non-trump
            var sorted = state.legalCards.OrderBy(PlayCardSort).ThenBy(RankSort).ToList();
            var candidate = sorted.First();
            if (EffectiveSuit(candidate) != state.trumpSuit || !options.lowGoesToTaker)
                return candidate;

            //  if the candidate is already above the lowest played (or other held) trump, use it
            var playedOrHeldCards = state.cardsPlayed.Concat(state.trick).Concat(sorted).Where(c => c != candidate).ToList();
            var lowestPlayedOrHeldTrump = playedOrHeldCards.Where(IsTrump).OrderBy(RankSort).FirstOrDefault();
            if (lowestPlayedOrHeldTrump == null || RankSort(lowestPlayedOrHeldTrump) < RankSort(candidate))
                return candidate;

            //  otherwise check for a touching card just above it (using only visible cards to feel more "human")
            var visibleCards = state.trick.Concat(state.legalCards);
            var touchingCandidate =
                sorted.FirstOrDefault(c => c != candidate && PlayCardSort(c) == PlayCardSort(candidate) && AreCardsEquivalent(c, candidate, visibleCards));
            if (touchingCandidate != null)
                return touchingCandidate;

            //  otherwise if candidate is most likely not low, use it (5+ or post bid discard is used)
            if (candidate.rank >= Rank.Five || HasPostBidDiscard)
                return candidate;

            //  otherwise pick the next highest trump worth the same points
            var nextCandidate = sorted
                .FirstOrDefault(c => c != candidate && PlayCardSort(c) == PlayCardSort(candidate) && RankSort(c) > RankSort(candidate));
            if (nextCandidate != null)
                return nextCandidate;

            //  and if all else fails, just use it
            return candidate;
        }

        private Card LowestMostValuablePointer(IEnumerable<Card> legalCards, IEnumerable<Card> knownCards, Card aboveCard = null)
        {
            return legalCards
                .Where(c => CardPoints(c) > 0 && !IsCardHigh(c, knownCards))
                .Where(c => aboveCard == null || EffectiveSuit(c) != EffectiveSuit(aboveCard) || RankSort(c) > RankSort(aboveCard))
                .Where(c => c.rank != Rank.Queen && c.rank != Rank.King)
                .OrderByDescending(CardPoints)
                .ThenBy(RankSort)
                .FirstOrDefault();
        }

        public override List<Card> SuggestPass(SuggestPassState<PitchOptions> state)
        {
            throw new NotImplementedException();
        }

        private static int CallForCard(Card c)
        {
            return (int)c.suit + (int)c.rank * 10;
        }

        private static Hand DetermineLegalDiscards(Hand hand)
        {
            //  allow anything to be discarded
            return new Hand(hand);
        }

        private static bool IsPitching(PlayerBase player)
        {
            return player.Bid >= (int)PitchBid.Pitching;
        }

        private int CardPoints(Card card)
        {
            return CardPoints(card, false);
        }

        private int CardPoints(Card card, bool isHolder)
        {
            if (IsTrump(card))
            {
                if (card.rank == Rank.Ace || card.rank == Rank.Jack || card.suit == Suit.Joker)
                    return 1;

                if (card.rank == Rank.Ten && options.tenOfTrumpReplacesGamePoint)
                    return 1;

                if (card.rank == Rank.Five && IsRankFiveWorth5)
                    return 5;

                if (card.rank == Rank.Three && IsRankThreeWorth3)
                    return 3;

                if (card.rank == Rank.Two && (isHolder || options.lowGoesToTaker))
                    return 1;
            }

            return 0;
        }

        private int DiscardSort(Card card)
        {
            if (IsTrump(card))
            {
                if (card.rank == Rank.Three && IsRankThreeWorth3 || card.rank == Rank.Five && IsRankFiveWorth5)
                    return 4;

                if (card.rank == Rank.Two || card.rank == Rank.Ace)
                    return 3;

                if (card.rank == Rank.Jack || card.suit == Suit.Joker)
                    return 2;

                if (options.tenOfTrumpReplacesGamePoint && card.rank == Rank.Ten)
                    return 2;

                return 1;
            }

            if (!options.tenOfTrumpReplacesGamePoint && card.rank == Rank.Ten)
                return 0;

            return -1;
        }

        private double EstimatedPoints(Hand hand, Suit t)
        {
            //  ensure all suits have at least some value so we never pick Suit.Unknown
            var estimatedPoints = 0.1;

            //  take inventory of the relevant cards in our hand
            var trumpInHand = Math.Min(6, hand.Count(c => EffectiveSuit(c, t) == t));
            var ace = hand.Any(c => c.rank == Rank.Ace && c.suit == t);
            var offace = hand.Any(c => c.rank == Rank.Jack && c.suit != t && EffectiveSuit(c, t) == t);
            var king = hand.Any(c => c.rank == Rank.King && c.suit == t);
            var queen = hand.Any(c => c.rank == Rank.Queen && c.suit == t);
            var jack = hand.Any(c => c.rank == Rank.Jack && c.suit == t);
            var offjack = hand.Any(c => c.rank == Rank.Jack && c.suit != t && EffectiveSuit(c, t) == t);
            var highjoker = hand.Any(c => c.rank == Rank.High && c.suit == Suit.Joker);
            var lowjoker = hand.Any(c => c.rank == Rank.Low && c.suit == Suit.Joker);
            var ten = hand.Any(c => c.rank == Rank.Ten && c.suit == t);
            var five = options.variation == PitchVariation.NinePoint && hand.Any(c => c.rank == Rank.Five && c.suit == t);
            var three = hand.Any(c => c.rank == Rank.Three && c.suit == t);
            var offthree = hand.Any(c => c.rank == Rank.Three && c.suit != t && EffectiveSuit(c, t) == t);
            var two = hand.Any(c => c.rank == Rank.Two && c.suit == t);

            //  calculate the points available to capture, starting with maxPoints - Ace
            var maxPoints = PitchOptions.availablePoints[options.variation];
            var capturablePoints = maxPoints - 1;

            //  estimate getting some extra trump from the draw option
            var estTrumpCount = trumpInHand;
            if (options.drawOption != PitchDrawOption.None)
                estTrumpCount += (6 - estTrumpCount) / 3;

            //  give some credit based on how many trump we have
            estimatedPoints += estTrumpCount * 0.1;

            if (offace)
            {
                //  if we have the off-ace in our hand, we can't capture it
                capturablePoints -= 1;
            }

            if (!options.lowGoesToTaker)
            {
                //  if low goes to the holder, we can't capture it
                capturablePoints--;
            }
            else if (two)
            {
                //  if low goes to taker, but the two is in our hand, we can't capture it
                capturablePoints--;
            }

            if (!options.tenOfTrumpReplacesGamePoint)
            {
                //  if we're playing with game point, we'll estimate that separately
                capturablePoints--;
            }

            if (five)
            {
                //  if we have the five in our hand, we can't capture it
                capturablePoints -= 5;
            }

            if (three && maxPoints >= 10)
            {
                //  if we have the three in our hand, we can't capture it
                capturablePoints -= 3;
            }

            if (offthree)
            {
                //  if we have the off-three in our hand, we can't capture it
                capturablePoints -= 3;
            }

            if (jack)
            {
                //  if we have the jack in our hand, we can't capture it
                capturablePoints--;
            }

            if (offjack)
            {
                //  if we have the off-jack in our hand, we can't capture it
                capturablePoints--;
            }

            if (highjoker)
            {
                //  if we have the high joker in our hand, we can't capture it
                capturablePoints--;
            }

            if (lowjoker)
            {
                //  if we have the low joker in our hand, we can't capture it
                capturablePoints--;
            }

            if (ten && options.tenOfTrumpReplacesGamePoint)
            {
                //  if we have the ten in our hand, we can't capture it
                capturablePoints--;
            }

            if (!HasPostBidDiscard && options.drawOption == PitchDrawOption.None && options.deck != DeckType.TwoAndNineToAce && options.deck != DeckType.NineToAce)
            {
                //  account for capturable points potentially not being in play
                capturablePoints /= 3; //  roughly approximate the 18/52 or 18/53 odds the card is in play
            }
            else if (!HasPostBidDiscard && options.drawOption != PitchDrawOption.None && trumpInHand < 4)
            {
                //  account for defense having enough trump to withold capturable points
                capturablePoints /= 2;
            }

            //  attempt to shoot if we have the AKQJ2 of trump (in 4-point only)
            if (ace && king && queen && jack && two && options.variation == PitchVariation.FourPoint)
                estimatedPoints = maxPoints;
            //  otherwise try to estimate how many points we can capture
            else
            {
                int estimatedCapture;

                if (ace)
                {
                    //  the ace is automatically worth one and is our best chance to capture other points
                    estimatedCapture = Math.Min(capturablePoints / 2, 2);
                    estimatedPoints += 1 + estimatedCapture;

                    capturablePoints = Math.Max(capturablePoints - estimatedCapture, 0);
                }

                if (offace)
                {
                    //  the off-ace is automatically worth one and has a slightly reduced ability to capture points
                    estimatedCapture = Math.Min(capturablePoints, 1);
                    estimatedPoints += 1 + estimatedCapture;

                    capturablePoints = Math.Max(capturablePoints - estimatedCapture, 0);
                }

                if (king)
                {
                    //  the king has a slightly reduced ability to capture points
                    estimatedCapture = Math.Min(capturablePoints, 1);
                    estimatedPoints += estimatedCapture;

                    //  if we don't have the ace and we don't have a post-bid discard, the King may be high
                    if (!ace && !HasPostBidDiscard)
                        estimatedPoints += 0.5;

                    capturablePoints = Math.Max(capturablePoints - estimatedCapture, 0);
                }

                if (queen)
                {
                    //  the queen may be able to capture a point
                    estimatedCapture = Math.Min(capturablePoints, 1);
                    estimatedPoints += estimatedCapture;

                    //capturablePoints = Math.Max(capturablePoints - estimatedCapture, 0);
                }

                if (jack)
                {
                    //  estimate the liklihood of taking the jack if it's in our hand
                    estimatedPoints += estTrumpCount >= 4 ? 1 : estTrumpCount >= 2 ? 0.5 : 0;
                }

                if (offjack)
                {
                    //  estimate the liklihood of taking the off-jack if it's in our hand
                    estimatedPoints += estTrumpCount >= 4 ? 1 : estTrumpCount >= 2 ? 0.5 : 0;
                }

                if (highjoker)
                {
                    //  estimate the liklihood of taking the high joker if it's in our hand
                    estimatedPoints += estTrumpCount >= 4 ? 1 : estTrumpCount >= 2 ? 0.5 : 0;
                }

                if (lowjoker)
                {
                    //  estimate the liklihood of taking the low joker if it's in our hand
                    estimatedPoints += estTrumpCount >= 4 ? 1 : estTrumpCount >= 2 ? 0.5 : 0;
                }

                if (ten && options.tenOfTrumpReplacesGamePoint)
                {
                    //  estimate the liklihood of taking the ten if it's in our hand
                    estimatedPoints += estTrumpCount >= 4 ? 1 : estTrumpCount >= 2 ? 0.5 : 0;
                }

                if (five)
                {
                    //  estimate the liklihood of taking the five if it's in our hand
                    //  Note: we already deducted this from capturable points above
                    estimatedPoints += estTrumpCount >= 5 ? 5 : estTrumpCount >= 3 ? 2.5 : 0;
                }

                if (three && maxPoints >= 10)
                {
                    //  estimate the liklihood of taking the three if it's in our hand
                    //  Note: we already deducted this from capturable points above
                    estimatedPoints += estTrumpCount >= 5 ? 3 : estTrumpCount >= 3 ? 1.5 : 0;
                }

                if (offthree)
                {
                    //  estimate the liklihood of taking the off-three if it's in our hand
                    estimatedPoints += estTrumpCount >= 5 ? 3 : estTrumpCount >= 3 ? 1.5 : 0;
                }

                if (!options.lowGoesToTaker)
                {
                    if (two)
                    {
                        //  automatically count the two if it goes to the holder
                        estimatedPoints += 1;
                    }
                    else if (three && !HasPostBidDiscard)
                    {
                        //  if we don't have the two and we don't have a post-bid discard, then the three may be low
                        estimatedPoints += 0.5;
                    }
                }

                if (!options.tenOfTrumpReplacesGamePoint)
                {
                    //  a very coarse estimate of capturing the game point
                    var est = (double)estTrumpCount / hand.Count;

                    if (!HasPostBidDiscard)
                    {
                        //  give some credit for high cards (including off-suit)
                        est += (double)hand.Count(c => c.rank == Rank.Ace || c.rank == Rank.King) / hand.Count;
                    }

                    estimatedPoints += Math.Min(est, 1);
                }
            }

            return estimatedPoints;
        }

        private int GamePointsX2(Card card)
        {
            return !options.tenOfTrumpReplacesGamePoint && gamePointsByRank.ContainsKey(card.rank) ? gamePointsByRank[card.rank] : 0;
        }

        private int GetMaxDiscardCount(PlayerBase player, List<Card> legalDiscards)
        {
            if (NumberOfCardsDealtPerPlayer > 6)
                return new Hand(player.Hand).Count - 3; // players must keep at least 3 if we dealt more than 6, currently, 9-point

            if (options.drawOption == PitchDrawOption.None && options.kitty > 0)
                return options.kitty;

            return legalDiscards.Count;
        }

        private Card GoodCardToGiveToPartner(IEnumerable<Card> legalCards, IEnumerable<Card> knownCards)
        {
            //  don't give away the highest cards in trump
            var candidatesToGive = legalCards.Where(c => !IsTrump(c) || (!IsCardHigh(c, knownCards) && RankSort(c) < RankSort(new Card(trump, Rank.Queen)))).ToList();

            //  prefer to give away the card worth the most with the lowest rank (if ties)
            var best = candidatesToGive.Where(c => CardPoints(c) != 0).OrderByDescending(CardPoints).ThenBy(RankSort).FirstOrDefault() ??
                       candidatesToGive.Where(c => c.rank < Rank.King && GamePointsX2(c) > 0).OrderByDescending(GamePointsX2).ThenBy(RankSort).FirstOrDefault();

            return best;
        }

        private bool IsCalledPartner(PlayerBase player)
        {
            return IsCallPartner && options._callPartnerSeat == player.Seat;
        }

        private bool IsTrickWorthTaking(IReadOnlyList<Card> trick)
        {
            return trick.Sum(CardPoints) > 0 || trick.Sum(GamePointsX2) >= 8;
        }

        private bool OnSameTeam(PlayersCollectionBase players, PlayerBase p1, PlayerBase p2)
        {
            //  we're always on our own team
            if (p1.Seat == p2.Seat)
                return true;

            //  special-case "call your partner"
            if (IsCallPartner)
            {
                //  if we're the pitcher, then our partner is the called partner
                if (IsPitching(p1))
                    return IsCalledPartner(p2);

                //  if we're the called partner, then our partner is the pitcher
                if (IsCalledPartner(p1))
                    return IsPitching(p2);

                //  otherwise our partners are anyone who is not the pitcher and not the called partner
                return !IsPitching(p2) && !IsCalledPartner(p2);
            }

            //  else use the normal partner check
            return players.PartnerOf(p1) == p2;
        }

        private int PlayCardSort(Card card)
        {
            //  we can play the two earlier if the point goes to the holder (not the taker)
            if (!options.lowGoesToTaker && IsTrump(card) && card.rank == Rank.Two)
                return 1;

            return DiscardSort(card);
        }
    }
}