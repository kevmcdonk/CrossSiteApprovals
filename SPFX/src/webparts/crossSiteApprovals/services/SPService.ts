import { IWebhook } from '../interfaces/IWebhook';
import { SPHttpClient, SPHttpClientResponse } from '@microsoft/sp-http';

export default class SPService  {

    public static add(feeling: string, spHttpClient: SPHttpClient, siteUrl: string, listName: string):void {
      
      spHttpClient.get(`${siteUrl}/_api/web/lists/getbytitle('${listName}')?select=id`, SPHttpClient.configurations.v1)
      .then((response: SPHttpClientResponse): Promise<{ ResponseItems: any }> => {
        return response.json();
      }, (error: any): void => {
        console.log('There was an error');
      })
      .then((response: any ): void => {
        var listId = response.Id;
        
        this.getEntityTypeName(spHttpClient, siteUrl, listName).then((listItemEntityTypeName: string): void => {
                  const body: string = JSON.stringify({
          "__metadata": {
            //'type': listItemEntityTypeName
            'type': 'SP.ListItem'
          },
          "resource": `${siteUrl}/_api/web/lists('${listId}')`,
          "notificationUrl": "https://143aec40.ngrok.io/api/CrossSiteApprovalsWebhook",
          "expirationDateTime": "2019-10-27T16:17:57+00:00",
          "clientState": "A0A354EC-97D4-4D83-9DDB-144077ADB449"
        });

        spHttpClient.post(`${siteUrl}/_api/web/lists('${listId}')/subscriptions`,
        SPHttpClient.configurations.v1,
        {
          headers: {
            'Accept': 'application/json;odata=nometadata',
            'Content-type': 'application/json;odata=verbose',
            'odata-version': ''
          },
          body: body
        });
      });
      });
  }

    public static getEntityTypeName(spHttpClient: SPHttpClient, siteUrl: string, listName: string): Promise<string> {
      return new Promise<string>((resolve: (listItemEntityTypeName: string) => void, reject: (error: any) => void): void => {
         let listItemEntityTypeName = "";
  
          spHttpClient.get(`${siteUrl}/_api/web/lists/getbytitle('${listName}')?$select=ListItemEntityTypeFullName`,
            SPHttpClient.configurations.v1,
            {
              headers: {
                'Accept': 'application/json;odata=nometadata',
                'odata-version': ''
              }
            })
            .then((response: SPHttpClientResponse): Promise<{ ListItemEntityTypeFullName: string }> => {
              return response.json();
            }, (error: any): void => {
              reject(error);
            })
            .then((response: { ListItemEntityTypeFullName: string }): void => {
              listItemEntityTypeName = response.ListItemEntityTypeFullName;
              resolve(listItemEntityTypeName);
            });
        });
  }
}
