import * as React from 'react';
import * as moment from 'moment';
import styles from './CrossSiteApprovals.module.scss';
import { ICrossSiteApprovalsProps } from '../interfaces/ICrossSiteApprovalsProps';
import { ICrossSiteApprovalsState, ISearchResult, IDetailsListItems} from '../interfaces/ICrossSiteApprovalsState';
//import { TextField, MaskedTextField } from 'office-ui-fabric-react/lib/TextField';
import { Dropdown, DropdownMenuItemType, IDropdownStyles, IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';
import { PrimaryButton, List } from 'office-ui-fabric-react';
//import SPService from '../services/SPService';
import { SPHttpClient, SPHttpClientConfiguration, SPHttpClientResponse, ODataVersion, ISPHttpClientConfiguration } from '@microsoft/sp-http';
import { SearchService } from '../services/SearchService';
import ListGetter from '../services/ListGetter';
import { DetailsList, DetailsRow, IDetailsRowProps, IDetailsRowStyles, IColumn, Selection, DetailsListLayoutMode } from 'office-ui-fabric-react/lib/DetailsList';
import { getTheme } from 'office-ui-fabric-react/lib/Styling';
import { MarqueeSelection } from 'office-ui-fabric-react/lib/MarqueeSelection';
import { Fabric } from 'office-ui-fabric-react/lib/Fabric';
import { SelectionMode } from '@uifabric/utilities';

const theme = getTheme();

export default class CrossSiteApprovals extends React.Component<ICrossSiteApprovalsProps, ICrossSiteApprovalsState> {
  private _allSites: ISearchResult[];
  private searchService: SearchService;
  private _allitems: IDetailsListItems[];
  private _selection: Selection;

  constructor(props: ICrossSiteApprovalsProps) {
    super(props);
    this.searchService = new SearchService();
    this._allitems = [];
    for (let i = 0; i < 1; i++) {
    this._allitems.push({
      key: i,
      notificationUrl: "This is a url",
      expirationDateTime: "2019-09-07",
      id: "This is an id",
      updated: "2019-09-07"
    });
    this._selection = new Selection({
      onSelectionChanged: () => this.setState({ selectionDetails: this._getSelectionDetails() })
      });
    this.state = {
        status: 'getPulse',
        webHookSourceName: '',
        webHookSourceSiteUrl: '',
        onSiteChange: '',
        onLibraryChange: 'Documents',
        subscriptionId: '',
        sites: [],
        lists: [],
        items: this._allitems,
        selectionDetails: this._getSelectionDetails(),
      };
    }
  }
  
public async componentDidMount(): Promise<void> {
  this._allSites = await this.searchService.GetSearchResults();
    const options = [];
    this._allSites.forEach(searchsites => {
      options.push({
        key: searchsites.title,
        text: searchsites.url
      });
    });
    this.setState({sites: options});
  }

private getSPLists(siteUrl: string): Promise<IDropdownOption[]> {
  //let serviceScope: ServiceScope = this.state.onSiteChange;//.getParent();  
  let _ListGetter:ListGetter = new ListGetter();
  return _ListGetter.getLists(siteUrl, false, this.props.spHttpClient);
}

private _columns: IColumn[] = [
  {
    key: 'notificationUrl',
    name: 'Notification Url',
    fieldName: 'notificationUrl',
    data: 'string'
  } as IColumn,
  {
    key: 'expirationDateTime',
    name: 'Expiration Date',
    fieldName: 'expirationDateTime',
    data: 'date'
  } as IColumn,
  {
    key: 'id',
    name: 'Subscription Id',
    fieldName: 'id',
    data: 'string'
  } as IColumn,
  /*{
    key: 'updated',
    name: 'Updated',
    fieldName: 'updated',
    data: 'string'
  } as IColumn*/
];

private _createSubscription(onSiteChange: string, onLibraryChange: string) {

  const restUrl = `${onSiteChange}/_api/web/lists/getbytitle('${onLibraryChange}')/subscriptions`;
  // Do a post request to the subscriptions endpoint
  this.props.context.spHttpClient.post(restUrl, SPHttpClient.configurations.v1, {
    body: JSON.stringify({
      "resource": `${onSiteChange}/_api/web/lists/getbytitle('${onLibraryChange}')`,
      "notificationUrl": this.props.notificationUrl,
      "expirationDateTime": moment().add(180, 'days'),
      "clientState": "A0A354EC-97D4-4D83-9DDB-144077ADB449"
    })
  }).then((response: SPHttpClientResponse) => {
    if (response.status >= 200 && response.status < 300) {
      alert(`Subscription added on list ${onLibraryChange}`);
      // Update the subscriptions list
      this._getSubscriptions(onSiteChange, onLibraryChange);
    } else {
      // Check the error message
      response.json().then(data => {
        if (typeof data.error !== "undefined") {
          alert(`ERROR:' ${data.error.message}`);
        }
      });
    }
  }).catch(err => {
    console.log('ERROR:', err);
    // Reset the subscription which is loading
  });
}

private _getSubscriptions(onSiteChange: string, onLibraryChange: string) {
  const restUrl = `${onSiteChange}/_api/web/lists/getbytitle('${onLibraryChange}')/subscriptions?$select=updated,expirationDateTime,id,notificationUrl`;
  // Call the subscription API to check all webhooks subs on the list
  this.props.context.spHttpClient.get(restUrl, SPHttpClient.configurations.v1)
    .then((response: SPHttpClientResponse) => { return response.json(); })
    .then(data => {
      this.setState({
        items: data.value,
      });
    }).catch(error => {
      console.log(`ERROR: ${error}`);
    });
}

private _deleteSubscription(onSiteChange: string, onLibraryChange: string, subscriptionId: string) {
  // Update the subscription
  const restUrl = `${onSiteChange}/_api/web/lists/getbytitle('${onLibraryChange}')/subscriptions('${subscriptionId}')`;
  this.props.context.spHttpClient.fetch(restUrl, SPHttpClient.configurations.v1, {
    method: 'DELETE'
  }).then((response: SPHttpClientResponse) => {
    if (response.status >= 200 && response.status < 300) {
      // Update the subscriptions list
      alert(`Subscription deleted: ${subscriptionId}`);
      this._getSubscriptions(onSiteChange, onLibraryChange);
    } else {
      // Check the error message
      response.json().then(data => {
        if (typeof data.error !== "undefined") {
          console.log('ERROR:', data.error.message);
        }
      });
    }
  }).catch(err => {
    console.log('ERROR:', err);
    // Reset the subscription which is loading
  });
}

  public render(): React.ReactElement<ICrossSiteApprovalsProps> {
    const { items, selectionDetails } = this.state;
    return (
      <Fabric>
      <div className={ styles.crossSiteApprovals }>
        <div className={ styles.container }>
          <div className={ styles.titlecontainer }>
            <span className={ styles.title }>Add a webhook</span>
          </div>
          <div className={ styles.dropdowncontainer}>
            <Dropdown className={ styles.dropdown } placeholder="Enter the site url" options={this.state.sites} onChange={this._onSourceSiteUrlChange} /> 
            <Dropdown className={ styles.dropdown } placeholder="Select a list" options={this.state.lists} onChange={this._onSourceNameChange}/> 
          </div>
            <div className={ styles.buttoncontainer }>
            <PrimaryButton className={ styles.topbutton } ariaDescription="Click to add webhook" onClick={() => this.addWebhook()}>
              Add Webhook
            </PrimaryButton>
            <PrimaryButton className={ styles.topbutton } ariaDescription="Click to view subscriptions" onClick={() => this.viewSubscriptions()}>
              View Subscriptions
            </PrimaryButton>
            </div>
        </div> 
        <br></br>
        <div className={styles.ChildClass}>{selectionDetails}</div>
        <MarqueeSelection selection={this._selection}>
          <DetailsList
            items={this.state.items}
            columns={this._columns}
            isHeaderVisible={true}
            compact={true}
            layoutMode={DetailsListLayoutMode.justified}
            onRenderRow={this._onRenderRow}
            selection={this._selection}
            selectionMode={SelectionMode.single}
            selectionPreservedOnEmptyClick={true}
            onItemInvoked={this._onItemInvoked}
            ariaLabelForSelectionColumn="Toggle selection"
            ariaLabelForSelectAllCheckbox="Toggle selection for all items"
            checkButtonAriaLabel="Row checkbox" /> 
        </MarqueeSelection>
          <PrimaryButton className={ styles.bottombutton } ariaDescription="Click to remove subscription" onClick={() => this._getSelectionId()}>
            Remove Subscription
          </PrimaryButton>
          </div>
        </Fabric>  
    );
  }

  private _getSelectionDetails(): string {
    const selectionCount = this._selection.getSelectedCount();

    switch (selectionCount) {
      default:
        return '  No items selected';
      case 1:
        return '  Item selected: ' + (this._selection.getSelection()[0] as IDetailsListItems).id;
    }
  }

  private _getSelectionId(): void {
    const selectionId = (this._selection.getSelection()[0] as IDetailsListItems).id;
    this.removeSubscription(selectionId);
  }

  private _onItemInvoked(item: IDetailsListItems): void {
    alert(`Item invoked: ${item.id}`);
  }

  private _onRenderRow = (props: IDetailsRowProps): JSX.Element => {
    const customStyles: Partial<IDetailsRowStyles> = {};
    if (props.itemIndex % 2 === 0) {
      // Every other row renders with a different background color
      customStyles.root = { backgroundColor: theme.palette.themeLighterAlt };
    }

    return <DetailsRow {...props} styles={customStyles} />;
  }

  private _onSourceSiteUrlChange = (event: React.FormEvent<HTMLDivElement>, item: IDropdownOption): void => {
    this.setState({webHookSourceSiteUrl:event.currentTarget.textContent});
    this.setState({onSiteChange:event.currentTarget.textContent});
    const siteUrl = event.currentTarget.textContent;
    this.getSPLists(siteUrl).then(loadedLists => {
        this.setState({lists: loadedLists});
      }); 
  }

  private _onSourceNameChange = (event: React.FormEvent<HTMLDivElement>, item: IDropdownOption): void => {
    this.setState({webHookSourceName:event.currentTarget.textContent});
    this.setState({onLibraryChange:event.currentTarget.textContent});
  }

  private addWebhook() {
    const siteUrl = this.state.onSiteChange;
    const DocumentLibrary = this.state.onLibraryChange;
    this._createSubscription(siteUrl, DocumentLibrary);
    //SPService.add('',this.props.spHttpClient, this.state.webHookSourceSiteUrl, this.state.webHookSourceName);
  }

  private viewSubscriptions() {
    const siteUrl = this.state.onSiteChange;
    const DocumentLibrary = this.state.onLibraryChange;
    this._getSubscriptions(siteUrl, DocumentLibrary);
  }

  private removeSubscription(subscriptionId: string) {
    const siteUrl = this.state.onSiteChange;
    const DocumentLibrary = this.state.onLibraryChange;
    this._deleteSubscription(siteUrl, DocumentLibrary, subscriptionId);
  }
}