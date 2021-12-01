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
                //  TODO: handle pass/double/redouble rebids
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
            //  TODO: lower-level cases

            if (response.bidIsDeclare && response.declareBid.level == 4 && response.declareBid.suit == Suit.Unknown)
                //  rebid after a 4NT response
                if (rebid.declareBid.suit == Suit.Unknown)
                    switch (rebid.declareBid.level)
                    {
                        //  1N-4N-5N
                        //  2N-4N-5N
                        //  3N-4N-5N
                        case 5:
                            rebid.Points.Min = InterpretedBid.SmallSlamPoints - 1 - response.Points.Min;
                            rebid.BidPointType = BidPointType.Hcp;
                            break;

                        //  1N-4N-6N
                        //  2N-4N-6N
                        //  3N-4N-6N
                        case 6:
                            rebid.Points.Min = InterpretedBid.SmallSlamPoints - response.Points.Min;
                            rebid.BidPointType = BidPointType.Hcp;
                            break;
                    }
        }

        private static void RebidPreemptOpening(InterpretedBid opening, InterpretedBid response, InterpretedBid rebid)
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

            if (response.bidIsDeclare && opening.declareBid.suit == response.declareBid.suit)
            {
                //  partner raised our suit
                if (response.declareBid.level == 2)
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
                            rebid.Points.Min = 18;
                            rebid.Points.Max = 19;
                            rebid.IsBalanced = true;
                            rebid.Description = "inviting game";
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
                            rebid.HandShape[rebid.declareBid.suit].Min = 4;
                            rebid.Description = "inviting game";
                            rebid.Validate = hand => !BasicBidding.IsGoodSuit(hand, rebid.declareBid.suit);
                        }
                        else
                        {
                            //  1C-2C-3C
                            //  1D-2D-3D
                            //  1H-2H-3H
                            //  1S-2S-3S
                            rebid.Points.Min = 17;
                            rebid.Points.Max = 18;
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
                            rebid.Description = "Sign-off at game";

                            //  prefer playing game in notrump for minor suits
                            if (BridgeBot.IsMinor(rebid.declareBid.suit) && rebid.LowestAvailableLevel(Suit.Unknown, true) <= 3)
                                rebid.Validate = hand => false;
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
                //  rebidding notrump
                if (rebid.declareBid.level == lowestAvailableLevel)
                {
                    //  minimum: lowest available level (13-15 points)
                    rebid.Points.Min = 13;
                    rebid.Points.Max = 15;
                    rebid.IsBalanced = true;
                    rebid.Description = "minimum";
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 1)
                {
                    //  maximum: jump (18-19 points)
                    rebid.Points.Min = 18;
                    rebid.Points.Max = 19;
                    rebid.IsBalanced = true;
                    rebid.Description = "jump";
                }
            }
            else if (response.bidIsDeclare && rebid.declareBid.suit == response.declareBid.suit)
            {
                //  raising responder's suit
                if (rebid.declareBid.level == lowestAvailableLevel)
                {
                    //  minimum: lowest available level; may have good 3-card support (13-15 points)
                    rebid.Points.Min = 13;
                    rebid.Points.Max = 15;
                    rebid.HandShape[rebid.declareBid.suit].Min = 3;
                    rebid.Description = $"Minimum raise; 3+ {rebid.declareBid.suit}";
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 1)
                {
                    //  medium: jump raise (16-18 points)
                    rebid.Points.Min = 16;
                    rebid.Points.Max = 18;
                    rebid.HandShape[rebid.declareBid.suit].Min = 4;
                    rebid.Description = $"Jump raise; 4+ {rebid.declareBid.suit}";
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 2)
                {
                    //  maximum: double jump (19-21 points)
                    rebid.Points.Min = 19;
                    rebid.Points.Max = 21;
                    rebid.HandShape[rebid.declareBid.suit].Min = 5;
                    rebid.Description = $"Double jump raise; 5+ {rebid.declareBid.suit}";
                }
            }
            else if (rebid.declareBid.suit == opening.declareBid.suit)
            {
                //  rebidding opener's suit
                if (rebid.declareBid.level == lowestAvailableLevel)
                {
                    //  minimum: lowest available level (13-15 points)
                    rebid.Points.Min = 13;
                    rebid.Points.Max = 15;
                    rebid.HandShape[rebid.declareBid.suit].Min = 6;
                    rebid.Description = $"Minimum rebid; 6+ {rebid.declareBid.suit}";
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 1)
                {
                    //  medium: jump rebid (16-18 points)
                    rebid.Points.Min = 16;
                    rebid.Points.Max = 18;
                    rebid.HandShape[rebid.declareBid.suit].Min = 6;
                    rebid.Description = $"Jump rebid; 6+ {rebid.declareBid.suit}";
                }
                else if (rebid.declareBid.level == lowestAvailableLevel + 2)
                {
                    //  maximum: double-jump rebid (19-21 points)
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
                    //  minimum: at the one level or at the two level that if lower ranking than opening suit
                    //  (not reversing; this has the wide range of 13–18 points)
                    rebid.Points.Min = 13;
                    rebid.Points.Max = 18;
                    rebid.HandShape[rebid.declareBid.suit].Min = 4;
                    rebid.Description = $"New suit; 4+ {rebid.declareBid.suit}";
                }
                else if (rebid.declareBid.suit > opening.declareBid.suit && rebid.declareBid.level == lowestAvailableLevel)
                {
                    //  medium: REVERSE in a new suit (16-21 points)
                    rebid.Points.Min = 16;
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
                    //  maximum: jump shift in a new suit (19-21 points)
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