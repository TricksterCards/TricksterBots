using System.Web.Http;
using Trickster.cloud;

namespace Trickster.Bots.Controllers
{
    public class SpadesController : ApiController
    {
        [HttpPost]
        [Route("suggest/spades/bid")]
        public string SuggestSpadesBid([FromBody] string postData)
        {
            return Suggester.SuggestBid<SpadesOptions>(postData, state => new SpadesBot(state.options, Suit.Spades));
        }

        [HttpPost]
        [Route("suggest/spades/card")]
        public string SuggestSpadesCard([FromBody] string postData)
        {
            return Suggester.SuggestNextCard<SpadesOptions>(postData, state => new SpadesBot(state.options, Suit.Spades));
        }
        
        [HttpPost]
        [Route("suggest/spades/pass")]
        public string SuggestSpadesPass([FromBody] string postData)
        {
            return Suggester.SuggestPass<SpadesOptions>(postData, state => new SpadesBot(state.options, Suit.Spades));
        }
    }
}