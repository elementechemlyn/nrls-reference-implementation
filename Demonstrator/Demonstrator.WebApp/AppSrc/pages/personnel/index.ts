import { PersonnelSvc }         from '../../core/services/PersonnelService';
import { ActorOrganisationSvc } from '../../core/services/ActorOrganisationService';
import { GenericSystemSvc }     from '../../core/services/GenericSystemService';
import { bindable, inject }     from 'aurelia-framework';
import { IPersonnel }           from '../../core/interfaces/IPersonnel';
import { IGenericSystem }       from '../../core/interfaces/IGenericSystem';
import { ActorTypes }           from '../../core/models/enums/ActorTypes';
import { IActorOrganisation }   from '../../core/interfaces/IActorOrganisation';
import { IBreadcrumb } from '../../core/interfaces/IBreadcrumb';
import { BenefitsSvc } from '../../core/services/BenefitsService';
import { AnalyticsSvc } from '../../core/services/AnalyticsService';

@inject(PersonnelSvc, ActorOrganisationSvc, GenericSystemSvc, BenefitsSvc, AnalyticsSvc)
export class Personnel {
    heading: string = 'Personnel';
    personnel: IPersonnel;
    organisation: IActorOrganisation;
    genericSystem: IGenericSystem;
    personnelId: string;
    systemsLoading: boolean = false;
    personnelLoading: boolean = false;
    breadcrumb: Array<IBreadcrumb> = [];
    benefitsFor: string;
    benefitsForId: string;
    benefitsForName: string;
    hasBenefits: boolean = false;
    systemModel: any = {};
    systemModelCount: number = 0; // should be 3

    constructor(private personnelSvc: PersonnelSvc, private actorOrganisationSvc: ActorOrganisationSvc,
        private genericSystemSvc: GenericSystemSvc, private benefitsSvc: BenefitsSvc, private analyticsSvc: AnalyticsSvc) { }

    activate(params) {
        this.personnelId = params.routeParamId;
    }

    created() {
        this.personnelLoading = true;
        this.personnelSvc.get(this.personnelId).then(personnel => {

            this.personnel = personnel;
            this.heading = this.personnel.name;
            this.personnelLoading = false;

            this.systemModelCount += 1;
            this.systemModel.personnel = this.personnel;

            this.getOrganisation(this.personnel.actorOrganisationId);
            this.getSystems();
        });      
    }

    private getOrganisation(orgId: string) {
        this.actorOrganisationSvc.getOne(orgId).then(organisation => {
            this.organisation = organisation;
            this.setBreadcrumb();

            this.systemModelCount += 1;
            this.systemModel.organisation = this.organisation;

            this.setBenefits();
        });
    }

    private setBenefits() {
        this.benefitsSvc.hasFor("Personnel", this.personnel.id).then(hasBenefits => {

            if (hasBenefits) {

                this.benefitsFor = "Personnel";
                this.benefitsForId = this.personnel.id;
                this.benefitsForName = `a ${this.personnel.name}`;
            } else {

                // The assumtion is that an organisation will always have benefits
                this.benefitsFor = "ActorOrganisation";
                this.benefitsForId = this.organisation.id;
                this.benefitsForName = `the ${this.organisation.name}`;
            }

            this.hasBenefits = true;
        });
    }

    private recordScenarioView() {

        this.analyticsSvc.scenarioContext(`${this.personnel.name} (Module: ${this.personnel.cModule})`);
    }

    private recordBenefitView() {

        this.analyticsSvc.benefitsButton(`${this.personnel.name} (Actual: ${this.benefitsForName})`);
    }

    private getSystems() {
        this.systemsLoading = true;
        this.genericSystemSvc.getList(this.personnel.systemIds).then(systems => {
            this.setSystems(systems);
            this.systemsLoading = false;
        });
    }

    private setSystems(systems: Array<IGenericSystem>) : void {

        if (systems && systems.length > 0) {

            this.genericSystem = systems[0];

            this.systemModelCount += 1;
            this.systemModel.genericSystem = this.genericSystem;
        }
        
    }

    private setBreadcrumb(): void {
        this.breadcrumb.push(<IBreadcrumb>{ title: 'Home', route: 'welcome' });
        this.breadcrumb.push(<IBreadcrumb>{ title: this.organisation.name, route: 'actor-organisation-personnel', param: this.organisation.id, paramName: this.organisation.safeName, isBack: true });
        this.breadcrumb.push(<IBreadcrumb>{ title: 'View Persona', isActive: true });
    }

}
