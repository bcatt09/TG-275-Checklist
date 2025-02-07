﻿namespace TG275Checklist.Model

open EsapiService
open Model
open PatientSetupTypes
open ChecklistTypes

open TG275Checklist.Sql
open TG275Checklist.Log
open FSharp.Data

open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types

module UpdateFunctions =

    /// <summary>
    /// Log into Eclipse and open patient
    /// </summary>
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

    /// <summary>
    /// Load course/plan list from Eclipse
    /// </summary>
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
                                    let oncologist = 
                                        // Try to get primary oncologist full display name from database
                                        match sqlGetOncologist plan.Course.Patient.Id with
                                        | Error _ -> 
                                            try $"Rx {plan.RTPrescription.Status} by {plan.RTPrescription.HistoryUserDisplayName}"
                                            with _ -> 
                                                try $"RT Plan {plan.ApprovalHistory |> Seq.map(fun x -> x.ApprovalStatus) |> Seq.head} by {plan.ApprovalHistory |> Seq.map(fun x -> x.UserDisplayName) |> Seq.head}"
                                                with _ ->
                                                    ""
                                        | Ok result -> result
                                    match existingCourse with
                                    | None -> 
                                        {
                                            PlanId = plan.Id
                                            CourseId = course.Id
                                            PatientName = $"{pat.LastName}, {pat.FirstName} ({pat.Id})"
                                            PlanDose = $"{plan.TotalDose} = {plan.DosePerFraction} x {plan.NumberOfFractions} Fx"
                                            Oncologist = oncologist
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
                                            Oncologist = oncologist
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

    /// <summary>
    /// Run the associated ESAPI function and return the success Msg
    /// </summary>
    let loadEsapiResultsAsync (checklistItem: ChecklistItem) =
        async{
            do! Async.SwitchToThreadPool()

            return LoadChecklistItemSuccess (checklistItem.AsyncToken |> Async.RunSynchronously)
        }
        
    /// <summary>
    /// Mark the first unloaded ChecklistItem within a CategoryChecklist as Loading
    /// </summary>
    let updateLoadingStatesWithinCategoryChecklist(cat: CategoryChecklist) =
        match cat.ChecklistItems |> List.tryFindIndex(fun item -> not item.Loaded) with
        | None -> cat
        | Some unloadedItemIndex ->
        let newChecklistItems =
            cat.ChecklistItems
            |> List.mapi(fun i item ->
                if i <> unloadedItemIndex
                then item
                else    // if i = unloadedItemIndex mark it as 'Loading'
                    { item with Loading = true })
        // Mark the category as 'Loading' and add the newly updated ChecklistItems
        { cat with
            Loading = true
            ChecklistItems = newChecklistItems }
            
    /// <summary>
    /// Look for first unloaded ChecklistItem within a PlanChecklist's CategoryChecklists and mark it as Loading
    /// </summary>
    let updateLoadingStatesWithinPlanChecklist (plan: PlanChecklist) =
        match plan.CategoryChecklists |> List.tryFindIndex(fun cat -> not cat.Loaded) with
        | None -> plan
        | Some unloadedCategoryIndex ->
            let newCategoryChecklists =
                plan.CategoryChecklists
                |> List.mapi(fun i cat ->
                    if i <> unloadedCategoryIndex
                    then cat
                    else    // if i = unloadedCategoryIndex we will repeat search for ChecklistItems
                        updateLoadingStatesWithinCategoryChecklist cat)
            // Mark the plan as 'Loading' and add the newly updated CategoryChecklists
            { plan with
                Loading = true
                CategoryChecklists = newCategoryChecklists }

    /// <summary>
    /// Look for first unloaded ChecklistItem within the selected plans and mark it as Loading
    /// </summary>
    let updateLoadingStates (model: Model) =
        match model.ChecklistScreenPlanChecklists |> List.tryFindIndex(fun plan -> not plan.Loaded) with
        | None -> model
        | Some unloadedPlanIndex -> 
            let newPlanChecklists =
                model.ChecklistScreenPlanChecklists
                |> List.mapi(fun i plan ->
                    if i <> unloadedPlanIndex
                    then plan
                    else    // if i = unloadedPlanIndex we will repeat search for CategoryChecklists
                        updateLoadingStatesWithinPlanChecklist plan)
            // Update with the newly updated PlanChecklists
            { model with
                ChecklistScreenPlanChecklists = newPlanChecklists }

    /// <summary>
    /// Find the currently Loading ChecklistItem within the CategoryChecklist and update its EsapiResults
    /// </summary>
    let updateCategoryChecklistWithLoadedEsapiResults (results: EsapiResults option) (cat: CategoryChecklist) =
        let newChecklistItems =
            cat.ChecklistItems
            |> List.map(fun item ->
                // Find current 'Loading' ChecklistItem and replace it with the newly loaded item
                if item.Loading
                then { item with 
                        EsapiResults = results
                        Loaded = true
                        Loading = false }
                else item)
        // Then build new CategoryChecklists with this new state of ChecklistItems
        { cat with 
            ChecklistItems = newChecklistItems
            Loaded = (newChecklistItems |> List.sumBy(fun x -> if not x.Loaded then 1 else 0)) = 0
            Loading = (newChecklistItems |> List.sumBy(fun x -> if not x.Loaded then 1 else 0)) <> 0 }
            
    /// <summary>
    /// Find the currently Loading ChecklistItem within the PlanChecklist and update its EsapiResults
    /// </summary>
    let updatePlanChecklistWithLoadedEsapiResults (results: EsapiResults option) (plan: PlanChecklist) =
        let newCategoryChecklists =
            plan.CategoryChecklists
            |> List.map(fun cat -> cat |> updateCategoryChecklistWithLoadedEsapiResults results)
        // Then build new PlanChecklists with this new state of CategoryChecklists
        { plan with 
            CategoryChecklists = newCategoryChecklists
            Loaded = (newCategoryChecklists |> List.sumBy(fun x -> if not x.Loaded then 1 else 0)) = 0
            Loading = (newCategoryChecklists |> List.sumBy(fun x -> if not x.Loaded then 1 else 0)) <> 0 }

    /// <summary>
    /// Find the currently Loading ChecklistItem within the Model and update its EsapiResults
    /// </summary>
    let updateModelWithLoadedEsapiResults (results: EsapiResults option) (model: Model) =
        let newPlanChecklists =
            model.ChecklistScreenPlanChecklists
            |> List.map(fun plan -> plan |> updatePlanChecklistWithLoadedEsapiResults results)
        // Then build new Model with this new state of PlanChecklists
        { model with ChecklistScreenPlanChecklists = newPlanChecklists }

    /// <summary>
    /// Find the currently Loading PlanChecklist, CategoryChecklist, and ChecklistItem
    /// </summary>
    let tryFindLoadingChecklistItem (model: Model) =
        model.ChecklistScreenPlanChecklists
        |> List.filter(fun x -> x.Loading)
        |> List.map(fun plan ->
            plan.CategoryChecklists
            |> List.filter(fun x -> x.Loading)
            |> List.map(fun cat ->
                cat.ChecklistItems
                |> List.filter(fun x -> x.Loading)
                |> List.map(fun item ->
                    plan, cat, item)))
        |> List.concat
        |> List.concat
        |> List.tryHead
        
    /// <summary>
    /// Mark all categories as not Loaded to refresh
    /// </summary>
    let markAllUnloaded (model: Model) =
        let newPlans =
            model.ChecklistScreenPlanChecklists
            |> List.map (fun x -> 
                { x with 
                    CategoryChecklists =
                        x.CategoryChecklists |> List.map (fun y -> { y with Loaded = false })
                })
        { model with ChecklistScreenPlanChecklists = newPlans }
        
    /// <summary>
    /// Initialize logger
    /// </summary>
    let startLog (model: Model) =
        try
            let log = PVH_Logger.Logger
            log.Initialize(
                "Physics-Check",
                model.SharedInfo.CurrentUser, 
                model.SharedInfo.PatientName, 
                model.PatientSetupScreenCourses 
                |> List.map (fun c -> 
                    c.Plans 
                    |> List.filter (fun p -> p.IsChecked)
                    |> List.map (fun p -> $"{p.PlanId} ({p.CourseId})"))
                |> List.concat
                |> String.concat ",",
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location))
            log.Log()
        with ex ->
            System.Windows.MessageBox.Show("Couldn't initialize log") |> ignore

    /// <summary>
    /// Load a new model with the target coverage statistics for the SelectedTarget being displayed
    /// </summary>
    let loadTargetCoverage (model: Model, planChecklist: PlanChecklist, categoryChecklist: CategoryChecklist, checklistItem: ChecklistItem, value) =
        let targetCoverage = { checklistItem.EsapiResults.Value.TargetCoverageDropdown.Value with SelectedTarget = value }
        let newTargetCoverage = { targetCoverage with DisplayedResults = targetCoverage.Results.[targetCoverage.SelectedTarget]}
        let newEsapiResults = Some ({ checklistItem.EsapiResults.Value with TargetCoverageDropdown = Some newTargetCoverage })
        // Set the ChecklistItem (Target Coverage Item) to loading
        let newChecklistItem = { checklistItem with EsapiResults = newEsapiResults }//Loading = true; Loaded = false }
        // Construct a new CategoryChecklist with the loading item and mark the Category as loading
        let newCategoryChecklist = { categoryChecklist with 
                                        //Loading = true; 
                                        //Loaded = false; 
                                        ChecklistItems = 
                                            categoryChecklist.ChecklistItems 
                                            |> List.map(fun curItem -> 
                                                if curItem.Text = checklistItem.Text 
                                                then newChecklistItem 
                                                else curItem ) }
        // Construct a new PlanChecklist with the new CategoryChecklist and mark the PlanChecklist as loading
        let newPlanChecklist = { planChecklist with 
                                    //Loading = true; 
                                    //Loaded = false;
                                    CategoryChecklists =
                                        planChecklist.CategoryChecklists
                                        |> List.map(fun curChecklist ->
                                            if curChecklist.Category = categoryChecklist.Category
                                            then newCategoryChecklist
                                            else curChecklist) }
        // Construct a new Model with the new PlanChecklist
        { model with 
            ChecklistScreenPlanChecklists = 
                model.ChecklistScreenPlanChecklists |> List.map(fun curChecklist -> 
                    if curChecklist.PlanDetails.CourseId = planChecklist.PlanDetails.CourseId && curChecklist.PlanDetails.PlanId = planChecklist.PlanDetails.PlanId
                    then newPlanChecklist
                    else curChecklist) }