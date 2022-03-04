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
}