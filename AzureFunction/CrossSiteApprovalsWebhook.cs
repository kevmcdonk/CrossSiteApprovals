using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace cps.crosssiteapprovals.function
{
    public class ActivityResponseValue
    {
        string id { get; set; }
    }

    public class ActivityResponse
    {
        ActivityResponseValue[] value { get; set; }
    }

    public static class CrossSiteApprovalsWebhook
    {
        [FunctionName("CrossSiteApprovalsWebhook")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null),
            /*Token(
                
                Identity = TokenIdentityMode.ClientCredentials,
                IdentityProvider = "AAD",
                Resource = "https://graph.microsoft.com"
                or
                 UserId = "Alan.eardley@cpsdemose5.onmicrosoft.com", 
                IdentityProvider = "AAD",
                Identity = TokenIdentityMode.UserFromId
            )*/
            ] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Webhook triggered.");

            // Grab the validationToken URL parameter
            string validationToken = req.Query["validationtoken"];

            // If a validation token is present, we need to respond within 5 seconds by  
            // returning the given validation token. This only happens when a new 
            // web hook is being added
            if (validationToken != null)
            {
                log.LogInformation($"Validation token {validationToken} received");
                return (ActionResult)new OkObjectResult(validationToken);
            }

            var tenantId = Environment.GetEnvironmentVariable("TenantId");
            var clientId = Environment.GetEnvironmentVariable("WEBSITE_AUTH_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("WEBSITE_AUTH_CLIENT_SECRET");

            // Configure app builder
            var authority = $"https://login.microsoftonline.com/{tenantId}";
            var app = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri(authority))
                .Build();

            // Acquire tokens for Graph API
            var scopes = new[] { "https://graph.microsoft.com/.default" };
            var authenticationResult = await app.AcquireTokenForClient(scopes).ExecuteAsync();

            // Create GraphClient and attach auth header to all request (acquired on previous step)
            var graphClient = new GraphServiceClient(
                "https://graph.microsoft.com/beta",
                new DelegateAuthenticationProvider(requestMessage =>
                {
                    requestMessage.Headers.Authorization =
                        new AuthenticationHeaderValue("bearer", authenticationResult.AccessToken);

                    return Task.FromResult(0);
                }));

            // Call Graph API
            //var listItem = new ListItem();
            //listItem.Fields.AdditionalData.Add("Title","Bob");
            var client = new HttpClient();
            client.BaseAddress = new Uri("https://graph.microsoft.com");
            var request = new HttpRequestMessage(HttpMethod.Get, "/beta/sites/cpsdemose5.sharepoint.com,bb2d762e-fe41-4adb-80f8-a94503df98f1,d0c0c59a-b834-48f4-aa00-4e23e5b18b47/drive/activities?$top=1");
            // var response = client.SendAsync(request).Result;
            //return response.Content.ReadAsStringAsync().Result;

            var defaultRequetHeaders = client.DefaultRequestHeaders;
            if (defaultRequetHeaders.Accept == null || !defaultRequetHeaders.Accept.Any(m => m.MediaType == "application/json"))
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            defaultRequetHeaders.Authorization = new AuthenticationHeaderValue("bearer", authenticationResult.AccessToken);

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                ActivityResponse activitiesResponse = JsonConvert.DeserializeObject(json) as ActivityResponse;
                JObject aResp = JsonConvert.DeserializeObject(json) as JObject;
                string id = aResp["value"][0]["id"].ToString();
                string editor = aResp["value"][0]["actor"]["user"]["email"].ToString();
                JToken actionToken = aResp["value"][0]["action"].First();
                string action = actionToken.ToObject<JProperty>().Name;
                //ActivityResponseValue activity = activitiesResponse["value"] as ActivityResponseValue;

                Dictionary<string, object> props = new Dictionary<string, object>();

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                JObject data = JsonConvert.DeserializeObject(requestBody) as JObject;

                var sourceListId = data["value"][0]["resource"].ToString();
                var sourceSiteUrl = data["value"][0]["SiteUrl"].ToString();
                if (sourceSiteUrl.StartsWith("/")) {
                    sourceSiteUrl = sourceSiteUrl.Substring(1,sourceSiteUrl.Length -1);
                }

                // populate properties, all of these work just fine
                props.Add("Title", data["value"][0]["clientState"].ToString());
                props.Add("SubscriptionId", data["value"][0]["subscriptionId"].ToString());
                props.Add("ExpirationDateTime", data["value"][0]["expirationDateTime"].ToString()); //TODO: Make DateTime field
                props.Add("Resource", sourceListId);
                props.Add("TenantId", data["value"][0]["tenantId"].ToString());
                props.Add("SiteUrl", sourceSiteUrl);
                props.Add("WebId", data["value"][0]["webId"].ToString());
                props.Add("ActivityId", id);
                props.Add("EditorEmail", editor);
                props.Add("Activity", action);

                // create list item with our properties dictionary
                var newItem = new ListItem
                {
                    Name = data["value"][0]["clientState"].ToString(),
                    Fields = new FieldValueSet()
                    {
                        AdditionalData = props
                    }
                };

                
                var user = await graphClient
                    .Sites.GetByPath("teams/CrossSiteApprovalsDemo", "cpsdemose5.sharepoint.com")
                    .Lists[data["value"][0]["resource"].ToString()]
                    .Items
                    .Request().AddAsync(newItem);

                log.LogInformation("User DisplayName: " + user);
            }
            return (ActionResult)new OkObjectResult("");
        }
    }
}
