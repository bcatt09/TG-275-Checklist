namespace TG275Checklist.Model

open Elmish
open Elmish.WPF

open Esapi
open VMS.TPS.Common.Model.API
open type System.Windows.Visibility

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
    let loginAsync args =
        async {
            // Log in to Eclipse and get initial patient info
            do! esapi.LogInAsync()
            do! esapi.OpenPatientAsync(args.PatientID)
            let! patientInfo = esapi.Run(fun (pat : Patient, app : Application) ->
                    {
                        PatientName = pat.Name
                        CurrentUser = app.CurrentUser.Name
                    })
                
            return LoginSuccess patientInfo
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
        | Login -> { m with StatusBar = Indeterminate { Status = "Logging in to Eclipse" } }, Cmd.OfAsync.either loginAsync m.Args id LoginFailed
        | LoginSuccess patientInfo -> { m with SharedInfo = patientInfo; StatusBar = readyStatus }, Cmd.ofMsg LoadCoursesIntoPatientSetup
        | LoginFailed x -> 
            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Login to Eclipse") |> ignore
            { m with StatusBar = NoLoadingBar "Failed to log in to Eclipse" }, Cmd.none
        // Initial load of patient plans
        | LoadCoursesIntoPatientSetup -> { m with StatusBar = Indeterminate { Status = "Loading Plans" } }, Cmd.OfAsync.either loadCoursesIntoPatientSetup m id LoadCoursesFailed
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
            { m with 
                ChecklistListScreen = 
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
                            Checklists = Checklists.fullChecklist |> Checklists.createFullChecklistWithAsyncTokens {PlanId = plan.PlanId; CourseId = plan.CourseId}
                        }
                    )
                } }, Cmd.ofMsg (ChecklistMsg ChecklistScreen.Msg.LoadChecklists)//Cmd.OfAsync.either (ChecklistMsg ChecklistScreen.populateEsapiResults m.ChecklistListScreen.Plans) id ChecklistScreen.Msg.LoadChecklistsFailure //Cmd.ofMsg (ChecklistMsg ChecklistScreen.Msg.LoadChecklists)
        // Checklist Screen
        | ChecklistMsg checklistMsg -> 
            let (checklistModel, checklistCmd) = ChecklistScreen.update checklistMsg m.ChecklistListScreen
            { m with ChecklistListScreen = checklistModel}, (Cmd.map ChecklistMsg checklistCmd)

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