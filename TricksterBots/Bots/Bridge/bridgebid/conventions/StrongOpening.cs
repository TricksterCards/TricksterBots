using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    //  TODO: review http://www.acbl.org/assets/documents/teachers/Teacher-Manuals/Commonly-Used-Conventions-Lesson-8.pdf to look for additional gaps
    internal class StrongOpening
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (bid.BidPhase == BidPhase.Opening) return InterpretOpening(bid);

            if (bid.Index >= 2 && bid.History[bid.Index - 2].BidConvention == BidConvention.StrongOpening)
            {
                InterpretResponse(bid);
                return true;
            }

            if (bid.Index >= 4 && bid.History[bid.Index - 4].BidConvention == BidConvention.StrongOpening)
            {
                InterpretRebid(bid.History[bid.Index - 2], bid);
                return true;
            }

            if (bid.Index >= 6 && bid.History[bid.Index - 6].BidConvention == BidConvention.StrongOpening)
            {
                InterpretResponderRebid(bid.History[bid.Index - 4], bid.History[bid.Index - 2], bid);
                return true;
            }

            return false;
        }

        private static bool InterpretOpening(InterpretedBid opening)
        {
            if (!opening.bidIsDeclare || opening.declareBid.suit != Suit.Clubs || opening.declareBid.level != 2)
                return false;

            //  2C
            opening.BidConvention = BidConvention.StrongOpening;
            opening.BidMessage = BidMessage.Forcing;
            opening.BidPointType = BidPointType.Distribution;
            opening.Points.Min = 22;
            opening.Description = string.Empty;
            //  TODO: opening.AlternateMatches = (hand, ibid) => BridgeBot.CountTricks(hand) >= 9;

            return true;
        }

        private static void InterpretRebid(InterpretedBid response, InterpretedBid rebid)
        {
            if (!rebid.bidIsDeclare)
                return;

            var lowestAvailableLevel = rebid.LowestAvailableLevel(rebid.declareBid.suit);

            if (rebid.declareBid.suit == Suit.Unknown)
            {
                if (rebid.declareBid.level == 2)
                {
                    //  2C-**-2N
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.Points.Min = 22;
                    rebid.Points.Max = 24;
                    rebid.IsBalanced = true;
                    rebid.Description = string.Empty;
                }
                else if (rebid.declareBid.level == 3)
                {
                    //  2C-**-3N
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.Points.Min = 25;
                    rebid.Points.Max = 27;
                    rebid.IsBalanced = true;
                    rebid.Description = string.Empty;
                }
                else if (rebid.declareBid.level == 4)
                {
                    //  2C-**-4N
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.Points.Min = 28;
                    rebid.Points.Max = 30;
                    rebid.IsBalanced = true;
                    rebid.Description = string.Empty;
                }
            }
            else if (response.BidConvention == BidConvention.Waiting)
            {
                if (rebid.declareBid.level == lowestAvailableLevel)
                {
                    //  2C-2D-2H
                    //  2C-2D-2S
                    //  2C-2D-3C
                    //  2C-2D-3D
                    rebid.BidMessage = BidMessage.Forcing;
                    rebid.HandShape[rebid.declareBid.suit].Min = 5;
                    rebid.Description = $"5+ {rebid.declareBid.suit}";
                    rebid.Validate = hand =>
                    {
                        var counts = BasicBidding.CountsBySuit(hand);
                        return counts[rebid.declareBid.suit] == counts.Max(c => c.Value);
                    };
                }
            }
            else if (rebid.declareBid.level < 4 && rebid.declareBid.level == lowestAvailableLevel)
            {
                //  rebids by opener are natural and the partnership is committed to game
                //  TODO: treat 3C as Stayman if partner responded 2N

                //  2C-2D-2H
                //  2C-2D-2S
                //  2C-2D-3D

                //  2C-2H-2S
                //  2C-2H-3C
                //  2C-2H-3D
                //  2C-2H-3H

                //  2C-2S-3C
                //  2C-2S-3D
                //  2C-2S-3H
                //  2C-2S-3S

                //  2C-3C-3D
                //  2C-3C-3H
                //  2C-3C-3S

                //  raising partner's suit requires 3+ cards, otherwise we need 5+
                var nCardsInSuit = response.bidIsDeclare && response.declareBid.suit == rebid.declareBid.suit ? 3 : 5;
                rebid.BidMessage = BidMessage.Forcing;
                rebid.HandShape[rebid.declareBid.suit].Min = nCardsInSuit;
                rebid.Description = $"{nCardsInSuit}+ {rebid.declareBid.suit}";
                rebid.Validate = hand =>
                {
                    var counts = BasicBidding.CountsBySuit(hand);
                    return counts[rebid.declareBid.suit] == counts.Max(c => c.Value);
                };
            }
        }

        private static void InterpretResponderRebid(InterpretedBid response, InterpretedBid rebid, InterpretedBid responderRebid)
        {
            if (!rebid.bidIsDeclare || !responderRebid.bidIsDeclare)
                return;

            var lowestAvailableLevel = responderRebid.LowestAvailableLevel(responderRebid.declareBid.suit);

            if (response.BidConvention == BidConvention.Waiting)
            {
                if (responderRebid.declareBid.level < 4)
                {
                    if (rebid.declareBid.suit == Suit.Hearts && responderRebid.declareBid.suit == Suit.Clubs
                        || rebid.declareBid.suit == Suit.Spades && responderRebid.declareBid.suit == Suit.Clubs
                        || rebid.declareBid.suit == Suit.Clubs && responderRebid.declareBid.suit == Suit.Diamonds
                        || rebid.declareBid.suit == Suit.Diamonds && responderRebid.declareBid.suit == Suit.Hearts
                    )
                    {
                        //  2C-2D-2H-3C
                        //  2C-2D-2S-3C
                        //  2C-2D-3C-3D
                        //  2C-2D-3D-3H
                        responderRebid.BidConvention = BidConvention.SecondNegative;
                        responderRebid.BidPointType = BidPointType.Hcp;
                        responderRebid.Points.Max = 3;
                        responderRebid.Description = "weak hand";
                    }
                    else if (responderRebid.declareBid.level == lowestAvailableLevel)
                    {
                        if (responderRebid.declareBid.suit == Suit.Unknown)
                        {
                            //  2C-2D-2H-2N
                            //  2C-2D-2S-2N
                            //  2C-2D-3C-3N
                            //  2C-2D-3D-3N
                            responderRebid.BidPointType = BidPointType.Hcp;
                            responderRebid.BidMessage = BidMessage.Forcing;
                            responderRebid.Points.Min = 5;
                            responderRebid.IsBalanced = true;
                            responderRebid.Description = string.Empty;
                        }
                        else if (responderRebid.declareBid.suit == rebid.declareBid.suit)
                        {
                            //  2C-2D-2H-3H
                            //  2C-2D-2S-3S
                            responderRebid.BidPointType = BidPointType.Dummy;
                            responderRebid.BidMessage = BidMessage.Forcing;
                            // TODO: Adjust min points to indicate slam interest
                            responderRebid.Points.Min = 5;
                            responderRebid.HandShape[responderRebid.declareBid.suit].Min = 3;
                            responderRebid.Description = $"3+ {responderRebid.declareBid.suit}";
                        }
                        else
                        {
                            //  2C-2D-2H-2S
                            //  2C-2D-2H-3C
                            //  2C-2D-2H-3D

                            //  2C-2D-2S-3C
                            //  2C-2D-2S-3D
                            //  2C-2D-2S-3H

                            //  2C-2D-3C-3D
                            //  2C-2D-3C-3H
                            //  2C-2D-3C-3S

                            //  2C-2D-3D-3H
                            //  2C-2D-3D-3S
                            responderRebid.BidMessage = BidMessage.Forcing;
                            responderRebid.Points.Min = 5;
                            responderRebid.HandShape[responderRebid.declareBid.suit].Min = 5;
                            responderRebid.Description = $"5+ {responderRebid.declareBid.suit}";
                        }
                    }
                }
                else if (responderRebid.declareBid.suit == rebid.declareBid.suit && responderRebid.declareBid.level == responderRebid.GameLevel)
                {
                    responderRebid.BidPointType = BidPointType.Dummy;
                    responderRebid.Points.Max = 4;
                    responderRebid.HandShape[responderRebid.declareBid.suit].Min = 4;
                    responderRebid.Description = $"4 {responderRebid.declareBid.suit}; no slam interest";
                }
            }
        }

        private static void InterpretResponse(InterpretedBid response)
        {
            if (!response.bidIsDeclare)
                return;

            var db = response.declareBid;
            var overcall = response.History[response.Index - 1];

            if (db.level == 2 && db.suit == Suit.Diamonds && overcall.bid == BidBase.Pass)
            {
                //  2C-(P)-2D
                response.BidConvention = BidConvention.Waiting;
                response.BidMessage = BidMessage.Forcing;
                response.Description = "any distribution";
                response.AlternateMatches = hand => true;
            }
            else if (db.level < 3 || db.level == 3 && BridgeBot.suitRank[db.suit] <= BridgeBot.suitRank[Suit.Diamonds])
            {
                response.BidMessage = BidMessage.Forcing;
                response.Points.Min = 8;

                if (db.suit == Suit.Unknown)
                {
                    //  2C-2N
                    response.BidPointType = BidPointType.Hcp;
                    response.IsBalanced = true;
                    response.Description = string.Empty;
                }
                else
                {
                    //  2C-(X)-2D
                    //  2C-2H
                    //  2C-2S
                    //  2C-3C
                    //  2C-3D
                    response.HandShape[db.suit].Min = 5;
                    response.IsGood = true;
                    response.Description = $"5+ {db.suit}";
                }
            }
        }
    }
}