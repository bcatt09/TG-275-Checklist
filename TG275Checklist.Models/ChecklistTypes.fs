namespace TG275Checklist.Model

module ChecklistTypes =
    
    type ChecklistCategory =
        | Prescription
        | Simulation
        | Contouring
        | StandardProcedure
        | PlanQuality
        | Verification
        | Isocenter
        | ImageGuidanceSetup
        | Scheduling
        | Replan
        | Deviations
        member this.ToReadableString() =
            match this with
            | Prescription -> "Prescription"
            | Simulation -> "Simulation"
            | Contouring -> "Contouring"
            | StandardProcedure -> "Standard Procedures"
            | PlanQuality -> "Plan Quality"
            | Verification -> "Dose Verification"
            | Isocenter -> "Isocenter Checks"
            | ImageGuidanceSetup -> "Image Guidance"
            | Scheduling -> "Task Schedules"
            | Replan -> "Checks for a Replan"
            | Deviations -> "Deviations"

    // Individual checklist item
    type ChecklistItem =
        {
            Text: string
            EsapiResults: EsapiResults option
            Function: PureEsapiFunction option
            AsyncToken: Async<EsapiResults option>
        }

    // Initial empty checklist item
    let initChecklistItem =
        {
            Text = ""
            EsapiResults = None
            Function = None
            AsyncToken = async { return None }
        }

    // Checklist items grouped by category
    type CategoryChecklist =
        {
            Category: ChecklistCategory
            Checklist: ChecklistItem list
            Loaded: bool
            Loading: bool
        }
    
    // Full checklist for a single plan
    type PlanChecklist =
        {
            PlanDetails: PlanInfo
            Checklists: CategoryChecklist list
        }