using System;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal class Response
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
                        InterpretResponseTo2NT(response);
                        break;
                    case 3:
                        InterpretResponseTo3NT(response);
                        break;
                }
            }
            else if (opening.declareBid.level > 1)
            {
                InterpretResponseToPreempt(opening, overcall, response);
            }
            else if (IsCuebidResponse(overcall, response))
            {
                InterpretCuebidResponse(opening, response);
            }
            else if (BridgeBot.IsMajor(opening.declareBid.suit))
            {
                InterpretResponseToMajor(opening, overcall, response);
            }
            else if (BridgeBot.IsMinor(opening.declareBid.suit))
            {
                InterpretResponseToMinor(opening, overcall, response);
            }
            else
            {
                throw new Exception("Response has an impossible state");
            }
        }

        private static void InterpretCuebidResponse(InterpretedBid opening, InterpretedBid response)
        {
            // From "Competitive Bidding" section on page 7 of ACBL SAYC System Booklet
            // https://web2.acbl.org/documentlibrary/play/SP3%20(bk)%20single%20pages.pdf
            response.HandShape[opening.declareBid.suit].Min = 8 - opening.HandShape[opening.declareBid.suit].Min;
            response.Points.Min = opening.GamePoints - opening.Points.Min;
            response.BidPointType = BidPointType.Dummy;
            response.BidMessage = BidMessage.Forcing;
            response.Description = "Game force; usually a raise";
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

                        //  1N-P
                        case Suit.Unknown:
                            response.Points.Max = 5;
                            response.Description = string.Empty;
                            break;
                    }

                    break;

                default:
                    switch (opening.declareBid.suit)
                    {
                        //  2C-P, 3C-P, ...
                        //  2D-P, 3D-P, ...
                        //  2H-P, 3H-P, ...
                        //  2S-P, 3S-P, ...
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
            //  TODO (SAYC Booklet):
            //  If an opponent bids over your 1NT opener (except double), conventional responses like Stayman and transfers are “off.”
            //  Bids are natural except for a cuebid, which may be used with game forcing strength as a substitute for Stayman. 
            //  
            //  If the opponents intervene over a conventional response, bids carry the same meaning as if there were no intervention.
            //  A bid says, “I’m bidding voluntarily, so I have a real fit with you.”

            switch (response.declareBid.level)
            {
                case 2:
                    if (response.declareBid.suit == Suit.Unknown)
                    {
                        //  1N-2N
                        response.BidPointType = BidPointType.Hcp;
                        response.Points.Min = 8;
                        response.Points.Max = 9;
                        response.IsBalanced = true;
                        response.Description = string.Empty;
                        //  also use this bid when we're not balanced if nothing else fits
                        response.AlternateMatches = hand =>
                        {
                            var hcp = BasicBidding.ComputeHighCardPoints(hand);
                            var counts = BasicBidding.CountsBySuit(hand);
                            return !BasicBidding.IsBalanced(hand)
                                && hcp >= 8
                                && hcp <= 9
                                && counts[Suit.Spades] < 4
                                && counts[Suit.Hearts] < 4
                                && counts[Suit.Diamonds] < 6
                                && counts[Suit.Clubs] < 6;
                        };
                    }
                    else
                    {
                        //  1N-2C (overridden by Stayman)
                        //  1N-2D (overridden by JacobyTransfer)
                        //  1N-2H (overridden by JacobyTransfer)
                        //  1N-2S (overridden by Relay)
                        response.BidMessage = BidMessage.Signoff;
                        response.Points.Max = 7;
                        response.HandShape[response.declareBid.suit].Min = 5;
                        response.Description = $"5+ {response.declareBid.suit}";
                    }

                    break;

                case 3:
                    if (response.declareBid.suit == Suit.Unknown)
                    {
                        //  1N-3N
                        response.BidMessage = BidMessage.Signoff;
                        response.BidPointType = BidPointType.Hcp;
                        response.Points.Min = 10;
                        response.Points.Max = 15;
                        response.IsBalanced = true;
                        response.Description = string.Empty;
                        //  also use this bid when we're not balanced if nothing else fits
                        response.AlternateMatches = hand =>
                        {
                            var hcp = BasicBidding.ComputeHighCardPoints(hand);
                            var counts = BasicBidding.CountsBySuit(hand);
                            return !BasicBidding.IsBalanced(hand)
                                && hcp >= 10
                                && hcp <= 15
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
                        response.Points.Min = 16;
                        response.HandShape[response.declareBid.suit].Min = 6;
                        response.Description = $"6+ {response.declareBid.suit}; slam interest";
                    }
                    else
                    {
                        //  1N-3C
                        //  1N-3D
                        response.Points.Min = 8;
                        response.Points.Max = 9;
                        response.IsGood = true;
                        response.HandShape[response.declareBid.suit].Min = 6;
                        response.Description = $"Good 6+ card {response.declareBid.suit} suit";
                    }

                    break;

                case 4:
                    if (response.declareBid.suit == Suit.Unknown)
                    {
                        //  1N-4N
                        response.Points.Min = 16;
                        response.Points.Max = 17;
                        response.BidPointType = BidPointType.Hcp;
                        response.Description = "Slam invitational";
                    }
                    else if (BridgeBot.IsMajor(response.declareBid.suit))
                    {
                        //  1N-4H
                        //  1N-4S
                        response.BidMessage = BidMessage.Signoff;
                        response.Points.Min = 10;
                        response.Points.Max = 15;
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
                        response.Points.Min = 18;
                        response.Points.Max = 19;
                        response.BidPointType = BidPointType.Hcp;
                        response.BidMessage = BidMessage.Signoff;
                        response.IsBalanced = true;
                        //  also use this bid when we're not balanced if nothing else fits
                        response.AlternateMatches = hand =>
                        {
                            var hcp = BasicBidding.ComputeHighCardPoints(hand);
                            var counts = BasicBidding.CountsBySuit(hand);
                            return !BasicBidding.IsBalanced(hand)
                                && hcp >= 18
                                && hcp <= 19
                                && counts[Suit.Spades] < 4
                                && counts[Suit.Hearts] < 4
                                && counts[Suit.Diamonds] < 6
                                && counts[Suit.Clubs] < 6;
                        };
                    }

                    break;
            }
        }

        private static void InterpretResponseTo2NT(InterpretedBid response)
        {
            switch (response.declareBid.level)
            {
                case 3:
                    if (response.declareBid.suit == Suit.Unknown)
                    {
                        //  2N-3N
                        response.BidMessage = BidMessage.Signoff;
                        response.BidPointType = BidPointType.Hcp;
                        response.Points.Min = 4;
                        response.Points.Max = 10;
                        response.IsBalanced = true;
                        response.Description = string.Empty;
                        //  also use this bid when we're not balanced if nothing else fits
                        response.AlternateMatches = hand =>
                        {
                            var hcp = BasicBidding.ComputeHighCardPoints(hand);
                            var counts = BasicBidding.CountsBySuit(hand);
                            return !BasicBidding.IsBalanced(hand)
                                && hcp >= 4
                                && hcp <= 10
                                && counts[Suit.Spades] < 4
                                && counts[Suit.Hearts] < 4;
                        };
                    }
                    else
                    {
                        //  2N-3C (overridden by Stayman)
                        //  2N-3D (overridden by JacobyTransfer)
                        //  2N-3H (overridden by JacobyTransfer)
                        //  2N-3S
                        response.BidMessage = BidMessage.Forcing;
                        response.Points.Min = 4;
                        response.Points.Max = 10;
                        response.HandShape[response.declareBid.suit].Min = 5;
                        response.Description = $"5+ {response.declareBid.suit}";
                    }

                    break;

                case 4:
                    if (response.declareBid.suit == Suit.Unknown)
                    {
                        //  2N-4N
                        response.Points.Min = 11;
                        response.Points.Max = 12;
                        response.BidPointType = BidPointType.Hcp;
                        response.Description = "Slam invitational";
                    }
                    else if (BridgeBot.IsMajor(response.declareBid.suit))
                    {
                        //  2N-4H
                        //  2N-4S
                        response.BidMessage = BidMessage.Signoff;
                        response.Points.Min = 4;
                        response.Points.Max = 10;
                        response.HandShape[response.declareBid.suit].Min = 6;
                        response.Description = $"6+ {response.declareBid.suit}";
                    }

                    //  2N-4C (see Gerber)
                    //  2N-4D (unused)
                    break;

                case 6:
                    if (response.declareBid.suit == Suit.Unknown)
                    {
                        //  2N-6N
                        response.Points.Min = 13;
                        response.Points.Max = 15;
                        response.BidPointType = BidPointType.Hcp;
                        response.BidMessage = BidMessage.Signoff;
                        response.IsBalanced = true;
                        //  also use this bid when we're not balanced if nothing else fits
                        response.AlternateMatches = hand =>
                        {
                            var hcp = BasicBidding.ComputeHighCardPoints(hand);
                            var counts = BasicBidding.CountsBySuit(hand);
                            return !BasicBidding.IsBalanced(hand)
                                && hcp >= 13
                                && hcp <= 15
                                && counts[Suit.Spades] < 4
                                && counts[Suit.Hearts] < 4;
                        };
                    }

                    break;
            }
        }

        private static void InterpretResponseTo3NT(InterpretedBid response)
        {
            if (response.declareBid.level > 4)
                return;

            if (response.declareBid.suit == Suit.Unknown)
            {
                //  3N-4N
                response.Points.Min = 6;
                response.Points.Max = 7;
                response.BidPointType = BidPointType.Hcp;
                response.Description = "Slam invitational";
            }
            else if (BridgeBot.IsMajor(response.declareBid.suit))
            {
                //  3N-4D (overridden by JacobyTransfer)
                //  3N-4H (overridden by JacobyTransfer)
                response.BidMessage = BidMessage.Signoff;
                response.HandShape[response.declareBid.suit].Min = 6;
                response.Description = $"6+ {response.declareBid.suit}";
            }
            //  3N-4C (see Gerber)
            //  3N-4D (unused)
        }

        private static void InterpretResponseToMajor(InterpretedBid opening, InterpretedBid overcall, InterpretedBid response)
        {
            switch (response.declareBid.level)
            {
                case 1:
                    switch (response.declareBid.suit)
                    {
                        //  1H-1S
                        case Suit.Spades:
                            //  opening had to be 1♥ to respond 1♠
                            response.Points.Min = 6;
                            response.BidMessage = BidMessage.Forcing;
                            response.HandShape[Suit.Spades].Min = NegativeDouble.CanUseAfter(opening, overcall) ? 5 : 4;
                            response.HandShape[Suit.Hearts].Max = 2; // we have at max two hearts because if we have 3+ we would have bid 2♥
                            response.Description = $"{response.HandShape[Suit.Spades].Min}+ Spades; 0-2 Hearts";
                            break;

                        //  1H-1N
                        //  1S-1N
                        case Suit.Unknown:
                            //  1NT response is saying we've got nothing to support the major opening
                            response.Points.Min = 6;
                            response.Points.Max = 10;
                            response.HandShape[opening.declareBid.suit].Max = 2;
                            response.Description = $"Any distribution; 0-2 {opening.declareBid.suit}";

                            break;
                    }

                    break;

                case 2:
                    switch (response.declareBid.suit)
                    {
                        case Suit.Clubs:
                        case Suit.Diamonds:
                        case Suit.Hearts:
                        case Suit.Spades:

                            //  1H-2H
                            //  1S-2S
                            if (response.declareBid.suit == opening.declareBid.suit)
                            {
                                //  bidding 2-level of partners 1-level opening major
                                //  three-card or longer support; 6–10 dummy points
                                response.Points.Min = 6;
                                response.Points.Max = 10;
                                response.BidPointType = BidPointType.Dummy;
                                response.HandShape[response.declareBid.suit].Min = 3;
                                response.Description = $"3+ {response.declareBid.suit}";
                            }
                            //  1H-2S
                            else if (response.declareBid.suit == Suit.Spades)
                            {
                                //  changing suits to a higher-ranking suit
                                response.BidMessage = BidMessage.Forcing;
                                response.Points.Min = 17;
                                response.HandShape[response.declareBid.suit].Min = 5;
                                response.Description = $"5+ {response.declareBid.suit}; slam interest";
                            }
                            //  1H-2C
                            //  1H-2D
                            //  1S-2C
                            //  1S-2D
                            //  1S-2H
                            else
                            {
                                //  changing suits to a lower ranking suit
                                var min = BridgeBot.IsMinor(response.declareBid.suit) ? 4 : 5;
                                response.BidMessage = BidMessage.Forcing;
                                response.Points.Min = 11;
                                response.BidPointType = BidPointType.Dummy;
                                response.HandShape[response.declareBid.suit].Min = min;
                                response.Description = $"{min}+ {response.declareBid.suit} (may have 3+ {opening.declareBid.suit})";
                            }

                            break;

                        //  1H-2N
                        //  1S-2N
                        case Suit.Unknown:
                            if (overcall.bid == BidBase.Pass)
                            {
                                response.Points.Min = 13;
                                response.BidPointType = BidPointType.Dummy;
                                response.BidConvention = BidConvention.Jacoby2NT;
                                response.BidMessage = BidMessage.Forcing;
                                response.HandShape[opening.declareBid.suit].Min = 4;
                                response.Description = $"4+ {opening.declareBid.suit}";
                            }
                            //  TODO: should we include an alternate meaning for 2NT after interference?
                            break;
                    }

                    break;

                case 3:
                    switch (response.declareBid.suit)
                    {
                        case Suit.Clubs:
                        case Suit.Diamonds:
                        case Suit.Hearts:
                        case Suit.Spades:

                            //  1H-3H
                            //  1S-3S
                            if (response.declareBid.suit == opening.declareBid.suit)
                            {
                                //  limit raise (10–11 dummy points with 3+ card support)
                                response.Points.Min = 10;
                                response.Points.Max = 11;
                                response.BidPointType = BidPointType.Dummy;
                                response.HandShape[response.declareBid.suit].Min = 3;
                                response.Description = $"Limit raise; 3+ {response.declareBid.suit}";
                            }
                            //  1H-3C
                            //  1H-3D
                            //  (excludes 1H-3S; reserved for splinter bids)
                            //  1S-3C
                            //  1S-3D
                            //  1S-3H
                            else if (response.declareBid.suit != Suit.Spades)
                            {
                                //  changing suits via a strong jump shift
                                response.Points.Min = 17;
                                response.BidMessage = BidMessage.Forcing;
                                response.HandShape[response.declareBid.suit].Min = 5;
                                response.Description = $"Strong jump shift; 5+ {response.declareBid.suit} and slam interest";
                            }

                            break;

                        //  1H-3N
                        //  1S-3N
                        case Suit.Unknown:
                            //  15–17 HCP, balanced hand with two-card support for partner.
                            response.Points.Min = 13;
                            response.Points.Max = 15;
                            response.BidPointType = BidPointType.Hcp;
                            response.IsBalanced = true;
                            response.HandShape[opening.declareBid.suit].Min = 2;
                            response.Description = $"2+ {opening.declareBid.suit}";
                            break;
                    }

                    break;

                case 4:
                    switch (response.declareBid.suit)
                    {
                        case Suit.Clubs:
                        case Suit.Diamonds:
                        case Suit.Hearts:
                        case Suit.Spades:

                            if (response.declareBid.suit == opening.declareBid.suit)
                            {
                                //  1H-4H
                                //  1S-4S
                                //  usually 5+ card support, a singleton or void, and fewer than 10 points.
                                response.Points.Max = 10;
                                response.BidPointType = BidPointType.Dummy;
                                response.HandShape[response.declareBid.suit].Min = 5;
                                response.Description = $"5+ {response.declareBid.suit} with a singleton or void";
                                response.Validate = hand => BasicBidding.CountsBySuit(hand).Any(cs => cs.Value <= 1);
                            }

                            break;

                        case Suit.Unknown:

                            break;
                    }

                    break;

                //  TODO: add more levels (preempts)
            }
        }

        private static void InterpretResponseToMinor(InterpretedBid opening, InterpretedBid overcall, InterpretedBid response)
        {
            switch (response.declareBid.level)
            {
                case 1:
                    switch (response.declareBid.suit)
                    {
                        //  1C-1D
                        //  1C-1H
                        //  1C-1S
                        //  1D-1H
                        //  1D-1S
                        case Suit.Diamonds:
                        case Suit.Hearts:
                        case Suit.Spades:
                            response.Points.Min = 6;
                            response.BidMessage = BidMessage.Forcing;
                            response.HandShape[response.declareBid.suit].Min =
                                BridgeBot.IsMajor(response.declareBid.suit) && NegativeDouble.CanUseAfter(opening, overcall) ? 5 : 4;
                            response.SetHandShapeMaxesOfOtherSuits(response.declareBid.suit, 6);
                            response.Description = $"{response.HandShape[response.declareBid.suit].Min}+ {response.declareBid.suit}";
                            break;

                        //  1C-1N
                        //  1D-1N
                        case Suit.Unknown:
                            response.Points.Min = 6;
                            response.Points.Max = 10;
                            response.IsBalanced = true;
                            response.NoFourCardMajors();
                            response.Description = "no 4-card major";
                            break;
                    }

                    break;

                case 2:
                    switch (response.declareBid.suit)
                    {
                        case Suit.Clubs:
                        case Suit.Diamonds:
                        case Suit.Hearts:
                        case Suit.Spades:

                            //  1C-2C
                            //  1D-2D
                            if (response.declareBid.suit == opening.declareBid.suit)
                            {
                                //  bidding 2-level of partners 1-level opening minor (supporting their bid)
                                var minCardsInSuit = 8 - opening.HandShape[opening.declareBid.suit].Min;
                                response.Points.Min = 6;
                                response.Points.Max = 10;
                                response.HandShape[response.declareBid.suit].Min = minCardsInSuit;
                                response.NoFourCardMajors();
                                response.Description = $"{minCardsInSuit}+ {response.declareBid.suit}; no 4-card major";
                            }
                            //  1C-2D
                            //  1C-2H
                            //  1C-2S
                            //  1D-2H
                            //  1D-2S
                            else
                            {
                                //  changing suits
                                response.BidMessage = BidMessage.Forcing;
                                response.Points.Min = BridgeBot.suitRank[response.declareBid.suit] > BridgeBot.suitRank[opening.declareBid.suit] ? 17 : 11;
                                response.HandShape[response.declareBid.suit].Min = 5;
                                response.SetHandShapeMaxesOfOtherSuits(response.declareBid.suit, 6);
                                response.Description = $"5+ {response.declareBid.suit}; slam interest";
                            }

                            break;

                        //  1C-2N
                        //  1D-2N
                        case Suit.Unknown:
                            response.Points.Min = 13;
                            response.Points.Max = 15;
                            response.BidMessage = BidMessage.Forcing; // game forcing
                            response.IsBalanced = true;
                            response.NoFourCardMajors();
                            response.Description = "no 4-card major";
                            break;
                    }

                    break;

                case 3:
                    switch (response.declareBid.suit)
                    {
                        case Suit.Clubs:
                        case Suit.Diamonds:
                        case Suit.Hearts:
                        case Suit.Spades:

                            //  1C-3C
                            //  1D-3D
                            if (response.declareBid.suit == opening.declareBid.suit)
                            {
                                //  bidding 3-level of partners 1-level opening minor (supporting their bid)
                                response.Points.Min = 11;
                                response.Points.Max = 12;
                                response.HandShape[response.declareBid.suit].Min = 5;
                                response.NoFourCardMajors();
                                response.Description = $"5+ {response.declareBid.suit}; no 4-card major";
                            }
                            //  1D-3C
                            else if (BridgeBot.suitRank[response.declareBid.suit] < BridgeBot.suitRank[opening.declareBid.suit])
                            {
                                //  bidding at 3-level in suit lower than partner's opening minor
                                response.Points.Min = 17;
                                response.HandShape[response.declareBid.suit].Min = 5;
                                response.SetHandShapeMaxesOfOtherSuits(response.declareBid.suit, 6);
                                response.Description = $"5+ {response.declareBid.suit}; slam interest";
                            }

                            //  1C-3D
                            //  1C-3H
                            //  1C-3S
                            //  1D-3H
                            //  1D-3S
                            //  partnership agreement (undefined)

                            break;

                        //  1C-3N
                        //  1D-3N
                        case Suit.Unknown:
                            response.Points.Min = 16;
                            response.Points.Max = 18;
                            response.IsBalanced = true;
                            response.NoFourCardMajors();
                            response.Description = "no 4-card major";
                            break;
                    }

                    break;
            }
        }

        private static void InterpretResponseToPreempt(InterpretedBid opening, InterpretedBid overcall, InterpretedBid response)
        {
            if (response.bid == BidBase.Pass)
                response.Description = "Unsuitable hand to continue";

            if (!response.bidIsDeclare)
                return;

            var openerTricks = opening.declareBid.level == 2 ? 5 : 6;

            if (response.declareBid.suit == Suit.Unknown)
            {
                if (response.declareBid.level == 2)
                {
                    response.BidConvention = BidConvention.ArtificialInquiry;
                    response.BidMessage = BidMessage.Forcing;
                    response.Description = "asking for a feature";
                }
                else if (response.declareBid.level == 3)
                {
                    response.BidMessage = BidMessage.Signoff;
                    response.Description = string.Empty;
                    response.AlternateMatches = hand => 9 <= openerTricks + BasicBidding.CountPlayingTricks(hand, response.declareBid.suit);
                }
                else if (response.declareBid.level == 4)
                {
                    response.BidConvention = BidConvention.Blackwood;
                    response.BidMessage = BidMessage.Forcing;
                    response.Description = "asking for Aces";
                    //  TODO: validate knowing count of Aces will help decision to bid slam
                    response.Validate = hand => false;
                }
                else if (response.declareBid.level == 5)
                {
                    //  TODO: Grand Slam Force
                }
            }
            else if (response.declareBid.suit == opening.declareBid.suit)
            {
                if (response.declareBid.level == opening.declareBid.level + 1 && response.declareBid.level <= response.GameLevel)
                {
                    //  simple raise with 3+ support
                    response.BidMessage = BidMessage.Signoff;
                    response.HandShape[response.declareBid.suit].Min = 3;
                    response.Description = $"3+ {response.declareBid.suit}";
                }
                else if (response.declareBid.level == response.GameLevel)
                {
                    //  raise to game with 4+ support
                    response.BidMessage = BidMessage.Signoff;
                    response.HandShape[response.declareBid.suit].Min = 4;
                    response.Description = $"4+ {response.declareBid.suit}";
                    response.AlternateMatches = hand => BasicBidding.CountPlayingTricks(hand, response.declareBid.suit) + openerTricks >= 6 + response.GameLevel;
                }
            }
            else if (response.declareBid.level < response.GameLevel)
            {
                //  new suit below the game level
                response.BidMessage = BidMessage.Forcing;
                response.HandShape[response.declareBid.suit].Min = 5;
                response.IsGood = true;
                response.Description = $"5+ {response.declareBid.suit}";
            }
        }

        private static bool IsCuebidResponse(InterpretedBid overcall, InterpretedBid response)
        {
            return overcall.bidIsDeclare &&
                   response.bidIsDeclare &&
                   overcall.declareBid.suit != Suit.Unknown &&
                   overcall.declareBid.suit == response.declareBid.suit &&
                   overcall.declareBid.level + 1 == response.declareBid.level;
        }
    }
}