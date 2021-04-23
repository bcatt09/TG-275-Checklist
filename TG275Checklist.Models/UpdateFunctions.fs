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
                                    | None -> 
                                        {
                                            PlanId = plan.Id
                                            CourseId = course.Id
                                            PatientName = $"{pat.LastName}, {pat.FirstName} ({pat.Id})"
                                            PlanDose = $"{plan.TotalDose} = {plan.DosePerFraction} x {plan.NumberOfFractions} Fx"
                                            Oncologist = oncologistLookup pat.PrimaryOncologistId
                                            IsChecked = false
                                        }
                                    | Some existingCourse ->
                                        // If the plan was already loaded, match it's states, otherwise keep it unchecked
                                        let existingPlan = existingCourse.Plans |> List.filter (fun p -> p.PlanId = plan.Id) |> List.tryExactlyOne
                                        { 
                                            PlanId = plan.Id
                                            CourseId = course.Id
                                            PatientName = $"{pat.LastName}, {pat.FirstName} ({pat.Id})"
                                            PlanDose = $"{plan.TotalDose} = {plan.DosePerFraction} x {plan.NumberOfFractions} Fx"
                                            Oncologist = oncologistLookup pat.PrimaryOncologistId
                                            IsChecked = 
                                                match existingPlan with
                                                | Some p -> p.IsChecked
                                                | None -> false
                                        }) 
                                |> Seq.toList })
                |> Seq.toList
            )
            return LoadCoursesSuccess courses
        }

    // Returns the list of FullChecklists, but with the first instance an an unLoaded CategoryChecklist marked with Loading = true
    let markNextUnloadedChecklist (model: Model) =
        let plans = model.ChecklistScreenPlanChecklists
        // Find which FullChecklist has an unloaded CategoryChecklist first
        match plans |> List.tryFindIndex(fun x -> x.CategoryChecklists |> List.filter(fun y -> not y.Loaded) |> List.length > 0) with
        | Some i -> 
            // Then find the first index of an unloaded CategoryChecklist
            match plans.[i].CategoryChecklists |> List.tryFindIndex(fun x -> not x.Loaded) with
            | Some k -> 
                // Take at the first index of an unloaded CategoryChecklist, and mark it as Loading
                let newMarkedFullChecklist = plans.[i].CategoryChecklists |> List.mapi (fun j x -> if j = k then { x with Loading = true } else x)
                // And return the list of FullChecklists where one of them now has a CategoryChecklist marked as Loading
                plans |> List.mapi (fun j x -> if j = i then { x with CategoryChecklists = newMarkedFullChecklist } else x)
            | None -> plans
        | None -> plans

    // Finds the next CategoryChecklist marked as Loading as populates the data from Eclipse
    let loadNextEsapiResultsAsync (model: Model) =
        async{
            do! Async.SwitchToThreadPool()

            let loadCategoryChecklist (checklist: PlanChecklist) =
                let newChecklist = 
                    checklist.CategoryChecklists
                    |> List.map (fun x -> 
                        if x.Loading
                        then 
                            { x with 
                                Loading = false
                                Loaded = true
                                ChecklistItems = x.ChecklistItems |> List.map(fun y -> { y with EsapiResults = y.AsyncToken |> Async.RunSynchronously 
                            }) }
                        else x)
                { checklist with CategoryChecklists = newChecklist }

            return LoadChecklistSuccess (model.ChecklistScreenPlanChecklists |> List.map (fun p -> loadCategoryChecklist p))
        }

    // Mark all categories as not Loaded to refresh
    let markAllUnloaded (model: Model) =
        let newPlans =
            model.ChecklistScreenPlanChecklists
            |> List.map (fun x -> 
                { x with 
                    CategoryChecklists =
                        x.CategoryChecklists |> List.map (fun y -> { y with Loaded = false })
                })
        { model with ChecklistScreenPlanChecklists = newPlans }

    let getLoadingChecklist (model: Model) =
        model.ChecklistScreenPlanChecklists
        |> List.map (fun p -> p.CategoryChecklists)
        |> List.concat
        |> List.filter (fun cl -> not cl.Loaded)
        |> List.tryHead

    let startLog (model: Model) =
        let log = NLog.LogManager.GetCurrentClassLogger()
        TG275Checklist.Log.Log.Initialize(
            model.SharedInfo.CurrentUser, 
            model.SharedInfo.PatientName, 
            model.PatientSetupScreenCourses 
            |> List.map (fun c -> 
                c.Plans 
                |> List.filter (fun p -> p.IsChecked)
                |> List.map (fun p -> $"{p.PlanId} ({p.CourseId})"))
            |> List.concat)
        log.Info("")