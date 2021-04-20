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

module Update =

    let update msg m =
        match msg with

          /////////////////////////////////////////////////////////
         ///////////////////// Main Window ///////////////////////
        /////////////////////////////////////////////////////////

        // Eclipse login
        | EclipseLogin ->
            { m with 
                StatusBar = indeterminateStatus "Logging in to Eclipse"
            }, Cmd.OfAsync.either loginAsync m.Args id EclipseLoginFailed
        | EclipseLoginSuccess patientInfo -> 
            { m with 
                SharedInfo = patientInfo
                StatusBar = readyStatus 
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
                StatusBar = indeterminateStatus "Loading Plans"
            }, Cmd.OfAsync.either loadCoursesIntoPatientSetup m id LoadCoursesFailed
        // Merge any newly loaded plans from Eclipse with existing plans which were loaded via the plugin ScriptContext to create final Courses to be displayed
        | LoadCoursesSuccess eclipseCourses ->
            { m with 
                PatientSetupScreenCourses = eclipseCourses
                StatusBar = readyStatus 
            }, Cmd.none
        | LoadCoursesFailed x ->
            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Load Course from Eclipse") |> ignore
            { m with 
                StatusBar = NoLoadingBar "Failed to load courses from Eclipse" 
            }, Cmd.none
        | Refresh -> 
            markAllUnloaded m, Cmd.ofMsg PrepToLoadNextChecklist

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

        // Display Checklist Screen
        | DisplayChecklistScreen -> 
            { m with 
                StatusBar = indeterminateStatus "Loading Eclipse Data"
                ChecklistScreenVisibility = Visible
                PatientSetupScreenVisibility = Collapsed
                PatientSetupScreenToggles = m.PatientSetupScreenToggles |> List.filter (fun t -> t.IsChecked)         
                ChecklistScreenPlanChecklists = 
                    [ for c in m.PatientSetupScreenCourses do
                        for p in c.Plans do
                            if p.IsChecked then 
                                yield p ]
                    |> List.map(fun plan ->
                        {
                            PlanDetails = plan
                            CategoryChecklists = fullChecklist |> createFullChecklistWithAsyncTokens plan
                        }
                )
            }, Cmd.ofMsg PrepToLoadNextChecklist
        | PrepToLoadNextChecklist ->
            { m with ChecklistScreenPlanChecklists = markNextUnloadedChecklist m }, Cmd.ofMsg UpdateLoadingMessage
        | UpdateLoadingMessage ->
            match getLoadingChecklist m with
            | None -> 
                { m with 
                    StatusBar = readyStatus
                }, Cmd.none
            | Some loadingList -> 
                { m with 
                    StatusBar = indeterminateStatus $"Loading {loadingList.Category.ToReadableString()}"
                } , Cmd.ofMsg LoadNextChecklist
        | LoadNextChecklist ->
            m, Cmd.OfAsync.either loadNextEsapiResultsAsync m id LoadChecklistFailure
        | LoadChecklistSuccess newPlanChecklists ->
            { m with
                ChecklistScreenPlanChecklists = newPlanChecklists
            }, Cmd.ofMsg PrepToLoadNextChecklist
        | LoadChecklistFailure x -> 
            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Populate Plan Results from Eclipse") |> ignore
            m, Cmd.ofMsg PrepToLoadNextChecklist
        | AllChecklistsLoaded ->
            { m with StatusBar = readyStatus }, Cmd.none

