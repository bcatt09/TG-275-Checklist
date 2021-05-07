namespace TG275Checklist.Model

open Elmish
open Elmish.WPF

open Model
open PatientSetupTypes
open ChecklistTypes
open ChecklistFunctions
open BaseChecklist
open UpdateFunctions

open type System.Windows.Visibility
open FSharp.Data

module Update =

    let update msg m =
        let log = NLog.LogManager.GetCurrentClassLogger()
        
        match msg with

          /////////////////////////////////////////////////////////
         ///////////////////// Main Window ///////////////////////
        /////////////////////////////////////////////////////////

        // Eclipse login
        | EclipseLogin ->
            { m with 
                StatusBar = StatusBar.indeterminate "Logging in to Eclipse"
            }, Cmd.OfAsync.either loginAsync m.Args id EclipseLoginFailed
        | EclipseLoginSuccess patientInfo -> 
            { m with 
                SharedInfo = patientInfo
                StatusBar = StatusBar.ready 
            }, Cmd.ofMsg LoadCoursesIntoPatientSetup
        | EclipseLoginFailed x -> 
            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Login to Eclipse") |> ignore
            { m with 
                StatusBar = NoLoadingBar "Failed to log in to Eclipse" 
            }, Cmd.none

          /////////////////////////////////////////////////////////
         //////////////// Patient Setup Screen ///////////////////
        /////////////////////////////////////////////////////////

        // Initial load of patient plans IDs from Eclipse
        | LoadCoursesIntoPatientSetup -> 
            { m with 
                StatusBar = StatusBar.indeterminate "Loading Plans"
            }, Cmd.OfAsync.either loadCoursesIntoPatientSetup m id LoadCoursesFailed
        // Merge any newly loaded plans from Eclipse with existing plans which were loaded via the plugin ScriptContext to create final Courses to be displayed
        | LoadCoursesSuccess eclipseCourses ->
            { m with 
                PatientSetupScreenCourses = eclipseCourses
                StatusBar = StatusBar.ready 
            }, Cmd.none
        | LoadCoursesFailed x ->
            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Load Course from Eclipse") |> ignore
            { m with 
                StatusBar = NoLoadingBar "Failed to load courses from Eclipse" 
            }, Cmd.none

        | PatientSetupToggleChanged  (id, ischecked) -> 
            { m with 
                PatientSetupScreenToggles = 
                    m.PatientSetupScreenToggles 
                    |> List.map(fun toggle -> 
                        if id = toggle
                        then 
                            match toggle with
                            | PreviousRT t -> PreviousRT ischecked
                            | Pacemaker t -> Pacemaker ischecked
                            | Registration t -> PatientSetupToggleType.Registration ischecked
                            | FourD t -> FourD ischecked
                            | DIBH t -> DIBH ischecked
                            | IMRT t -> IMRT ischecked
                            | SRS t -> SRS ischecked
                            | SBRT t -> SBRT ischecked
                        else toggle)
            }, Cmd.none
        // Plan checked/unchecked in Patient Setup Screen
        | PatientSetupUsePlanChanged (id, ischecked) -> 
            { m with 
                PatientSetupScreenCourses = 
                    m.PatientSetupScreenCourses 
                    |> List.map(fun course -> 
                        { course with 
                            Plans = course.Plans 
                            |> List.map(fun p -> 
                                if p.bindingId = id 
                                then { p with IsChecked = ischecked } 
                                else p) 
                        })
            }, Cmd.none
        // Course expanded/collapsed in Patient Setup Screen
        | PatientSetupCourseIsExpandedChanged (id, isexpanded) -> 
            { m with 
                PatientSetupScreenCourses = 
                    m.PatientSetupScreenCourses
                    |> List.map (fun c -> 
                        if c.CourseId = id 
                        then { c with IsExpanded = isexpanded } 
                        else c)
            }, Cmd.none
        
          /////////////////////////////////////////////////////////
         ////////////////// Checklist Screen /////////////////////
        /////////////////////////////////////////////////////////
        | DisplayChecklistScreen -> 
            startLog m
            { m with 
                StatusBar = StatusBar.indeterminate "Loading Eclipse Data"
                ChecklistScreenVisibility = Visible
                PatientSetupScreenVisibility = Collapsed
                PatientSetupScreenToggles = m.PatientSetupScreenToggles |> List.filter (fun t -> t.IsChecked)         
                ChecklistScreenPlanChecklists = 
                    [ for c in m.PatientSetupScreenCourses do
                        for p in c.Plans do
                            if p.IsChecked then 
                                yield p ]
                    |> List.map(fun plan ->
                        { PlanChecklist.init with
                            PlanDetails = plan
                            CategoryChecklists = fullChecklist |> createFullChecklistWithAsyncTokens plan })
            }, Cmd.ofMsg UpdateLoadingState
        | UpdateLoadingState ->
            (updateLoadingStates m), Cmd.ofMsg SelectLoadingChecklistItem
        | SelectLoadingChecklistItem ->
            match tryFindLoadingChecklistItem m with
            | Some (plan, cat, item) -> 
                { m with StatusBar = StatusBar.indeterminate $"{plan.PlanDetails.PlanId} - {cat.Category}" }, Cmd.ofMsg (LoadChecklistItem item)
            | None -> m, Cmd.ofMsg LoadingComplete
        | LoadChecklistItem checklistItem ->
            m, Cmd.OfAsync.either loadEsapiResultsAsync checklistItem id LoadChecklistItemFailure
        | LoadChecklistItemSuccess esapiResults ->
            (m |> updateModelWithLoadedEsapiResults esapiResults), Cmd.ofMsg UpdateLoadingState
        | LoadChecklistItemFailure ex ->
            match tryFindLoadingChecklistItem m with
            | Some (_, cat, item) ->
                let func = item.Function.Value.ToString()
                match func.IndexOf('+') with
                | -1 -> log.Error(ex, func)
                | i -> log.Error(ex, func.Substring(i+1))
            | None -> ()
            let failedResult = { EsapiResults.init with Text = EsapiCalls.fail "Error processing results"}
            (m |> updateModelWithLoadedEsapiResults (Some failedResult)), Cmd.ofMsg UpdateLoadingState
        | LoadingComplete ->
            { m with StatusBar = StatusBar.ready }, Cmd.none




            
        // Refresh data maybe? TODO: Check if this actually refreshes anything
        | Debugton -> 
            markAllUnloaded m, Cmd.ofMsg UpdateLoadingState