using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Trickster.Bots.Controllers
{
    public class TestFileController : ApiController
    {
        [HttpGet]
        [Route("test/{filename}")]
        public string TestFile(string filename)
        {
            return Suggester.SuggestFromStateFile(filename);
        }
        
    }
}
