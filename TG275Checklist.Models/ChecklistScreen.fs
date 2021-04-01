namespace TG275Checklist.Model

open Elmish
open Elmish.WPF

open Esapi
open VMS.TPS.Common.Model.API
open type System.Windows.Visibility

module ChecklistScreen = 

    open Checklists

    //let getChecklistBindingId category = category.ToString()

    //type SelectedPlan =
    //    {
    //        PlanDetails: PlanDetails
    //        Checklists: CategoryChecklist list
    //    }

    //type Model =
    //    {
    //        Plans: SelectedPlan list
    //        Visibility: System.Windows.Visibility
    //    }
    //let init () =
    //    {
    //        Plans = []
    //        Visibility = Collapsed
    //    }

    //type Msg =
    //| Message
    //| LoadChecklists
    //| LoadChecklistsSuccess of SelectedPlan list
    //| LoadChecklistsFailure of exn

    //let getPlan (pat:Patient) planDetails =
    //    pat.Courses
    //    |> Seq.map(fun c -> c.PlanSetups |> Seq.cast<PlanSetup>)
    //    |> Seq.concat
    //    |> Seq.filter(fun p -> p.Id = planDetails.PlanId && p.Course.Id = planDetails.CourseId)
    //    |> Seq.exactlyOne

    //let populateEsapiResults (plans: SelectedPlan list) =
    //    async{
    //        let newPlansWithEsapiResults =
    //            plans
    //            |> List.map (fun p ->
    //                { p with 
    //                    Checklists = 
    //                        p.Checklists
    //                        |> List.map (fun x -> 
    //                            let newChecklist = x.Checklist |> List.map(fun y -> { y with EsapiResults = y.AsyncToken |> Async.RunSynchronously})
    //                            { x with Checklist = newChecklist })})

    //        return LoadChecklistsSuccess newPlansWithEsapiResults
    //        //return LoadChecklistsSuccess plans
    //    }

    //let update msg m =
    //    match msg with
    //    | LoadChecklists -> 
    //        m, Cmd.OfAsync.either populateEsapiResults m.Plans id LoadChecklistsFailure
    //    | LoadChecklistsSuccess newPlans -> { m with Plans = newPlans }, Cmd.none
    //    | LoadChecklistsFailure x -> 
    //        System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Populate Plan Results from Eclipse") |> ignore
    //        m, Cmd.none
    //    | _ -> m, Cmd.none

    //let checklistItemBindings () : Binding<((Model * SelectedPlan) * CategoryChecklist) * ChecklistItem, Msg> list =
    //    [
    //        "Text" |> Binding.oneWay (fun (_, item) -> item.Text)
    //        "EsapiText" |> Binding.oneWay (fun (_, item) -> match item.EsapiResults with | None -> "" | Some result -> result.Text)
    //    ]

    //let checklistBindings () : Binding<(Model * SelectedPlan) * CategoryChecklist, Msg> list =
    //    [
    //        "ChecklistItems" |> Binding.subModelSeq((fun (_, checklist) -> checklist.Checklist), (fun (item:ChecklistItem) -> item.Text), checklistItemBindings)
    //        "Category" |> Binding.oneWay(fun (_, checklist) -> checklist.Category.ToReadableString())
    //    ]

    //let planBindings () : Binding<Model * SelectedPlan, Msg> list =
    //    [
    //        "CourseId" |> Binding.oneWay(fun (_, plan) -> plan.PlanDetails.CourseId)
    //        "PlanId" |> Binding.oneWay(fun (_, plan) -> plan.PlanDetails.PlanId)
    //        "Checklists" |> Binding.subModelSeq((fun (_, plan) -> plan.Checklists), (fun (c:CategoryChecklist) -> getChecklistBindingId c.Category), checklistBindings)
    //    ]

    //let bindings () : Binding<Model, Msg> list =
    //    [
    //        "Plans" |> Binding.subModelSeq((fun m -> m.Plans), (fun (p:SelectedPlan) -> PatientSetupScreen.getPlanBindingId p.PlanDetails.CourseId p.PlanDetails.PlanId), planBindings)
    //        "ChecklistScreenVisibility" |> Binding.oneWay(fun m -> m.Visibility)
    //    ]