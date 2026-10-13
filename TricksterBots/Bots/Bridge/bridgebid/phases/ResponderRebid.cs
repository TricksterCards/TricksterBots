using System;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal class ResponderRebid
    {
        public static void Interpret(InterpretedBid rebid)
        {
            if (SecondCall(rebid))
                return;

            TryPlaceContract(rebid);
        }

        //  responder's second call in an uncontested auction after opener's suit opening and rebid
        private static bool SecondCall(InterpretedBid bid)
        {
            if (!bid.bidIsDeclare || bid.Index < 6 || bid.Options.bidding == BridgeBiddingScheme.Acol)
                return false;

            var opening = bid.History[bid.Index - 6];
            var response = bid.History[bid.Index - 4];
            var openerRebid = bid.History[bid.Index - 2];
            if (!opening.bidIsDeclare || opening.declareBid.suit == Suit.Unknown || opening.declareBid.level != 1 ||
                !response.bidIsDeclare || !openerRebid.bidIsDeclare || response.BidConvention != BidConvention.None ||
                openerRebid.BidConvention != BidConvention.None)
                return false;

            for (var i = bid.Index - 1; i >= 0; i -= 2)
                if (bid.History[i].bid != BidBase.Pass)
                    return false;

            var suit = bid.declareBid.suit;
            var level = bid.declareBid.level;
            var isCheapest = level == bid.LowestAvailableLevel(suit, true);
            var openingSuit = opening.declareBid.suit;
            var rebidSuit = openerRebid.declareBid.suit;
            var firstSuit = response.declareBid.suit;
            var partner = new InterpretedBid.PlayerSummary(bid.History, bid.Index - 2);
            var partnerMin = Math.Max(partner.Points.Min, 12);
            var isNewSuit = suit != Suit.Unknown && suit != openingSuit && suit != rebidSuit && suit != firstSuit;

            if (firstSuit == Suit.Unknown && response.declareBid.level == 1 && isNewSuit && isCheapest && level == 2)
            {
                //  1S-1N-2D-2H: after a 1NT response, a new suit is to play (but raise partner's second suit with 4)
                bid.Points.Max = 9;
                bid.HandShape[suit].Min = 6;
                bid.BidMessage = BidMessage.Signoff;
                bid.Description = $"To play; 6+ {suit}";
                if (rebidSuit != Suit.Unknown)
                    bid.Validate = hand => BasicBidding.CountsBySuit(hand)[rebidSuit] < 4;
                return true;
            }

            if (isNewSuit && level == 1)
            {
                //  1C-1D-1H-1S: natural and forcing for one round
                bid.BidMessage = BidMessage.Forcing;
                bid.Points.Min = 6;
                bid.HandShape[suit].Min = 4;
                bid.Description = $"New suit; 4+ {suit}";
                return true;
            }

            if (rebidSuit == Suit.Unknown && openerRebid.declareBid.level == 1 && isNewSuit && firstSuit != Suit.Unknown)
            {
                if (isCheapest && level == 2 && BridgeBot.suitRank[suit] < BridgeBot.suitRank[firstSuit])
                {
                    //  1C-1H-1N-2D: a weak two-suiter, not forcing
                    bid.BidPointType = BidPointType.Hcp;
                    bid.Points.Max = 10;
                    bid.HandShape[suit].Min = 5;
                    bid.Description = $"Not forcing; 5+ {suit}";
                    return true;
                }

                if (level == bid.LowestAvailableLevel(suit, true) + 1 && level <= 3)
                {
                    //  1C-1H-1N-3D: a jump shift is game forcing (with shortness in an unbid suit; otherwise just bid 3NT)
                    var ourSuits = new[] { openingSuit, firstSuit, suit };
                    bid.BidMessage = BidMessage.GameForcing;
                    bid.Points.Min = 25 - partnerMin;
                    bid.HandShape[suit].Min = 5;
                    bid.Description = $"Game forcing; 5+ {suit}";
                    bid.Validate = hand =>
                    {
                        var counts = BasicBidding.CountsBySuit(hand);
                        return SuitRank.stdSuits.Any(s => !ourSuits.Contains(s) && counts[s] <= 1);
                    };
                    return true;
                }
            }

            if (rebidSuit != Suit.Unknown && rebidSuit != openingSuit && rebidSuit != firstSuit && suit == openingSuit && isCheapest && level <= 3)
            {
                //  1H-1S-2C-2H: preference to opener's first suit (which is at least as long as his second);
                //  while 1NT is available, only with a real fit
                var isForced = openerRebid.BidMessage == BidMessage.Forcing;
                var oneNotrumpAvailable = bid.LowestAvailableLevel(Suit.Unknown, true) == 1;
                var unbidSuits = SuitRank.stdSuits.Where(s => s != openingSuit && s != firstSuit && s != rebidSuit).ToList();
                bid.BidPointType = BidPointType.Hcp;
                bid.Points.Max = isForced ? 37 : 24 - partnerMin;
                bid.HandShape[suit].Min = 2;
                bid.BidMessage = isForced ? BidMessage.Invitational : BidMessage.Signoff;
                bid.Description = $"Preference; 2+ {suit}";
                bid.Validate = hand =>
                {
                    var counts = BasicBidding.CountsBySuit(hand);
                    if (oneNotrumpAvailable)
                        return counts[suit] >= 4;

                    //  with a doubleton and opening values, prefer fourth suit forcing (or notrump) to a waiting preference;
                    //  at the 3-level, with the unbid suit stopped, 3NT describes the hand better
                    if (isForced && counts[suit] == 2 && (BasicBidding.ComputeHighCardPoints(hand) > 12 ||
                                                          level == 3 && unbidSuits.Count > 0 && unbidSuits.All(s => BasicBidding.HasStopper(hand, s))))
                        return false;

                    return counts[suit] >= counts[rebidSuit] || counts[suit] >= 3 && counts[rebidSuit] < 4 || isForced && counts[rebidSuit] < 4;
                };
                return true;
            }

            if (suit != Suit.Unknown && suit == firstSuit && isCheapest && openerRebid.BidMessage == BidMessage.Forcing && rebidSuit != openingSuit &&
                rebidSuit != Suit.Unknown && BridgeBot.suitRank[rebidSuit] > BridgeBot.suitRank[openingSuit] && openerRebid.declareBid.level == 2)
            {
                //  1D-1S-2H-2S: after opener's reverse, rebid a 5-card suit with a weak hand
                bid.Points.Max = 9;
                bid.HandShape[suit].Min = 5;
                bid.Description = $"Minimum; 5+ {suit}";
                return true;
            }

            return false;
        }

        public static void TryPlaceContract(InterpretedBid bid)
        {
            if (!bid.bidIsDeclare)
                return;

            //  try to answer the questions HOW HIGH and WHERE
            var isNT = bid.declareBid.suit == Suit.Unknown;
            var playerSummary = new InterpretedBid.PlayerSummary(bid.History, bid.Index - 4);
            var partnerSummary = new InterpretedBid.PlayerSummary(bid.History, bid.Index - 2);
            var playerMinOfSuit = isNT ? 0 : playerSummary.HandShape[bid.declareBid.suit].Min;
            var partnerMinOfSuit = isNT ? 0 : partnerSummary.HandShape[bid.declareBid.suit].Min;

            if (!isNT && bid.History[bid.Index - 4].bid == BridgeBid.Redouble && partnerMinOfSuit < 3 &&
                bid.History[bid.Index - 6].bidIsDeclare && bid.History[bid.Index - 6].declareBid.suit == Suit.Unknown &&
                bid.History[bid.Index - 2].bidIsDeclare && bid.History[bid.Index - 2].declareBid.suit != bid.declareBid.suit &&
                bid.declareBid.level == bid.LowestAvailableLevel(bid.declareBid.suit, true))
            {
                //  1N-(X)-XX-(P)-2C-(P)-2D: partner ran from the redouble, so a new suit with a weak hand is to play
                bid.Points.Max = 7;
                bid.BidMessage = BidMessage.Signoff;
                bid.HandShape[bid.declareBid.suit].Min = 5;
                bid.Description = $"To play; 5+ {bid.declareBid.suit}";
                return;
            }

            if (!isNT && bid.declareBid.level < 4 && partnerMinOfSuit < 3 && playerMinOfSuit < 3)
            {
                //  a new suit by responder is FORCING (unless opener rebid 1NT and responder's second suit is a non-jump bid that is lower ranking than responder's first suit)
                bid.BidMessage = BidMessage.Forcing;
                bid.Points.Min = bid.GamePoints - partnerSummary.Points.Min;
                bid.HandShape[bid.declareBid.suit].Min = 4;
                bid.Description = "New suit";
                return;
            }

            if (!isNT && playerMinOfSuit > 0 && partnerMinOfSuit == 0 && bid.declareBid.level < bid.GameLevel &&
                bid.declareBid.level == bid.LowestAvailableLevel(bid.declareBid.suit, true) &&
                playerSummary.Points.Max + partnerSummary.Points.Min < InterpretedBid.InvitationalPoints)
            {
                //  responder already denied invitational values, so rebidding a long suit is to play
                bid.Points.Max = playerSummary.Points.Max;
                bid.BidPointType = BidPointType.Hcp;
                bid.BidMessage = BidMessage.Signoff;
                bid.HandShape[bid.declareBid.suit].Min = 6;
                bid.Description = $"To play; 6+ {bid.declareBid.suit}";
                return;
            }

            if (bid.declareBid.level >= 6)
            {
                //  6X, 7X
                //  small or grand slam (minimum 33 or 37 combined points, respectively)
                bid.Points.Min = (bid.declareBid.level == 6 ? InterpretedBid.SmallSlamPoints : InterpretedBid.GrandSlamPoints) - partnerSummary.Points.Min;
                bid.Points.Max = bid.declareBid.level == 6 ? InterpretedBid.GrandSlamPoints - 1 - partnerSummary.Points.Min : InterpretedBid.GrandSlamPoints;
                bid.BidPointType = BidPointType.Hcp;
                bid.BidMessage = BidMessage.Signoff;
                bid.Description = $"{(bid.declareBid.level == 6 ? "Small" : "Grand")} slam";
            }
            else if (bid.declareBid.level == 4 && isNT)
            {
                //  4N
                bid.Points.Min = InterpretedBid.SmallSlamPoints - 1 - partnerSummary.Points.Min;
                bid.BidPointType = BidPointType.Hcp;
                bid.BidConvention = BidConvention.Blackwood;
                bid.BidMessage = BidMessage.Forcing;
                bid.Description = "asking for Aces";
                //  TODO: validate knowing count of Aces will help decision to bid slam
                bid.Validate = hand => false;
            }
            else if (bid.declareBid.level == bid.GameLevel && (isNT || playerMinOfSuit > 0 || partnerMinOfSuit > 0))
            {
                //  sign-off at game of a previously bid suit or in notrump: 3NT, 4H, 4S, 5C, 5D
                //  (3NT does not require a balanced partner; with game values and no fit it's the standard spot,
                //  and suit games still rank higher when a fit is known)
                bid.Points.Min = bid.GamePoints - partnerSummary.Points.Min;
                bid.BidMessage = BidMessage.Signoff;
                bid.Description = "Sign-off at game";

                //  opposite an unbalanced partner, 3NT needs a balanced-ish hand and the unbid/opponents' suits stopped
                if (isNT && !partnerSummary.IsBalanced)
                    bid.Validate = NotrumpValidator(bid);

                //  after partner's natural 2NT jump rebid (18-19 balanced), prefer 3NT to five of a minor
                var partnerLast = bid.History[bid.Index - 2];
                if (BridgeBot.IsMinor(bid.declareBid.suit) && partnerLast.bidIsDeclare && partnerLast.declareBid.suit == Suit.Unknown &&
                    partnerLast.declareBid.level == 2 && partnerLast.BidConvention == BidConvention.None && partnerSummary.Points.Min >= 18 &&
                    bid.LowestAvailableLevel(Suit.Unknown, true) <= 3)
                    bid.Validate = hand => false;
            }
            else if (bid.declareBid.level == (isNT ? 2 : 3))
            {
                //  invite game: 2NT, 3 of a previously bid suit
                bid.Points.Min = InterpretedBid.InvitationalPoints - partnerSummary.Points.Min;
                bid.Points.Max = bid.GamePoints - 1 - partnerSummary.Points.Min;
                //  2NT is expected to be balanced (higher NT is not), though opposite an unbalanced partner stoppers matter more
                bid.IsBalanced = isNT && partnerSummary.IsBalanced;
                if (isNT && !partnerSummary.IsBalanced)
                {
                    //  (but a known major fit comes first)
                    var stoppers = NotrumpValidator(bid);
                    bid.Priority = 95;
                    bid.Validate = hand =>
                    {
                        var counts = BasicBidding.CountsBySuit(hand);
                        return stoppers(hand) && !SuitRank.stdSuits.Any(s => BridgeBot.IsMajor(s) && partnerSummary.HandShape[s].Min > 0 &&
                                                                             counts[s] + partnerSummary.HandShape[s].Min >= 8);
                    };
                }
                bid.Description = "Inviting game";
            }
            else if (bid.declareBid.level == (isNT ? 1 : 2))
            {
                //  sign-off in partscore: Pass, 1NT, 2 of a previously bid suit
                //  (if partner's range is still wide, this just shows a minimum, and partner may continue)
                bid.Points.Min = 19 - partnerSummary.Points.Min;
                bid.Points.Max = InterpretedBid.InvitationalPoints - 1 - partnerSummary.Points.Min;
                bid.BidMessage = partnerSummary.Points.Max <= 15 ? BidMessage.Signoff : BidMessage.Invitational;
                bid.Description = "No interest in game";
                //  opposite an unbalanced partner, notrump above the 1-level needs stoppers
                if (isNT && !partnerSummary.IsBalanced && bid.declareBid.level >= 2)
                    bid.Validate = NotrumpValidator(bid);
            }

            if (bid.Points.Min > 0)
            {
                //  we picked a bid above
                if (isNT)
                {
                    //  ensure we count HCP for NT bids
                    bid.BidPointType = BidPointType.Hcp;
                }
                else
                {
                    var firstTeamIndexInSuit =
                        bid.History.FindIndex(b => (bid.Index - b.Index) % 2 == 0 && b.bidIsDeclare && b.declareBid.suit == bid.declareBid.suit);
                    if (firstTeamIndexInSuit != -1 && (bid.Index - firstTeamIndexInSuit) % 4 != 0)
                    {
                        //  use dummy points if our partner bid the suit first (meaning we'll be the dummy),
                        //  but shortness in partner's other suit isn't worth anything
                        bid.BidPointType = BidPointType.Dummy;
                        //  (only when raising partner's second suit after our own unlimited response)
                        var trump = bid.declareBid.suit;
                        var partnerOpening = bid.History.Take(bid.Index).FirstOrDefault(b => (bid.Index - b.Index) % 4 == 2 && b.bidIsDeclare);
                        var partnerOtherSuits = SuitRank.stdSuits.Where(s => s != trump && partnerSummary.HandShape[s].Min >= 4).ToList();
                        if (partnerOtherSuits.Count > 0 && playerSummary.Points.Max >= 37 && partnerOpening != null && partnerOpening.declareBid.suit != trump)
                            bid.PointCounter = hand =>
                            {
                                var counts = BasicBidding.CountsBySuit(hand);
                                return BasicBidding.ComputeHighCardPoints(hand) + SuitRank.stdSuits
                                    .Where(s => s != trump && !partnerOtherSuits.Contains(s))
                                    .Sum(s => counts[s] == 0 ? 5 : counts[s] == 1 ? 3 : counts[s] == 2 ? 1 : 0);
                            };
                    }

                    if (partnerMinOfSuit > 0)
                    {
                        //  if partner has shown length, ensure 8+ card combined fit in chosen suit
                        //  (if partner raised a suit we've shown, the fit is agreed, so our shown length is enough)
                        var fitLength = 8 - partnerSummary.HandShape[bid.declareBid.suit].Min;
                        bid.HandShape[bid.declareBid.suit].Min = playerMinOfSuit >= 4 && partnerMinOfSuit >= 3 ? Math.Min(fitLength, playerMinOfSuit) : fitLength;
                    }
                    else if (playerMinOfSuit > 0)
                    {
                        //  with no info from partner in our suit...
                        if (bid.declareBid.level < bid.GameLevel)
                        {
                            //  we need 6+ cards to rebid our suit below game (and invitations count HCP, as length is already promised)
                            bid.HandShape[bid.declareBid.suit].Min = 6;
                            if (bid.declareBid.level == 3)
                                bid.BidPointType = BidPointType.Hcp;
                            //  with a known 8-card fit in partner's suit, show support instead of rebidding a minor
                            if (BridgeBot.IsMinor(bid.declareBid.suit))
                                bid.Validate = hand =>
                                {
                                    var counts = BasicBidding.CountsBySuit(hand);
                                    return !SuitRank.stdSuits.Any(s => s != bid.declareBid.suit &&
                                        partnerSummary.HandShape[s].Min > 0 && counts[s] + partnerSummary.HandShape[s].Min >= 8);
                                };
                        }
                        else if (bid.declareBid.level == bid.GameLevel)
                            //  we need 7+ cards to rebid our suit at game
                            bid.HandShape[bid.declareBid.suit].Min = 7;
                    }

                    //  rebidding our own long suit to invite counts HCP (the length is already promised)
                    if (bid.declareBid.level == 3 && bid.declareBid.level < bid.GameLevel && playerMinOfSuit > 0 && partnerMinOfSuit < 3)
                        bid.BidPointType = BidPointType.Hcp;

                    //  once partner has raised our suit, a different suit below game doesn't earn dummy points
                    var lastPartnerBid = bid.History[bid.Index - 2];
                    if (lastPartnerBid.bidIsDeclare && lastPartnerBid.declareBid.suit != Suit.Unknown && lastPartnerBid.declareBid.suit != bid.declareBid.suit &&
                        playerSummary.HandShape[lastPartnerBid.declareBid.suit].Min >= 4 && partnerSummary.HandShape[lastPartnerBid.declareBid.suit].Min >= 3 &&
                        bid.declareBid.level < bid.GameLevel)
                    {
                        bid.BidPointType = BidPointType.Hcp;
                        bid.PointCounter = null;
                    }

                    //  a minor-suit invitation is really aiming for 3NT
                    if (BridgeBot.IsMinor(bid.declareBid.suit) && bid.declareBid.level < bid.GameLevel && bid.BidMessage != BidMessage.Signoff &&
                        bid.Points.Max < 37 && bid.Points.Min > 0)
                        bid.Points.Max = Math.Min(bid.Points.Max, 25 - 1 - partnerSummary.Points.Min);

                    //  rebidding our own suit at game needs 7 cards, or a solid 6-card suit
                    if (bid.declareBid.level == bid.GameLevel && playerMinOfSuit > 0 && partnerMinOfSuit == 0)
                    {
                        var ownSuit = bid.declareBid.suit;
                        bid.HandShape[ownSuit].Min = 6;
                        bid.Validate = hand => hand.Count(c => c.suit == ownSuit) >= 7 ||
                                               hand.Count(c => c.suit == ownSuit && c.rank >= Rank.Queen) == 3;
                    }

                    if (bid.HandShape[bid.declareBid.suit].Min > 0) bid.Description += $"; {bid.HandShape[bid.declareBid.suit].Min}+ {bid.declareBid.suit}";
                }
            }
        }

        //  no singleton/void, plus a stopper in every suit our side hasn't bid naturally (including the opponents' suits and
        //  an artificial fourth suit), unless partner has bid notrump naturally (showing a balanced hand with those stoppers)
        internal static HandValidator NotrumpValidator(InterpretedBid bid)
        {
            var ourBids = bid.History.Take(bid.Index).Where(b => (bid.Index - b.Index) % 2 == 0 && b.bidIsDeclare && b.BidConvention == BidConvention.None).ToList();
            if (ourBids.Any(b => (bid.Index - b.Index) % 4 == 2 && b.declareBid.suit == Suit.Unknown))
                return hand => true;

            var ourSuits = ourBids.Where(b => b.declareBid.suit != Suit.Unknown).Select(b => b.declareBid.suit).ToList();
            var suitsToStop = SuitRank.stdSuits.Where(s => !ourSuits.Contains(s)).ToList();

            return hand =>
            {
                var counts = BasicBidding.CountsBySuit(hand);
                return SuitRank.stdSuits.All(s => counts[s] >= 2) && suitsToStop.All(s => BasicBidding.HasStopper(hand, s));
            };
        }
    }
}