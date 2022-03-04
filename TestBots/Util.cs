using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace TestBots
{
    internal static class Util
    {
        public static string PrettyHand(Hand hand)
        {
            return string.Join(" ", hand.Select(c => c.StdNotation));
        }
    }

    public class TestPlayer : PlayerBase
    {
        public TestPlayer(int seat, int bid = BidBase.NoBid, string handString = "")
        {
            Bid = bid;
            BidHistory = new List<int>();
            CardsTaken = string.Empty;
            Folded = false;
            GameScore = 0;
            GoodSuit = Suit.Unknown;
            Hand = handString;
            HandScore = 0;
            PlayedCards = new List<PlayedCard>();
            Seat = seat;
            VoidSuits = ComputeVoidSuits(handString);
        }

        private static List<Suit> ComputeVoidSuits(string handString)
        {
            var hand = new Hand(handString);
            return SuitRank.stdSuits.Where(suit => hand.All(c => c.suit != suit)).ToList();
        }
    }

    public class TestCardState<T> : SuggestCardState<T> where T : IGameOptions, new()
    {
        public TestCardState(IReadOnlyList<TestPlayer> players, string trickString = "")
        {
            options = new T();
            player = players[0];
            trumpSuit = options.GetType().Name == "SpadesOptions" ? Suit.Spades : Suit.Unknown;

            cardsPlayed = new Hand(string.Join("", players.Select(p => p.CardsTaken)));
            isPartnerTakingTrick = false;
            this.players = players;
            trick = new Hand(trickString);

            if (trick.Count > 0)
            {
                var ledSuit = trick.First().suit;
                var highRank = trick.Where(c => c.suit == ledSuit).Max(c => c.rank);
                var takingCardIndex = new List<Card>(trick).FindIndex(c => c.suit == ledSuit && c.rank == highRank);

                cardTakingTrick = new Card(ledSuit, highRank);
                trickTaker = players[1 + takingCardIndex];
                isPartnerTakingTrick = takingCardIndex == 1;

                legalCards = new Hand(new Hand(players[0].Hand).Where(c => c.suit == ledSuit));
                if (legalCards.Count == 0)
                    legalCards = new Hand(players[0].Hand);
            }
            else
            {
                cardTakingTrick = null;
                isPartnerTakingTrick = false;
                legalCards = new Hand(players[0].Hand);
                trickTaker = null;
            }
        }
    }
}