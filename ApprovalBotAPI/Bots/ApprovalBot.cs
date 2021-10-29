using JiraClientLibrary;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using ReferencesDbLibrary;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApprovalBotAPI.Bots
{
    public class ApprovalBot : TeamsActivityHandler
    {
        private readonly IDatabaseData _referencesDb;
        private readonly JiraClient _jiraClient;

        public ApprovalBot(IDatabaseData referencesDb, JiraClient jiraClient)
        {
            _referencesDb = referencesDb;
            _jiraClient = jiraClient;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Display typing dots while processing message
            await turnContext.SendActivityAsync(new Activity { Type = ActivityTypes.Typing }, cancellationToken);

            var conv = turnContext.Activity.GetConversationReference();
            string receivedText = turnContext.Activity.Text?.ToLower().Trim();

            if (receivedText?.Split(" ")[0] == "approve")
            {
                JObject value =  (JObject)turnContext.Activity.Value;

                string issueKey = (string)value["issueKey"];
                string accountId = (string)value["accountId"];
                string bearerToken = await _jiraClient.GetUserBearerToken(accountId);

                bool result = await _jiraClient.SubmitDecision(issueKey, accountId, bearerToken, Decision.Approve);

                await turnContext.SendActivityAsync($"{issueKey} has been approved. Thank you!");
            }
            else if (receivedText?.Split(" ")[0] == "deny")
            {
                JObject value = (JObject)turnContext.Activity.Value;

                string issueKey = (string)value["issueKey"];
                string accountId = (string)value["accountId"];
                string bearerToken = await _jiraClient.GetUserBearerToken(accountId);

                bool result = await _jiraClient.SubmitDecision(issueKey, accountId, bearerToken, Decision.Deny);

                await turnContext.SendActivityAsync($"{issueKey} has been denied. Thank you!");
            }
            else
            {
                await turnContext.SendActivityAsync("I'm sorry, but I don't understand that command.");
            }
        }

        protected override async Task OnInstallationUpdateAddAsync(ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var conversationReference = turnContext.Activity.GetConversationReference();
            var member = await TeamsInfo.GetMemberAsync(turnContext, conversationReference.User.Id, cancellationToken);

            await _referencesDb.AddConversationReference(member.Email, conversationReference);
        }
    }
}
