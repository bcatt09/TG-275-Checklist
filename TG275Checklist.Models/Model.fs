namespace TG275Checklist.Model

open Elmish
open Elmish.WPF

open Esapi
open VMS.TPS.Common.Model.API
open type System.Windows.Visibility

// Raw Course/Plan information (Needs to be outside of a module for XAMLs)
type Plan =
    {
        Id: string
    }
type Course =
    {
        Id: string
        Plans: Plan list
    }

// Arguments passed from the Eclipse plugin (Doesn't have to be outside of a module, I just don't have a better place for it)
type StandaloneApplicationArgs =
    {
        PatientID: string
        Courses: Course list
        OpenedCourseID: string
        OpenedPlanID: string
    }

//// Checklist options which are held in the PatientSetup Screen
//module PatientSetupOptions =
//    type SelectedPlan =
//        {
//            PlanId: string
//            CourseId: string
//        }
//    type ToggleType =
//        | PreviousRT of bool
//        | Pacemaker of bool
//        | Registration of bool
//        | FourD of bool
//        | DIBH of bool
//        | IMRT of bool
//        | SRS of bool
//        | SBRT of bool
//        member this.IsChecked = 
//            match this with
//            | PreviousRT value -> value
//            | Pacemaker value -> value
//            | Registration value -> value
//            | FourD value -> value
//            | DIBH value -> value
//            | IMRT value -> value
//            | SRS value -> value
//            | SBRT value -> value
//        member this.Text =
//            match this with
//            | PreviousRT _ -> "Previous RT"
//            | Pacemaker _ -> "Pacemaker/ICD"
//            | Registration _ -> "Registration"
//            | FourD _ -> "4D Simulation"
//            | DIBH _ -> "DIBH"
//            | IMRT _ -> "IMRT/VMAT"
//            | SRS _ -> "SRS"
//            | SBRT _ -> "SBRT"
//    let toggleList = [ PreviousRT false; Pacemaker false; Registration false; FourD false; DIBH false; IMRT false; SRS false; SBRT false ]
    
//    type Model =
//        {
//            Plans: SelectedPlan list
//            Toggles: ToggleType list
//        }
//    let init () = { Plans = []; Toggles = [] }

//module PatientSetupScreen =
//    open PatientSetupOptions
//    // Courses/Plans to be used in PatientSetup Screen
//    type PlanWithOptions =
//        {
//            Id: string
//            IsChecked: bool     // Is it checked off to be used in checklists?
//            bindingid: string   // Used for subModel bindings
//        }
//    let getPlanBindingId courseId planId = courseId + "\\" + planId
//    type CourseWithOptions =
//        {
//            Id: string
//            Plans: PlanWithOptions list
//            IsExpanded: bool    // Is the course expanded (mainly used for initial model)
//        }

//    // PatientSetup Model
//    type Model =
//        {
//            Courses: CourseWithOptions list
//            Toggles: ToggleType list
//            Visibility: System.Windows.Visibility
//        }

//    // Initial model
//    let init (args:StandaloneApplicationArgs) =
//        {
//            Courses = args.Courses |> List.map (fun c -> 
//                { 
//                    Id = c.Id; 
//                    IsExpanded = c.Id = args.OpenedCourseID; 
//                    Plans = c.Plans |> List.map (fun p ->
//                        {
//                            Id = p.Id;
//                            IsChecked = p.Id = args.OpenedPlanID;
//                            bindingid = getPlanBindingId c.Id p.Id 
//                        })
//                })
//            Toggles = toggleList
//            Visibility = Visible
//        }
//    let initFromCourses args courses = { init(args) with Courses = courses }

//    // Internal Messages
//    type Msg =
//    | ToggleChanged of ToggleType * bool
//    | UsePlanChanged of string * bool
//    | CourseIsExpandedChanged of string * bool
//    | NextButtonClicked

//    // Messages sent to MainWindow
//    type MainWindowMsg =
//    | NoMainWindowMsg
//    | PatientSetupCompleted of PatientSetupOptions.Model

//    // Handle Messages
//    let update msg (m:Model) =
//        match msg with
//        | ToggleChanged  (id, ischecked) -> 
//            { m with 
//                Toggles = m.Toggles 
//                |> List.map(fun toggle -> 
//                    if id = toggle
//                    then 
//                        match toggle with
//                        | PreviousRT t -> PreviousRT ischecked
//                        | Pacemaker t -> Pacemaker ischecked
//                        | Registration t -> Registration ischecked
//                        | FourD t -> FourD ischecked
//                        | DIBH t -> DIBH ischecked
//                        | IMRT t -> IMRT ischecked
//                        | SRS t -> SRS ischecked
//                        | SBRT t -> SBRT ischecked
//                    else toggle)
//            }, Cmd.none, NoMainWindowMsg
//        | UsePlanChanged (id, ischecked) -> 
//            { m with 
//                Courses = m.Courses 
//                |> List.map(fun course -> 
//                    { course with 
//                        Plans = course.Plans 
//                        |> List.map(fun p -> 
//                            if p.bindingid = id 
//                            then { Id = p.Id; IsChecked = ischecked; bindingid = p.bindingid } 
//                            else p) 
//                    })
//            }, Cmd.none, NoMainWindowMsg 
//        | CourseIsExpandedChanged (id, isexpanded) -> 
//            { m with 
//                Courses = m.Courses
//                |> List.map (fun c -> if c.Id = id then { c with IsExpanded = isexpanded } else c)
//            }, Cmd.none, NoMainWindowMsg 
//        | NextButtonClicked -> 
//            { m with Visibility = Collapsed }, 
//            Cmd.none, 
//            PatientSetupCompleted 
//                { 
//                    Plans = 
//                        [ for c in m.Courses do
//                            for p in c.Plans do
//                                if p.IsChecked then 
//                                    yield { PlanId = p.Id; CourseId = c.Id } ]
//                    Toggles = m.Toggles |> List.filter (fun t -> t.IsChecked)          
//                }

//    // WPF Bindings
//    let planBindings () : Binding<(Model * CourseWithOptions) * PlanWithOptions, Msg> list =
//        [
//            "Id" |> Binding.oneWay (fun (_, p) -> p.Id)
//            "IsChecked" |> Binding.twoWay ((fun (_, p:PlanWithOptions) -> p.IsChecked), (fun value (_, p:PlanWithOptions) -> UsePlanChanged (p.bindingid, value)))
//        ]
//    let courseBindings () : Binding<Model * CourseWithOptions, Msg> list =
//        [
//            "Id" |> Binding.oneWay (fun (_, c) -> c.Id)
//            "Plans" |> Binding.subModelSeq((fun (_, c) -> c.Plans), (fun (p:PlanWithOptions) -> p.bindingid), planBindings)
//            "IsExpanded" |> Binding.twoWay ((fun (_, c) -> c.IsExpanded), (fun value (_, c:CourseWithOptions) -> CourseIsExpandedChanged (c.Id, value)))
//        ]
//    let toggleBindings () : Binding<Model * ToggleType, Msg> list =
//        [
//            "Text" |> Binding.oneWay (fun (_, f) -> f.Text)
//            "IsChecked" |> Binding.twoWay ((fun (_, f:ToggleType) -> f.IsChecked), (fun value (_, f:ToggleType) -> ToggleChanged (f, value)))
//        ]
//    let bindings () : Binding<Model, Msg> list =
//        [
//            "Courses" |> Binding.subModelSeq((fun m -> m.Courses), (fun (c:CourseWithOptions) -> c.Id), courseBindings)
//            "PatientSetupToggles" |> Binding.subModelSeq((fun m -> m.Toggles), (fun (t:ToggleType) -> t), toggleBindings)
//            "PatientSetupCompleted" |> Binding.cmd NextButtonClicked
//            "PatientSetupScreenVisibility" |> Binding.oneWay(fun m -> m.Visibility)
//        ]

//module Checklists =
//    type ChecklistCategory =
//        | Prescription
//        | Simulation
//        | Contouring
//        | StandardProcedure
//        | PlanQuality
//        | Verification
//        | Isocenter
//        | ImageGuidanceSetup
//        | Scheduling
//        | Replan
//        | Deviations
//        member this.ToReadableString() =
//            match this with
//            | Prescription -> "Prescription"
//            | Simulation -> "Simulation"
//            | Contouring -> "Contouring"
//            | StandardProcedure -> "Standard Procedures"
//            | PlanQuality -> "Plan Quality"
//            | Verification -> "Dose Verification"
//            | Isocenter -> "Isocenter Checks"
//            | ImageGuidanceSetup -> "Image Guidance"
//            | Scheduling -> "Task Schedules"
//            | Replan -> "Checks for a Replan"
//            | Deviations -> "Deviations"

//    type EsapiResults =
//        {
//            Text: string
//            //OtherThingsToDisplay1: 'a option
//            //OtherThingsToDisplay2: 'a option
//        }
//    type EsapiFunction = string -> EsapiResults
//    type ChecklistItem =
//        {
//            Text: string
//            EsapiResults: EsapiResults option
//            Function: EsapiFunction option
//        }
//    type Checklist =
//        {
//            Category: ChecklistCategory
//            Checklist: ChecklistItem list
//        }

//    let createChecklist category list =
//        {
//            Category = category
//            Checklist = list
//        }

//    //let runFunctionInEsapi =

//    let testFunction (plan:PlanSetup) =
//        { Text = plan.TotalDose.ToString() }

//    let prescriptionChecklist =
//        createChecklist Prescription (List.map(fun (text, fxn) -> { Text = text; EsapiResults = None; Function = fxn }) <|
//            [
//                "Prescription (with respect to standard of care, institutional clinical guidelines or clinical trial is applicable)", None//Some testFunction
//                "Final plan and prescription approval by physician", None
//                "Site and laterality (incl. medical chart to confirm laterality)", None
//                "Prescription vs consult note (e.g. physician report in EMR on plans for treatment)", None
//                "Total dose, dose/fractionation, number of fractions", None
//                "Fractionation pattern and regimen (e.g. daily, BID, Quad Shot, regular plan follow by boost, etc.)", None
//                "Energy matches prescription", None
//                "Modality (e.g. electrons, photons, protons, etc.)", None
//                "Technique (e.g. 3D, IMRT, VMAT, SBRT, etc.) matches prescription", None
//                "Bolus", None
//                "Additional shielding (e.g. eye shields, testicular shields, etc. as applicable)", None
//                "Course intent/diagnosis", None
//            ])

//    let simulationChecklist =
//        createChecklist Simulation (List.map(fun text -> { Text = text; EsapiResults = None; Function = None }) <|
//            [
//                "Physician directive for imaging technique, setup and immobilization (this may include: contrast, scanning orientation, immobilization device, etc.)"
//                "Description of target location on physician planning directive (e.g. RUL Lung, H&N, L1‐L4)"
//                "Patient set up, positioning and immobilization*: (a) Appropriate for site and/or per clinical standard procedures, (b) Written or photographic documentation of patient positioning, immobilization and ancillary devices, including setup note"
//                "Image quality and usability: CT Scan Artifacts, Scan sup/inf Range Includes Enough Data, Scan FOV encompasses all required information, Use of Contrast"
//                "Motion management: (a) MD directive, (b) breath‐hold parameters, (c) gating parameters, (d) 4D‐CT parameters and data set"
//                "Registration/Fusion of image sets (CT, PET, MRI, etc.)"
//                "Patient Orientation ‐ CT information matches patient setup"
//                "Transfer and selection of image set in treatment planning system"
//            ])

//    let contouringChecklist =
//        createChecklist Contouring (List.map(fun text -> { Text = text; EsapiResults = None; Function = None }) <|
//            [
//                "Target(s)* ‐ e.g. discernible errors, missing slices, mislabeling, gross anatomical deviations."
//                "Organs‐at‐risk (OAR's)"
//                "PTV and OAR Margin ‐ as specified in the chart and/or per protocol"
//                "Body/External contour"
//                "Density overrides applied as needed (ex. High‐Z material, contrast, artifacts, etc.)"
//                "Consideration of Supporting Structures (i.e. couch, immobilization and ancillary devices, etc.)"
//                "Approval of contours by MD"
//            ])

//    let standardOperatingProceduresChecklist =
//        createChecklist StandardProcedure (List.map(fun text -> { Text = text; EsapiResults = None; Function = None }) <|
//            [
//                "Course and plan ID"
//                "Treatment technique (e.g. 3D, IMRT, VMAT, SBRT, etc.)"
//                "Delivery system (e.g. standard linac, CyberKnife, Tomotherapy, etc. as applicable)"
//                "Beam arrangement"
//                "Beam deliverability"
//                "MU"
//                "Energy"
//                "Dose rate"
//                "Field delivery times"
//                "Field size and aperture"
//                "Bolus utilization"
//                "Beam modifiers (e.g. wedges, electron and photon blocks, trays, etc.)"
//                "Treatment plan warnings/errors"
//                "Tolerance table"
//                "Potential for collision"
//                "Setup shifts use standard SOP"
//                "Physics consult (e.g. evaluation of dose to pacemaker, previous treatment, etc.)"
//            ])

//    let planQualityChecklist =
//        createChecklist PlanQuality (List.map(fun text -> { Text = text; EsapiResults = None; Function = None }) <|
//            [
//                "Target coverage and target planning objectives"
//                "Sparing of OARs and OAR planning objectives"
//                "Plan conforms to clinical trial (as applicable)"
//                "Structures used during optimization"
//                "Physician designed apertures"
//                "Dose distribution"
//                "Hot spots"
//                "Reference points"
//                "Plan normalization"
//                "Calculation algorithm and calculation grid size"
//                "Prior Radiation accounted for in plan"
//                "Plan Sum (e.g. Original plus boost plans)"
//            ])
       
//    let doseVerificationChecklist =
//        createChecklist Verification (List.map(fun text -> { Text = text; EsapiResults = None; Function = None }) <|
//            [
//                "Second calculation check and/or QA performed"
//                "Verification plan for patient specific QA measurement"
//                "Request for in‐vivo dosimetry"
//            ])
        
//    let isocenterChecklist =
//        createChecklist Isocenter (List.map(fun text -> { Text = text; EsapiResults = None; Function = None }) <|
//            [
//                "Isocenter: placement and consistency between patient marking and setup instructions"
//                "Additional shifts"
//                "Multiple isocenters"
//            ])
        
//    let imageGuidanceChecklist =
//        createChecklist ImageGuidanceSetup (List.map(fun text -> { Text = text; EsapiResults = None; Function = None }) <|
//            [
//                "Matching instructions (e.g. 2D/2D, 3D, etc.) and MD directive for IGRT"
//                "Matching structures"
//                "Reference CT"
//                "Isocenter on reference image(s), 2D or 3D"
//                "DRR association"
//                "DRR image quality"
//                "Imaging technique"
//                "Imaging regimen (e.g. daily, weekly, daily followed by weekly, etc.)"
//                "Parameters and setup for specialized devices (e.g. ExacTrac, VisionRT, RPM, etc.)"
//                "Isocenter for specialized devices (e.g. VisionRT, ExacTrac, etc.)"
//            ])
        
//    let schedulingChecklist =
//        createChecklist Scheduling (List.map(fun text -> { Text = text; EsapiResults = None; Function = None }) <|
//            [
//                "Plan is scheduled for treatment"
//                "Scheduling of safety‐critical tasks (e.g. weekly chart checks, IMRT QA, etc.)"
//            ])
        
//    let replanChecklist =
//        createChecklist Replan (List.map(fun text -> { Text = text; EsapiResults = None; Function = None }) <|
//            [
//                "Full plan check if new plan generated"
//                "Old/new CT registration"
//                "Isocenter placement"
//                "Deformed or new contours"
//                "DVH Comparison"
//                "CTV/PTV coverage"
//                "Organs at risk dose limits"
//            ])
        
//    let deviationChecklist =
//        createChecklist Deviations (List.map(fun text -> { Text = text; EsapiResults = None; Function = None }) <|
//            [
//                "Any unexpected deviations entered into incident learning system"
//            ])

//    let fullChecklist = [prescriptionChecklist; simulationChecklist; contouringChecklist; standardOperatingProceduresChecklist; 
//                        planQualityChecklist; doseVerificationChecklist; isocenterChecklist; imageGuidanceChecklist; 
//                        schedulingChecklist; replanChecklist; deviationChecklist]

//module ChecklistScreen =
//    open Checklists
//    let exampleChecklist = { Category = Prescription; Checklist = [ { Text = "LOOK AT THE PLAN"; EsapiResults = None; Function = None } ] }
//    let getChecklistBindingId category = category.ToString()
//    type PlanDetails =
//        {
//            CourseId: string
//            PlanId: string
//            // Dose
//            // Approvals
//        }
//    type SelectedPlan =
//        {
//            PlanDetails: PlanDetails
//            Checklists: Checklist list
//        }
//    type Model =
//        {
//            Plans: SelectedPlan list
//            Visibility: System.Windows.Visibility
//        }
//    let init () =
//        {
//            Plans = []
//            Visibility = Collapsed
//        }
//    let initChecklist () = 
//        {
//            Category = Prescription;
//            Checklist = [{Text="LOOK AT THE PLAN";EsapiResults=Some {Text = "DONE AND DONE"}; Function = None}]
//        }
        
//    //let init () =
//    //    {
//    //        PlanDetails = { CourseId = ""; PlanId = "" }
//    //        Category = Prescription
//    //        Checklist = [{Text="LOOK AT THE PLAN";EsapiText=None}]
//    //        Visibility = Collapsed
//    //    }

//    type Msg =
//    | Message
//    | LoadChecklists
//    | LoadChecklistsSuccess
//    | LoadChecklistsFailure


//    let checklistItemBindings () : Binding<((Model * SelectedPlan) * Checklist) * ChecklistItem, Msg> list =
//        [
//            "Text" |> Binding.oneWay (fun (_, item) -> item.Text)
//            "EsapiText" |> Binding.oneWay (fun (_, item) -> match item.EsapiResults with | None -> "" | Some result -> result.Text)
//        ]

//    let checklistBindings () : Binding<(Model * SelectedPlan) * Checklist, Msg> list =
//        [
//            "ChecklistItems" |> Binding.subModelSeq((fun (_, checklist) -> checklist.Checklist), (fun (item:ChecklistItem) -> item.Text), checklistItemBindings)
//            "Category" |> Binding.oneWay(fun (_, checklist) -> checklist.Category.ToReadableString())
//        ]

//    let planBindings () : Binding<Model * SelectedPlan, Msg> list =
//        [
//            "CourseId" |> Binding.oneWay(fun (_, plan) -> plan.PlanDetails.CourseId)
//            "PlanId" |> Binding.oneWay(fun (_, plan) -> plan.PlanDetails.PlanId)
//            "Checklists" |> Binding.subModelSeq((fun (_, plan) -> plan.Checklists), (fun (c:Checklist) -> getChecklistBindingId c.Category), checklistBindings)
//        ]

//    let bindings () : Binding<Model, Msg> list =
//        [
//            "Plans" |> Binding.subModelSeq((fun m -> m.Plans), (fun (p:SelectedPlan) -> PatientSetupScreen.getPlanBindingId p.PlanDetails.CourseId p.PlanDetails.PlanId), planBindings)
//            "ChecklistScreenVisibility" |> Binding.oneWay(fun m -> m.Visibility)
//        ]

//module App =
//    open PatientSetupScreen

//    // Info displayed by the Main Window
//    type SharedInfo =
//        {
//            PatientName: string
//            CurrentUser: string
//        }

//    // Status Bar at bottom of window
//    type StatusBar =
//    | NoLoadingBar of string
//    | Indeterminate of IndeterminateStatusBar
//    | Determinate of DeterminateStatusBar
//    and IndeterminateStatusBar = { Status: string }
//    and DeterminateStatusBar = { Status: string; min: int; max: int }

//    // Main Model
//    type Model =
//        { 
//            PatientSetupScreen: PatientSetupScreen.Model
//            ChecklistListScreen: ChecklistScreen.Model
//            PatientSetupOptions: PatientSetupOptions.Model
//            SharedInfo: SharedInfo
//            StatusBar: StatusBar
//            Args: StandaloneApplicationArgs
//        }

//    // Messages
//    type Msg =
//        // Eclipse login
//        | Login        
//        | LoginSuccess of SharedInfo
//        | LoginFailed of exn
//        // Patient Setup
//        | LoadCoursesIntoPatientSetup
//        | LoadCoursesSuccess of CourseWithOptions list
//        | LoadCoursesFailed of exn
//        | PatientSetupMsg of PatientSetupScreen.Msg
//        | LoadChecklistScreen of PatientSetupOptions.Model
//        // Checklist
//        | ChecklistMsg of ChecklistScreen.Msg


//        | Debugton

//    // Default status bar
//    let readyStatus = NoLoadingBar "Ready"

//    // Initial empty model
//    let init args =
//        { 
//            PatientSetupScreen = PatientSetupScreen.init(args)
//            SharedInfo =
//                {
//                    PatientName = ""
//                    CurrentUser = ""
//                }
//            ChecklistListScreen = ChecklistScreen.init()
//            PatientSetupOptions = PatientSetupOptions.init()
//            StatusBar = readyStatus
//            Args = args
//        }, Cmd.ofMsg Login
    
//    // Initial Eclipse login function
//    let login args =
//        async {
//            // Log in to Eclipse and get initial patient info
//            try
//                do! esapi.LogInAsync()
//                do! esapi.OpenPatientAsync(args.PatientID)
//                let! patientInfo = esapi.Run(fun (pat : Patient, app : Application) ->
//                        {
//                            PatientName = pat.Name
//                            CurrentUser = app.CurrentUser.Name
//                        })
                
//                return LoginSuccess patientInfo
//            with ex -> return LoginFailed ex
//        }

//    // Load courses/plans from Eclipse
//    let loadCoursesIntoPatientSetup model =
//        async {
//            let! courses = esapi.Run(fun (pat : Patient) ->
//                pat.Courses 
//                |> Seq.sortByDescending(fun course -> match Option.ofNullable course.StartDateTime with | Some time -> time | None -> new System.DateTime())
//                |> Seq.map (fun course -> 
//                    // If the course was already loaded, match its states, otherwise keep it collapsed
//                    let existingCourse = model.PatientSetupScreen.Courses |> List.filter (fun c -> c.Id = course.Id) |> List.tryExactlyOne
//                    { 
//                        Id = course.Id; 
//                        IsExpanded = 
//                            match existingCourse with
//                            | Some c -> c.IsExpanded
//                            | None -> false
//                        Plans = course.PlanSetups 
//                                |> Seq.sortByDescending(fun plan -> match Option.ofNullable plan.CreationDateTime with | Some time -> time | None -> new System.DateTime())
//                                |> Seq.map(fun plan -> 
//                                    match existingCourse with
//                                    | None -> 
//                                        {
//                                            Id = plan.Id
//                                            IsChecked = false
//                                            bindingid = getPlanBindingId course.Id plan.Id
//                                        }
//                                    | Some existingCourse ->
//                                        // If the plan was already loaded, match it's states, otherwise keep it unchecked
//                                        let existingPlan = existingCourse.Plans |> List.filter (fun p -> p.Id = plan.Id) |> List.tryExactlyOne
//                                        { 
//                                            Id = plan.Id; 
//                                            IsChecked = 
//                                                match existingPlan with
//                                                | Some p -> p.IsChecked
//                                                | None -> false
//                                            bindingid = getPlanBindingId course.Id plan.Id 
//                                        }) 
//                                |> Seq.toList })
//                |> Seq.toList
//            )
//            return LoadCoursesSuccess courses
//        }

//    // Handle any messages
//    let update msg m =
//        match msg with
//        // Eclipse login
//        | Login -> { m with StatusBar = Indeterminate { Status = "Logging in to Eclipse" } }, Cmd.OfAsync.either login (m.Args) id LoginFailed
//        | LoginSuccess patientInfo -> { m with SharedInfo = patientInfo; StatusBar = readyStatus }, Cmd.ofMsg LoadCoursesIntoPatientSetup
//        | LoginFailed x -> 
//            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Login to Eclipse") |> ignore
//            { m with StatusBar = NoLoadingBar "Failed to log in to Eclipse" }, Cmd.none
//        // Initial load of patient plans
//        | LoadCoursesIntoPatientSetup -> { m with StatusBar = Indeterminate { Status = "Loading Plans" } }, Cmd.OfAsync.either loadCoursesIntoPatientSetup (m) id LoadCoursesFailed
//        | LoadCoursesSuccess eclipseCourses -> // Merge any newly loaded plans from Eclipse with existing plans which were loaded via the plugin ScriptContext to create final Courses to be displayed
//            { m with 
//                PatientSetupScreen = 
//                    { m.PatientSetupScreen with 
//                        Courses = eclipseCourses
//                    }; 
//                StatusBar = NoLoadingBar "Ready" 
//            }, Cmd.none
//        | LoadCoursesFailed x ->
//            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Load Course from Eclipse") |> ignore
//            { m with StatusBar = NoLoadingBar "Failed to load courses from Eclipse" }, Cmd.none
//        // Patient Setup Screen
//        | PatientSetupMsg patSetupMsg -> 
//            let (patSetupModel, patSetupCmd, patSetupExtraMsg) = PatientSetupScreen.update patSetupMsg m.PatientSetupScreen
//            match patSetupExtraMsg with
//            | NoMainWindowMsg -> { m with PatientSetupScreen = patSetupModel }, Cmd.none
//            | PatientSetupCompleted options -> { m with PatientSetupScreen = patSetupModel; PatientSetupOptions = options }, Cmd.ofMsg (LoadChecklistScreen options)
//        | LoadChecklistScreen options -> 
//            { m with ChecklistListScreen = 
//                { m.ChecklistListScreen with 
//                    Visibility = Visible;
//                    Plans = options.Plans 
//                    |> List.map(fun plan ->
//                        {
//                            PlanDetails =
//                                {
//                                    PlanId = plan.PlanId;
//                                    CourseId = plan.CourseId
//                                }
//                            Checklists = Checklists.fullChecklist
//                        }
//                    )
//                } }, Cmd.ofMsg Debugton
//        // Checklist Screen
//        | ChecklistMsg _ -> m, Cmd.none

//        | Debugton -> System.Windows.MessageBox.Show(sprintf "Plans:\n%s\n\nOptions:\n%s"
//                                    (m.PatientSetupOptions.Plans |> List.map(fun p -> $"{p.PlanId} ({p.CourseId})") |> String.concat "\n")
//                                    (m.PatientSetupOptions.Toggles |> List.map (fun t -> t.ToString()) |> String.concat "\n")
//                                   )|> ignore; m, Cmd.none

//    // WPF bindings
//    let bindings () : Binding<Model, Msg> list = 
//        [
//            // MainWindow Info
//            "PatientName" |> Binding.oneWay (fun m -> m.SharedInfo.PatientName)
//            "CurrentUser" |> Binding.oneWay (fun m -> m.SharedInfo.CurrentUser)
//            "StatusBarStatus" |> Binding.oneWay (fun m -> match m.StatusBar with | NoLoadingBar status -> status | Indeterminate bar -> bar.Status | Determinate bar -> bar.Status)
//            "StatusBarVisibility" |> Binding.oneWay (fun m -> match m.StatusBar with | NoLoadingBar _ -> Collapsed | _ -> Visible)
//            "StatusBarIsIndeterminate" |> Binding.oneWay (fun m -> match m.StatusBar with | Determinate _ -> false | _ -> true)

//            // Patient Setup Screen
//            "PatientSetupScreen" |> Binding.subModel((fun m -> m.PatientSetupScreen), snd, PatientSetupMsg, PatientSetupScreen.bindings)
            
//            // Checklist Screen
//            "ChecklistScreen" |> Binding.subModel((fun m -> m.ChecklistListScreen), snd, ChecklistMsg, ChecklistScreen.bindings)

//            "Debugton" |> Binding.cmd Debugton
//        ]