using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePoint.WebHooks.Common.Models
{

    public class NotificationCollection
    {
        public NotificationModel[] value { get; set; }
    }

    public class Value
    {
        public string subscriptionId { get; set; }
        public string clientState { get; set; }
        public DateTime expirationDateTime { get; set; }
        public string resource { get; set; }
        public string tenantId { get; set; }
        public string siteUrl { get; set; }
        public string webId { get; set; }
    }

}
