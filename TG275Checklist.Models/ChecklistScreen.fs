namespace TG275Checklist.Model

open Elmish
open Elmish.WPF
open type System.Windows.Visibility

module ChecklistScreen = 

    open Checklists

    let exampleChecklist = { Category = Prescription; Checklist = [ { Text = "LOOK AT THE PLAN"; EsapiResults = None; Function = None } ] }
    let getChecklistBindingId category = category.ToString()

    type PlanDetails =
        {
            CourseId: string
            PlanId: string
            // Dose
            // Approvals
        }
    type SelectedPlan =
        {
            PlanDetails: PlanDetails
            Checklists: CategoryChecklist list
        }

    type Model =
        {
            Plans: SelectedPlan list
            Visibility: System.Windows.Visibility
        }
    let init () =
        {
            Plans = []
            Visibility = Collapsed
        }

    type Msg =
    | Message
    | LoadChecklists
    | LoadChecklistsSuccess of CategoryChecklist list
    | LoadChecklistsFailure of exn

    let update msg m =
        match msg with
        | LoadChecklists -> 
            { m with 
                Plans = 
                    m.Plans
                    |> List.map(fun p ->
                        {
                            PlanDetails = p.PlanDetails;asrt
                            Checklists = 
                                p.Checklists
                                |> List.map(fun x ->
                                    x
                                )
                        })
            }, Cmd.none
        | _ -> m, Cmd.none

    let checklistItemBindings () : Binding<((Model * SelectedPlan) * CategoryChecklist) * ChecklistItem, Msg> list =
        [
            "Text" |> Binding.oneWay (fun (_, item) -> item.Text)
            "EsapiText" |> Binding.oneWay (fun (_, item) -> match item.EsapiResults with | None -> "" | Some result -> result.Text)
        ]

    let checklistBindings () : Binding<(Model * SelectedPlan) * CategoryChecklist, Msg> list =
        [
            "ChecklistItems" |> Binding.subModelSeq((fun (_, checklist) -> checklist.Checklist), (fun (item:ChecklistItem) -> item.Text), checklistItemBindings)
            "Category" |> Binding.oneWay(fun (_, checklist) -> checklist.Category.ToReadableString())
        ]

    let planBindings () : Binding<Model * SelectedPlan, Msg> list =
        [
            "CourseId" |> Binding.oneWay(fun (_, plan) -> plan.PlanDetails.CourseId)
            "PlanId" |> Binding.oneWay(fun (_, plan) -> plan.PlanDetails.PlanId)
            "Checklists" |> Binding.subModelSeq((fun (_, plan) -> plan.Checklists), (fun (c:CategoryChecklist) -> getChecklistBindingId c.Category), checklistBindings)
        ]

    let bindings () : Binding<Model, Msg> list =
        [
            "Plans" |> Binding.subModelSeq((fun m -> m.Plans), (fun (p:SelectedPlan) -> PatientSetupScreen.getPlanBindingId p.PlanDetails.CourseId p.PlanDetails.PlanId), planBindings)
            "ChecklistScreenVisibility" |> Binding.oneWay(fun m -> m.Visibility)
        ]