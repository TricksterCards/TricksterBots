using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal class OpenerRebid
    {
        public static void Interpret(InterpretedBid rebid)
        {
            var opening = rebid.History[rebid.Index - 4];
            var response = rebid.History[rebid.Index - 2];

            if (!rebid.bidIsDeclare)
            {
                if (rebid.bid == BridgeBid.Double)
                {
                    if (!SacrificeDouble(opening, response, rebid))
                        ReopeningDouble(opening, response, rebid);
                }
                else if (rebid.bid == BidBase.Pass && IsPartnersPenaltyDouble(response, rebid))
                {
                    //  leave partner's penalty double in unless we're void in their suit
                    var theirSuit = rebid.History[rebid.Index - 3].declareBid.suit;
                    rebid.Description = "Accept penalty double";
                    rebid.Validate = hand => BasicBidding.CountsBySuit(hand)[theirSuit] > 0;
                }
                //  TODO: handle redouble rebids
            }
            else if (opening.declareBid.suit != Suit.Unknown && IsPartnersPenaltyDouble(response, rebid) &&
                     rebid.declareBid.suit == opening.declareBid.suit && rebid.declareBid.level == rebid.LowestAvailableLevel(rebid.declareBid.suit, true))
            {
                //  pulling partner's penalty double with a void in their suit and a long suit of our own
                var theirSuit = rebid.History[rebid.Index - 3].declareBid.suit;
                rebid.HandShape[rebid.declareBid.suit].Min = 6;
                rebid.HandShape[theirSuit].Max = 0;
                rebid.Points.Max = 15;
                rebid.Description = $"6+ {rebid.declareBid.suit}; no defense against {theirSuit}";
            }
            else if (opening.declareBid.suit == Suit.Unknown)
            {
                RebidNTOpening(opening, response, rebid);
            }
            else if (opening.declareBid.level == 1)
            {
                RebidSuitOpening(opening, response, rebid);
            }
            else
            {
                RebidPreemptOpening(opening, response, rebid);
            }
        }

        private static void RebidNTOpening(InterpretedBid opening, InterpretedBid response, InterpretedBid rebid)
        {
            // If responder invites with 2NT then bid 3NT with 16-17 HCP
            if (response.bidIsDeclare && response.declareBid.level == 2 && response.declareBid.suit == Suit.Unknown 
                && rebid.declareBid.level == 3 && rebid.declareBid.suit == Suit.Unknown)
            {
                rebid.Points.Min = 16;
                rebid.Points.Max = 17;
                rebid.BidPointType = BidPointType.Hcp;
                rebid.Description = "Accept invitation and sign-off at game";
                return;
            }
            

            if (response.bidIsDeclare && response.declareBid.level == 4 && response.declareBid.suit == Suit.Unknown)
                //  rebid after a 4NT response
                if (rebid.declareBid.suit == Suit.Unknown)
                    switch (rebid.declareBid.level)
                    {
                        //  1N-4N-5N
                        //  2N-4N-5N
                        //  3N-4N-5N
                        case 5:
                            rebid.Validate = hand => false;
                            break;

                        //  1N-4N-6N
                        //  2N-4N-6N
                        //  3N-4N-6N
                        //  accept the quantitative invitation unless we're minimum
                        case 6:
                            rebid.Points.Min = InterpretedBid.SmallSlamPoints - response.Points.Max;
                            rebid.BidPointType = BidPointType.Hcp;
                            rebid.BidMessage = BidMessage.Signoff;
                            rebid.Description = "Accept invitation to small slam";
                            break;
                    }
        }

        //  1S-(3D)-3S-(5D)-X: with a fit and the balance of power, punish the opponents' sacrifice
        private static bool SacrificeDouble(InterpretedBid opening, InterpretedBid response, InterpretedBid rebid)
        {
            var theirBid = rebid.History[rebid.Index - 1];
            if (!theirBid.bidIsDeclare || theirBid.declareBid.level < 5 || !opening.bidIsDeclare || !response.bidIsDeclare ||
                response.declareBid.suit != opening.declareBid.suit || opening.declareBid.suit == Suit.Unknown)
                return false;

            rebid.BidPointType = BidPointType.Hcp;
            rebid.Points.Min = 12;
            rebid.Description = "Penalty; we have the balance of power";
            return true;
        }

        //  the suits the opponents have bid naturally (artificial bids like Michaels don't show the suit bid)
        private static List<Suit> TheirSuits(InterpretedBid rebid)
        {
            return rebid.History.Take(rebid.Index)
                .Where(b => (rebid.Index - b.Index) % 2 == 1 && b.bidIsDeclare && b.declareBid.suit != Suit.Unknown && b.BidConvention == BidConvention.None)
                .Select(b => b.declareBid.suit).Distinct().ToList();
        }

        private static bool HasStoppersInTheirSuits(InterpretedBid rebid, Hand hand)
        {
            return TheirSuits(rebid).All(s => BasicBidding.HasStopper(hand, s));
        }

        //  after interference, a natural notrump rebid promises a stopper in each suit the opponents have bid
        //  (AlternateMatches bypasses Validate, so it needs the stoppers too)
        private static void RequireStoppersInTheirSuits(InterpretedBid rebid)
        {
            var theirSuits = TheirSuits(rebid);
            if (theirSuits.Count == 0)
                return;

            HandValidator hasStoppers = hand => theirSuits.All(s => BasicBidding.HasStopper(hand, s));
            var validate = rebid.Validate;
            rebid.Validate = hand => hasStoppers(hand) && (validate == null || validate(hand));
            var alternateMatches = rebid.AlternateMatches;
            if (alternateMatches != null)
                rebid.AlternateMatches = hand => hasStoppers(hand) && alternateMatches(hand);

            var stopperDescription = $"stopper in {string.Join(" and ", theirSuits)}";
            rebid.Description = string.IsNullOrEmpty(rebid.Description) ? stopperDescription : $"{rebid.Description}; {stopperDescription}";
        }

        //  1X-(suit)-X(penalty)-(P)-?
        private static bool IsPartnersPenaltyDouble(InterpretedBid response, InterpretedBid rebid)
        {
            var overcall = rebid.History[rebid.Index - 3];
            return response.bid == BridgeBid.Double && response.BidConvention == BidConvention.None && overcall.bidIsDeclare &&
                   overcall.declareBid.suit != Suit.Unknown && rebid.History[rebid.Index - 1].bid == BidBase.Pass;
        }

        //  1S-(2D)-P-(P)-X: with shortness in their suit, opener reopens with a takeout double (partner may pass for penalty)
        //  1D-(P)-P-(1S)-X: in the direct seat, the double also shows extra values
        private static void ReopeningDouble(InterpretedBid opening, InterpretedBid response, InterpretedBid rebid)
        {
            if (!opening.bidIsDeclare || opening.declareBid.suit == Suit.Unknown || opening.declareBid.level != 1 ||
                opening.BidConvention != BidConvention.None || response.bid != BidBase.Pass)
                return;

            var isDirect = rebid.History[rebid.Index - 1].bidIsDeclare;
            var theirBid = isDirect ? rebid.History[rebid.Index - 1] : rebid.History[rebid.Index - 3];
            if (!theirBid.bidIsDeclare || theirBid.declareBid.suit == Suit.Unknown || theirBid.declareBid.suit == opening.declareBid.suit ||
                theirBid.declareBid.level > 2 || !isDirect && rebid.History[rebid.Index - 1].bid != BidBase.Pass)
                return;

            var theirSuit = theirBid.declareBid.suit;
            var ourSuit = opening.declareBid.suit;
            rebid.BidConvention = BidConvention.TakeoutDouble;
            rebid.BidMessage = BidMessage.Forcing;
            rebid.Points.Min = isDirect ? 16 : 13;
            //  stronger hands make a natural forcing bid instead
            rebid.Points.Max = 18;
            rebid.HandShape[theirSuit].Max = 2;
            rebid.Priority = 95;
            rebid.Description = isDirect ? "Takeout; extra values" : "Reopening takeout double";
            //  with a 6-card suit, rebid it instead
            rebid.Validate = hand => BasicBidding.CountsBySuit(hand)[ourSuit] < 6;
        }

        internal static void RebidPreemptOpening(InterpretedBid opening, InterpretedBid response, InterpretedBid rebid)
        {
            if (!response.bidIsDeclare || !rebid.bidIsDeclare || response.BidMessage == BidMessage.Signoff)
                return;

            if (response.declareBid.suit != opening.declareBid.suit)
            {
                if (rebid.declareBid.suit == Suit.Unknown)
                {
                    //  maximum, bid notrump
                    rebid.Points.Min = 9;
                    rebid.Points.Max = 10;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.Description = "Maximum";
                }
                else if (rebid.declareBid.suit == response.declareBid.suit)
                {
                    if (BridgeBot.IsMajor(rebid.declareBid.suit))
                    {
                        //  raise a new major with 3-card support or a doubleton honor
                        rebid.HandShape[rebid.declareBid.suit].Min = 3;
                        rebid.Description = $"3+ {rebid.declareBid.suit}";
                    }
                }
                else if (rebid.declareBid.suit == opening.declareBid.suit)
                {
                    //  minimum, rebid original suit
                    rebid.Points.Min = 5;
                    rebid.Points.Max = 8;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.Description = "Minimum";
                }
                else
                {
                    //  maximum, name a new suit
                    rebid.Points.Min = 9;
                    rebid.Points.Max = 10;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.Description = "Maximum";
                    //  TODO: what makes us pick this over the NT option above?
                    rebid.Validate = hand => false;
                }
            }
        }

        private static void RebidSuitOpening(InterpretedBid opening, InterpretedBid response, InterpretedBid rebid)
        {
            var lowestAvailableLevel = rebid.LowestAvailableLevel(rebid.declareBid.suit);
            var responseSuit = response.bidIsDeclare ? response.declareBid.suit : Suit.Unknown;
            var isNaturalNewSuitResponse = response.bidIsDeclare && responseSuit != Suit.Unknown && responseSuit != opening.declareBid.suit &&
                                           response.BidConvention == BidConvention.None;
            var respondedAtOneLevel = isNaturalNewSuitResponse && response.declareBid.level == 1;
            var respondedTwoOverOne = isNaturalNewSuitResponse && response.declareBid.level == 2 &&
                                      response.declareBid.level == response.LowestAvailableLevel(responseSuit, true);

            //  with a second 5-card suit (and no interference), show it rather than jump in our first suit
            bool IsTwoSuited(Hand hand)
            {
                if (rebid.History[rebid.Index - 1].bidIsDeclare)
                    return false;

                var counts = BasicBidding.CountsBySuit(hand);
                return SuitRank.stdSuits.Any(s => s != opening.declareBid.suit && s != responseSuit && counts[s] >= 5);
            }

            if (TakeoutDouble.IsJordan(response))
            {
                //  1M-(X)-2N-(P)-3M: minimum; 1M-(X)-2N-(P)-4M: accept the limit raise
                if (rebid.declareBid.suit == opening.declareBid.suit)
                {
                    rebid.BidMessage = BidMessage.Signoff;
                    if (rebid.declareBid.level == 3)
                    {
                        rebid.Points.Max = 14;
                        rebid.Description = "Minimum";
                    }
                    else if (rebid.declareBid.level == 4)
                    {
                        rebid.Points.Min = 15;
                        rebid.Description = "Sign-off at game";
                    }
                }

                return;
            }

            //  partner raised or otherwise showed 3+ support (e.g. a cuebid raise like 1S-(3D)-4D)
            var partnerShowedFit = response.bidIsDeclare && opening.declareBid.suit == response.declareBid.suit ||
                                   response.HandShape[opening.declareBid.suit].Min >= 3;
            if (partnerShowedFit && rebid.declareBid.suit == Suit.Unknown && rebid.declareBid.level == 4)
            {
                //  1M-2M-4N, 1M-3M-4N, 1S-(3D)-4D-4N: with a fit agreed, 4NT asks for Aces
                rebid.BidConvention = BidConvention.Blackwood;
                rebid.BidMessage = BidMessage.Forcing;
                rebid.Description = "asking for Aces";
                //  TODO: validate knowing count of Aces will help decision to bid slam
                rebid.Validate = hand => false;
                return;
            }

            if (response.bidIsDeclare && opening.declareBid.suit == response.declareBid.suit)
            {
                //  partner raised our suit; revalue as declarer with the fit (length plus side shortness)
                var trump = opening.declareBid.suit;
                int FitPoints(Hand hand) => BasicBidding.ComputeFitPoints(hand, trump, false);

                if (response.declareBid.level == 2 && rebid.History[rebid.Index - 1].bidIsDeclare)
                {
                    //  1C-2C-(3D)-3H: competing with extra values
                    var theirSuit = rebid.History[rebid.Index - 1].declareBid.suit;
                    if (rebid.declareBid.suit != trump && rebid.declareBid.suit != theirSuit && rebid.declareBid.suit != Suit.Unknown &&
                        rebid.declareBid.level == rebid.LowestAvailableLevel(rebid.declareBid.suit, true) && rebid.declareBid.level < rebid.GameLevel)
                    {
                        rebid.BidMessage = BidMessage.Forcing;
                        rebid.Points.Min = 16;
                        rebid.HandShape[rebid.declareBid.suit].Min = 4;
                        rebid.Description = $"Extra values; 4+ {rebid.declareBid.suit}";
                    }
                }
                else if (response.declareBid.level == 2)
                {
                    //  partner raised our suit to the two-level
                    if (rebid.declareBid.level < rebid.GameLevel && rebid.declareBid.level == lowestAvailableLevel)
                    {
                        if (rebid.declareBid.suit == Suit.Unknown)
                        {
                            //  1C-2C-2N
                            //  1D-2D-2N
                            //  1H-2H-2N
                            //  1S-2S-2N
                            rebid.Points.Min = 17;
                            rebid.Points.Max = 18;
                            rebid.PointCounter = FitPoints;
                            rebid.IsBalanced = true;
                            rebid.Description = "inviting game";
                        }
                        else if (rebid.declareBid.suit != response.declareBid.suit && BridgeBot.IsMinor(trump))
                        {
                            //  1C-2C-2D
                            //  1D-2D-2H
                            //  ...
                            //  after a minor raise, a new suit shows values there (looking for 3NT or game)
                            rebid.BidMessage = BidMessage.Forcing;
                            rebid.Points.Min = 17;
                            rebid.PointCounter = FitPoints;
                            rebid.HandShape[rebid.declareBid.suit].Min = 4;
                            rebid.Description = $"4+ {rebid.declareBid.suit}; game interest";
                        }
                        else if (rebid.declareBid.suit != response.declareBid.suit)
                        {
                            //  1H-2H-3C
                            //  1H-2H-3D
                            //  1H-2H-2S
                            //  ...
                            rebid.BidConvention = BidConvention.HelpSuitGameTry;
                            rebid.BidMessage = BidMessage.Forcing;
                            rebid.Points.Min = 17;
                            rebid.Points.Max = 18;
                            rebid.PointCounter = FitPoints;
                            rebid.HandShape[rebid.declareBid.suit].Min = 4;
                            rebid.Description = "inviting game";
                            rebid.Validate = hand => !BasicBidding.IsGoodSuit(hand, rebid.declareBid.suit, 4);
                        }
                        else
                        {
                            //  1C-2C-3C
                            //  1D-2D-3D
                            //  1H-2H-3H
                            //  1S-2S-3S
                            rebid.Points.Min = 17;
                            rebid.Points.Max = 18;
                            rebid.PointCounter = FitPoints;
                            rebid.Description = "Inviting game";
                            rebid.HandShape[rebid.declareBid.suit].Min = 6;
                            rebid.IsPreemptive = true;
                            rebid.Description = $"6+ {rebid.declareBid.suit}";
                        }
                    }
                    else if (rebid.declareBid.level == rebid.GameLevel)
                    {
                        if (rebid.declareBid.suit == response.declareBid.suit ||
                            rebid.declareBid.suit == Suit.Unknown && BridgeBot.IsMinor(opening.declareBid.suit))
                        {
                            //  1C-2C-3N (5C if 3N is not available)
                            //  1D-2D-3N (5D if 3N is not available)
                            //  1H-2H-4H
                            //  1S-2S-4S
                            rebid.BidMessage = BidMessage.Signoff;
                            rebid.Points.Min = 19;
                            rebid.Points.Max = 21;
                            if (rebid.declareBid.suit == Suit.Unknown)
                                rebid.BidPointType = BidPointType.Hcp;
                            else
                            {
                                rebid.Points.Max = 37;
                                rebid.PointCounter = FitPoints;
                            }

                            rebid.Description = "Sign-off at game";

                            if (rebid.declareBid.suit == Suit.Unknown)
                                RequireStoppersInTheirSuits(rebid);
                            //  prefer playing game in notrump for minor suits (when we can stop their suits)
                            else if (BridgeBot.IsMinor(rebid.declareBid.suit) && rebid.LowestAvailableLevel(Suit.Unknown, true) <= 3)
                                rebid.Validate = hand => !HasStoppersInTheirSuits(rebid, hand);
                        }
                    }
                }
                else if (response.declareBid.level == 3 && rebid.declareBid.level == rebid.GameLevel)
                {
                    //  partner raised our suit to the three level (limit raise)
                    if (rebid.declareBid.suit == response.declareBid.suit ||
                        rebid.declareBid.suit == Suit.Unknown && BridgeBot.IsMinor(opening.declareBid.suit))
                    {
                        //  1C-2C-3N (5C if 3N is not available)
                        //  1D-2D-3N (5D if 3N is not available)
                        //  1H-3H-4H
                        //  1S-3S-4S
                        rebid.BidMessage = BidMessage.Signoff;
                        rebid.BidPointType = BidPointType.Hcp;
                        rebid.Points.Min = 14;
                        rebid.Description = "Sign-off at game";

                        if (BridgeBot.IsMinor(rebid.declareBid.suit) && rebid.LowestAvailableLevel(Suit.Unknown, true) <= 3)
                            //  prefer playing game in notrump for minor suits (when we can stop their suits)
                            rebid.Validate = hand => !HasStoppersInTheirSuits(rebid, hand);
                        else
                            //  if we're unbalanced, get to game even with a minimum
                            rebid.AlternateMatches = hand => !BasicBidding.IsBalanced(hand);

                        if (rebid.declareBid.suit == Suit.Unknown)
                            RequireStoppersInTheirSuits(rebid);
                    }
                }
            }
            else if (rebid.declareBid.suit == Suit.Unknown)
            {
                //  a natural 2NT response already limited responder's hand, so place the
                //  contract instead of using the NT ladder (which would misread 4NT as a jump)
                if (response.bidIsDeclare && response.declareBid.suit == Suit.Unknown && response.declareBid.level == 2 &&
                    response.BidConvention == BidConvention.None)
                {
                    if (rebid.declareBid.level == 3)
                    {
                        rebid.Points.Min = 13;
                        rebid.BidPointType = BidPointType.Hcp;
                        rebid.BidMessage = BidMessage.Signoff;
                        rebid.Description = "Sign-off at game";
                    }
                }
                //  rebidding notrump
                else if (rebid.declareBid.level > 3)
                {
                    //  natural rebids stop at 3NT, so 4NT and above aren't part of the ladder (fit-agreed 4NT is Blackwood above)
                }
                else if (rebid.declareBid.level == lowestAvailableLevel)
                {
                    //  minimum: lowest available level (12-14 HCP; with 15-17 we'd have opened 1NT)
                    //  but when 3NT is already the cheapest notrump (e.g. after a jump shift), it shows any balanced hand
                    var isOnlyNotrump = lowestAvailableLevel == 3;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.Points.Min = 12;
                    rebid.Points.Max = isOnlyNotrump ? 19 : 14;
                    rebid.IsBalanced = true;
                    rebid.Description = isOnlyNotrump ? "no fit" : "minimum";
                    //  with a singleton in partner's suit and no other descriptive rebid, 1NT is still the best minimum
                    if (respondedAtOneLevel && rebid.declareBid.level == 1)
                        rebid.AlternateMatches = hand =>
                        {
                            var hcp = BasicBidding.ComputeHighCardPoints(hand);
                            var counts = BasicBidding.CountsBySuit(hand);
                            return hcp >= 12 && hcp <= 14 && counts[responseSuit] == 1 && counts.Values.Max() <= 5 && counts.Values.Count(c => c == 1 || c == 0) == 1;
                        };
                    RequireStoppersInTheirSuits(rebid);
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 1)
                {
                    //  maximum: jump (18-19 HCP), unless we can raise partner's major
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.Points.Min = 18;
                    rebid.Points.Max = 19;
                    rebid.IsBalanced = true;
                    rebid.Description = "jump";
                    if (respondedAtOneLevel && BridgeBot.IsMajor(responseSuit))
                        rebid.Validate = hand => BasicBidding.CountsBySuit(hand)[responseSuit] < 4;
                    RequireStoppersInTheirSuits(rebid);
                }
                else if (respondedAtOneLevel && rebid.declareBid.level == 3)
                {
                    //  1m-1X-3N: a long running suit and 18+ HCP
                    var ourSuit = opening.declareBid.suit;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.Points.Min = 18;
                    rebid.Points.Max = 21;
                    rebid.BidMessage = BidMessage.Signoff;
                    rebid.HandShape[ourSuit].Min = 6;
                    rebid.Description = $"Running {ourSuit}";
                    rebid.Validate = hand => hand.Count(c => c.suit == ourSuit && c.rank >= Rank.Queen) == 3;
                    RequireStoppersInTheirSuits(rebid);
                }
            }
            else if (response.bidIsDeclare && rebid.declareBid.suit == response.declareBid.suit)
            {
                //  raising responder's suit: a 1-level major promised 4+, so raise with 4 (or 3 with a singleton or void);
                //  after a two-over-one, support completes an 8-card fit (3 cards opposite a 5-card major)
                var support = respondedAtOneLevel ? 4 : 8 - response.HandShape[responseSuit].Min;
                var isCompeting = rebid.History[rebid.Index - 1].bidIsDeclare;
                var pointType = respondedAtOneLevel || isCompeting ? BidPointType.Dummy : BidPointType.Distribution;
                //  in competition, measure jumps from the cheapest level available over the interference
                if (isCompeting)
                    lowestAvailableLevel = rebid.LowestAvailableLevel(rebid.declareBid.suit, true);
                if (rebid.declareBid.level > rebid.GameLevel || isCompeting && lowestAvailableLevel > 3)
                {
                    //  slam bids aren't natural raises, nor are high-level raises over the opponents' preempt
                }
                else if (rebid.declareBid.level == lowestAvailableLevel)
                {
                    //  minimum: lowest available level (13-15 points)
                    rebid.BidPointType = pointType;
                    rebid.Points.Min = 13;
                    rebid.Points.Max = 15;
                    rebid.HandShape[rebid.declareBid.suit].Min = 3;
                    rebid.Description = $"Minimum raise; 3+ {rebid.declareBid.suit}";
                    rebid.Validate = hand =>
                    {
                        var counts = BasicBidding.CountsBySuit(hand);
                        return counts[responseSuit] >= support || respondedAtOneLevel && SuitRank.stdSuits.Any(s => counts[s] <= 1);
                    };
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 1)
                {
                    //  medium: jump raise (16-18 points)
                    rebid.BidPointType = pointType;
                    rebid.Points.Min = 16;
                    rebid.Points.Max = 18;
                    rebid.HandShape[rebid.declareBid.suit].Min = support;
                    rebid.Description = $"Jump raise; {support}+ {rebid.declareBid.suit}";
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 2)
                {
                    //  maximum: double jump (19-21 points)
                    rebid.BidPointType = pointType;
                    rebid.Points.Min = 19;
                    rebid.Points.Max = 21;
                    rebid.HandShape[rebid.declareBid.suit].Min = support;
                    rebid.Description = $"Double jump raise; {support}+ {rebid.declareBid.suit}";
                }
            }
            else if (rebid.declareBid.suit == opening.declareBid.suit)
            {
                //  rebidding opener's suit
                if (rebid.declareBid.level == lowestAvailableLevel)
                {
                    //  minimum: lowest available level (13-15 points); after a two-over-one, a 5-card suit will do
                    var length = respondedTwoOverOne ? 5 : 6;
                    rebid.Points.Min = 11;
                    rebid.Points.Max = 15;
                    rebid.HandShape[rebid.declareBid.suit].Min = length;
                    rebid.Description = $"Minimum rebid; {length}+ {rebid.declareBid.suit}";
                    //  with only 5 cards, prefer raising partner's two-over-one suit with 4-card support
                    if (respondedTwoOverOne)
                        rebid.Validate = hand =>
                        {
                            var counts = BasicBidding.CountsBySuit(hand);
                            return counts[rebid.declareBid.suit] >= 6 || counts[responseSuit] < 4;
                        };
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 1)
                {
                    //  medium: jump rebid (16-18 points), unless we're two-suited
                    rebid.Points.Min = 16;
                    rebid.Points.Max = 18;
                    rebid.HandShape[rebid.declareBid.suit].Min = 6;
                    rebid.Description = $"Jump rebid; 6+ {rebid.declareBid.suit}";
                    rebid.Validate = hand => !IsTwoSuited(hand);
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 2 && BridgeBot.IsMajor(rebid.declareBid.suit))
                {
                    //  maximum: double-jump rebid (19-21 points), unless we're two-suited; 6 cards will do opposite a 1NT response
                    var length = response.bidIsDeclare && responseSuit == Suit.Unknown && response.declareBid.level == 1 ? 6 : 7;
                    rebid.Points.Min = 19;
                    rebid.Points.Max = 21;
                    rebid.HandShape[rebid.declareBid.suit].Min = length;
                    rebid.Description = $"Double jump rebid; {length}+ {rebid.declareBid.suit}";
                    rebid.Validate = hand => !IsTwoSuited(hand);
                }
            }
            else
            {
                //  new suit
                var isReverse = BridgeBot.suitRank[rebid.declareBid.suit] > BridgeBot.suitRank[opening.declareBid.suit] &&
                                rebid.declareBid.level > 1;
                if (!isReverse && rebid.declareBid.level == lowestAvailableLevel && rebid.declareBid.level <= 3)
                {
                    //  minimum: a non-reversing new suit (this has the wide range of 13–18 points; less for a light opening, but not in competition)
                    rebid.Points.Min = rebid.History[rebid.Index - 1].bidIsDeclare ? 13 : 11;
                    //  after a two-over-one, a new suit is forcing and unlimited (and at the 3-level shows extra values)
                    rebid.Points.Max = respondedTwoOverOne ? 37 : 18;
                    if (respondedTwoOverOne)
                    {
                        rebid.BidMessage = BidMessage.Forcing;
                        if (rebid.declareBid.level == 3)
                            rebid.Points.Min = 15;
                    }
                    rebid.HandShape[rebid.declareBid.suit].Min = 4;
                    rebid.Description = $"New suit; 4+ {rebid.declareBid.suit}";
                    rebid.Validate = hand =>
                    {
                        var counts = BasicBidding.CountsBySuit(hand);
                        //  prefer raising partner's 1-level major with 4-card support
                        if (respondedAtOneLevel && BridgeBot.IsMajor(responseSuit) && counts[responseSuit] >= 4)
                            return false;

                        //  prefer a 2NT jump with a balanced 18-19
                        var hcp = BasicBidding.ComputeHighCardPoints(hand);
                        return !(rebid.declareBid.level == 2 && BasicBidding.IsBalanced(hand) && hcp >= 18 && hcp <= 19);
                    };
                }
                else if (isReverse && rebid.declareBid.level == lowestAvailableLevel)
                {
                    //  medium: REVERSE in a new suit (17+ points, counting length)
                    rebid.Points.Min = 17;
                    rebid.Points.Max = 21;
                    rebid.BidMessage = BidMessage.Forcing;
                    rebid.HandShape[rebid.declareBid.suit].Min = rebid.declareBid.suit == Suit.Diamonds ? 3 : 4;
                    rebid.HandShape[opening.declareBid.suit].Min = rebid.declareBid.suit == Suit.Diamonds ? 4 : 5;
                    rebid.Description =
                        $"Reverse; {rebid.HandShape[rebid.declareBid.suit].Min}+ {rebid.declareBid.suit} and {rebid.HandShape[opening.declareBid.suit].Min}+ {opening.declareBid.suit}";
                    //  ensure we don't use a reverse with a flat (4-3-3-3) hand
                    rebid.Validate = hand => !BasicBidding.IsFlat(hand);
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 1)
                {
                    //  maximum: jump shift in a new suit (19-21 points, unbalanced; not needed after a two-over-one)
                    rebid.Points.Min = 19;
                    rebid.Points.Max = 21;
                    rebid.BidMessage = BidMessage.Forcing;
                    rebid.HandShape[rebid.declareBid.suit].Min = 4;
                    rebid.Description = $"Jump shift; 4+ {rebid.declareBid.suit}";
                    rebid.Validate = hand => !respondedTwoOverOne && !BasicBidding.IsBalanced(hand);
                }
            }        }
    }
}