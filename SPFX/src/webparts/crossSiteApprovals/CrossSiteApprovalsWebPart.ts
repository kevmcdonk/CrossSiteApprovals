import * as React from 'react';
import * as ReactDom from 'react-dom';
import { Version } from '@microsoft/sp-core-library';
import { BaseClientSideWebPart } from '@microsoft/sp-webpart-base';
import {
  IPropertyPaneConfiguration,
  PropertyPaneTextField
} from '@microsoft/sp-property-pane';

import * as strings from 'CrossSiteApprovalsWebPartStrings';
import CrossSiteApprovals from './components/CrossSiteApprovals';
import { ICrossSiteApprovalsProps } from './interfaces/ICrossSiteApprovalsProps';

export interface ICrossSiteApprovalsWebPartProps {
  description: string;
}

export default class CrossSiteApprovalsWebPart extends BaseClientSideWebPart<ICrossSiteApprovalsWebPartProps> {

  public render(): void {
    const element: React.ReactElement<ICrossSiteApprovalsProps > = React.createElement(
      CrossSiteApprovals,
      {
        description: this.properties.description,
        spHttpClient: this.context.spHttpClient
      }
    );

    ReactDom.render(element, this.domElement);
  }

  protected onDispose(): void {
    ReactDom.unmountComponentAtNode(this.domElement);
  }

  protected get dataVersion(): Version {
    return Version.parse('1.0');
  }

  protected getPropertyPaneConfiguration(): IPropertyPaneConfiguration {
    return {
      pages: [
        {
          header: {
            description: strings.PropertyPaneDescription
          },
          groups: [
            {
              groupName: strings.BasicGroupName,
              groupFields: [
                PropertyPaneTextField('description', {
                  label: strings.DescriptionFieldLabel
                })
              ]
            }
          ]
        }
      ]
    };
  }
}
