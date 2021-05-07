namespace TG275Checklist.Model

module ChecklistTypes =
    
    /// <summary>
    /// Available check categories which are mapped into side panel buttons
    /// </summary>
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
        override this.ToString() =
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

    /// <summary>
    /// Individual checklist item (Text + ESAPI function/results)
    /// </summary>
    type ChecklistItem =
        {
            Text: string
            EsapiResults: EsapiResults option
            Function: PureEsapiFunction option
            AsyncToken: Async<EsapiResults option>
            Loaded: bool
            Loading: bool
        }
        static member init =
            {
                Text = ""
                EsapiResults = None
                Function = None
                AsyncToken = async { return None }
                Loaded = false
                Loading = false
            }

    /// <summary>
    /// ChecklistItems grouped by category
    /// </summary>
    type CategoryChecklist =
        {
            Category: ChecklistCategory
            ChecklistItems: ChecklistItem list
            Loaded: bool
            Loading: bool
        }
        static member init =
            {
                Category = Prescription
                ChecklistItems = []
                Loaded = false
                Loading = false
            }
    
    /// <summary>
    /// Full group of CategoryChecklists for a single Plan
    /// </summary>
    type PlanChecklist =
        {
            PlanDetails: PlanInfo
            CategoryChecklists: CategoryChecklist list
            Loaded: bool
            Loading: bool
        }
        static member init =
            {
                PlanDetails = PlanInfo.init
                CategoryChecklists = []
                Loaded = false
                Loading = false
            }