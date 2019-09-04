using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace cps.crosssiteapprovals.function
{
    public static class CrossSiteApprovalsWebhook
    {
        [FunctionName("CrossSiteApprovalsWebhook")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
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
/*
            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;
*/
            return (ActionResult)new OkObjectResult($"Webhook set up");
        }
    }
}
