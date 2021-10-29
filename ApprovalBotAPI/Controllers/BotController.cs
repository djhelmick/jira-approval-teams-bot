using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApprovalBotAPI.Controllers
{
    [ApiController]
    [Route("api/messages")]
    public class BotController : ControllerBase
    {
        private readonly IBot _bot;
        private readonly IBotFrameworkHttpAdapter _adapter;

        public BotController(IBot bot, IBotFrameworkHttpAdapter adapter)
        {
            _bot = bot;
            _adapter = adapter;
        }

        [HttpPost]
        public async Task ProcessAsyncTask()
        {
            await _adapter.ProcessAsync(Request, Response, _bot);
        }
    }
}
