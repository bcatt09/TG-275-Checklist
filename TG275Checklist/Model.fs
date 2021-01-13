namespace TG275Checklist

open Elmish
open Elmish.WPF

open Esapi
open TG275Checklist.Views
open VMS.TPS.Common.Model.API

module ChecklistOptions =
    type Plan =
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
            Plans: Plan list
            Toggles: ToggleType list
        }
    let emptyChecklistOptions = { Plans = []; Toggles = [] }

module PatientSetup =
    open ChecklistOptions
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

    // Courses/Plans to be used in PatientSetup Screen
    type Plan =
        {
            bindingid: string   // used for subModel bindings
            Id: string
            IsChecked: bool       // is it checked off to be used in checklists?
        }
    type Course =
        {
            Id: string
            Plans: Plan list
        }

    // PatientSetup Model
    type Model =
        {
            Courses: Course list
            Toggles: Toggle list
        }

    // Initial model
    let init () =
        {
            Courses = []
            Toggles = toggleList
        }
    let initFromCourses (courses) = { init() with Courses = courses }

    // Internal Messages
    type Msg =
    | ToggleChanged of int * bool
    | UsePlanChanged of string * bool
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
        | NextButtonClicked -> m, Cmd.none, PatientSetupCompleted 
                                                { 
                                                    Plans = 
                                                        [ for c in m.Courses do
                                                            for p in c.Plans do
                                                                if p.IsChecked then 
                                                                    yield { PlanId = p.Id; CourseId = c.Id } ]
                                                    Toggles = m.Toggles |> List.filter (fun t -> t.IsChecked) |> List.map (fun t -> t.Type)            
                                                }

    // WPF Bindings
    let planBindings () : Binding<(Model * Course) * Plan, Msg> list =
        [
            "Id" |> Binding.oneWay (fun (_, p) -> p.Id)
            "IsChecked" |> Binding.twoWay ((fun (_, p:Plan) -> p.IsChecked), (fun value (_, p:Plan) -> UsePlanChanged (p.bindingid, value)))
        ]
    let courseBindings () : Binding<Model * Course, Msg> list =
        [
            "Id" |> Binding.oneWay (fun (_, c) -> c.Id)
            "Plans" |> Binding.subModelSeq((fun (_, c) -> c.Plans), (fun (p:Plan) -> p.bindingid), planBindings)
        ]
    let toggleBindings () : Binding<Model * Toggle, Msg> list =
        [
            "Text" |> Binding.oneWay (fun (_, f) -> f.Text)
            "IsChecked" |> Binding.twoWay ((fun (_, f:Toggle) -> f.IsChecked), (fun value (_, f:Toggle) -> ToggleChanged (f.Id, value)))
        ]
    let bindings () : Binding<Model, Msg> list =
        [
            "Courses" |> Binding.subModelSeq((fun m -> m.Courses), (fun (c:Course) -> c.Id), courseBindings)
            "PatientSetupToggles" |> Binding.subModelSeq((fun m -> m.Toggles), (fun (t:Toggle) -> t.Id), toggleBindings)
            "PatientSetupCompleted" |> Binding.cmd NextButtonClicked
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
        }

module App =
    open PatientSetup
    // Arguments passed from the Eclipse plugin
    type StandaloneApplicationArgs =
        {
            PatientID: string
            CourseID: string
            PlanID: string
        }

    // Info displayed by the Main Window
    type SharedInfo =
        {
            PatientName: string
            CurrentUser: string
        }

    // Status Bar at bottom of window
    type StatusBar =
    | None of string
    | Indeterminate of IndeterminateStatusBar
    | Determinate of DeterminateStatusBar
    and IndeterminateStatusBar = { Status: string }
    and DeterminateStatusBar = { Status: string; min: int; max: int }

    // Main Model
    type Model =
        { 
            PatientSetupScreen: PatientSetup.Model
            ChecklistList: Checklist.Model list
            ChecklistOptions: ChecklistOptions.ChecklistOptions
            SharedInfo: SharedInfo
            MainWindow: MainWindow
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
        | LoadCoursesSuccess of Course list
        | LoadCoursesFailed of exn
        | PatientSetupMsg of PatientSetup.Msg
        | LoadChecklistScreen


        | Debugton

    // Initial empty model
    let readyStatus = None "Ready"
    let init (window:MainWindow) (args:StandaloneApplicationArgs) =
        { 
            PatientSetupScreen = PatientSetup.init()
            SharedInfo =
                {
                    PatientName = ""
                    CurrentUser = ""
                }
            ChecklistList = []
            ChecklistOptions = ChecklistOptions.emptyChecklistOptions
            MainWindow = window
            StatusBar = readyStatus
            Args = args
        }, Cmd.ofMsg Login
    
    // Initial Eclipse login function
    let login (window:MainWindow, args:StandaloneApplicationArgs) =
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

                // Dispose of the Esapi service and Application to prevent crashing
                window.Closed.AddHandler(fun _ _ -> esapi.Dispose())
                
                return LoginSuccess patientInfo
            with ex -> return LoginFailed ex
        }

    let loadCoursesIntoPatientSetup () =
        async {
            let! courses = esapi.Run(fun (pat : Patient) ->
                pat.Courses 
                |> Seq.map (fun c -> { Id = c.Id; Plans = c.PlanSetups |> Seq.map(fun p -> { Id = p.Id; IsChecked = false; bindingid = c.Id + p.Id }) |> Seq.toList })
                |> Seq.toList
            )
            return LoadCoursesSuccess courses
        }
        

    // Handle any messages
    let update msg m =
        match msg with
        // Eclipse login
        | Login -> { m with StatusBar = Indeterminate { Status = "Logging in to Eclipse" } }, Cmd.OfAsync.either login (m.MainWindow, m.Args) id LoginFailed
        | LoginSuccess patientInfo -> { m with SharedInfo = patientInfo; StatusBar = readyStatus }, Cmd.ofMsg LoadCoursesIntoPatientSetup
        | LoginFailed x -> 
            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Login to Eclipse") |> ignore
            { m with StatusBar = None "Failed to log in to Eclipse" }, Cmd.none
        // Initial load of patient plans
        | LoadCoursesIntoPatientSetup -> { m with StatusBar = Indeterminate { Status = "Loading Plans" } }, Cmd.OfAsync.either loadCoursesIntoPatientSetup () id LoadCoursesFailed
        | LoadCoursesSuccess courses -> { m with PatientSetupScreen = initFromCourses(courses); StatusBar = None "Ready" }, Cmd.none
        | LoadCoursesFailed x ->
            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Load Course from Eclips") |> ignore
            { m with StatusBar = None "Failed to load courses from Eclipse" }, Cmd.none
        // Patient Setup Screen
        | PatientSetupMsg patSetupMsg -> 
            let (patSetupModel, patSetupCmd, patSetupExtraMsg) = PatientSetup.update patSetupMsg m.PatientSetupScreen
            match patSetupExtraMsg with
            | NoMainWindowMsg -> { m with PatientSetupScreen = patSetupModel }, Cmd.none
            | PatientSetupCompleted options -> { m with PatientSetupScreen = patSetupModel; ChecklistOptions = options }, Cmd.ofMsg LoadChecklistScreen
        | LoadChecklistScreen -> m, Cmd.ofMsg Debugton

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
            "StatusBarStatus" |> Binding.oneWay (fun m -> match m.StatusBar with | None status -> status | Indeterminate bar -> bar.Status | Determinate bar -> bar.Status)
            "StatusBarVisibility" |> Binding.oneWay (fun m -> match m.StatusBar with | None _ -> System.Windows.Visibility.Collapsed | _ -> System.Windows.Visibility.Visible)
            "StatusBarIsIndeterminate" |> Binding.oneWay (fun m -> match m.StatusBar with | Determinate _ -> false | _ -> true)

            // Patient Setup Screen
            "PatientSetup" |> Binding.subModel((fun m -> m.PatientSetupScreen), snd, PatientSetupMsg, PatientSetup.bindings)

            "Debugton" |> Binding.cmd Debugton
        ]