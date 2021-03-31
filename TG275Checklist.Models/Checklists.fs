namespace TG275Checklist.Model

open VMS.TPS.Common.Model.API
open Esapi

module Checklists = 

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

    type EsapiResults =
        {
            Text: string
            //OtherThingsToDisplay1: 'a option
            //OtherThingsToDisplay2: 'a option
        }

    type PlanDetails =
        {
            CourseId: string
            PlanId: string
            // Dose
            // Approvals
        }

    type PureEsapiFunction = PlanSetup -> EsapiResults
    type EsapiChecklistFunction = PlanDetails -> Async<EsapiResults>

    let runEsapiFunction planDetails (esapiFunc: PureEsapiFunction option) =
        async {
            match esapiFunc with
            | Some func ->
                let! result = esapi.Run(fun (pat: Patient) ->
                    let plan = 
                        pat.Courses
                        |> Seq.map(fun c -> c.PlanSetups |> Seq.cast<PlanSetup>)
                        |> Seq.concat
                        |> Seq.filter(fun p -> p.Id = planDetails.PlanId && p.Course.Id = planDetails.CourseId)
                        |> Seq.exactlyOne
                    func plan
                )
                return Some result
            | None -> return None
        }
    type ChecklistItem =
        {
            Text: string
            EsapiResults: EsapiResults option
            Function: PureEsapiFunction option
            AsyncToken: Async<EsapiResults option>
        }
    type CategoryChecklist =
        {
            Category: ChecklistCategory
            Checklist: ChecklistItem list
        }

    let createChecklistItemWithAsyncToken planDetails listItem =
        { listItem with 
            AsyncToken = 
                listItem.Function |> runEsapiFunction planDetails }

    let createCategoryChecklistWithAsyncTokens planDetails checklist = 
        { checklist with 
            Checklist = checklist.Checklist |> List.map(fun x -> x |> createChecklistItemWithAsyncToken planDetails)}

    let createFullChecklistWithAsyncTokens planDetails checklist =
        checklist |> List.map (fun x -> x |> createCategoryChecklistWithAsyncTokens planDetails)

        //member this.CalcCategoryChecklist planInfo =
        //    this.Checklist |>
        //    List.map(fun x ->
        //        match x.Function with
        //        | None -> async{return None}
        //        | Some fxn -> runEsapiFunction fxn planInfo)

    let createCategoryChecklist category list =
        {
            Category = category
            Checklist = list
        }

    let blankChecklistItem =
        {
            Text = ""
            EsapiResults = None
            Function = None
            AsyncToken = async { return None }
        }
        
    let blankFunction (plan: PlanSetup) : Async<EsapiResults> option =
        None

    let testFunction (plan:PlanSetup) =
        System.Threading.Thread.Sleep(5000)
        { Text = plan.TotalDose.ToString() }

    let prescriptionChecklist =
        createCategoryChecklist Prescription (List.map(fun (text, fxn) -> { blankChecklistItem with Text = text; Function = fxn }) <| // match fxn with | None -> None | Some fn -> Some(runEsapiFunction fn)  }) <|
            [
                "Prescription (with respect to standard of care, institutional clinical guidelines or clinical trial is applicable)", Some testFunction
                "Final plan and prescription approval by physician", None
                "Site and laterality (incl. medical chart to confirm laterality)", None
                "Prescription vs consult note (e.g. physician report in EMR on plans for treatment)", None
                "Total dose, dose/fractionation, number of fractions", None
                "Fractionation pattern and regimen (e.g. daily, BID, Quad Shot, regular plan follow by boost, etc.)", None
                "Energy matches prescription", None
                "Modality (e.g. electrons, photons, protons, etc.)", None
                "Technique (e.g. 3D, IMRT, VMAT, SBRT, etc.) matches prescription", None
                "Bolus", None
                "Additional shielding (e.g. eye shields, testicular shields, etc. as applicable)", None
                "Course intent/diagnosis", None
            ])

    let simulationChecklist =
        createCategoryChecklist Simulation (List.map(fun text -> { blankChecklistItem with Text = text }) <|
            [
                "Physician directive for imaging technique, setup and immobilization (this may include: contrast, scanning orientation, immobilization device, etc.)"
                "Description of target location on physician planning directive (e.g. RUL Lung, H&N, L1‐L4)"
                "Patient set up, positioning and immobilization*: (a) Appropriate for site and/or per clinical standard procedures, (b) Written or photographic documentation of patient positioning, immobilization and ancillary devices, including setup note"
                "Image quality and usability: CT Scan Artifacts, Scan sup/inf Range Includes Enough Data, Scan FOV encompasses all required information, Use of Contrast"
                "Motion management: (a) MD directive, (b) breath‐hold parameters, (c) gating parameters, (d) 4D‐CT parameters and data set"
                "Registration/Fusion of image sets (CT, PET, MRI, etc.)"
                "Patient Orientation ‐ CT information matches patient setup"
                "Transfer and selection of image set in treatment planning system"
            ])

    let contouringChecklist =
        createCategoryChecklist Contouring (List.map(fun text -> { blankChecklistItem with Text = text }) <|
            [
                "Target(s)* ‐ e.g. discernible errors, missing slices, mislabeling, gross anatomical deviations."
                "Organs‐at‐risk (OAR's)"
                "PTV and OAR Margin ‐ as specified in the chart and/or per protocol"
                "Body/External contour"
                "Density overrides applied as needed (ex. High‐Z material, contrast, artifacts, etc.)"
                "Consideration of Supporting Structures (i.e. couch, immobilization and ancillary devices, etc.)"
                "Approval of contours by MD"
            ])

    let standardOperatingProceduresChecklist =
        createCategoryChecklist StandardProcedure (List.map(fun text -> { blankChecklistItem with Text = text }) <|
            [
                "Course and plan ID"
                "Treatment technique (e.g. 3D, IMRT, VMAT, SBRT, etc.)"
                "Delivery system (e.g. standard linac, CyberKnife, Tomotherapy, etc. as applicable)"
                "Beam arrangement"
                "Beam deliverability"
                "MU"
                "Energy"
                "Dose rate"
                "Field delivery times"
                "Field size and aperture"
                "Bolus utilization"
                "Beam modifiers (e.g. wedges, electron and photon blocks, trays, etc.)"
                "Treatment plan warnings/errors"
                "Tolerance table"
                "Potential for collision"
                "Setup shifts use standard SOP"
                "Physics consult (e.g. evaluation of dose to pacemaker, previous treatment, etc.)"
            ])

    let planQualityChecklist =
        createCategoryChecklist PlanQuality (List.map(fun text -> { blankChecklistItem with Text = text }) <|
            [
                "Target coverage and target planning objectives"
                "Sparing of OARs and OAR planning objectives"
                "Plan conforms to clinical trial (as applicable)"
                "Structures used during optimization"
                "Physician designed apertures"
                "Dose distribution"
                "Hot spots"
                "Reference points"
                "Plan normalization"
                "Calculation algorithm and calculation grid size"
                "Prior Radiation accounted for in plan"
                "Plan Sum (e.g. Original plus boost plans)"
            ])
       
    let doseVerificationChecklist =
        createCategoryChecklist Verification (List.map(fun text -> { blankChecklistItem with Text = text }) <|
            [
                "Second calculation check and/or QA performed"
                "Verification plan for patient specific QA measurement"
                "Request for in‐vivo dosimetry"
            ])
        
    let isocenterChecklist =
        createCategoryChecklist Isocenter (List.map(fun text -> { blankChecklistItem with Text = text }) <|
            [
                "Isocenter: placement and consistency between patient marking and setup instructions"
                "Additional shifts"
                "Multiple isocenters"
            ])
        
    let imageGuidanceChecklist =
        createCategoryChecklist ImageGuidanceSetup (List.map(fun text -> { blankChecklistItem with Text = text }) <|
            [
                "Matching instructions (e.g. 2D/2D, 3D, etc.) and MD directive for IGRT"
                "Matching structures"
                "Reference CT"
                "Isocenter on reference image(s), 2D or 3D"
                "DRR association"
                "DRR image quality"
                "Imaging technique"
                "Imaging regimen (e.g. daily, weekly, daily followed by weekly, etc.)"
                "Parameters and setup for specialized devices (e.g. ExacTrac, VisionRT, RPM, etc.)"
                "Isocenter for specialized devices (e.g. VisionRT, ExacTrac, etc.)"
            ])
        
    let schedulingChecklist =
        createCategoryChecklist Scheduling (List.map(fun text -> { blankChecklistItem with Text = text }) <|
            [
                "Plan is scheduled for treatment"
                "Scheduling of safety‐critical tasks (e.g. weekly chart checks, IMRT QA, etc.)"
            ])
        
    let replanChecklist =
        createCategoryChecklist Replan (List.map(fun text -> { blankChecklistItem with Text = text }) <|
            [
                "Full plan check if new plan generated"
                "Old/new CT registration"
                "Isocenter placement"
                "Deformed or new contours"
                "DVH Comparison"
                "CTV/PTV coverage"
                "Organs at risk dose limits"
            ])
        
    let deviationChecklist =
        createCategoryChecklist Deviations (List.map(fun text -> { blankChecklistItem with Text = text }) <|
            [
                "Any unexpected deviations entered into incident learning system"
            ])

    let fullChecklist = [prescriptionChecklist; simulationChecklist; contouringChecklist; standardOperatingProceduresChecklist; 
                        planQualityChecklist; doseVerificationChecklist; isocenterChecklist; imageGuidanceChecklist; 
                        schedulingChecklist; replanChecklist; deviationChecklist]

    //let calcFullChecklist (checklist:CategoryChecklist list) planInfo =
    //    checklist
    //    |> List.map(fun x -> x.Checklist)
    //    |> List.concat
    //    |> List.map(fun x ->
    //            x.Text,
    //            match x.Function with
    //            | None -> async{return None}
    //            | Some fxn -> runEsapiFunction fxn planInfo
    //    )