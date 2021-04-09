namespace TG275Checklist.Model

open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types
open System.Windows.Controls
open System.Globalization

module EsapiCalls =

    let tab = "     "

    type laterality =
    | Right
    | Left
    | NA

      ///////////////////////////////////////////////////////////
     ///////////////////// Prescription ////////////////////////
    ///////////////////////////////////////////////////////////

    let (|Photon|Electron|Unknown|) (modality: string) =
        if modality.Contains("X") then Photon
        else if modality.ToUpper().Contains("E") then Electron
        else Unknown

    let getPrescriptionVsPlanApprovals (plan: PlanSetup) =
        let rx = plan.RTPrescription
        let rxText = 
            if isNull rx
            then "No prescription linked to plan"
            else $"{rx.Status} by {rx.HistoryUserDisplayName} ({rx.HistoryUserName}) at {rx.HistoryDateTime}"
        
        let planText = 
            match plan.ApprovalHistory |> Seq.filter (fun h -> h.ApprovalStatus = PlanSetupApprovalStatus.Reviewed) |> Seq.tryHead with
            | Some approval -> $"{approval.ApprovalStatus} by {approval.UserDisplayName} ({approval.UserId}) at {approval.ApprovalDateTime}"
            | None -> "Plan has not been Approved/Reviewed"

        { Text = $"Prescription:\n{tab}{rxText}\nPlan:\n{tab}{planText}" }

    let getPrescriptionVsPlanDose (plan: PlanSetup) =
        let rx = plan.RTPrescription
        let rxText = 
            if isNull rx
            then "No prescription linked to plan"
            else
                rx.Targets 
                |> Seq.map (fun r -> $"{tab}{r.TargetId}:\n{tab}{tab}{r.DosePerFraction * float r.NumberOfFractions} = {r.DosePerFraction} x {r.NumberOfFractions} Fx") 
                |> String.concat "\n"

        let planText = $"{tab}{plan.TargetVolumeID}:\n{tab}{tab}{plan.TotalDose} = {plan.DosePerFraction} x {plan.NumberOfFractions} Fx"

        { Text = $"Prescription:\n{rxText}\nPlan:\n{planText}" }

    let getPrescriptionVsPlanEnergy (plan: PlanSetup) =
        let rx = plan.RTPrescription
        let rxText = 
            if isNull rx
            then "No prescription linked to plan"
            else rx.Energies |> String.concat $"\n{tab}"

        let planText = 
            plan.Beams 
            |> Seq.filter (fun b -> not b.IsSetupField)
            |> Seq.map (fun b -> b.EnergyModeDisplayName) 
            |> Seq.distinct
            |> String.concat $"\n{tab}"

        { Text = $"Prescription:\n{tab}{rxText}\nPlan:\n{tab}{planText}" }

    let getPrescriptionVsPlanModality (plan: PlanSetup) =
        let rx = plan.RTPrescription
        let rxText =
            if isNull rx
            then "No prescription linked to plan"
            else rx.EnergyModes |> Seq.map (fun e -> CultureInfo("en-US").TextInfo.ToTitleCase(e.ToLower())) |> String.concat $"\n{tab}"

        let planText =
            plan.Beams
            |> Seq.filter (fun b -> not b.IsSetupField)
            |> Seq.map (fun b ->
                match b.EnergyModeDisplayName with
                | Photon -> "Photon"
                | Electron -> "Electron"
                | Unknown -> "Unknown")
            |> Seq.distinct
            |> String.concat $"\n{tab}"

        { Text = $"Prescription:\n{tab}{rxText}\nPlan:\n{tab}{planText}" }

    let testFunction (plan: PlanSetup) =
        System.Threading.Thread.Sleep(5000)
        { Text = plan.TotalDose.ToString() }

