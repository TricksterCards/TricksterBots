using System.Collections.Generic;
using System.Linq;
using Trickster.Bots;
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
            return new List<Suit>();
            //var hand = new Hand(handString);
            //return SuitRank.stdSuits.Where(suit => hand.All(c => c.suit != suit)).ToList();
        }
    }

    public class TestCardState<T> : SuggestCardState<T> where T : IGameOptions, new()
    {
        public TestCardState(IReadOnlyList<TestPlayer> players, string trickString = "", string legalCards = "")
        {
            options = new T();
            player = players[0];
            trumpSuit = options.GetType().Name == "SpadesOptions" ? Suit.Spades : Suit.Unknown;

            cardsPlayed = new Hand(string.Join("", players.Select(p => p.CardsTaken)));
            cardTakingTrick = null;
            isPartnerTakingTrick = false;
            this.legalCards = new Hand(string.IsNullOrEmpty(legalCards) ? players[0].Hand : legalCards);
            this.players = players;
            trick = new Hand(trickString);
            trickTaker = null;
        }
    }
}