import * as React from 'react';
import styles from './CrossSiteApprovals.module.scss';
import { ICrossSiteApprovalsProps } from '../interfaces/ICrossSiteApprovalsProps';
import { ICrossSiteApprovalsState, ISearchResult} from '../interfaces/ICrossSiteApprovalsState';
import { TextField, MaskedTextField } from 'office-ui-fabric-react/lib/TextField';
import { Dropdown, DropdownMenuItemType, IDropdownStyles, IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';
import { PrimaryButton } from 'office-ui-fabric-react';
import SPService from '../services/SPService';
//import { SPHttpClient, SPHttpClientConfiguration, SPHttpClientResponse, ODataVersion, ISPHttpClientConfiguration } from '@microsoft/sp-http';
import { SearchService } from '../services/SearchService';
import { Search } from 'sp-pnp-js';

export default class CrossSiteApprovals extends React.Component<ICrossSiteApprovalsProps, ICrossSiteApprovalsState, ISearchResult> {
  private _allItems: ISearchResult[];
  private searchService: SearchService;

  constructor(props: ICrossSiteApprovalsProps) {
    super(props);
    this.searchService = new SearchService();
    this.state = {
        status: 'getPulse',
        webHookSourceName: '',
        webHookSourceSiteUrl: '',
        sites: []
      };
    }
  
  public async componentDidMount(): Promise<void> {
    
    this._allItems = await this.searchService.GetSearchResults();
    
    const options = [];
    this._allItems.forEach(c => {
      options.push({
        key: c.url,
        text: c.title
      });
    });
    this.setState({sites: options});
  } 

  public options2: IDropdownOption[] = [
    { key: 'fruits', text: 'Fruits' },
    { key: 'apple', text: 'Apple' }
  ];

  public render(): React.ReactElement<ICrossSiteApprovalsProps> {
    return (
      <div className={ styles.crossSiteApprovals }>
        <div className={ styles.container }>
          <div className={ styles.row }>
            <div className={ styles.column }>
              <span className={ styles.title }>Add a webhook</span>
              <Dropdown placeholder="Enter the site url" options={this.state.sites} onChange={this._onSourceSiteUrlChange} /> 
              <TextField placeholder="Enter the library name" value={this.state.webHookSourceName} onChange={this._onSourceNameChange}/>
              <PrimaryButton ariaDescription="Click to add webhook" onClick={() => this.addWebhook()}>
                Add webhook
              </PrimaryButton>
            </div>
          </div>
        </div>
      </div>
    );
  }

  private _onSourceSiteUrlChange = (event: React.FormEvent<HTMLDivElement>, item: IDropdownOption): void => {
    console.log(`Selection change: ${item.text} ${item.selected ? 'selected' : 'unselected'}`);
    this.setState({webHookSourceSiteUrl:event.type});
  }

  private _onSourceNameChange = (event: React.ChangeEvent<HTMLInputElement>) : void => {
    this.setState({webHookSourceName:event.target.value});
  }

  private addWebhook() {
    SPService.add('',this.props.spHttpClient, this.state.webHookSourceSiteUrl, this.state.webHookSourceName);
  }
}
