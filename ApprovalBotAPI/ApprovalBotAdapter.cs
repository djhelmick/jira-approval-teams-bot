using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApprovalBotAPI
{
    public class ApprovalBotAdapter : BotFrameworkHttpAdapter
    {
        public ApprovalBotAdapter(IConfiguration configuration, ILogger<ApprovalBotAdapter> logger)
            : base(configuration, logger)
        {

        }
    }
}
