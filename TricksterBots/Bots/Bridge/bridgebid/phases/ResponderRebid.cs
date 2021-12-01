using Trickster.cloud;

namespace Trickster.Bots
{
    internal class ResponderRebid
    {
        public static void Interpret(InterpretedBid rebid)
        {
            TryPlaceContract(rebid);
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

            if (!isNT && bid.declareBid.level < 4 && partnerMinOfSuit < 3 && playerMinOfSuit < 3)
            {
                //  a new suit by responder is FORCING (unless opener rebid 1NT and responder's second suit is a non-jump bid that is lower ranking than responder's first suit)
                bid.BidMessage = BidMessage.Forcing;
                bid.Points.Min = bid.GamePoints - partnerSummary.Points.Min;
                bid.HandShape[bid.declareBid.suit].Min = 4;
                bid.Description = "New suit";
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
            else if (bid.declareBid.level == bid.GameLevel && (isNT && partnerSummary.IsBalanced || playerMinOfSuit > 0 || partnerMinOfSuit > 0))
            {
                //  sign-off at game of a previously bid suit: 3NT, 4H, 4S, 5C, 5D
                bid.Points.Min = bid.GamePoints - partnerSummary.Points.Min;
                bid.BidMessage = BidMessage.Signoff;
                bid.Description = "Sign-off at game";
                bid.IsBalanced = isNT;
            }
            else if (bid.declareBid.level == (isNT ? 2 : 3))
            {
                //  invite game: 2NT, 3 of a previously bid suit
                bid.Points.Min = InterpretedBid.InvitationalPoints - partnerSummary.Points.Min;
                bid.Points.Max = bid.GamePoints - 1 - partnerSummary.Points.Min;
                bid.IsBalanced = isNT; // 2NT is expected to be balanced (higher NT is not)
                bid.Description = "Inviting game";
            }
            else if (bid.declareBid.level == (isNT ? 1 : 2))
            {
                //  sign-off in partscore: Pass, 1NT, 2 of a previously bid suit
                bid.Points.Min = 19 - partnerSummary.Points.Min;
                bid.Points.Max = InterpretedBid.InvitationalPoints - 1 - partnerSummary.Points.Min;
                bid.BidMessage = BidMessage.Signoff;
                bid.Description = "No interest in game";
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
                        //  use dummy points if our partner bid the suit first (meaning we'll be the dummy)
                        bid.BidPointType = BidPointType.Dummy;

                    if (partnerMinOfSuit > 0)
                    {
                        //  if partner has shown length, ensure 8+ card combined fit in chosen suit
                        bid.HandShape[bid.declareBid.suit].Min = 8 - partnerSummary.HandShape[bid.declareBid.suit].Min;
                    }
                    else if (playerMinOfSuit > 0)
                    {
                        //  with no info from partner in our suit...
                        if (bid.declareBid.level < bid.GameLevel)
                            //  we need 6+ cards to rebid our suit below game
                            bid.HandShape[bid.declareBid.suit].Min = 6;
                        else if (bid.declareBid.level == bid.GameLevel)
                            //  we need 7+ cards to rebid our suit at game
                            bid.HandShape[bid.declareBid.suit].Min = 7;
                    }

                    if (bid.HandShape[bid.declareBid.suit].Min > 0) bid.Description += $"; {bid.HandShape[bid.declareBid.suit].Min}+ {bid.declareBid.suit}";
                }
            }
        }
    }
}