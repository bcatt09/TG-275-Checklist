namespace TG275Checklist.Model

open Elmish
open Elmish.WPF

open Esapi
open Checklists
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

module Model =
    
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
    // Default status bar
    let readyStatus = NoLoadingBar "Ready"

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
    let getChecklistBindingId category = category.ToString()

    type ChecklistSelectedPlan =
        {
            PlanDetails: PlanDetails
            Checklists: CategoryChecklist list
        }
    // TODO: get rid of this or Checklists.PlanDetails this is used in App.fs LoadChecklistScreen Msg
    type PatientSetupOptionsSelectedPlan =
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
    
      ////////////////////////////////////
     /////////// Model //////////////////
    ////////////////////////////////////
    
    type Model =
        { 
            PatientSetupScreenCourses: CourseWithOptions list
            PatientSetupScreenToggles: ToggleType list
            PatientSetupScreenVisibility: System.Windows.Visibility
            ChecklistScreenPlans: ChecklistSelectedPlan list
            ChecklistScreenVisibility: System.Windows.Visibility
            PatientSetupOptionsPlans: PatientSetupOptionsSelectedPlan list
            PatientSetupOptionsToggles: ToggleType list
            SharedInfo: SharedInfo
            StatusBar: StatusBar
            Args: StandaloneApplicationArgs
        }
        

      ////////////////////////////////////
     ///////////// Messages /////////////
    ////////////////////////////////////
    type Msg =
        // Eclipse login
        | Login        
        | LoginSuccess of SharedInfo
        | LoginFailed of exn
        // Patient Setup
        | PatientSetupToggleChanged of ToggleType * bool
        | PatientSetupUsePlanChanged of string * bool
        | PatientSetupCourseIsExpandedChanged of string * bool
        | PatientSetupNextButtonClicked
        | PatientSetupCompleted of Model
        | LoadCoursesIntoPatientSetup
        | LoadCoursesSuccess of CourseWithOptions list
        | LoadCoursesFailed of exn
        | LoadChecklistScreen of Model
        // Checklist
        | LoadChecklists
        | LoadChecklistsSuccess of ChecklistSelectedPlan list
        | LoadChecklistsFailure of exn


        | Debugton
    
      /////////////////////////////////////
     //////// Initial empty model ////////
    /////////////////////////////////////
    let init args =
        { 
            PatientSetupScreenCourses = args.Courses |> List.map (fun c -> 
                { 
                    Id = c.Id; 
                    IsExpanded = c.Id = args.OpenedCourseID; 
                    Plans = c.Plans |> List.map (fun p ->
                        {
                            Id = p.Id;
                            IsChecked = p.Id = args.OpenedPlanID && c.Id = args.OpenedCourseID;
                            bindingid = getPlanBindingId c.Id p.Id 
                        })
                })
            PatientSetupScreenToggles = toggleList
            PatientSetupScreenVisibility = Visible
            SharedInfo =
                {
                    PatientName = ""
                    CurrentUser = ""
                }
            ChecklistScreenPlans = []
            ChecklistScreenVisibility = Collapsed
            PatientSetupOptionsPlans = []
            PatientSetupOptionsToggles = []
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
                    let existingCourse = model.PatientSetupScreenCourses |> List.filter (fun c -> c.Id = course.Id) |> List.tryExactlyOne
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

    let populateEsapiResults (plans: ChecklistSelectedPlan list) =
        async{
            do! Async.SwitchToThreadPool()
            let newPlansWithEsapiResults =
                plans
                |> List.map (fun p ->
                    { p with 
                        Checklists = 
                            p.Checklists
                            |> List.map (fun x -> 
                                let newChecklist = x.Checklist |> List.map(fun y -> { y with EsapiResults = y.AsyncToken |> Async.RunSynchronously})
                                { x with Checklist = newChecklist })})

            return LoadChecklistsSuccess newPlansWithEsapiResults
            //return LoadChecklistsSuccess plans
        }

      ///////////////////////////////////////////
     /////////////// Update ////////////////////
    ///////////////////////////////////////////
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
                PatientSetupScreenCourses = eclipseCourses
                StatusBar = NoLoadingBar "Ready" 
            }, Cmd.none
        | LoadCoursesFailed x ->
            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Load Course from Eclipse") |> ignore
            { m with StatusBar = NoLoadingBar "Failed to load courses from Eclipse" }, Cmd.none

        // Patient Setup Screen
        | LoadChecklistScreen newModel -> 
            { m with 
                StatusBar = Indeterminate { Status = "Loading Eclipse Data" }
                ChecklistScreenVisibility = Visible;
                ChecklistScreenPlans = newModel.PatientSetupOptionsPlans 
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
            }, Cmd.ofMsg LoadChecklists
        | PatientSetupToggleChanged  (id, ischecked) -> 
            { m with 
                PatientSetupOptionsToggles = 
                    m.PatientSetupOptionsToggles 
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
            }, Cmd.none
        | PatientSetupUsePlanChanged (id, ischecked) -> 
            { m with 
                PatientSetupScreenCourses = 
                    m.PatientSetupScreenCourses 
                    |> List.map(fun course -> 
                        { course with 
                            Plans = course.Plans 
                            |> List.map(fun p -> 
                                if p.bindingid = id 
                                then { Id = p.Id; IsChecked = ischecked; bindingid = p.bindingid } 
                                else p) 
                        })
            }, Cmd.none
        | PatientSetupCourseIsExpandedChanged (id, isexpanded) -> 
            { m with 
                PatientSetupScreenCourses = 
                    m.PatientSetupScreenCourses
                    |> List.map (fun c -> if c.Id = id then { c with IsExpanded = isexpanded } else c)
            }, Cmd.none
        | PatientSetupNextButtonClicked -> 
            { m with PatientSetupScreenVisibility = Collapsed }, 
            Cmd.ofMsg (PatientSetupCompleted 
                { m with
                    PatientSetupOptionsPlans = 
                        [ for c in m.PatientSetupScreenCourses do
                            for p in c.Plans do
                                if p.IsChecked then 
                                    yield { PlanId = p.Id; CourseId = c.Id } ]
                    PatientSetupOptionsToggles = m.PatientSetupOptionsToggles |> List.filter (fun t -> t.IsChecked)          
                })
        | PatientSetupCompleted newModel -> 
            newModel, Cmd.ofMsg (LoadChecklistScreen newModel)
        
        // Checklist Screen
        | LoadChecklists -> 
            m, Cmd.OfAsync.either populateEsapiResults m.ChecklistScreenPlans id LoadChecklistsFailure
        | LoadChecklistsSuccess newPlans -> 
            { m with 
                StatusBar = NoLoadingBar "Ready" 
                ChecklistScreenPlans = newPlans 
            }, Cmd.none
        | LoadChecklistsFailure x -> 
            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Populate Plan Results from Eclipse") |> ignore
            { m with StatusBar = NoLoadingBar "Ready" }, Cmd.none

        | Debugton -> System.Windows.MessageBox.Show(sprintf "Plans:\n%s\n\nOptions:\n%s"
                                    (m.PatientSetupOptionsPlans |> List.map(fun p -> $"{p.PlanId} ({p.CourseId})") |> String.concat "\n")
                                    (m.PatientSetupOptionsToggles |> List.map (fun t -> t.ToString()) |> String.concat "\n")
                                    )|> ignore; m, Cmd.none

      //////////////////////////////////////////
     ///////////////// Bindings ///////////////
    //////////////////////////////////////////
    let patientSetupPlanBindings () : Binding<(Model * CourseWithOptions) * PlanWithOptions, Msg> list =
        [
            "Id" |> Binding.oneWay (fun (_, p) -> p.Id)
            "IsChecked" |> Binding.twoWay ((fun (_, p:PlanWithOptions) -> p.IsChecked), (fun value (_, p:PlanWithOptions) -> PatientSetupUsePlanChanged (p.bindingid, value)))
        ]
    let courseBindings () : Binding<Model * CourseWithOptions, Msg> list =
        [
            "Id" |> Binding.oneWay (fun (_, c) -> c.Id)
            "Plans" |> Binding.subModelSeq((fun (_, c) -> c.Plans), (fun (p:PlanWithOptions) -> p.bindingid), patientSetupPlanBindings)
            "IsExpanded" |> Binding.twoWay ((fun (_, c) -> c.IsExpanded), (fun value (_, c:CourseWithOptions) -> PatientSetupCourseIsExpandedChanged (c.Id, value)))
        ]
    let toggleBindings () : Binding<Model * ToggleType, Msg> list =
        [
            "Text" |> Binding.oneWay (fun (_, f) -> f.Text)
            "IsChecked" |> Binding.twoWay ((fun (_, f:ToggleType) -> f.IsChecked), (fun value (_, f:ToggleType) -> PatientSetupToggleChanged (f, value)))
        ]

    let checklistItemBindings () : Binding<((Model * ChecklistSelectedPlan) * CategoryChecklist) * ChecklistItem, Msg> list =
        [
            "Text" |> Binding.oneWay (fun (_, item) -> item.Text)
            "EsapiText" |> Binding.oneWay (fun (_, item) -> match item.EsapiResults with | None -> "" | Some result -> result.Text)
        ]

    let checklistBindings () : Binding<(Model * ChecklistSelectedPlan) * CategoryChecklist, Msg> list =
        [
            "ChecklistItems" |> Binding.subModelSeq((fun (_, checklist) -> checklist.Checklist), (fun (item:ChecklistItem) -> item.Text), checklistItemBindings)
            "Category" |> Binding.oneWay(fun (_, checklist) -> checklist.Category.ToReadableString())
        ]

    let checklistPlanBindings () : Binding<Model * ChecklistSelectedPlan, Msg> list =
        [
            "CourseId" |> Binding.oneWay(fun (_, plan) -> plan.PlanDetails.CourseId)
            "PlanId" |> Binding.oneWay(fun (_, plan) -> plan.PlanDetails.PlanId)
            "Checklists" |> Binding.subModelSeq((fun (_, plan) -> plan.Checklists), (fun (c:CategoryChecklist) -> getChecklistBindingId c.Category), checklistBindings)
        ]
    let bindings () : Binding<Model, Msg> list = 
        [
            // MainWindow Info
            "PatientName" |> Binding.oneWay (fun m -> m.SharedInfo.PatientName)
            "CurrentUser" |> Binding.oneWay (fun m -> m.SharedInfo.CurrentUser)
            "StatusBarStatus" |> Binding.oneWay (fun m -> match m.StatusBar with | NoLoadingBar status -> status | Indeterminate bar -> bar.Status | Determinate bar -> bar.Status)
            "StatusBarVisibility" |> Binding.oneWay (fun m -> match m.StatusBar with | NoLoadingBar _ -> Collapsed | _ -> Visible)
            "StatusBarIsIndeterminate" |> Binding.oneWay (fun m -> match m.StatusBar with | Determinate _ -> false | _ -> true)

            // Patient Setup Screen
            "Courses" |> Binding.subModelSeq((fun m -> m.PatientSetupScreenCourses), (fun (c:CourseWithOptions) -> c.Id), courseBindings)
            "PatientSetupToggles" |> Binding.subModelSeq((fun m -> m.PatientSetupScreenToggles), (fun (t:ToggleType) -> t), toggleBindings)
            "PatientSetupCompleted" |> Binding.cmd PatientSetupNextButtonClicked
            "PatientSetupScreenVisibility" |> Binding.oneWay(fun m -> m.PatientSetupScreenVisibility)
            
            // Checklist Screen
            "Plans" |> Binding.subModelSeq((fun m -> m.ChecklistScreenPlans), (fun (p:ChecklistSelectedPlan) -> getPlanBindingId p.PlanDetails.CourseId p.PlanDetails.PlanId), checklistPlanBindings)
            "ChecklistScreenVisibility" |> Binding.oneWay(fun m -> m.ChecklistScreenVisibility)

            "Debugton" |> Binding.cmd Debugton
        ]