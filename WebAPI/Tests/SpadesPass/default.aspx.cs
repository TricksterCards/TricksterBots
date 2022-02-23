using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Trickster.cloud;

namespace Trickster.Bots.Tests.SpadesPass
{
    public partial class _default : Page
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { IncludeFields = true };

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

                var stateJson = JsonSerializer.Serialize(passState, _jsonSerializerOptions);
                var prefix = Request.Url.GetLeftPart(UriPartial.Authority);

                using (var wc = new WebClient())
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/json";
                    var resultJson = wc.UploadString($"{prefix}/suggest/spades/pass", JsonSerializer.Serialize(stateJson));

                    var thePass = JsonSerializer.Deserialize<List<Card>>(JsonSerializer.Deserialize<string>(resultJson) ?? "[]");

                    insertHere.Controls.Add(new HtmlGenericControl("p")
                    {
                        InnerText = $"Cards to be passed with bid {bid}: {string.Join(", ", thePass?.Select(c => c.ToString()) ?? new[] { "none" })}"
                    });
                }
            }
        }
    }
}