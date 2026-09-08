using System;
using Trickster.cloud;

namespace Trickster.Bots
{
    //  Acol opener rebids: NT rebids show 15-16 / 17-18 / 19, suit rebids show 5+ cards
    internal class AcolOpenerRebid
    {
        public static void Interpret(InterpretedBid rebid)
        {
            var opening = rebid.History[rebid.Index - 4];
            var response = rebid.History[rebid.Index - 2];

            if (!rebid.bidIsDeclare)
            {
                //  TODO: handle pass/double/redouble rebids
            }
            else if (opening.declareBid.suit == Suit.Unknown)
            {
                RebidNTOpening(response, rebid);
            }
            else if (opening.declareBid.level == 1)
            {
                RebidSuitOpening(opening, response, rebid);
            }
            else
            {
                OpenerRebid.RebidPreemptOpening(opening, response, rebid);
            }
        }

        private static void RebidNTOpening(InterpretedBid response, InterpretedBid rebid)
        {
            //  if responder invites with 2NT (11-12) then bid 3NT with a maximum
            if (response.bidIsDeclare && response.declareBid.level == 2 && response.declareBid.suit == Suit.Unknown
                && rebid.declareBid.level == 3 && rebid.declareBid.suit == Suit.Unknown)
            {
                rebid.Points.Min = 13;
                rebid.Points.Max = 14;
                rebid.BidPointType = BidPointType.Hcp;
                rebid.Description = "Accept invitation and sign-off at game";
                return;
            }

            if (response.bidIsDeclare && response.declareBid.level == 4 && response.declareBid.suit == Suit.Unknown)
                //  rebid after a quantitative 4NT response
                if (rebid.declareBid.suit == Suit.Unknown)
                    switch (rebid.declareBid.level)
                    {
                        //  1N-4N-5N
                        case 5:
                            rebid.Points.Min = InterpretedBid.SmallSlamPoints - 1 - response.Points.Min;
                            rebid.BidPointType = BidPointType.Hcp;
                            break;

                        //  1N-4N-6N
                        case 6:
                            rebid.Points.Min = InterpretedBid.SmallSlamPoints - response.Points.Min;
                            rebid.BidPointType = BidPointType.Hcp;
                            break;
                    }
        }

        private static void RebidSuitOpening(InterpretedBid opening, InterpretedBid response, InterpretedBid rebid)
        {
            var lowestAvailableLevel = rebid.LowestAvailableLevel(rebid.declareBid.suit);

            if (response.bidIsDeclare && opening.declareBid.suit == response.declareBid.suit)
            {
                //  partner raised our suit
                if (response.declareBid.level == 2)
                {
                    //  partner made a single raise (6-9)
                    if (rebid.declareBid.level < rebid.GameLevel && rebid.declareBid.level == lowestAvailableLevel)
                    {
                        if (rebid.declareBid.suit == Suit.Unknown)
                        {
                            //  1x-2x-2N
                            rebid.Points.Min = 17;
                            rebid.Points.Max = 18;
                            rebid.IsBalanced = true;
                            rebid.Description = "inviting game";
                        }
                        else if (rebid.declareBid.suit != response.declareBid.suit)
                        {
                            //  1x-2x-<new suit>: help-suit game try
                            rebid.BidConvention = BidConvention.HelpSuitGameTry;
                            rebid.BidMessage = BidMessage.Forcing;
                            rebid.Points.Min = 16;
                            rebid.Points.Max = 18;
                            rebid.HandShape[rebid.declareBid.suit].Min = 4;
                            rebid.Description = "inviting game";
                            rebid.Validate = hand => !BasicBidding.IsGoodSuit(hand, rebid.declareBid.suit, 4);
                        }
                        else
                        {
                            //  1x-2x-3x: re-raise inviting game
                            rebid.Points.Min = 16;
                            rebid.Points.Max = 18;
                            rebid.HandShape[rebid.declareBid.suit].Min = 5;
                            rebid.Description = $"Inviting game; 5+ {rebid.declareBid.suit}";
                        }
                    }
                    else if (rebid.declareBid.level == rebid.GameLevel)
                    {
                        if (rebid.declareBid.suit == response.declareBid.suit ||
                            rebid.declareBid.suit == Suit.Unknown && BridgeBot.IsMinor(opening.declareBid.suit))
                        {
                            //  1C-2C-3N (5C if 3N is not available)
                            //  1H-2H-4H
                            //  1S-2S-4S
                            rebid.BidMessage = BidMessage.Signoff;
                            rebid.Points.Min = 19;
                            rebid.Points.Max = 21;
                            rebid.Description = "Sign-off at game";

                            //  prefer playing game in notrump for minor suits
                            if (BridgeBot.IsMinor(rebid.declareBid.suit) && rebid.LowestAvailableLevel(Suit.Unknown, true) <= 3)
                                rebid.Validate = hand => false;
                        }
                    }
                }
                else if (response.declareBid.level == 3 && rebid.declareBid.level == rebid.GameLevel)
                {
                    //  partner made a limit raise (10-12); accept with 14+ or any unbalanced hand
                    if (rebid.declareBid.suit == response.declareBid.suit ||
                        rebid.declareBid.suit == Suit.Unknown && BridgeBot.IsMinor(opening.declareBid.suit))
                    {
                        rebid.BidMessage = BidMessage.Signoff;
                        rebid.Points.Min = 14;
                        rebid.Description = "Sign-off at game";

                        if (BridgeBot.IsMinor(rebid.declareBid.suit) && rebid.LowestAvailableLevel(Suit.Unknown, true) <= 3)
                            //  prefer playing game in notrump for minor suits
                            rebid.Validate = hand => false;
                        else
                            //  if we're unbalanced, get to game even with a minimum
                            rebid.AlternateMatches = hand => !BasicBidding.IsBalanced(hand);
                    }
                }
            }
            else if (rebid.declareBid.suit == Suit.Unknown)
            {
                //  a natural 2NT response (10-12) already limited responder's hand, so place the
                //  contract instead of using the NT ladder (which would misread 4NT as a jump)
                if (response.bidIsDeclare && response.declareBid.suit == Suit.Unknown && response.declareBid.level == 2 &&
                    response.BidConvention == BidConvention.None)
                {
                    if (rebid.declareBid.level == 3)
                    {
                        rebid.Points.Min = 13;
                        rebid.BidPointType = BidPointType.Hcp;
                        rebid.BidMessage = BidMessage.Signoff;
                        rebid.Description = "Accept invitation and sign-off at game";
                    }
                }
                //  rebidding notrump after a suit opening shows a balanced hand too strong for 1NT
                else if (rebid.declareBid.level == lowestAvailableLevel)
                {
                    //  1x-1y-1N (or 2N over a 2-level response): 15-16
                    rebid.Points.Min = 15;
                    rebid.Points.Max = 16;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.IsBalanced = true;
                    rebid.Description = "minimum";
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 1)
                {
                    //  jump in notrump: 17-18
                    rebid.Points.Min = 17;
                    rebid.Points.Max = 18;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.IsBalanced = true;
                    rebid.Description = "jump";
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 2)
                {
                    //  double jump in notrump: 19
                    rebid.Points.Min = 19;
                    rebid.Points.Max = 20;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.IsBalanced = true;
                    rebid.Description = "double jump";
                }
            }
            else if (response.bidIsDeclare && rebid.declareBid.suit == response.declareBid.suit)
            {
                //  raise with enough support for an 8-card fit given responder's promised length
                //  (a 2-level major response promised 5, so a 3-card raise is enough)
                var minSupport = Math.Min(Math.Max(8 - response.HandShape[rebid.declareBid.suit].Min, 3), 4);

                if (rebid.declareBid.level == lowestAvailableLevel)
                {
                    //  minimum raise (12-15)
                    rebid.BidPointType = BidPointType.Dummy;
                    rebid.Points.Min = 12;
                    rebid.Points.Max = 15;
                    rebid.HandShape[rebid.declareBid.suit].Min = minSupport;
                    rebid.Description = $"Minimum raise; {minSupport}+ {rebid.declareBid.suit}";

                    //  prefer supporting a known 8-card major fit over other minimum rebids
                    if (BridgeBot.IsMajor(rebid.declareBid.suit))
                        rebid.Priority = 50;
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 1)
                {
                    //  jump raise (16-18)
                    rebid.BidPointType = BidPointType.Dummy;
                    rebid.Points.Min = 16;
                    rebid.Points.Max = 18;
                    rebid.HandShape[rebid.declareBid.suit].Min = 4;
                    rebid.Description = $"Jump raise; 4+ {rebid.declareBid.suit}";
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 2)
                {
                    //  double jump raise (19-21)
                    rebid.BidPointType = BidPointType.Dummy;
                    rebid.Points.Min = 19;
                    rebid.Points.Max = 21;
                    rebid.HandShape[rebid.declareBid.suit].Min = 4;
                    rebid.Description = $"Double jump raise; 4+ {rebid.declareBid.suit}";
                }
            }
            else if (rebid.declareBid.suit == opening.declareBid.suit)
            {
                //  rebidding opener's suit shows extra length
                if (rebid.declareBid.level == lowestAvailableLevel)
                {
                    //  minimum rebid (12-15) with 5+ cards
                    rebid.Points.Min = 12;
                    rebid.Points.Max = 15;
                    rebid.HandShape[rebid.declareBid.suit].Min = 5;
                    rebid.Description = $"Minimum rebid; 5+ {rebid.declareBid.suit}";
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 1)
                {
                    //  jump rebid (16-18) with 6+ cards
                    rebid.Points.Min = 16;
                    rebid.Points.Max = 18;
                    rebid.HandShape[rebid.declareBid.suit].Min = 6;
                    rebid.Description = $"Jump rebid; 6+ {rebid.declareBid.suit}";
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 2)
                {
                    //  double jump rebid (19-21) with 7+ cards
                    rebid.Points.Min = 19;
                    rebid.Points.Max = 21;
                    rebid.HandShape[rebid.declareBid.suit].Min = 7;
                    rebid.Description = $"Double jump rebid; 7+ {rebid.declareBid.suit}";
                }
            }
            else
            {
                //  new suit
                if (rebid.declareBid.level == 1 ||
                    rebid.declareBid.level == 2 &&
                    BridgeBot.suitRank[rebid.declareBid.suit] < BridgeBot.suitRank[opening.declareBid.suit])
                {
                    //  minimum: not reversing (wide range 12-18)
                    rebid.Points.Min = 12;
                    rebid.Points.Max = 18;
                    rebid.HandShape[rebid.declareBid.suit].Min = 4;
                    rebid.Description = $"New suit; 4+ {rebid.declareBid.suit}";
                }
                else if (BridgeBot.suitRank[rebid.declareBid.suit] > BridgeBot.suitRank[opening.declareBid.suit] && rebid.declareBid.level == lowestAvailableLevel)
                {
                    //  reverse (16+): first suit is longer than the second
                    rebid.Points.Min = 16;
                    rebid.Points.Max = 19;
                    rebid.BidMessage = BidMessage.Forcing;
                    rebid.HandShape[rebid.declareBid.suit].Min = 4;
                    rebid.HandShape[opening.declareBid.suit].Min = 5;
                    rebid.Description = $"Reverse; 4+ {rebid.declareBid.suit} and 5+ {opening.declareBid.suit}";
                    //  ensure we don't use a reverse with a flat (4-3-3-3) hand
                    rebid.Validate = hand => !BasicBidding.IsFlat(hand);
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 1)
                {
                    //  jump shift in a new suit (19-21)
                    rebid.Points.Min = 19;
                    rebid.Points.Max = 21;
                    rebid.BidMessage = BidMessage.Forcing;
                    rebid.HandShape[rebid.declareBid.suit].Min = 4;
                    rebid.Description = $"Jump shift; 4+ {rebid.declareBid.suit}";
                }
            }
        }
    }
}
