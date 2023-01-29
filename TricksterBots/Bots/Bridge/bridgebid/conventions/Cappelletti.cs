using System.Linq;
using Trickster.cloud;


// TODO: If we want to use this convention in the furture, it needs to be updated...
namespace Trickster.Bots
{
    internal class Cappelletti
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (IsCappelletti(bid))
            {
                InterpretOvercall(bid);
                return true;
            }

            if (bid.BidPhase == BidPhase.Advance && bid.History[bid.Index - 2].BidConvention == BidConvention.Cappelletti)
            {
                InterpretAdvance(bid.History[bid.Index - 2], bid);
                return true;
            }

            if (bid.Index >= 4 && bid.History[bid.Index - 4].BidConvention == BidConvention.Cappelletti)
            {
                InterpretRebid(bid.History[bid.Index - 4], bid.History[bid.Index - 2], bid);
                return true;
            }

            if (bid.Index >= 6
                && bid.History[bid.Index - 6].BidConvention == BidConvention.Cappelletti
                && bid.History[bid.Index - 4].BidConvention == BidConvention.AskingForMinor)
                return InterpretAdvanceRebid(bid.History[bid.Index - 6], bid);

            return false;
        }

        private static void Advance2C(InterpretedBid advance)
        {
            if (advance.bid == BidBase.Pass)
            {
                //  (1N)-2C-(P)-P
                advance.HandShape[Suit.Clubs].Min = 6;
                advance.Description = "Good 6+ Clubs";
                advance.Validate = hand => BasicBidding.IsGoodSuit(hand, Suit.Clubs);
            }

            if (advance.bid == BridgeBid.Redouble)
            {
                //  (1N)-2C-(X)-XX
                advance.BidPointType = BidPointType.Hcp;
                advance.Points.Min = 7;
                advance.IsBalanced = true;
                advance.Description = "support for any suit";
            }

            if (!advance.bidIsDeclare)
                return;

            if (advance.declareBid.level > 2)
                return;

            switch (advance.declareBid.suit)
            {
                case Suit.Diamonds:
                    //  (1N)-2C-(P)-2D
                    advance.BidConvention = BidConvention.Waiting;
                    advance.Description = string.Empty;
                    advance.AlternateMatches = hand => true;
                    break;

                case Suit.Hearts:
                case Suit.Spades:
                    //  (1N)-2C-(P)-2H
                    //  (1N)-2C-(P)-2S
                    advance.IsGood = true;
                    advance.HandShape[advance.declareBid.suit].Min = 5;
                    advance.Description = $"5+ {advance.declareBid.suit}";
                    break;

                case Suit.Unknown:
                    //  (1N)-2C-(P)-2N
                    advance.BidPointType = BidPointType.Hcp;
                    advance.Points.Min = 11;
                    advance.IsBalanced = true;
                    advance.Description = string.Empty;
                    break;
            }
        }

        private static void Advance2D(InterpretedBid advance)
        {
            if (advance.bid == BidBase.Pass)
            {
                //  (1N)-2D-(P)-P
                advance.HandShape[Suit.Diamonds].Min = 6;
                advance.Description = "Good 6+ Diamonds";
                advance.Validate = hand => BasicBidding.IsGoodSuit(hand, Suit.Diamonds);
            }

            if (!advance.bidIsDeclare)
                return;

            if (advance.declareBid.level > 3)
                return;

            switch (advance.declareBid.suit)
            {
                case Suit.Clubs:
                    //  (1N)-2D-(P)-3C
                    advance.IsGood = true;
                    advance.HandShape[Suit.Clubs].Min = 6;
                    advance.Description = "6+ Clubs";
                    break;

                //  Diamonds are not expected here; we would pass our partner's 2D call instead

                case Suit.Hearts:
                case Suit.Spades:
                    if (advance.declareBid.level == 2)
                    {
                        //  (1N)-2D-(P)-2H
                        //  (1N)-2D-(P)-2S
                        advance.HandShape[advance.declareBid.suit].Min = 3;
                        advance.Description = string.Empty;
                        advance.Validate = hand =>
                        {
                            //  give preference to our longer major
                            var major = advance.declareBid.suit;
                            var otherMajor = major == Suit.Hearts ? Suit.Spades : Suit.Hearts;
                            var counts = BasicBidding.CountsBySuit(hand);
                            return counts[major] >= counts[otherMajor];
                        };
                    }
                    else
                    {
                        //  (1N)-2D-(P)-3H
                        //  (1N)-2D-(P)-3S
                        advance.HandShape[advance.declareBid.suit].Min = 4;
                        advance.Description = "Inviting game";
                        advance.Validate = hand =>
                        {
                            //  give preference to our longer major
                            var major = advance.declareBid.suit;
                            var otherMajor = major == Suit.Hearts ? Suit.Spades : Suit.Hearts;
                            var counts = BasicBidding.CountsBySuit(hand);
                            return counts[major] >= counts[otherMajor];
                        };
                    }

                    break;

                case Suit.Unknown:
                    if (advance.declareBid.level == 2)
                    {
                        //  (1N)-2D-(P)-2N
                        advance.BidConvention = BidConvention.AskingForMinor;
                        advance.BidMessage = BidMessage.Forcing;
                        advance.Description = string.Empty;
                        advance.AlternateMatches = hand => true;
                        //  TODO: some length in both minors should be required - how much?
                    }

                    break;
            }
        }

        private static void Advance2N(InterpretedBid advance)
        {
            if (!advance.bidIsDeclare)
                return;

            if (BridgeBot.IsMinor(advance.declareBid.suit))
            {
                if (advance.declareBid.level == 3)
                {
                    //  (1N)-2N-(P)-3C
                    //  (1N)-2N-(P)-3D
                    advance.BidMessage = BidMessage.Signoff;
                    advance.Description = "Weak preference; no game interest";
                    advance.AlternateMatches = hand =>
                    {
                        var counts = BasicBidding.CountsBySuit(hand);
                        var minor = advance.declareBid.suit;
                        var other = minor == Suit.Clubs ? Suit.Diamonds : Suit.Clubs;
                        return counts[minor] >= counts[other];
                    };
                }
                else if (advance.declareBid.level == 4)
                {
                    //  (1N)-2N-(P)-4C
                    //  (1N)-2N-(P)-4D
                    advance.HandShape[advance.declareBid.suit].Min = 3;
                    advance.Description = $"Inviting game; 3+ {advance.declareBid.suit}";
                }
            }
            else if (BridgeBot.IsMajor(advance.declareBid.suit))
            {
                if (advance.declareBid.level == 3)
                {
                    //  (1N)-2N-(P)-3H
                    //  (1N)-2N-(P)-3S
                    advance.HandShape[advance.declareBid.suit].Min = 6;
                    advance.Description = $"6+ {advance.declareBid.suit}";
                }
            }
        }

        private static void AdvanceDouble(InterpretedBid advance)
        {
            if (!advance.bidIsDeclare)
                return;

            if (advance.declareBid.level > 2)
                return;

            if (advance.declareBid.suit == Suit.Unknown)
                return;

            //  pull to a suit contract if weak
            //  (1N)-X-(P)-2C
            //  (1N)-X-(P)-2D
            //  (1N)-X-(P)-2H
            //  (1N)-X-(P)-2S
            advance.BidPointType = BidPointType.Hcp;
            advance.Points.Max = 4;
            advance.HandShape[advance.declareBid.suit].Min = 5;
            advance.Description = $"Weak hand; 5+ {advance.declareBid.suit}";
        }

        private static void AdvanceMajor(InterpretedBid overcall, InterpretedBid advance)
        {
            if (advance.bid == BidBase.Pass)
            {
                //  (1N)-2H-(P)-P
                //  (1N)-2S-(P)-P
                advance.HandShape[overcall.declareBid.suit].Min = 3;
                advance.Description = $"3+ {overcall.declareBid.suit}";
            }

            if (!advance.bidIsDeclare)
                return;

            if (advance.declareBid.suit == overcall.declareBid.suit)
            {
                if (advance.declareBid.level == 3)
                {
                    //  (1N)-2H-(P)-3H
                    //  (1N)-2S-(P)-3S
                    advance.BidPointType = BidPointType.Dummy;
                    advance.Points.Min = 7;
                    advance.Points.Max = 10;
                    advance.HandShape[advance.declareBid.suit].Min = 3;
                    advance.Description = $"3+ {advance.declareBid.suit}";
                }
            }
            else if (advance.declareBid.suit == Suit.Unknown)
            {
                if (advance.declareBid.level == 2)
                {
                    //  (1N)-2H-(P)-2N
                    //  (1N)-2S-(P)-2N
                    advance.BidConvention = BidConvention.AskingForMinor;
                    advance.BidMessage = BidMessage.Forcing;
                    advance.Description = string.Empty;
                    advance.AlternateMatches = hand => true;
                }
            }
            else if (advance.declareBid.level == advance.LowestAvailableLevel(advance.declareBid.suit))
            {
                //  a new suit is natural and non-forcing
                //  (1N)-2H-(P)-3C
                //  (1N)-2H-(P)-3D
                //  (1N)-2H-(P)-2S
                //  (1N)-2S-(P)-3C
                //  (1N)-2S-(P)-3D
                //  (1N)-2S-(P)-3H
                advance.IsGood = true;
                advance.HandShape[advance.declareBid.suit].Min = 5;
                advance.Description = $"5+ {advance.declareBid.suit}";
            }
        }

        private static void InterpretAdvance(InterpretedBid overcall, InterpretedBid advance)
        {
            if (overcall.bid == BridgeBid.Double)
            {
                AdvanceDouble(advance);
                return;
            }

            switch (overcall.declareBid.suit)
            {
                case Suit.Clubs:
                    Advance2C(advance);
                    break;

                case Suit.Diamonds:
                    Advance2D(advance);
                    break;

                case Suit.Hearts:
                case Suit.Spades:
                    AdvanceMajor(overcall, advance);
                    break;

                case Suit.Unknown:
                    Advance2N(advance);
                    break;
            }
        }

        private static bool InterpretAdvanceRebid(InterpretedBid overcall, InterpretedBid rebid)
        {
            if (!rebid.bidIsDeclare)
                return false;

            if (!BridgeBot.IsMajor(rebid.declareBid.suit))
                return false;

            if (overcall.declareBid.suit != rebid.declareBid.suit)
                return false;

            if (rebid.declareBid.level != 3)
                return false;

            //  (1N)-2H-(P)-2N-(P)-3C-(P)-3H
            //  (1N)-2H-(P)-2N-(P)-3D-(P)-3H
            //  (1N)-2S-(P)-2N-(P)-3C-(P)-3S
            //  (1N)-2S-(P)-2N-(P)-3D-(P)-3S
            rebid.BidPointType = BidPointType.Dummy;
            rebid.Points.Min = 10;
            rebid.Points.Max = 12;
            rebid.HandShape[rebid.declareBid.suit].Min = 3;
            rebid.Description = $"Inviting game; 3+ {rebid.declareBid.suit}";

            return true;
        }

        private static void InterpretOvercall(InterpretedBid overcall)
        {
            if (overcall.bid == BridgeBid.Double)
            {
                //  (1N)-X
                //  (1N)-P-(P)-X
                overcall.BidConvention = BidConvention.Cappelletti;
                overcall.BidPointType = BidPointType.Hcp;
                overcall.Points.Min = 15;
                overcall.IsBalanced = true;
                overcall.Description = "equivalent to 1NT opening bid";
            }
            else if (overcall.bidIsDeclare)
            {
                //  (1N)-2?
                //  (1N)-P-(P)-2?
                overcall.BidConvention = BidConvention.Cappelletti;
                overcall.BidPointType = BidPointType.Hcp;
                overcall.Points.Max = 14;

                switch (overcall.declareBid.suit)
                {
                    case Suit.Clubs:
                        //  (1N)-2C
                        //  (1N)-P-(P)-2C
                        overcall.IsGood = true;
                        overcall.Description = "6+ card suit";
                        overcall.Validate = hand =>
                        {
                            var counts = BasicBidding.CountsBySuit(hand);
                            return SuitRank.stdSuits.Any(suit => counts[suit] >= 6 && BasicBidding.IsGoodSuit(hand, suit));
                        };
                        break;

                    case Suit.Diamonds:
                        //  (1N)-2D
                        //  (1N)-P-(P)-2D
                        overcall.HandShape[Suit.Hearts].Min = 5;
                        overcall.HandShape[Suit.Spades].Min = 5;
                        overcall.Description = "5-5 in Hearts and Spades";
                        break;

                    case Suit.Hearts:
                        //  (1N)-2H
                        //  (1N)-P-(P)-2H
                        overcall.HandShape[Suit.Hearts].Min = 5;
                        overcall.Description = "5-5 in Hearts and a minor";
                        overcall.Validate = hand =>
                        {
                            var counts = BasicBidding.CountsBySuit(hand);
                            return counts[Suit.Clubs] >= 5 || counts[Suit.Diamonds] >= 5;
                        };
                        break;

                    case Suit.Spades:
                        //  (1N)-2S
                        //  (1N)-P-(P)-2S
                        overcall.HandShape[Suit.Spades].Min = 5;
                        overcall.Description = "5-5 in Spades and a minor";
                        overcall.Validate = hand =>
                        {
                            var counts = BasicBidding.CountsBySuit(hand);
                            return counts[Suit.Clubs] >= 5 || counts[Suit.Diamonds] >= 5;
                        };
                        break;

                    case Suit.Unknown:
                        //  (1N)-2N
                        //  (1N)-P-(P)-2N
                        overcall.HandShape[Suit.Clubs].Min = 5;
                        overcall.HandShape[Suit.Diamonds].Min = 5;
                        overcall.Description = "5-5 in Clubs and Diamonds";
                        break;
                }
            }
        }

        private static void InterpretRebid(InterpretedBid overcall, InterpretedBid advance, InterpretedBid rebid)
        {
            if (!rebid.bidIsDeclare)
                return;

            if (advance.BidConvention == BidConvention.Waiting)
                if (rebid.declareBid.suit != Suit.Unknown && rebid.declareBid.level == rebid.LowestAvailableLevel(rebid.declareBid.suit))
                {
                    //  (1N)-2C-(P)-2D-(P)-2H
                    //  (1N)-2C-(P)-2D-(P)-2S
                    //  (1N)-2C-(P)-2D-(P)-3C
                    //  (1N)-2C-(P)-2D-(P)-3D
                    rebid.IsGood = true;
                    rebid.HandShape[rebid.declareBid.suit].Min = 6;
                    rebid.Description = $"6+ {rebid.declareBid.suit}";
                }

            if (advance.BidConvention == BidConvention.AskingForMinor && rebid.declareBid.level == 3)
                if (BridgeBot.IsMinor(rebid.declareBid.suit))
                {
                    if (overcall.declareBid.suit == Suit.Diamonds)
                    {
                        //  (1N)-2D-(P)-2N-(P)-3C
                        //  (1N)-2D-(P)-2N-(P)-3D
                        rebid.Description = "Better minor";
                        rebid.AlternateMatches = hand =>
                        {
                            var counts = BasicBidding.CountsBySuit(hand);
                            var minor = rebid.declareBid.suit;
                            var other = minor == Suit.Clubs ? Suit.Diamonds : Suit.Clubs;
                            return counts[minor] >= counts[other];
                        };
                    }
                    else
                    {
                        //  (1N)-2H-(P)-2N-(P)-3C
                        //  (1N)-2S-(P)-2N-(P)-3C
                        //  (1N)-2H-(P)-2N-(P)-3D
                        //  (1N)-2S-(P)-2N-(P)-3D
                        rebid.HandShape[rebid.declareBid.suit].Min = 5;
                        rebid.Description = $"5+ {rebid.declareBid.suit}";
                    }
                }
        }

        //  Cappelletti is used only for overcalling 1NT at the 2-level
        private static bool IsCappelletti(InterpretedBid bid)
        {
            if (bid.BidPhase != BidPhase.Overcall || bid.bidIsDeclare && bid.declareBid.level > 2)
                return false;

            var last = bid.History.Take(bid.Index).Last(b => b.bid != BidBase.Pass);

            if (last.BidPhase != BidPhase.Opening)
                return false;

            return last.bidIsDeclare && last.declareBid.suit == Suit.Unknown && last.declareBid.level == 1;
        }
    }
}