using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.SharePoint.Client;

namespace SharePoint.WebHooks.Common.Models
{
    public class TableChangeToken : TableEntity
    {
        public string StringValue { get; set; }
     }
}
