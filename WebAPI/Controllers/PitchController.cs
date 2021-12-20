using System.Web.Http;
using Trickster.cloud;

namespace Trickster.Bots.Controllers
{
    public class PitchController : ApiController
    {
        [HttpPost]
        [Route("suggest/pitch/bid")]
        public string SuggestPitchBid([FromBody] string postData)
        {
            return Suggester.SuggestBid<PitchOptions>(postData, state => new PitchBot(state.options, state.trumpSuit));
        }

        [HttpPost]
        [Route("suggest/pitch/card")]
        public string SuggestPitchCard([FromBody] string postData)
        {
            return Suggester.SuggestNextCard<PitchOptions>(postData, state => new PitchBot(state.options, state.trumpSuit));
        }

        [HttpPost]
        [Route("suggest/pitch/discard")]
        public string SuggestPitchDiscard([FromBody] string postData)
        {
            return Suggester.SuggestDiscard<PitchOptions>(postData, state => new PitchBot(state.options, state.trumpSuit));
        }
    }
}