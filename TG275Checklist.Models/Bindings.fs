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
    let patientSetupPlanBindings () : Binding<(Model * CourseInfo) * PlanInfo, Msg> list =
        [
            "Id" |> Binding.oneWay (fun (_, p) -> p.PlanId)
            "IsChecked" |> Binding.twoWay (
                (fun (_, p:PlanInfo) -> p.IsChecked), 
                (fun value (_, p:PlanInfo) -> PatientSetupUsePlanChanged (p.bindingId, value)))
        ]
    let courseBindings () : Binding<Model * CourseInfo, Msg> list =
        [
            "Id" |> Binding.oneWay (fun (_, c) -> c.CourseId)
            "Plans" |> Binding.subModelSeq(
                (fun (_, c) -> c.Plans), 
                (fun (p:PlanInfo) -> p.bindingId), 
                patientSetupPlanBindings)
            "IsExpanded" |> Binding.twoWay (
                (fun (_, c) -> c.IsExpanded), 
                (fun value (_, c:CourseInfo) -> PatientSetupCourseIsExpandedChanged (c.CourseId, value)))
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
    let checklistItemBindings () : Binding<((Model * PlanChecklist) * CategoryChecklist) * ChecklistItem, Msg> list =
        [
            "Text" |> Binding.oneWay (fun (_, item) -> item.Text)
            "EsapiText" |> Binding.oneWayOpt (fun (_, item) -> 
                match item.EsapiResults with 
                | None -> None 
                | Some result -> Some result.Text)
            "TreatmentAppointmentDates" |> Binding.oneWayOpt (fun (_, item) ->
                match item.EsapiResults with
                | None -> None
                | Some result -> //result.TreatmentAppointments)
                    match result.TreatmentAppointments with
                    | Some appts -> Some (ResizeArray<System.DateTime> (appts |> List.map(fun x -> x.ApptTime)))  // Convert to System.Collections.Generic.List for use in C# XMAL Converter
                    | None -> None)
            "TreatmentAppointments" |> Binding.oneWayOpt (fun (_, item) ->
                match item.EsapiResults with
                | None -> None
                | Some result -> //result.TreatmentAppointments)
                    match result.TreatmentAppointments with
                    | Some appts -> Some (ResizeArray<TreatmentAppointmentInfo> appts)  // Convert to System.Collections.Generic.List for use in C# XMAL Converter
                    | None -> None)
        ]

    let checklistBindings () : Binding<(Model * PlanChecklist) * CategoryChecklist, Msg> list =
        [
            "ChecklistItems" |> Binding.subModelSeq(
                (fun (_, checklist) -> checklist.ChecklistItems), 
                (fun (item:ChecklistItem) -> item.Text), 
                checklistItemBindings)
            "Category" |> Binding.oneWay(fun (_, checklist) -> checklist.Category.ToReadableString())
        ]

    let checklistPlanBindings () : Binding<Model * PlanChecklist, Msg> list =
        [
            "CourseId" |> Binding.oneWay(fun (_, plan) -> plan.PlanDetails.CourseId)
            "PlanId" |> Binding.oneWay(fun (_, plan) -> plan.PlanDetails.PlanId)
            "PlanDose" |> Binding.oneWay(fun (_, plan) -> plan.PlanDetails.PlanDose)
            "Oncologist" |> Binding.oneWay(fun (_, plan) -> plan.PlanDetails.Oncologist)
            "CategoryChecklists" |> Binding.subModelSeq(
                (fun (_, plan) -> plan.CategoryChecklists), 
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
                (fun (c:CourseInfo) -> c.CourseId), 
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
                (fun m -> m.ChecklistScreenPlanChecklists), 
                (fun (p:PlanChecklist) -> p.PlanDetails.bindingId), 
                checklistPlanBindings)
            "ChecklistScreenVisibility" |> Binding.oneWay(fun m -> m.ChecklistScreenVisibility)

            "Debugton" |> Binding.cmd Debugton

            "TestFormatting" |> Binding.oneWay(fun m -> 
                "Text that's <Pass>good</Pass> and <FAIL>bad</FAIL> and <warn>who even knows</warn>"
            )
        ]