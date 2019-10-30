//import { ServiceScope, ServiceKey } from '@microsoft/sp-core-library';
import { SPHttpClient, SPHttpClientResponse } from '@microsoft/sp-http';
import { IDropdownOption } from 'office-ui-fabric-react';
//import { IWebPartContext } from '@microsoft/sp-webpart-base';

/**
 * Interface for a service that retrieves lists from the current site
 */
export interface IListGetter<T> {
    /**
     * Retrieves the ID and Title of all the lists in a SharePoint site
     * @param onSiteChange - the IWebPartContext object provided by the web part consuming this service
     * @param includeHidden - whether you want to include hidden lists in the results
     */
    getLists(onSiteChange: any, includeHidden: Boolean, spHttpClient: SPHttpClient): Promise<T>;
}

/**
 * An implementation of the IListGetter service
 * @class
 */
export default class ListGetter implements IListGetter<IDropdownOption[]> {
    /**
     * SPFx services must include a constructor that accepts an argument of type ServiceScope
     * @constructor
     * @param serviceScope
     */
    //constructor(serviceScope: ServiceScope) {
    //}

    /**
     * Retrieves the ID and Title of all the lists in a SharePoint site
     * @param onSiteChange - the IWebPartContext object provided by the web part consuming this service
     * @param includeHidden - whether you want to include hidden lists in the results
     */
    public getLists(onSiteChange: string, includeHidden: Boolean = false, spHttpClient: SPHttpClient): Promise<IDropdownOption[]> {
        const endpoint = includeHidden 
          ? '/_api/web/lists?$select=Title,Id' 
          : '/_api/web/lists?$filter=Hidden%20eq%20false&$select=Title,Id';
        return new Promise<IDropdownOption[]>((resolve: (options: IDropdownOption[]) => void, reject: (error: any) => void) => {
            spHttpClient
                .get(onSiteChange + endpoint, SPHttpClient.configurations.v1)
                .then((response: SPHttpClientResponse) => {
                    response.json().then((lists: any) => {
                        const dropdownOptions: IDropdownOption[] = lists.value.map(list => {
                            return <IDropdownOption>({
                                key: list.Id,
                                text: list.Title
                            });
                        });
                        resolve(dropdownOptions);
                    });
                });
        });
    }
}