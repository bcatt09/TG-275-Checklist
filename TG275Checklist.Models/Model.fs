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

// Checklist options which are held in the PatientSetup Screen
module PatientSetupOptions =
    type SelectedPlan =
        {
            PlanId: string
            CourseId: string
        }
    type ToggleType =
        | PreviousRT of bool
        | Pacemaker of bool
        | Registration of bool
        | FourD of bool
        | DIBH of bool
        | IMRT of bool
        | SRS of bool
        | SBRT of bool
        member this.IsChecked = 
            match this with
            | PreviousRT value -> value
            | Pacemaker value -> value
            | Registration value -> value
            | FourD value -> value
            | DIBH value -> value
            | IMRT value -> value
            | SRS value -> value
            | SBRT value -> value
        member this.Text =
            match this with
            | PreviousRT _ -> "Previous RT"
            | Pacemaker _ -> "Pacemaker/ICD"
            | Registration _ -> "Registration"
            | FourD _ -> "4D Simulation"
            | DIBH _ -> "DIBH"
            | IMRT _ -> "IMRT/VMAT"
            | SRS _ -> "SRS"
            | SBRT _ -> "SBRT"
    let toggleList = [ PreviousRT false; Pacemaker false; Registration false; FourD false; DIBH false; IMRT false; SRS false; SBRT false ]
    
    type Model =
        {
            Plans: SelectedPlan list
            Toggles: ToggleType list
        }
    let init () = { Plans = []; Toggles = [] }

module PatientSetupScreen =
    open PatientSetupOptions
    // Courses/Plans to be used in PatientSetup Screen
    type PlanWithOptions =
        {
            Id: string
            IsChecked: bool     // Is it checked off to be used in checklists?
            bindingid: string   // Used for subModel bindings
        }
    let getPlanBindingId courseId planId = courseId + "\\" + planId
    type CourseWithOptions =
        {
            Id: string
            Plans: PlanWithOptions list
            IsExpanded: bool    // Is the course expanded (mainly used for initial model)
        }

    // PatientSetup Model
    type Model =
        {
            Courses: CourseWithOptions list
            Toggles: ToggleType list
            Visibility: System.Windows.Visibility
        }

    // Initial model
    let init (args:StandaloneApplicationArgs) =
        {
            Courses = args.Courses |> List.map (fun c -> 
                { 
                    Id = c.Id; 
                    IsExpanded = c.Id = args.OpenedCourseID; 
                    Plans = c.Plans |> List.map (fun p ->
                        {
                            Id = p.Id;
                            IsChecked = p.Id = args.OpenedPlanID;
                            bindingid = getPlanBindingId c.Id p.Id 
                        })
                })
            Toggles = toggleList
            Visibility = Visible
        }
    let initFromCourses args courses = { init(args) with Courses = courses }

    // Internal Messages
    type Msg =
    | ToggleChanged of ToggleType * bool
    | UsePlanChanged of string * bool
    | CourseIsExpandedChanged of string * bool
    | NextButtonClicked

    // Messages sent to MainWindow
    type MainWindowMsg =
    | NoMainWindowMsg
    | PatientSetupCompleted of PatientSetupOptions.Model

    // Handle Messages
    let update msg (m:Model) =
        match msg with
        | ToggleChanged  (id, ischecked) -> 
            { m with 
                Toggles = m.Toggles 
                |> List.map(fun toggle -> 
                    if id = toggle
                    then 
                        match toggle with
                        | PreviousRT t -> PreviousRT ischecked
                        | Pacemaker t -> Pacemaker ischecked
                        | Registration t -> Registration ischecked
                        | FourD t -> FourD ischecked
                        | DIBH t -> DIBH ischecked
                        | IMRT t -> IMRT ischecked
                        | SRS t -> SRS ischecked
                        | SBRT t -> SBRT ischecked
                    else toggle)
            }, Cmd.none, NoMainWindowMsg
        | UsePlanChanged (id, ischecked) -> 
            { m with 
                Courses = m.Courses 
                |> List.map(fun course -> 
                    { course with 
                        Plans = course.Plans 
                        |> List.map(fun p -> 
                            if p.bindingid = id 
                            then { Id = p.Id; IsChecked = ischecked; bindingid = p.bindingid } 
                            else p) 
                    })
            }, Cmd.none, NoMainWindowMsg 
        | CourseIsExpandedChanged (id, isexpanded) -> 
            { m with 
                Courses = m.Courses
                |> List.map (fun c -> if c.Id = id then { c with IsExpanded = isexpanded } else c)
            }, Cmd.none, NoMainWindowMsg 
        | NextButtonClicked -> 
            { m with Visibility = Collapsed }, 
            Cmd.none, 
            PatientSetupCompleted 
                { 
                    Plans = 
                        [ for c in m.Courses do
                            for p in c.Plans do
                                if p.IsChecked then 
                                    yield { PlanId = p.Id; CourseId = c.Id } ]
                    Toggles = m.Toggles |> List.filter (fun t -> t.IsChecked)          
                }

    // WPF Bindings
    let planBindings () : Binding<(Model * CourseWithOptions) * PlanWithOptions, Msg> list =
        [
            "Id" |> Binding.oneWay (fun (_, p) -> p.Id)
            "IsChecked" |> Binding.twoWay ((fun (_, p:PlanWithOptions) -> p.IsChecked), (fun value (_, p:PlanWithOptions) -> UsePlanChanged (p.bindingid, value)))
        ]
    let courseBindings () : Binding<Model * CourseWithOptions, Msg> list =
        [
            "Id" |> Binding.oneWay (fun (_, c) -> c.Id)
            "Plans" |> Binding.subModelSeq((fun (_, c) -> c.Plans), (fun (p:PlanWithOptions) -> p.bindingid), planBindings)
            "IsExpanded" |> Binding.twoWay ((fun (_, c) -> c.IsExpanded), (fun value (_, c:CourseWithOptions) -> CourseIsExpandedChanged (c.Id, value)))
        ]
    let toggleBindings () : Binding<Model * ToggleType, Msg> list =
        [
            "Text" |> Binding.oneWay (fun (_, f) -> f.Text)
            "IsChecked" |> Binding.twoWay ((fun (_, f:ToggleType) -> f.IsChecked), (fun value (_, f:ToggleType) -> ToggleChanged (f, value)))
        ]
    let bindings () : Binding<Model, Msg> list =
        [
            "Courses" |> Binding.subModelSeq((fun m -> m.Courses), (fun (c:CourseWithOptions) -> c.Id), courseBindings)
            "PatientSetupToggles" |> Binding.subModelSeq((fun m -> m.Toggles), (fun (t:ToggleType) -> t), toggleBindings)
            "PatientSetupCompleted" |> Binding.cmd NextButtonClicked
            "PatientSetupScreenVisibility" |> Binding.oneWay(fun m -> m.Visibility)
        ]

module ChecklistScreen =
    type ChecklistCategory =
        | Prescription
        | Simulation
        | Contouring
        | StandardProcedure
        | DoseDistribution
        | Verification
        | Isocenter
        | ImageGuidanceSetup
        | Scheduling
        | Replan
        | Deviations
    type ChecklistItem =
        {
            Text: string
            EsapiText: string option
            //OtherThingsToDisplay1: 'a option
            //OtherThingsToDisplay2: 'a option
        }
    type Checklist =
        {
            Category: ChecklistCategory
            Checklist: ChecklistItem list
        }
    let exampleChecklist = { Category = Prescription; Checklist = [ { Text = "LOOK AT THE PLAN"; EsapiText = None } ] }
    let getChecklistBindingId category = category.ToString()
    type PlanDetails =
        {
            CourseId: string
            PlanId: string
            // Dose
            // Approvals
        }
    type SelectedPlan =
        {
            PlanDetails: PlanDetails
            Checklists: Checklist list
        }
    type Model =
        {
            Plans: SelectedPlan list
            Visibility: System.Windows.Visibility
        }
    let init () =
        {
            Plans = []
            Visibility = Collapsed
        }
    let initChecklist () = 
        {
            Category = Prescription;
            Checklist = [{Text="LOOK AT THE PLAN";EsapiText=Some "DONE AND DONE"}]
        }
        
    //let init () =
    //    {
    //        PlanDetails = { CourseId = ""; PlanId = "" }
    //        Category = Prescription
    //        Checklist = [{Text="LOOK AT THE PLAN";EsapiText=None}]
    //        Visibility = Collapsed
    //    }

    type Msg =
    | Message


    let checklistItemBindings () : Binding<((Model * SelectedPlan) * Checklist) * ChecklistItem, Msg> list =
        [
            "Text" |> Binding.oneWay (fun (_, item) -> item.Text)
            "EsapiText" |> Binding.oneWay (fun (_, item) -> item.EsapiText)
        ]

    let checklistBindings () : Binding<(Model * SelectedPlan) * Checklist, Msg> list =
        [
            "ChecklistItems" |> Binding.subModelSeq((fun (_, checklist) -> checklist.Checklist), (fun (item:ChecklistItem) -> item.Text), checklistItemBindings)
            "Category" |> Binding.oneWay(fun (_, checklist) -> checklist.Category.ToString())
        ]

    let planBindings () : Binding<Model * SelectedPlan, Msg> list =
        [
            "CourseId" |> Binding.oneWay(fun (_, plan) -> plan.PlanDetails.CourseId)
            "PlanId" |> Binding.oneWay(fun (_, plan) -> plan.PlanDetails.PlanId)
            "Checklists" |> Binding.subModelSeq((fun (_, plan) -> plan.Checklists), (fun (c:Checklist) -> getChecklistBindingId c.Category), checklistBindings)
        ]

    let bindings () : Binding<Model, Msg> list =
        [
            "Plans" |> Binding.subModelSeq((fun m -> m.Plans), (fun (p:SelectedPlan) -> PatientSetupScreen.getPlanBindingId p.PlanDetails.CourseId p.PlanDetails.PlanId), planBindings)
            "ChecklistScreenVisibility" |> Binding.oneWay(fun m -> m.Visibility)
        ]

module App =
    open PatientSetupScreen

    // Info displayed by the Main Window
    type SharedInfo =
        {
            PatientName: string
            CurrentUser: string
        }

    // Status Bar at bottom of window
    type StatusBar =
    | NoLoadingBar of string
    | Indeterminate of IndeterminateStatusBar
    | Determinate of DeterminateStatusBar
    and IndeterminateStatusBar = { Status: string }
    and DeterminateStatusBar = { Status: string; min: int; max: int }

    // Main Model
    type Model =
        { 
            PatientSetupScreen: PatientSetupScreen.Model
            ChecklistListScreen: ChecklistScreen.Model
            PatientSetupOptions: PatientSetupOptions.Model
            SharedInfo: SharedInfo
            StatusBar: StatusBar
            Args: StandaloneApplicationArgs
        }

    // Messages
    type Msg =
        // Eclipse login
        | Login        
        | LoginSuccess of SharedInfo
        | LoginFailed of exn
        // Patient Setup
        | LoadCoursesIntoPatientSetup
        | LoadCoursesSuccess of CourseWithOptions list
        | LoadCoursesFailed of exn
        | PatientSetupMsg of PatientSetupScreen.Msg
        | LoadChecklistScreen of PatientSetupOptions.Model
        // Checklist
        | ChecklistMsg of ChecklistScreen.Msg


        | Debugton

    // Default status bar
    let readyStatus = NoLoadingBar "Ready"

    // Initial empty model
    let init args =
        { 
            PatientSetupScreen = PatientSetupScreen.init(args)
            SharedInfo =
                {
                    PatientName = ""
                    CurrentUser = ""
                }
            ChecklistListScreen = ChecklistScreen.init()
            PatientSetupOptions = PatientSetupOptions.init()
            StatusBar = readyStatus
            Args = args
        }, Cmd.ofMsg Login
    
    // Initial Eclipse login function
    let login args =
        async {
            // Log in to Eclipse and get initial patient info
            try
                do! esapi.LogInAsync()
                do! esapi.OpenPatientAsync(args.PatientID)
                let! patientInfo = esapi.Run(fun (pat : Patient, app : Application) ->
                        {
                            PatientName = pat.Name
                            CurrentUser = app.CurrentUser.Name
                        })
                
                return LoginSuccess patientInfo
            with ex -> return LoginFailed ex
        }

    // Load courses/plans from Eclipse
    let loadCoursesIntoPatientSetup model =
        async {
            let! courses = esapi.Run(fun (pat : Patient) ->
                pat.Courses 
                |> Seq.sortByDescending(fun course -> match Option.ofNullable course.StartDateTime with | Some time -> time | None -> new System.DateTime())
                |> Seq.map (fun course -> 
                    // If the course was already loaded, match its states, otherwise keep it collapsed
                    let existingCourse = model.PatientSetupScreen.Courses |> List.filter (fun c -> c.Id = course.Id) |> List.tryExactlyOne
                    { 
                        Id = course.Id; 
                        IsExpanded = 
                            match existingCourse with
                            | Some c -> c.IsExpanded
                            | None -> false
                        Plans = course.PlanSetups 
                                |> Seq.sortByDescending(fun plan -> match Option.ofNullable plan.CreationDateTime with | Some time -> time | None -> new System.DateTime())
                                |> Seq.map(fun plan -> 
                                    match existingCourse with
                                    | None -> 
                                        {
                                            Id = plan.Id
                                            IsChecked = false
                                            bindingid = getPlanBindingId course.Id plan.Id
                                        }
                                    | Some existingCourse ->
                                        // If the plan was already loaded, match it's states, otherwise keep it unchecked
                                        let existingPlan = existingCourse.Plans |> List.filter (fun p -> p.Id = plan.Id) |> List.tryExactlyOne
                                        { 
                                            Id = plan.Id; 
                                            IsChecked = 
                                                match existingPlan with
                                                | Some p -> p.IsChecked
                                                | None -> false
                                            bindingid = getPlanBindingId course.Id plan.Id 
                                        }) 
                                |> Seq.toList })
                |> Seq.toList
            )
            return LoadCoursesSuccess courses
        }

    // Handle any messages
    let update msg m =
        match msg with
        // Eclipse login
        | Login -> { m with StatusBar = Indeterminate { Status = "Logging in to Eclipse" } }, Cmd.OfAsync.either login (m.Args) id LoginFailed
        | LoginSuccess patientInfo -> { m with SharedInfo = patientInfo; StatusBar = readyStatus }, Cmd.ofMsg LoadCoursesIntoPatientSetup
        | LoginFailed x -> 
            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Login to Eclipse") |> ignore
            { m with StatusBar = NoLoadingBar "Failed to log in to Eclipse" }, Cmd.none
        // Initial load of patient plans
        | LoadCoursesIntoPatientSetup -> { m with StatusBar = Indeterminate { Status = "Loading Plans" } }, Cmd.OfAsync.either loadCoursesIntoPatientSetup (m) id LoadCoursesFailed
        | LoadCoursesSuccess eclipseCourses -> // Merge any newly loaded plans from Eclipse with existing plans which were loaded via the plugin ScriptContext to create final Courses to be displayed
            { m with 
                PatientSetupScreen = 
                    { m.PatientSetupScreen with 
                        Courses = eclipseCourses
                    }; 
                StatusBar = NoLoadingBar "Ready" 
            }, Cmd.none
        | LoadCoursesFailed x ->
            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Load Course from Eclipse") |> ignore
            { m with StatusBar = NoLoadingBar "Failed to load courses from Eclipse" }, Cmd.none
        // Patient Setup Screen
        | PatientSetupMsg patSetupMsg -> 
            let (patSetupModel, patSetupCmd, patSetupExtraMsg) = PatientSetupScreen.update patSetupMsg m.PatientSetupScreen
            match patSetupExtraMsg with
            | NoMainWindowMsg -> { m with PatientSetupScreen = patSetupModel }, Cmd.none
            | PatientSetupCompleted options -> { m with PatientSetupScreen = patSetupModel; PatientSetupOptions = options }, Cmd.ofMsg (LoadChecklistScreen options)
        | LoadChecklistScreen options -> 
            { m with ChecklistListScreen = 
                { m.ChecklistListScreen with 
                    Visibility = Visible;
                    Plans = options.Plans 
                    |> List.map(fun plan ->
                        {
                            PlanDetails =
                                {
                                    PlanId = plan.PlanId;
                                    CourseId = plan.CourseId
                                }
                            Checklists = [ChecklistScreen.initChecklist()]
                        }
                    )
                } }, Cmd.ofMsg Debugton
        // Checklist Screen
        | ChecklistMsg _ -> m, Cmd.none

        | Debugton -> System.Windows.MessageBox.Show(sprintf "Plans:\n%s\n\nOptions:\n%s"
                                    (m.PatientSetupOptions.Plans |> List.map(fun p -> $"{p.PlanId} ({p.CourseId})") |> String.concat "\n")
                                    (m.PatientSetupOptions.Toggles |> List.map (fun t -> t.ToString()) |> String.concat "\n")
                                   )|> ignore; m, Cmd.none

    // WPF bindings
    let bindings () : Binding<Model, Msg> list = 
        [
            // MainWindow Info
            "PatientName" |> Binding.oneWay (fun m -> m.SharedInfo.PatientName)
            "CurrentUser" |> Binding.oneWay (fun m -> m.SharedInfo.CurrentUser)
            "StatusBarStatus" |> Binding.oneWay (fun m -> match m.StatusBar with | NoLoadingBar status -> status | Indeterminate bar -> bar.Status | Determinate bar -> bar.Status)
            "StatusBarVisibility" |> Binding.oneWay (fun m -> match m.StatusBar with | NoLoadingBar _ -> Collapsed | _ -> Visible)
            "StatusBarIsIndeterminate" |> Binding.oneWay (fun m -> match m.StatusBar with | Determinate _ -> false | _ -> true)

            // Patient Setup Screen
            "PatientSetupScreen" |> Binding.subModel((fun m -> m.PatientSetupScreen), snd, PatientSetupMsg, PatientSetupScreen.bindings)
            
            // Checklist Screen
            "ChecklistScreen" |> Binding.subModel((fun m -> m.ChecklistListScreen), snd, ChecklistMsg, ChecklistScreen.bindings)

            "Debugton" |> Binding.cmd Debugton
        ]