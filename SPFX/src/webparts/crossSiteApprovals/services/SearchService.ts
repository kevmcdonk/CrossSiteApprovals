'use strict';
//import * as pnp from 'sp-pnp-js';
import pnp from "sp-pnp-js";
import { ISearchService, ISearchResult } from '../interfaces/ICrossSiteApprovalsState';
//import { sp } from "@pnp/sp";

export class SearchService implements ISearchService
{
  
    public GetSearchResults() : Promise<ISearchResult[]>{
        const _results:ISearchResult[] = [];
  
        return new Promise<ISearchResult[]>((resolve,reject) => {
                pnp.sp.search({
                     Querytext:'contentclass:STS_Site',
                     RowLimit:500,
                     StartRow:0,
                     TrimDuplicates:false
                    })
                .then((results) => {
                   results.PrimarySearchResults.forEach((result)=>{
                    _results.push({
                        title:result.Title,
                        url:result.Path 
                    });
                   });
                })
                .then(
                   () => { resolve(_results);}
                )
                .catch(
                    () => {reject(new Error("Error")); }
                );
                  
        });
    }
}