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
            let! patientInfo = esapi.Run(fun (pat: Patient, app: Application) ->
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
                    let existingCourse = model.PatientSetupScreenCourses |> List.filter (fun c -> c.CourseId = course.Id) |> List.tryExactlyOne
                    { 
                        CourseId = course.Id; 
                        IsExpanded = 
                            match existingCourse with
                            | Some c -> c.IsExpanded
                            | None -> false
                        Plans = course.PlanSetups 
                                |> Seq.sortByDescending(fun plan -> match Option.ofNullable plan.CreationDateTime with | Some time -> time | None -> new System.DateTime())
                                |> Seq.map(fun plan -> 
                                    match existingCourse with
                                    | Some existingCourse ->
                                        // If the plan was already loaded, match it's states, otherwise keep it unchecked
                                        let existingPlan = existingCourse.Plans |> List.filter (fun p -> p.PlanId = plan.Id) |> List.tryExactlyOne
                                        { 
                                            PlanId = plan.Id
                                            CourseId = course.Id
                                            Dose = $"{plan.TotalDose} = {plan.DosePerFraction} x {plan.NumberOfFractions} Fx"
                                            PatientName = $"{pat.LastName}, {pat.FirstName} ({pat.Id})"
                                            Oncologist = "Dr"
                                            IsChecked = 
                                                match existingPlan with
                                                | Some p -> p.IsChecked
                                                | None -> false
                                            bindingid = getPlanBindingId course.Id plan.Id 
                                        }
                                    | None -> 
                                        {
                                            PlanId = plan.Id
                                            CourseId = course.Id
                                            Dose = $"{plan.TotalDose} = {plan.DosePerFraction} x {plan.NumberOfFractions} Fx"
                                            PatientName = $"{pat.LastName}, {pat.FirstName} ({pat.Id})"
                                            Oncologist = "Dr"
                                            IsChecked = false
                                            bindingid = getPlanBindingId course.Id plan.Id
                                        })
                                |> Seq.toList })
                |> Seq.toList
            )
            return LoadCoursesSuccess courses
        }

    // Returns the list of FullChecklists, but with the first instance an an unLoaded CategoryChecklist marked with Loading = true
    let markNextUnloadedChecklist (model: Model) =
        let plans = model.ChecklistScreenPlans
        // Find which FullChecklist has an unloaded CategoryChecklist first
        match plans |> List.tryFindIndex(fun x -> x.Checklists |> List.filter(fun y -> not y.Loaded) |> List.length > 0) with
        | Some i -> 
            // Then find the first index of an unloaded CategoryChecklist
            match plans.[i].Checklists |> List.tryFindIndex(fun x -> not x.Loaded) with
            | Some k -> 
                // Take at the first index of an unloaded CategoryChecklist, and mark it as Loading
                let newMarkedFullChecklist = plans.[i].Checklists |> List.mapi (fun j x -> if j = k then { x with Loading = true } else x)
                // And return the list of FullChecklists where one of them now has a CategoryChecklist marked as Loading
                plans |> List.mapi (fun j x -> if j = i then { x with Checklists = newMarkedFullChecklist } else x)
            | None -> plans
        | None -> plans

    // Finds the next CategoryChecklist marked as Loading as populates the data from Eclipse
    let loadNextEsapiResultsAsync (model: Model) =
        async{
            do! Async.SwitchToThreadPool()

            let loadCategoryChecklist (checklist: PlanChecklist) =
                let newChecklist = 
                    checklist.Checklists
                    |> List.map (fun x -> 
                        if x.Loading
                        then 
                            { x with 
                                Loading = false
                                Loaded = true
                                Checklist = x.Checklist |> List.map(fun y -> { y with EsapiResults = y.AsyncToken |> Async.RunSynchronously 
                            }) }
                        else x)
                { checklist with Checklists = newChecklist }

            return LoadChecklistSuccess (model.ChecklistScreenPlans |> List.map (fun p -> loadCategoryChecklist p))
        }

    // Mark all categories as not Loaded to refresh
    let markAllUnloaded (model: Model) =
        let newPlans =
            model.ChecklistScreenPlans
            |> List.map (fun x -> 
                { x with 
                    Checklists =
                        x.Checklists |> List.map (fun y -> { y with Loaded = false })
                })
        { model with ChecklistScreenPlans = newPlans }

    let getLoadingChecklist (model: Model) =
        model.ChecklistScreenPlans
        |> List.map (fun p -> p.Checklists)
        |> List.concat
        |> List.filter (fun cl -> not cl.Loaded)
        |> List.tryHead







        ////////////////// Stuck on Loading Prescription with no animation