import { IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';

export interface ICrossSiteApprovalsState {
  status: string;
  webHookSourceSiteUrl: string;
  webHookSourceName: string;
  sites: IDropdownOption[];
}

export interface ISearchResult {
  title: string;
  url: string;
}

export interface ISearchService{
 GetSearchResults() : Promise<ISearchResult[]>;
}