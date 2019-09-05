'use strict';
import * as pnp from 'sp-pnp-js';
import { ISearchService, ISearchResult } from '../interfaces/ICrossSiteApprovalsState';

export class MockSearchService implements ISearchService
{
    public GetSearchResults() : Promise<ISearchResult[]>{
        return new Promise<ISearchResult[]>((resolve,reject) => {
          
      resolve([
                    {title:'Title 1',url:'http://asdada Jump '},
                    {title:'Title 2',url:'http://asdada Jump '},
                    ]);
        });
    }
}