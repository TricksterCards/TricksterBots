using Trickster.cloud;

namespace Trickster.Bots
{
    internal class Stayman
    {
        //  Stayman can be used after a notrump opening or after a notrump rebid of a strong opening (2C) where responder is waiting (2D).
        //  Stayman is "off" if opponents interfere with anything other than a double, but a cuebid can be used with game-forcing strength as a substitute.
        public static bool CanUseStayman(InterpretedBid bid)
        {
            if (!CanUseAlternateStayman(bid))
                return false;

            var interference = bid.History[bid.Index - 1];
            if (interference.bid != BidBase.Pass && interference.bid != BridgeBid.Double)
                return false;

            return true;
        }

        public static bool Interpret(InterpretedBid bid)
        {
            if (CanUseStayman(bid)) return InterpretStayman(bid);

            if (CanUseAlternateStayman(bid)) return InterpretCuebidStayman(bid.History[bid.Index - 1], bid);

            if (bid.Index >= 4 && bid.History[bid.Index - 2].BidConvention == BidConvention.Stayman)
            {
                InterpretOpenerRebid(bid);
                return true;
            }

            if (bid.Index >= 6 && bid.History[bid.Index - 4].BidConvention == BidConvention.Stayman) return InterpretResponderRebid(bid);

            return false;
        }

        private static bool CanUseAlternateStayman(InterpretedBid bid)
        {
            if (bid.Index < 2)
                return false;

            var opening = bid.History[bid.Index - 2];
            if (opening.BidConvention != BidConvention.None)
                return false;

            if (!opening.bidIsDeclare || opening.declareBid.suit != Suit.Unknown)
                return false;

            if (bid.BidPhase != BidPhase.Response && bid.BidPhase != BidPhase.ResponderRebid)
                return false;

            if (bid.BidPhase == BidPhase.ResponderRebid)
            {
                var originalOpening = bid.History[bid.Index - 6];
                if (originalOpening.BidConvention != BidConvention.StrongOpening)
                    return false;

                var originalResponse = bid.History[bid.Index - 4];
                if (originalResponse.BidConvention != BidConvention.Waiting)
                    return false;
            }

            return true;
        }

        private static bool InterpretCuebidStayman(InterpretedBid interference, InterpretedBid response)
        {
            if (!response.bidIsDeclare)
                return false;

            var cuebidSuit = interference.declareBid.suit;
            if (response.declareBid.suit != cuebidSuit)
                return false;

            if (response.declareBid.level > 3)
                return false;

            if (response.declareBid.level != interference.declareBid.level + 1)
                return false;

            response.BidConvention = BidConvention.Stayman;
            response.BidMessage = BidMessage.Forcing;
            response.BidPointType = BidPointType.Hcp;
            response.Points.Min = 10;

            if (BridgeBot.IsMajor(cuebidSuit))
            {
                var other = cuebidSuit == Suit.Hearts ? Suit.Spades : Suit.Hearts;
                response.HandShape[other].Min = 4;
                response.Description = $"4+ {other}";
            }
            else
            {
                response.Validate = hand =>
                {
                    //  we should have 4H or 4S
                    var counts = BasicBidding.CountsBySuit(hand);
                    return counts[Suit.Hearts] >= 4 || counts[Suit.Spades] >= 4;
                };
                response.Description = "asking for a major";
            }

            return true;
        }

        private static void InterpretOpenerRebid(InterpretedBid bid)
        {
            if (!bid.bidIsDeclare)
                return;

            var stayman = bid.History[bid.Index - 2].declareBid;
            var db = bid.declareBid;

            //  the only accepted responses are in a suit at the same level
            if (db.suit == Suit.Unknown || db.level != stayman.level)
                return;

            switch (db.suit)
            {
                //  2C-2D
                //  3C-3D
                //  4C-4D
                case Suit.Diamonds:
                    bid.BidConvention = BidConvention.AnswerStayman;
                    bid.HandShape[Suit.Hearts].Max = 3;
                    bid.HandShape[Suit.Spades].Max = 3;
                    bid.Description = "No 4+ card major";
                    break;
                //  2C-2H
                //  2C-2S
                //  3C-3H
                //  3C-3S
                //  4C-4H
                //  4C-4S
                case Suit.Hearts:
                case Suit.Spades:
                    bid.HandShape[db.suit].Min = 4;
                    bid.Description = $"4+ {db.suit}";
                    if (db.suit == Suit.Spades)
                    {
                        bid.HandShape[Suit.Hearts].Max = 3;
                        bid.Description += "; denies 4 Hearts";
                    }

                    break;
            }
        }

        private static bool InterpretResponderRebid(InterpretedBid rebid)
        {
            var answer = rebid.History[rebid.Index - 2];

            if (!rebid.bidIsDeclare || !answer.bidIsDeclare)
                return false;

            var opening = rebid.History[rebid.Index - 6];

            if (rebid.declareBid.level <= 3 && BridgeBot.IsMajor(rebid.declareBid.suit) && rebid.declareBid.suit != answer.declareBid.suit)
            {
                //  show a 5-card major by bidding it (implies 4 of the other major)
                var otherMajor = rebid.declareBid.suit == Suit.Hearts ? Suit.Spades : Suit.Hearts;
                rebid.HandShape[rebid.declareBid.suit].Min = 5;
                rebid.HandShape[otherMajor].Min = 4;
                rebid.Description = $"5+ {rebid.declareBid.suit} and 4+ {otherMajor}";

                if (rebid.declareBid.level == 2)
                {
                    //  show invitational hand by rebidding at the 2-level
                    rebid.Points.Min = InterpretedBid.InvitationalPoints - opening.Points.Min;
                    rebid.Points.Max = rebid.GamePoints - 1 - opening.Points.Min;
                }
                else
                {
                    //  show a game forcing hand by jumping a level
                    rebid.BidMessage = BidMessage.Forcing;
                    rebid.Points.Min = rebid.GamePoints - opening.Points.Min;
                }

                return true;
            }

            if (rebid.declareBid.level == 3 && BridgeBot.IsMinor(rebid.declareBid.suit))
            {
                //  (SAYC Booklet): if responder rebids three of either minor, he shows slam interest and at least a five-card suit
                rebid.Points.Min = InterpretedBid.SmallSlamPoints - opening.Points.Min;
                rebid.HandShape[rebid.declareBid.suit].Min = 5;
                rebid.Description = $"5+ {rebid.declareBid.suit}; slam interest";
                return true;
            }

            return false;
        }

        private static bool InterpretStayman(InterpretedBid response)
        {
            if (!response.bidIsDeclare)
                return false;

            if (response.declareBid.suit != Suit.Clubs)
                return false;

            if (response.declareBid.level != response.LowestAvailableLevel(response.declareBid.suit))
                return false;

            //  1N-2C
            //  2N-3C
            //  3N-4C
            //  2C-2D-2N-3C
            //  2C-2D-3N-4C
            response.BidConvention = BidConvention.Stayman;
            response.BidMessage = BidMessage.Forcing;
            response.Points.Min = response.declareBid.level == 1 ? 8 : response.declareBid.level == 2 ? 4 : 0;
            response.Description = "asking for a major";
            response.Priority = 1; // always prefer Stayman over other bids when valid
            response.Validate = hand =>
            {
                //  we should have 4H or 4S (any more and we'll use a transfer instead)
                var counts = BasicBidding.CountsBySuit(hand);
                return counts[Suit.Hearts] == 4 || counts[Suit.Spades] == 4;
            };

            return true;
        }
    }
}