﻿import { WebAPI } from '../WebApi';
import { bindable, inject } from 'aurelia-framework';
import { IPointer } from '../interfaces/IPointer';
import { IRequest } from '../interfaces/IRequest';

@inject(WebAPI)
export class PointerSvc {

    baseUrl: string = 'Nrls';

    constructor(private api: WebAPI) { }

    /**
     * Requests all Pointers for a Patient from the backend service.
     * @param nhsNumber A valid patient NHS Number without dashes.
     * @returns A list of FHIR DocumentReferences in the form of IPointer.
     */
    getPointers(request: IRequest) {
        let pointers = this.api.do<Array<IPointer>>(`${this.baseUrl}/${request.id}`, null, 'get', request.headers);
        return pointers;
    }

}