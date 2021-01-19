namespace TG275Checklist.Model

open Elmish
open Elmish.WPF

open Esapi
open VMS.TPS.Common.Model.API
open type System.Windows.Visibility

// Raw Course/Plan information
type Plan =
    {
        Id: string
    }
type Course =
    {
        Id: string
        Plans: Plan list
    }
// Arguments passed from the Eclipse plugin
type StandaloneApplicationArgs =
    {
        PatientID: string
        Courses: Course list
        OpenedCourseID: string
        OpenedPlanID: string
    }

module ChecklistOptions =
    type SelectedPlan =
        {
            PlanId: string
            CourseId: string
        }
    type ToggleType =
        | PreviousRT
        | Pacemaker
        | Registration
        | FourD
        | DIBH
        | IMRT
        | SRS
        | SBRT
    
    type ChecklistOptions =
        {
            Plans: SelectedPlan list
            Toggles: ToggleType list
        }
    let emptyChecklistOptions = { Plans = []; Toggles = [] }

module PatientSetup =
    open ChecklistOptions
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
    // Patient setup toggles to be used in PatientSetup Screen
    type Toggle =
        {
            Type: ToggleType
            Text: string
            IsChecked: bool
            Id: int
        }
    let private getText toggleType =
        match toggleType with
        | PreviousRT _ -> "Previous RT"
        | Pacemaker _ -> "Pacemaker/ICD"
        | Registration _ -> "Registration"
        | FourD _ -> "4D Simulation"
        | DIBH _ -> "DIBH"
        | IMRT _ -> "IMRT/VMAT"
        | SRS _ -> "SRS"
        | SBRT _ -> "SBRT"
    let toggleList = 
        [
            {
                Type = PreviousRT;
                Text = getText PreviousRT;
                IsChecked = false; 
                Id = 1
            };
            {
                Type = Pacemaker;
                Text = getText Pacemaker;
                IsChecked = false; 
                Id = 2
            };
            {
                Type = Registration;
                Text = getText Registration;
                IsChecked = false; 
                Id = 3
            };
            {
                Type = FourD;
                Text = getText FourD;
                IsChecked = false; 
                Id = 4
            };
            {
                Type = DIBH;
                Text = getText DIBH;
                IsChecked = false; 
                Id = 5
            };
            {
                Type = IMRT;
                Text = getText IMRT;
                IsChecked = false; 
                Id = 6
            };
            {
                Type = SRS;
                Text = getText SRS;
                IsChecked = false; 
                Id = 7
            };
            {
                Type = SBRT;
                Text = getText SBRT;
                IsChecked = false; 
                Id = 8
            };
        ]

    // PatientSetup Model
    type Model =
        {
            Courses: CourseWithOptions list
            Toggles: Toggle list
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
    | ToggleChanged of int * bool
    | UsePlanChanged of string * bool
    | CourseIsExpandedChanged of string * bool
    | NextButtonClicked

    // Messages sent to MainWindow
    type MainWindowMsg =
    | NoMainWindowMsg
    | PatientSetupCompleted of ChecklistOptions

    // Handle Messages
    let update msg (m:Model) =
        match msg with
        | ToggleChanged  (id, ischecked) -> 
            { m with 
                Toggles = m.Toggles 
                |> List.map(fun toggle -> 
                    if toggle.Id = id 
                    then { toggle with IsChecked = ischecked } 
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
        | NextButtonClicked -> { m with Visibility = Collapsed }, Cmd.none, PatientSetupCompleted 
                                                { 
                                                    Plans = 
                                                        [ for c in m.Courses do
                                                            for p in c.Plans do
                                                                if p.IsChecked then 
                                                                    yield { PlanId = p.Id; CourseId = c.Id } ]
                                                    Toggles = m.Toggles |> List.filter (fun t -> t.IsChecked) |> List.map (fun t -> t.Type)            
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
    let toggleBindings () : Binding<Model * Toggle, Msg> list =
        [
            "Text" |> Binding.oneWay (fun (_, f) -> f.Text)
            "IsChecked" |> Binding.twoWay ((fun (_, f:Toggle) -> f.IsChecked), (fun value (_, f:Toggle) -> ToggleChanged (f.Id, value)))
        ]
    let bindings () : Binding<Model, Msg> list =
        [
            "Courses" |> Binding.subModelSeq((fun m -> m.Courses), (fun (c:CourseWithOptions) -> c.Id), courseBindings)
            "PatientSetupToggles" |> Binding.subModelSeq((fun m -> m.Toggles), (fun (t:Toggle) -> t.Id), toggleBindings)
            "PatientSetupCompleted" |> Binding.cmd NextButtonClicked
            "PatientSetupVisibility" |> Binding.oneWay(fun m -> m.Visibility)
        ]

module Checklist =
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
            Description: string
            EsapiText: string option
            //OtherThingsToDisplay1: 'a option
            //OtherThingsToDisplay2: 'a option
        }
    type PlanDetails =
        {
            CourseId: string
            PlanId: string
        }
    type Model =
        {
            PlanDetails: PlanDetails
            Category: ChecklistCategory
            Checklist: ChecklistItem list
            Visibility: System.Windows.Visibility
        }
    let init () =
        {
            PlanDetails = { CourseId = ""; PlanId = "" }
            Category = Prescription
            Checklist = []
            Visibility = Collapsed
        }

    type Msg =
    | Message

    let bindings () : Binding<Model, Msg> list =
        [
            "ChecklistVisibility" |> Binding.oneWay(fun m -> m.Visibility)
        ]

module App =
    open PatientSetup

    // Info displayed by the Main Window
    type SharedInfo =
        {
            PatientName: string
            CurrentUser: string
        }

    type CurrentScreen =
    | PatientSetupScreen
    | ChecklistScreen

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
            CurrentScreen: CurrentScreen
            PatientSetupScreen: PatientSetup.Model
            ChecklistListScreen: Checklist.Model
            ChecklistOptions: ChecklistOptions.ChecklistOptions
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
        | PatientSetupMsg of PatientSetup.Msg
        | LoadChecklistScreen
        | ChecklistMsg of Checklist.Msg


        | Debugton

    // Initial empty model
    let readyStatus = NoLoadingBar "Ready"
    let init (args:StandaloneApplicationArgs) =
        { 
            CurrentScreen = PatientSetupScreen
            PatientSetupScreen = PatientSetup.init(args)
            SharedInfo =
                {
                    PatientName = ""
                    CurrentUser = ""
                }
            ChecklistListScreen = Checklist.init()
            ChecklistOptions = ChecklistOptions.emptyChecklistOptions
            StatusBar = readyStatus
            Args = args
        }, Cmd.ofMsg Login
    
    // Initial Eclipse login function
    let login (args:StandaloneApplicationArgs) =
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
    let loadCoursesIntoPatientSetup (model:Model) =
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
            let (patSetupModel, patSetupCmd, patSetupExtraMsg) = PatientSetup.update patSetupMsg m.PatientSetupScreen
            match patSetupExtraMsg with
            | NoMainWindowMsg -> { m with PatientSetupScreen = patSetupModel }, Cmd.none
            | PatientSetupCompleted options -> { m with PatientSetupScreen = patSetupModel; ChecklistOptions = options }, Cmd.ofMsg LoadChecklistScreen
        | LoadChecklistScreen -> { m with CurrentScreen = ChecklistScreen; ChecklistListScreen = { m.ChecklistListScreen with Visibility = Visible } }, Cmd.ofMsg Debugton
        // Checklist Screen
        | ChecklistMsg _ -> m, Cmd.none

        | Debugton -> System.Windows.MessageBox.Show(sprintf "Plans:\n%s\n\nOptions:\n%s"
                                    (m.ChecklistOptions.Plans |> List.map(fun p -> $"{p.PlanId} ({p.CourseId})") |> String.concat "\n")
                                    (m.ChecklistOptions.Toggles |> List.map (fun t -> t.ToString()) |> String.concat "\n")
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
            "CurrentScreen" |> Binding.oneWay(fun m -> m.CurrentScreen.ToString())

            // Patient Setup Screen
            "PatientSetup" |> Binding.subModel((fun m -> m.PatientSetupScreen), snd, PatientSetupMsg, PatientSetup.bindings)
            
            // Checklist Screen
            "Checklist" |> Binding.subModel((fun m -> m.ChecklistListScreen), snd, ChecklistMsg, Checklist.bindings)

            "Debugton" |> Binding.cmd Debugton
        ]