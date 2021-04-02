namespace TG275Checklist.Model

open EsapiService
open Model
open PatientSetupTypes
open ChecklistTypes

open VMS.TPS.Common.Model.API

module UpdateFunctions =

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
                
            return EclipseLoginSuccess patientInfo
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

    // Populate ESAPI results in checklist
    let populateEsapiResultsAsync (plans: FullChecklist list) =
        async{
            do! Async.SwitchToThreadPool()
            let newPlansWithEsapiResults =
                plans
                |> List.map (fun p ->
                    { p with 
                        Checklists = 
                            p.Checklists
                            |> List.map (fun x -> 
                                let newChecklist = x.Checklist |> List.map(fun y -> { y with EsapiResults = y.AsyncToken |> Async.RunSynchronously })
                                { x with Checklist = newChecklist })})

            return LoadChecklistsSuccess newPlansWithEsapiResults
        }

