import { SPHttpClient } from '@microsoft/sp-http';

export interface ICrossSiteApprovalsProps {
  description: string;
  spHttpClient: SPHttpClient;
}