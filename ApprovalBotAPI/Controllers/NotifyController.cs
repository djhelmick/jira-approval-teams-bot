using ApprovalBotAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ReferencesDbLibrary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ApprovalBotAPI.Controllers
{
    [ApiController]
    [Route("api/notify")]
    public class NotifyController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IConfiguration _configuration;
        private readonly string _appId;
        private readonly IDatabaseData _referencesDb;

        public NotifyController(IBotFrameworkHttpAdapter adapter, IConfiguration configuration, IDatabaseData referencesDb)
        {
            _adapter = adapter;
            _configuration = configuration;
            _referencesDb = referencesDb;
            _appId = configuration["MicrosoftAppId"] ?? string.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string email)
        {
            var conversationReference = await _referencesDb.GetConversationReference(email);
            await ((ApprovalBotAdapter)_adapter).ContinueConversationAsync(_appId, conversationReference, async (
                ITurnContext turnContext,
                CancellationToken cancellationToken) =>
            {
                await turnContext.SendActivityAsync($"Hi! This is a proactive notification sent at {DateTime.Now}.");
            }, default(CancellationToken));

            // Let the caller know proactive messages have been sent
            return new ContentResult()
            {
                Content = "<html><body><h1>Proactive messages have been sent.</h1></body></html>",
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
            };
        }

        [HttpPost]
        public async Task<IActionResult> Post(NotifyRequest notifyRequest)
        {
            dynamic issue = JsonConvert.DeserializeObject<ExpandoObject>(notifyRequest.Issue.ToString(), new ExpandoObjectConverter());

            string issueKey = issue.key;
            string reporter = issue.fields.reporter.displayName;
            string summary = issue.fields.summary;
            string description = issue.fields.description;

            string approverDisplayName = issue.fields.customfield_10003[0].displayName;
            string approverEmail = issue.fields.customfield_10003[0].emailAddress;
            string approverAccountId = issue.fields.customfield_10003[0].accountId;

            var card = new HeroCard
            {
                Title = $"{issueKey}: {summary}",
                Subtitle = $"A new request from {reporter} requires your approval.",
                Images = new List<CardImage> { new CardImage("https://pbs.twimg.com/profile_images/907268759500607489/ZUu4kQCr_400x400.jpg") },
                Text =  (description.Length > 255 ? description.Substring(0, 255).Trim() + "..." : description),
                Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.MessageBack,
                        title: "Approve",
                        displayText: $"Approve {issueKey}",
                        text: $"Approve {issueKey}",
                        value: $"{{\"issueKey\": \"{issueKey}\", \"decision\": \"approve\", \"accountId\": \"{approverAccountId}\" }}"),
                        new CardAction(ActionTypes.MessageBack,
                        title: "Deny",
                        displayText: $"Deny {issueKey}",
                        text: $"Deny {issueKey}",
                        value: $"{{\"issueKey\": \"{issueKey}\", \"decision\": \"deny\", \"accountId\": \"{approverAccountId}\" }}"),
                        new CardAction(ActionTypes.OpenUrl, 
                        title: "View Request",
                        value: $"{_configuration.GetSection("JiraClient")["JiraBaseUrl"]}/servicedesk/customer/portal/2/{issueKey}")
                    }
            };
            var reply = MessageFactory.Attachment(card.ToAttachment());

            var conversationReference = await _referencesDb.GetConversationReference(approverEmail);

            await ((ApprovalBotAdapter)_adapter).ContinueConversationAsync(_appId, conversationReference, async (
                ITurnContext turnContext,
                CancellationToken cancellationToken) =>
            {
                await turnContext.SendActivityAsync(reply, cancellationToken);
            }, default(CancellationToken));

            // Let the caller know proactive messages have been sent
            return new ContentResult()
            {
                Content = "<html><body><h1>Proactive messages have been sent.</h1></body></html>",
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
            };
        }
    }
}
