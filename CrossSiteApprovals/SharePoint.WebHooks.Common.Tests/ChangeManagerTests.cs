using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharePoint.WebHooks.Common.Models;

namespace SharePoint.WebHooks.Common.Tests
{
    [TestClass]
    public class ChangeManagerTests
    {
        [TestInitialize]
        public void Setup()
        {
            foreach(var appSetting in ConfigurationManager.AppSettings.AllKeys)
            {
                Environment.SetEnvironmentVariable(appSetting, ConfigurationManager.AppSettings[appSetting]);
            }
        }

        [TestMethod]
        public async void ProcessNotificationTest()
        {
            NotificationModel notification = new NotificationModel()
            {
                ClientState = Guid.NewGuid().ToString(),
                ExpirationDateTime = DateTime.Now.AddMonths(6),
                Resource = "477db145-8939-4684-9ec4-8988ffb0d2b8",
                SiteUrl = "/teams/CrossSiteApprovalsDemo",
                SubscriptionId = "ac5f9901-9231-4d8b-ba6f-e3cae3856b30",
                TenantId = "3aad59e2-dbed-4fc0-9780-91156f9e0bc4",
                WebId = "d0c0c59a-b834-48f4-aa00-4e23e5b18b47"
            };
            
            ChangeManager changeManager = new ChangeManager();
            await changeManager.ProcessNotification(notification, null);
        }
    }
}
