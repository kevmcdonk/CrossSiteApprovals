using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using SharePoint.WebHooks.Common;
using SharePoint.WebHooks.Common.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AzureFunctionV1
{
    public static class CrossSiteApprovalsWebhook
    {
        [FunctionName("CrossSiteApprovalsWebhook")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            
            log.Info("Notification from webhook received");
            try
            {
                log.Info("Storage connection string:" + System.Environment.GetEnvironmentVariable("StorageConnectionString"));
                string validationToken = req.GetQueryNameValuePairs()
                    .FirstOrDefault(q => string.Compare(q.Key, "validationtoken", true) == 0)
                    .Value;

                // If a validation token is present, we need to respond within 5 seconds by  
                // returning the given validation token. This only happens when a new 
                // web hook is being added
                if (validationToken != null)
                {
                    log.Info($"Validation token {validationToken} received");
                    var response = req.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(validationToken);
                    return response;
                }
                // Get notification from body
                var notificationText = await req.Content.ReadAsStringAsync();
                log.Info("Webhook text: " + notificationText);
                NotificationCollection notifications = JsonConvert.DeserializeObject<NotificationCollection>(notificationText);
                var notification = notifications.value[0] as NotificationModel;
                //NotificationModel notification = await req.Content.ReadAsAsync<NotificationModel>();

                ChangeManager changeManager = new ChangeManager();
                log.Info("Url: " + notification.SiteUrl);
                await changeManager.ProcessNotification(notification, log);

                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch(System.Exception exp)
            {
                log.Error("Unable to complete function: " + exp.Message + ":::" + exp.StackTrace);
                throw exp;
            }
        }
    }
}
