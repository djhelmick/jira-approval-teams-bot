using JiraClientLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net.Http;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Collections;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace JiraClientLibrary
{
    public enum Decision
    {
        Approve,
        Deny
    }

    public class JiraClient
    {
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly string _secret;
        private readonly string _clientId;

        public JiraClient(IConfiguration configuration)
        {
            _configuration = configuration;
            _baseUrl = _configuration.GetSection("JiraClient")["JiraBaseUrl"];
            _secret = _configuration.GetSection("JiraClient")["JiraSecret"];
            _clientId = _configuration.GetSection("JiraClient")["JiraClientId"];
        }

        public async Task<bool> SubmitDecision(string issue,
                                   string accountId,
                                   string bearerToken,
                                   Decision decisionToSubmit)
        {
            var approval = await GetApproval(issue, bearerToken);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            var json = (decisionToSubmit == Decision.Approve ? "{ \"decision\": \"approve\" }" : "{ \"decision\": \"decline\" }");
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync($"{_baseUrl}/rest/servicedeskapi/request/{approval.IssueKey}/approval/{approval.ApprovalId}", content);
            string responseString = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine(responseString + "\n");
            dynamic body = JObject.Parse(responseString);

            return true;
        }

        public async Task<Approval> GetApproval(string issue, string bearerToken)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            var response = await client.GetAsync($"{_baseUrl}/rest/servicedeskapi/request/{issue}/approval");
            string responseString = await response.Content.ReadAsStringAsync();
            dynamic body = JObject.Parse(responseString);

            string approvalId = body.values[0].id;
            var approvers = body.values[0].approvers;
            var approver = approvers[0].approver;

            return new Approval
            {
                IssueKey = issue,
                ApprovalId = approvalId,
                AccountId = approver.accountId,
                Email = approver.emailAddress
            };
        }

        public async Task<string> GetUserBearerToken(string accountId)
        {
            string jwtToken = GenerateJwtToken(accountId);
            var client = new HttpClient();

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                { "assertion", jwtToken },
                { "scope", "READ WRITE" }
            });

            var response = await client.PostAsync("https://oauth-2-authorization-server.services.atlassian.com/oauth2/token", content);

            var responseString = await response.Content.ReadAsStringAsync();
            dynamic responseBody = JObject.Parse(responseString);
            string bearerToken = responseBody.access_token;

            return bearerToken;
        }

        public string GenerateJwtToken(string accountId)
        {
            var mySecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_secret));
            var issuedAt = DateTimeOffset.UtcNow;

            var token = new JwtSecurityToken(
                    issuer: $"urn:atlassian:connect:clientid:{_clientId}",
                    audience: "https://oauth-2-authorization-server.services.atlassian.com",
                    claims: new Claim[]
                    {
                        new Claim("sub", $"urn:atlassian:connect:useraccountid:{accountId}")
                    },
                    expires: issuedAt.AddSeconds(60).UtcDateTime,
                    signingCredentials: new SigningCredentials(mySecurityKey, "HS256")
                );
            token.Payload["tnt"] = _baseUrl;
            token.Payload["iat"] = issuedAt.ToUnixTimeSeconds();

            var tokenHandler = new JwtSecurityTokenHandler();
            var writtenToken = tokenHandler.WriteToken(token);

            return writtenToken;
        }
    }
}
