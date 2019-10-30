import { IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';

export interface ICrossSiteApprovalsState {
  status: string;
  webHookSourceSiteUrl: string;
  webHookSourceName: string;
  onSiteChange: any;
  onLibraryChange: any;
  subscriptionId: any;
  sites: IDropdownOption[];
  lists: IDropdownOption[];
  items: IDetailsListItems[];
  selectionDetails: string;
}

export interface ISearchResult {
  title: string;
  url: string;
}

export interface ISearchService{
 GetSearchResults() : Promise<ISearchResult[]>;
}

export interface IDetailsListItems {
  key: number;
  notificationUrl: string;
  expirationDateTime: string; 
  id: string;
  updated: string;
}