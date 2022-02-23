using System;
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
            var passState = new SuggestPassState<SpadesOptions>
            {
                options = new SpadesOptions { nilPass = 4 },
                player = new PlayerBase { Hand = "ASKSQSJSTD9D8D7D6H5H4C3C2C", Bid = 0 },
                trumpSuit = Suit.Spades,
                passCount = 4
            };

            using (var wc = new WebClient())
            {
                wc.Headers[HttpRequestHeader.ContentType] = "application/json";
                var resultJson = wc.UploadString("suggest/spades/pass", JsonSerializer.Serialize(passState, _jsonSerializerOptions));
                insertHere.Controls.Add(new HtmlGenericControl("p") { InnerText = resultJson });
            }
        }
    }
}