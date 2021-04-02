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
                StatusBar = Indeterminate { Status = "Logging in to Eclipse" } 
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
                StatusBar = Indeterminate { Status = "Loading Plans" } 
            }, Cmd.OfAsync.either loadCoursesIntoPatientSetup m id LoadCoursesFailed
        // Merge any newly loaded plans from Eclipse with existing plans which were loaded via the plugin ScriptContext to create final Courses to be displayed
        | LoadCoursesSuccess eclipseCourses ->
            { m with 
                PatientSetupScreenCourses = eclipseCourses
                StatusBar = NoLoadingBar "Ready" 
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
                                if p.bindingid = id 
                                then { Id = p.Id; IsChecked = ischecked; bindingid = p.bindingid } 
                                else p) 
                        })
            }, Cmd.none
        // Course expanded/collapsed in Patient Setup Screen
        | PatientSetupCourseIsExpandedChanged (id, isexpanded) -> 
            { m with 
                PatientSetupScreenCourses = 
                    m.PatientSetupScreenCourses
                    |> List.map (fun c -> if c.Id = id then { c with IsExpanded = isexpanded } else c)
            }, Cmd.none
        
          /////////////////////////////////////////////////////////
         ////////////////// Checklist Screen /////////////////////
        /////////////////////////////////////////////////////////

        // Display Checklist Screen
        | DisplayChecklistScreen -> 
            { m with 
                StatusBar = Indeterminate { Status = "Loading Eclipse Data" }
                ChecklistScreenVisibility = Visible
                PatientSetupScreenVisibility = Collapsed
                PatientSetupScreenToggles = m.PatientSetupScreenToggles |> List.filter (fun t -> t.IsChecked)         
                ChecklistScreenPlans = 
                [ for c in m.PatientSetupScreenCourses do
                    for p in c.Plans do
                        if p.IsChecked then 
                            yield { PlanId = p.Id; CourseId = c.Id } ]
                |> List.map(fun plan ->
                    {
                        PlanDetails =
                            {
                                PlanId = plan.PlanId;
                                CourseId = plan.CourseId
                            }
                        Checklists = fullChecklist |> createFullChecklistWithAsyncTokens {PlanId = plan.PlanId; CourseId = plan.CourseId}
                    }
                )
            }, Cmd.ofMsg LoadChecklists

        // Populate all data from Eclipse
        | LoadChecklists -> 
            m, Cmd.OfAsync.either populateEsapiResultsAsync m.ChecklistScreenPlans id LoadChecklistsFailure
        | LoadChecklistsSuccess newPlans -> 
            { m with 
                StatusBar = NoLoadingBar "Ready" 
                ChecklistScreenPlans = newPlans 
            }, Cmd.none
        | LoadChecklistsFailure x -> 
            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Populate Plan Results from Eclipse") |> ignore
            { m with StatusBar = NoLoadingBar "Ready" }, Cmd.none

        | Debugton -> System.Windows.MessageBox.Show(sprintf "Plans:\n%s\n\nOptions:\n%s"
                                    (m.ChecklistScreenPlans |> List.map(fun p -> $"{p.PlanDetails.PlanId} ({p.PlanDetails.CourseId})") |> String.concat "\n")
                                    (m.PatientSetupScreenToggles |> List.map (fun t -> t.ToString()) |> String.concat "\n")
                                    )|> ignore; m, Cmd.none

