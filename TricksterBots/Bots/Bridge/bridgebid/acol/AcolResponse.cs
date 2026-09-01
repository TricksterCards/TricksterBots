using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    //  Acol responses: 4-card raises, natural 2NT (10-12), wide-range 1NT (6-9)
    internal class AcolResponse
    {
        public static void Interpret(InterpretedBid response)
        {
            var opening = response.History[response.Index - 2];
            var overcall = response.History[response.Index - 1];

            if (response.bid == BidBase.Pass)
            {
                InterpretPass(opening, response);
            }
            else if (response.bid == BridgeBid.Double || response.bid == BridgeBid.Redouble)
            {
                //  leave as penalty double/redouble by default
            }
            else if (opening.declareBid.suit == Suit.Unknown)
            {
                switch (opening.declareBid.level)
                {
                    case 1:
                        InterpretResponseTo1NT(response);
                        break;
                    case 2:
                        Response.InterpretResponseTo2NT(response);
                        break;
                    case 3:
                        Response.InterpretResponseTo3NT(response);
                        break;
                }
            }
            else if (opening.declareBid.level > 1)
            {
                Response.InterpretResponseToPreempt(opening, overcall, response);
            }
            else if (Response.IsCuebidResponse(overcall, response))
            {
                Response.InterpretCuebidResponse(opening, response);
            }
            else
            {
                InterpretResponseToSuit(opening, overcall, response);
            }
        }

        private static void InterpretPass(InterpretedBid opening, InterpretedBid response)
        {
            response.BidMessage = BidMessage.Signoff;

            switch (opening.declareBid.level)
            {
                case 1:
                    switch (opening.declareBid.suit)
                    {
                        //  1C-P
                        //  1D-P
                        //  1H-P
                        //  1S-P
                        case Suit.Clubs:
                        case Suit.Diamonds:
                        case Suit.Hearts:
                        case Suit.Spades:
                            response.Points.Max = 5;
                            response.Description = "Game is unlikely";
                            break;

                        //  1N-P (weak NT: pass anything below invitational values)
                        case Suit.Unknown:
                            response.Points.Max = 10;
                            response.Description = string.Empty;
                            break;
                    }

                    break;

                default:
                    switch (opening.declareBid.suit)
                    {
                        //  2D-P, 3C-P, ...
                        case Suit.Clubs:
                        case Suit.Diamonds:
                        case Suit.Hearts:
                        case Suit.Spades:
                            response.HandShape[opening.declareBid.suit].Max = 2;
                            response.Description = $"Not enough tricks for game; 0-2 {opening.declareBid.suit}";
                            break;

                        //  2N-P, 3N-P, ...
                        case Suit.Unknown:
                            response.Points.Max = 3;
                            response.Description = string.Empty;
                            break;
                    }

                    break;
            }
        }

        private static void InterpretResponseTo1NT(InterpretedBid response)
        {
            switch (response.declareBid.level)
            {
                case 2:
                    if (response.declareBid.suit == Suit.Unknown)
                    {
                        //  1N-2N
                        response.BidPointType = BidPointType.Hcp;
                        response.Points.Min = 11;
                        response.Points.Max = 12;
                        response.IsBalanced = true;
                        response.Description = string.Empty;
                        //  also use this bid when we're not balanced if nothing else fits
                        response.AlternateMatches = hand =>
                        {
                            var hcp = BasicBidding.ComputeHighCardPoints(hand);
                            var counts = BasicBidding.CountsBySuit(hand);
                            return !BasicBidding.IsBalanced(hand)
                                && hcp >= 11
                                && hcp <= 12
                                && counts[Suit.Spades] < 4
                                && counts[Suit.Hearts] < 4
                                && counts[Suit.Diamonds] < 6
                                && counts[Suit.Clubs] < 6;
                        };
                    }
                    else
                    {
                        //  1N-2C (overridden by Stayman)
                        //  1N-2D (overridden by JacobyTransfer when transfers are on)
                        //  1N-2H (overridden by JacobyTransfer when transfers are on)
                        //  1N-2S (overridden by Relay when transfers are on)
                        //  weak takeout: to play opposite the weak NT
                        response.BidMessage = BidMessage.Signoff;
                        response.Points.Max = 10;
                        response.HandShape[response.declareBid.suit].Min = 5;
                        response.Description = $"Weak takeout; 5+ {response.declareBid.suit}";
                    }

                    break;

                case 3:
                    if (response.declareBid.suit == Suit.Unknown)
                    {
                        //  1N-3N
                        response.BidMessage = BidMessage.Signoff;
                        response.BidPointType = BidPointType.Hcp;
                        response.Points.Min = 13;
                        response.Points.Max = 18;
                        response.IsBalanced = true;
                        response.Description = string.Empty;
                        //  also use this bid when we're not balanced if nothing else fits
                        response.AlternateMatches = hand =>
                        {
                            var hcp = BasicBidding.ComputeHighCardPoints(hand);
                            var counts = BasicBidding.CountsBySuit(hand);
                            return !BasicBidding.IsBalanced(hand)
                                && hcp >= 13
                                && hcp <= 18
                                && counts[Suit.Spades] < 4
                                && counts[Suit.Hearts] < 4
                                && counts[Suit.Diamonds] < 6
                                && counts[Suit.Clubs] < 6;
                        };
                    }
                    else if (BridgeBot.IsMajor(response.declareBid.suit))
                    {
                        //  1N-3H
                        //  1N-3S
                        response.BidMessage = BidMessage.Forcing;
                        response.Points.Min = 13;
                        response.HandShape[response.declareBid.suit].Min = 5;
                        response.Description = $"5+ {response.declareBid.suit}; game forcing";
                    }
                    else
                    {
                        //  1N-3C
                        //  1N-3D
                        response.Points.Min = 11;
                        response.Points.Max = 12;
                        response.IsGood = true;
                        response.HandShape[response.declareBid.suit].Min = 6;
                        response.Description = $"Good 6+ card {response.declareBid.suit} suit";
                    }

                    break;

                case 4:
                    if (response.declareBid.suit == Suit.Unknown)
                    {
                        //  1N-4N
                        response.Points.Min = 19;
                        response.Points.Max = 20;
                        response.BidPointType = BidPointType.Hcp;
                        response.Description = "Slam invitational";
                    }
                    else if (BridgeBot.IsMajor(response.declareBid.suit))
                    {
                        //  1N-4H
                        //  1N-4S
                        response.BidMessage = BidMessage.Signoff;
                        response.Points.Min = 13;
                        response.Points.Max = 17;
                        response.HandShape[response.declareBid.suit].Min = 6;
                        response.Description = $"6+ {response.declareBid.suit}";
                    }

                    //  1N-4C (see Gerber)
                    //  1N-4D (unused)
                    break;

                case 6:
                    if (response.declareBid.suit == Suit.Unknown)
                    {
                        //  1N-6N
                        response.Points.Min = 21;
                        response.Points.Max = 22;
                        response.BidPointType = BidPointType.Hcp;
                        response.BidMessage = BidMessage.Signoff;
                        response.IsBalanced = true;
                        //  also use this bid when we're not balanced if nothing else fits
                        response.AlternateMatches = hand =>
                        {
                            var hcp = BasicBidding.ComputeHighCardPoints(hand);
                            var counts = BasicBidding.CountsBySuit(hand);
                            return !BasicBidding.IsBalanced(hand)
                                && hcp >= 21
                                && hcp <= 22
                                && counts[Suit.Spades] < 4
                                && counts[Suit.Hearts] < 4
                                && counts[Suit.Diamonds] < 6
                                && counts[Suit.Clubs] < 6;
                        };
                    }

                    break;
            }
        }

        private static void InterpretResponseToSuit(InterpretedBid opening, InterpretedBid overcall, InterpretedBid response)
        {
            var openSuit = opening.declareBid.suit;

            switch (response.declareBid.level)
            {
                case 1:
                    if (response.declareBid.suit == Suit.Unknown)
                    {
                        //  1x-1N: no support, no suit to show at the 1-level
                        response.Points.Min = 6;
                        response.Points.Max = 9;
                        response.HandShape[openSuit].Max = 3;
                        response.Description = $"No fit; 0-3 {openSuit}";
                    }
                    else
                    {
                        //  new suit at the 1-level: natural and forcing
                        response.Points.Min = 6;
                        response.BidMessage = BidMessage.Forcing;
                        response.HandShape[response.declareBid.suit].Min =
                            BridgeBot.IsMajor(response.declareBid.suit) && NegativeDouble.CanUseAfter(opening, overcall) ? 5 : 4;
                        response.SetHandShapeMaxesOfOtherSuits(response.declareBid.suit, 6);
                        response.Description = $"{response.HandShape[response.declareBid.suit].Min}+ {response.declareBid.suit}";
                    }

                    break;

                case 2:
                    if (response.declareBid.suit == Suit.Unknown)
                    {
                        //  1x-2N: natural, invitational (10-12), denies 4-card support
                        response.Points.Min = 10;
                        response.Points.Max = 12;
                        response.BidPointType = BidPointType.Hcp;
                        response.IsBalanced = true;
                        response.HandShape[openSuit].Max = 3;
                        if (BridgeBot.IsMinor(openSuit))
                            response.NoFourCardMajors();
                        response.Description = "Natural, inviting game";
                    }
                    else if (response.declareBid.suit == openSuit)
                    {
                        //  1x-2x: single raise (6-9 with 4+ support)
                        var minCardsInSuit = 8 - opening.HandShape[openSuit].Min;
                        response.Points.Min = 6;
                        response.Points.Max = 9;
                        response.BidPointType = BidPointType.Dummy;
                        response.HandShape[response.declareBid.suit].Min = minCardsInSuit;
                        response.Description = $"Single raise; {minCardsInSuit}+ {response.declareBid.suit}";
                    }
                    else if (BridgeBot.suitRank[response.declareBid.suit] < BridgeBot.suitRank[openSuit])
                    {
                        //  new suit at the 2-level (non-jump): 9+ points, natural and forcing
                        //  a 2-level major response (2H over 1S) promises 5+ since opener may raise with 3
                        var minCardsInSuit = BridgeBot.IsMinor(response.declareBid.suit) ? 4 : 5;
                        response.Points.Min = 9;
                        response.BidMessage = BidMessage.Forcing;
                        response.HandShape[response.declareBid.suit].Min = minCardsInSuit;
                        response.SetHandShapeMaxesOfOtherSuits(response.declareBid.suit, 6);
                        response.Description = $"{minCardsInSuit}+ {response.declareBid.suit}";
                    }
                    else
                    {
                        //  jump shift: strong (16+) with a good 5+ card suit
                        response.Points.Min = 16;
                        response.BidMessage = BidMessage.Forcing;
                        response.HandShape[response.declareBid.suit].Min = 5;
                        response.Description = $"Strong jump shift; 5+ {response.declareBid.suit} and slam interest";
                    }

                    break;

                case 3:
                    if (response.declareBid.suit == Suit.Unknown)
                    {
                        //  1x-3N: balanced 13-15, denies 4-card support
                        response.Points.Min = 13;
                        response.Points.Max = 15;
                        response.BidPointType = BidPointType.Hcp;
                        response.IsBalanced = true;
                        response.HandShape[openSuit].Max = 3;
                        if (BridgeBot.IsMinor(openSuit))
                            response.NoFourCardMajors();
                        response.Description = string.Empty;
                    }
                    else if (response.declareBid.suit == openSuit)
                    {
                        //  1x-3x: limit raise (10-12 with 4+ support)
                        var minCardsInSuit = 8 - opening.HandShape[openSuit].Min;
                        response.Points.Min = 10;
                        response.Points.Max = 12;
                        response.BidPointType = BidPointType.Dummy;
                        response.HandShape[response.declareBid.suit].Min = minCardsInSuit;
                        response.Description = $"Limit raise; {minCardsInSuit}+ {response.declareBid.suit}";
                    }
                    else
                    {
                        //  jump shift: strong (16+) with a good 5+ card suit
                        response.Points.Min = 16;
                        response.BidMessage = BidMessage.Forcing;
                        response.HandShape[response.declareBid.suit].Min = 5;
                        response.Description = $"Strong jump shift; 5+ {response.declareBid.suit} and slam interest";
                    }

                    break;

                case 4:
                    if (response.declareBid.suit == openSuit && BridgeBot.IsMajor(openSuit))
                    {
                        //  1H-4H
                        //  1S-4S: raise to game (13-15 with 4+ support)
                        response.BidMessage = BidMessage.Signoff;
                        response.Points.Min = 13;
                        response.Points.Max = 15;
                        response.BidPointType = BidPointType.Dummy;
                        response.HandShape[response.declareBid.suit].Min = 4;
                        response.Description = $"Raise to game; 4+ {response.declareBid.suit}";
                        //  or bid game preemptively with a weak, shapely raise
                        response.AlternateMatches = hand =>
                        {
                            var counts = BasicBidding.CountsBySuit(hand);
                            return BasicBidding.ComputeHighCardPoints(hand) < 10
                                && counts[openSuit] >= 5
                                && SuitRank.stdSuits.Any(s => counts[s] <= 1);
                        };
                    }

                    break;
            }
        }
    }
}
