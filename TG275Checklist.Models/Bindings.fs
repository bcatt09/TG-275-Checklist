namespace TG275Checklist.Model

open Elmish.WPF

open Model
open PatientSetupTypes
open ChecklistTypes
open type System.Windows.Visibility

module Bindings =

      /////////////////////////////////////////////////////////
     //////////////// Patient Setup Screen ///////////////////
    /////////////////////////////////////////////////////////
    let patientSetupPlanBindings () : Binding<(Model * PatientSetupCourse) * PatientSetupPlan, Msg> list =
        [
            "Id" |> Binding.oneWay (fun (_, p) -> p.Id)
            "IsChecked" |> Binding.twoWay (
                (fun (_, p:PatientSetupPlan) -> p.IsChecked), 
                (fun value (_, p:PatientSetupPlan) -> PatientSetupUsePlanChanged (p.bindingid, value)))
        ]
    let courseBindings () : Binding<Model * PatientSetupCourse, Msg> list =
        [
            "Id" |> Binding.oneWay (fun (_, c) -> c.Id)
            "Plans" |> Binding.subModelSeq(
                (fun (_, c) -> c.Plans), 
                (fun (p:PatientSetupPlan) -> p.bindingid), 
                patientSetupPlanBindings)
            "IsExpanded" |> Binding.twoWay (
                (fun (_, c) -> c.IsExpanded), 
                (fun value (_, c:PatientSetupCourse) -> PatientSetupCourseIsExpandedChanged (c.Id, value)))
        ]
    let toggleBindings () : Binding<Model * PatientSetupToggleType, Msg> list =
        [
            "Text" |> Binding.oneWay (fun (_, f) -> f.Text)
            "IsChecked" |> Binding.twoWay (
                (fun (_, f:PatientSetupToggleType) -> f.IsChecked), 
                (fun value (_, f:PatientSetupToggleType) -> PatientSetupToggleChanged (f, value)))
        ]
        
      /////////////////////////////////////////////////////////
     ////////////////// Checklist Screen /////////////////////
    /////////////////////////////////////////////////////////
    let checklistItemBindings () : Binding<((Model * FullChecklist) * CategoryChecklist) * ChecklistItem, Msg> list =
        [
            "Text" |> Binding.oneWay (fun (_, item) -> item.Text)
            "EsapiText" |> Binding.oneWay (fun (_, item) -> 
                match item.EsapiResults with 
                | None -> "" 
                | Some result -> result.Text)
        ]

    let checklistBindings () : Binding<(Model * FullChecklist) * CategoryChecklist, Msg> list =
        [
            "ChecklistItems" |> Binding.subModelSeq(
                (fun (_, checklist) -> checklist.Checklist), 
                (fun (item:ChecklistItem) -> item.Text), 
                checklistItemBindings)
            "Category" |> Binding.oneWay(fun (_, checklist) -> checklist.Category.ToReadableString())
        ]

    let checklistPlanBindings () : Binding<Model * FullChecklist, Msg> list =
        [
            "CourseId" |> Binding.oneWay(fun (_, plan) -> plan.PlanDetails.CourseId)
            "PlanId" |> Binding.oneWay(fun (_, plan) -> plan.PlanDetails.PlanId)
            "Checklists" |> Binding.subModelSeq(
                (fun (_, plan) -> plan.Checklists), 
                (fun (c:CategoryChecklist) -> c.Category.ToString()), 
                checklistBindings)
        ]

      /////////////////////////////////////////////////////////
     ///////////////////// Main Window ///////////////////////
    /////////////////////////////////////////////////////////
    let bindings () : Binding<Model, Msg> list = 
        [
            // MainWindow Info
            "PatientName" |> Binding.oneWay (fun m -> m.SharedInfo.PatientName)
            "CurrentUser" |> Binding.oneWay (fun m -> m.SharedInfo.CurrentUser)
            "StatusBarStatus" |> Binding.oneWay (fun m -> 
                match m.StatusBar with 
                | NoLoadingBar status -> status 
                | Indeterminate bar -> bar.Status 
                | Determinate bar -> bar.Status)
            "StatusBarVisibility" |> Binding.oneWay (fun m -> 
                match m.StatusBar with 
                | NoLoadingBar _ -> Collapsed 
                | _ -> Visible)
            "StatusBarIsIndeterminate" |> Binding.oneWay (fun m -> 
                match m.StatusBar with 
                | Determinate _ -> false 
                | _ -> true)

            // Patient Setup Screen
            "Courses" |> Binding.subModelSeq(
                (fun m -> m.PatientSetupScreenCourses), 
                (fun (c:PatientSetupCourse) -> c.Id), 
                courseBindings)
            "PatientSetupToggles" |> Binding.subModelSeq(
                (fun m -> m.PatientSetupScreenToggles), 
                (fun (t:PatientSetupToggleType) -> t), 
                toggleBindings)
            "PatientSetupCompleted" |> Binding.cmdIf(
                (fun _ -> DisplayChecklistScreen), 
                (fun m -> m.StatusBar = NoLoadingBar "Ready"))
            "PatientSetupScreenVisibility" |> Binding.oneWay(fun m -> m.PatientSetupScreenVisibility)
            
            // Checklist Screen
            "Plans" |> Binding.subModelSeq(
                (fun m -> m.ChecklistScreenPlans), 
                (fun (p:FullChecklist) -> getPlanBindingId p.PlanDetails.CourseId p.PlanDetails.PlanId), 
                checklistPlanBindings)
            "ChecklistScreenVisibility" |> Binding.oneWay(fun m -> m.ChecklistScreenVisibility)

            "Refresh" |> Binding.cmd Refresh
        ]