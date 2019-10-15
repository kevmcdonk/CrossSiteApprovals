using Microsoft.SharePoint.Client;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using SharePointPnP.IdentityModel;
using SharePoint.WebHooks.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeDevPnP.Core;
using Microsoft.Azure.WebJobs.Host;

namespace SharePoint.WebHooks.Common
{
    /// <summary>
    /// Helper class that deals with asynchronous and synchronous SharePoint list web hook events processing
    /// </summary>
    public class ChangeManager
    {
        #region Constants and variables
        private string accessToken = null;
        #endregion



        #region Synchronous processing of a web hook notification
        /// <summary>
        /// Processes a received notification. This typically is triggered via an Azure Web Job that reads the Azure storage queue
        /// </summary>
        /// <param name="notification">Notification to process</param>
        public async Task ProcessNotification(NotificationModel notification, TraceWriter log)
        {
            ClientContext cc = null;
            ClientContext acc = null;
            string notificationStep = "1";
            try
            {
                #region Setup an app-only client context
                AuthenticationManager am = new AuthenticationManager();
                notificationStep = "1a";
                string url = String.Format("https://{0}{1}", System.Environment.GetEnvironmentVariable("TenantName"), notification.SiteUrl);
                notificationStep = "1b" + url;
                string realm = TokenHelper.GetRealmFromTargetUrl(new Uri(url));

                string approvalUrl = System.Environment.GetEnvironmentVariable("ApprovalSiteUrl");
                log.Info("ApprovalSiteUrl: " + approvalUrl);
                notificationStep = "1b" + url;
                string approvalRealm = TokenHelper.GetRealmFromTargetUrl(new Uri(approvalUrl));
                notificationStep = "1c";
                string clientId = System.Environment.GetEnvironmentVariable("ClientId");
                notificationStep = "1d";
                string clientSecret = System.Environment.GetEnvironmentVariable("ClientSecret");

                notificationStep = "2";
                if (new Uri(url).DnsSafeHost.Contains("spoppe.com"))
                {
                    log.Info("Government cloud login (I think)");
                    cc = am.GetAppOnlyAuthenticatedContext(url, realm, clientId, clientSecret, acsHostUrl: "windows-ppe.net", globalEndPointPrefix: "login");
                    acc = am.GetAppOnlyAuthenticatedContext(approvalUrl, approvalRealm, clientId, clientSecret, acsHostUrl: "windows-ppe.net", globalEndPointPrefix: "login");
                    log.Info("Government cloud login (I think) token received");
                }
                else
                {
                    log.Info("App only login");
                    notificationStep = $"2";
                    cc = am.GetAppOnlyAuthenticatedContext(url, clientId, clientSecret);
                    acc = am.GetAppOnlyAuthenticatedContext(approvalUrl, clientId, clientSecret);
                    log.Info("App only login token received");
                }
                notificationStep = "3";
                cc.ExecutingWebRequest += Cc_ExecutingWebRequest;
                #endregion
                notificationStep = "3a";
                #region Grab the list for which the web hook was triggered
                ListCollection lists = cc.Web.Lists;
                notificationStep = "3b";
                Guid listId = new Guid(notification.Resource);
                notificationStep = "3c";
                log.Info("Loaded source list");
                IEnumerable<List> results = cc.LoadQuery<List>(lists.Where(lst => lst.Id == listId));
                notificationStep = $"3d-{listId.ToString()},{notification.Resource.ToString()}, {notification.SiteUrl}";
                cc.ExecuteQueryRetry();
                log.Info("Loaded source list loaded");
                notificationStep = "3e";
                List notificationSourceList = results.FirstOrDefault();
                if (notificationSourceList == null)
                {
                    // list has been deleted inbetween the event being fired and the event being processed
                    return;
                }
                notificationStep = "4";
                #endregion

                #region Grab the list used to write the web hook history
                // Ensure reference to the history list, create when not available
                List approvalsList = acc.Web.GetListByTitle("Approvals");

                if (approvalsList == null)
                {
                    log.Info("Creating approvals list as not in place");
                    approvalsList = acc.Web.CreateList(ListTemplateType.GenericList, "Approvals", false);
                    this.AddTextField(approvalsList, "ClientState", "ClientState", cc);
                    this.AddTextField(approvalsList, "SubscriptionId", "SubscriptionId", cc);
                    this.AddTextField(approvalsList, "ExpirationDateTime", "ExpirationDateTime", cc);
                    this.AddTextField(approvalsList, "Resource", "Resource", cc);
                    this.AddTextField(approvalsList, "TenantId", "TenantId", cc);
                    this.AddTextField(approvalsList, "SiteUrl", "SiteUrl", cc);
                    this.AddTextField(approvalsList, "WebId", "WebId", cc);
                    this.AddTextField(approvalsList, "ItemId", "ItemId", cc);
                    this.AddTextField(approvalsList, "ActivityId", "Activity Id", cc);
                    this.AddTextField(approvalsList, "EditorEmail", "EditorEmail", cc);
                    this.AddTextField(approvalsList, "Activity", "Activity", cc);
                    approvalsList.Update();
                    acc.ExecuteQuery();
                }
                notificationStep = "5";
                #endregion

                #region Grab the list changes and do something with them
                // grab the changes to the provided list using the GetChanges method 
                // on the list. Only request Item changes as that's what's supported via
                // the list web hooks
                ChangeQuery changeQuery = new ChangeQuery(false, true);
                changeQuery.Item = true;
                changeQuery.RecursiveAll = true;
                changeQuery.User = true;
                changeQuery.FetchLimit = 1000; // Max value is 2000, default = 1000
                notificationStep = "6";
                ChangeToken lastChangeToken = null;
                Guid id = new Guid(notification.SubscriptionId);

                string storageConnectionString = System.Environment.GetEnvironmentVariable("StorageConnectionString");
                const string tableName = "crosssiteappchangetokens";

                // Connect to storage account / container
                var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(storageConnectionString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference(tableName);
                notificationStep = "7";
                await table.CreateIfNotExistsAsync();
                TableResult result = await table.ExecuteAsync(TableOperation.Retrieve<TableChangeToken>("List", id.ToString()));
                TableChangeToken loadedChangeToken = null;
                if (result.Result != null)
                {
                    lastChangeToken = new ChangeToken();
                    loadedChangeToken = (result.Result as TableChangeToken);
                    lastChangeToken.StringValue = loadedChangeToken.StringValue;
                }
                notificationStep = "8";
                // Start pulling down the changes
                bool allChangesRead = false;
                do
                {
                    // should not occur anymore now that we record the starting change token at 
                    // subscription creation time, but it's a safety net
                    if (lastChangeToken == null)
                    {
                        lastChangeToken = new ChangeToken();
                        // See https://blogs.technet.microsoft.com/stefan_gossner/2009/12/04/content-deployment-the-complete-guide-part-7-change-token-basics/
                        lastChangeToken.StringValue = string.Format("1;3;{0};{1};-1", notification.Resource, DateTime.Now.AddMinutes(-5).ToUniversalTime().Ticks.ToString());
                    }

                    // Assign the change token to the query...this determines from what point in
                    // time we'll receive changes
                    changeQuery.ChangeTokenStart = lastChangeToken;

                    // Execute the change query
                    var changes = notificationSourceList.GetChanges(changeQuery);
                    cc.Load(changes);
                    cc.ExecuteQueryRetry();
                    notificationStep = "9";
                    if (changes.Count > 0)
                    {
                        foreach (Change change in changes)
                        {
                            lastChangeToken = change.ChangeToken;

                            if (change is ChangeItem)
                            {
                                // do "work" with the found change
                                DoWork(acc, approvalsList, cc, notificationSourceList, change, notification, log);
                            }
                        }

                        // We potentially can have a lot of changes so be prepared to repeat the 
                        // change query in batches of 'FetchLimit' untill we've received all changes
                        if (changes.Count < changeQuery.FetchLimit)
                        {
                            allChangesRead = true;
                        }
                    }
                    else
                    {
                        allChangesRead = true;
                    }
                    // Are we done?
                } while (allChangesRead == false);

                // Persist the last used changetoken as we'll start from that one
                // when the next event hits our service
                if (loadedChangeToken != null)
                {
                    // Only persist when there's a change in the change token
                    if (!loadedChangeToken.StringValue.Equals(lastChangeToken.StringValue, StringComparison.InvariantCultureIgnoreCase))
                    {
                        loadedChangeToken.StringValue = lastChangeToken.StringValue;
                        await table.ExecuteAsync(TableOperation.InsertOrReplace(loadedChangeToken));
                        notificationStep = "10";
                    }
                }
                else
                {
                    // should not occur anymore now that we record the starting change token at 
                    // subscription creation time, but it's a safety net
                    var newToken = new TableChangeToken()
                    {
                        PartitionKey = "List",
                        RowKey = id.ToString(),
                        StringValue = lastChangeToken.StringValue
                    };
                    await table.ExecuteAsync(TableOperation.InsertOrReplace(newToken));
                    notificationStep = "11";
                }

                #endregion

                #region "Update" the web hook expiration date when needed
                // Optionally add logic to "update" the expirationdatetime of the web hook
                // If the web hook is about to expire within the coming 5 days then prolong it
                if (notification.ExpirationDateTime.AddDays(-5) < DateTime.Now)
                {
                    WebHookManager webHookManager = new WebHookManager();
                    Task<bool> updateResult = Task.WhenAny(
                        webHookManager.UpdateListWebHookAsync(
                            url,
                            listId.ToString(),
                            notification.SubscriptionId,
                            System.Environment.GetEnvironmentVariable("WebHookEndPoint"),
                            DateTime.Now.AddMonths(3),
                            this.accessToken)
                        ).Result;
                    notificationStep = "12";
                    if (updateResult.Result == false)
                    {
                        throw new Exception(String.Format("The expiration date of web hook {0} with endpoint {1} could not be updated", notification.SubscriptionId, System.Environment.GetEnvironmentVariable("WebHookEndPoint")));
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                // Log error
                //Console.WriteLine(ex.ToString());
                throw new Exception("Step " + notificationStep, ex);
            }
            finally
            {
                if (cc != null)
                {
                    cc.Dispose();
                }
            }
        }

        /// <summary>
        /// Method doing actually something with the changes obtained via the web hook notification. 
        /// In this demo we're just logging to a list, in your implementation you do what you need to do :-)
        /// </summary>
        private static void DoWork(ClientContext approvalCC, List approvalList, ClientContext notificationCC, List notificationSourceList, Change change, NotificationModel notification, TraceWriter log)
        {
            log.Info("Loading source item with id: " + ((ChangeItem)change).ItemId);
            log.Info("Notification Source List name:" + notificationSourceList.Title + " " + notificationSourceList.ParentWebUrl);
            ListItem li = notificationSourceList.GetItemById(((ChangeItem)change).ItemId);
            notificationCC.Load(li);
            notificationCC.ExecuteQuery();
            log.Info("Loaded source item with ID: " + ((ChangeItem)change).ItemId);
            // Only add approval item if in PEnding approval status
            if (li.FieldValues["_ModerationStatus"].ToString() == "2")
            {

                var changeItem = change as ChangeItem;

                CamlQuery camlQuery = new CamlQuery();
                camlQuery.ViewXml = "<View><Query><Where><And><And>";
                camlQuery.ViewXml += $"<Eq><FieldRef Name='ClientState' /><Value Type='Text'>{notification.ClientState}</Value></Eq>";
                camlQuery.ViewXml += $"<Eq><FieldRef Name='Resource' /><Value Type='Text'>{notification.Resource}</Value></Eq></And>";
                camlQuery.ViewXml += $"<And><Eq><FieldRef Name='ItemId' /><Value Type='Text'>{changeItem.ItemId}</Value></Eq>";
                camlQuery.ViewXml += $"<Eq><FieldRef Name='ActivityId' /><Value Type='Text'>{changeItem.UniqueId}</Value></Eq>";
                camlQuery.ViewXml += $"</And></And></Where></Query></View>";

                ListItemCollection matchingItems = approvalList.GetItems(camlQuery);
                approvalCC.Load(matchingItems);
                approvalCC.ExecuteQuery();

                if (matchingItems.Count() == 0)
                {
                    ListItemCreationInformation newItem = new ListItemCreationInformation();
                    ListItem item = approvalList.AddItem(newItem);
                    var editor = li.FieldValues["Editor"] as FieldUserValue;

                    item["Title"] = string.Format("List {0} had a Change of type \"{1}\" on the item with Id {2}.", notificationSourceList.Title, change.ChangeType.ToString(), (change as ChangeItem).ItemId);
                    item["ClientState"] = notification.ClientState;
                    item["SubscriptionId"] = notification.SubscriptionId;
                    item["ExpirationDateTime"] = notification.ExpirationDateTime;
                    item["Resource"] = notification.Resource;
                    item["TenantId"] = notification.TenantId;
                    item["SiteUrl"] = notification.SiteUrl;
                    item["WebId"] = notification.WebId;
                    item["ItemId"] = changeItem.ItemId;
                    item["ActivityId"] = changeItem.UniqueId;
                    item["EditorEmail"] = editor.Email;
                    item["Activity"] = change.ChangeType.ToString();
                    item.Update();
                    approvalCC.ExecuteQueryRetry();
                }
            }
        }

        private void AddTextField(List list, string displayName, string fieldName, ClientContext context)
        {
            Field field = list.Fields.AddFieldAsXml($"<Field DisplayName='{displayName}' Name='{fieldName}' Title='{fieldName}' Type='Text' />",
                                           true,
                                           AddFieldOptions.DefaultValue);
            FieldNumber fldNumber = context.CastTo<FieldNumber>(field);
            fldNumber.Update();
        }

        private void Cc_ExecutingWebRequest(object sender, WebRequestEventArgs e)
        {
            // Capture the OAuth access token since we want to reuse that one in our REST requests
            this.accessToken = e.WebRequestExecutor.RequestHeaders.Get("Authorization").Replace("Bearer ", "");
        }
        #endregion
    }
}
