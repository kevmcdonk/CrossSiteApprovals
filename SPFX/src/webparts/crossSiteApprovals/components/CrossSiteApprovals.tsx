import * as React from 'react';
import styles from './CrossSiteApprovals.module.scss';
import { ICrossSiteApprovalsProps } from '../interfaces/ICrossSiteApprovalsProps';
import { ICrossSiteApprovalsState} from '../interfaces/ICrossSiteApprovalsState';
import { escape } from '@microsoft/sp-lodash-subset';
import { TextField, MaskedTextField } from 'office-ui-fabric-react/lib/TextField';
import { PrimaryButton } from 'office-ui-fabric-react';
import SPService from '../services/SPService';

export default class CrossSiteApprovals extends React.Component<ICrossSiteApprovalsProps, ICrossSiteApprovalsState> {

  constructor(props: ICrossSiteApprovalsProps) {
    super(props);

    
    this.state = {
        status: 'getPulse',
        webHookSourceName: '',
        webHookSourceSiteUrl: ''
      };
    }

  public render(): React.ReactElement<ICrossSiteApprovalsProps> {
    return (
      <div className={ styles.crossSiteApprovals }>
        <div className={ styles.container }>
          <div className={ styles.row }>
            <div className={ styles.column }>
              <span className={ styles.title }>Add a webhook</span>
              <TextField placeholder="Enter the site url" value={this.state.webHookSourceSiteUrl} onChange={this._onSourceSiteUrlChange}/>
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

  private _onSourceSiteUrlChange = (event: React.ChangeEvent<HTMLInputElement>) : void => {
    this.setState({webHookSourceSiteUrl:event.target.value});
  }
  private _onSourceNameChange = (event: React.ChangeEvent<HTMLInputElement>) : void => {
    this.setState({webHookSourceName:event.target.value});
  }

  private addWebhook() {
    SPService.add('',this.props.spHttpClient, this.state.webHookSourceSiteUrl, this.state.webHookSourceName);
  }
}
