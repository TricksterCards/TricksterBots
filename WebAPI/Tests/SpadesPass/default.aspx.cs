using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Trickster.cloud;

namespace Trickster.Bots.Tests.SpadesPass
{
    public partial class _default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            const string hand = "ASKSQSJSAD9D8D3DAH2HAC3C2C";

            for (var bid = 0; bid <= 1; ++bid)
            {
                var passState = new SuggestPassState<SpadesOptions>
                {
                    options = new SpadesOptions { nilPass = 4 },
                    player = new PlayerBase { Hand = hand, Bid = bid },
                    trumpSuit = Suit.Spades,
                    hand = new Hand(hand),
                    passCount = 4
                };

                var bot = new SpadesBot(passState.options, passState.trumpSuit);
                var cards = bot.SuggestPass(passState);

                insertHere.Controls.Add(new HtmlGenericControl("p")
                {
                    InnerText = $"Cards to be passed with bid {bid}: {string.Join(", ", cards?.Select(c => c.ToString()) ?? new[] { "none" })}"
                });
            }
        }
    }
}