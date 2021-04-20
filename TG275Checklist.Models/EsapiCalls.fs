namespace TG275Checklist.Model

open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types
open System.Windows.Controls
open System.Globalization

module EsapiCalls =

    let tab = "     "

    let tagString (tag: string * string) text =
        $"{fst tag}{text}{snd tag}"

    let pass = "<Pass>", "</Pass>"
    let fail = "<Fail>", "</Fail>"
    let warn = "<Warn>", "</Warn>"

    let stringOutput text =
        { Text = text }

    type laterality =
    | Right
    | Left
    | NA

      ///////////////////////////////////////////////////////////
     /////////////////////// Plan Info /////////////////////////
    ///////////////////////////////////////////////////////////

    let getPlanInfoDose (plan: PlanSetup) =
        $"{plan.TotalDose} = {plan.DosePerFraction} x {plan.NumberOfFractions}"

    let getPlanInfoPatientName (plan: PlanSetup) =
        let pat = plan.Course.Patient
        $"{pat.LastName}, {pat.FirstName} ({pat.Id})"

    let getPlanInfoOncologist (plan: PlanSetup) =
        let pat = plan.Course.Patient
        $"{pat.PrimaryOncologistId} maybe?"

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

        stringOutput $"Prescription:\n{tab}{rxText}\nPlan:\n{tab}{planText}"

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

        stringOutput $"Prescription:\n{rxText}\nPlan:\n{planText}"

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

        stringOutput $"Prescription:\n{tab}{rxText}\nPlan:\n{tab}{planText}"

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

        stringOutput $"Prescription:\n{tab}{rxText}\nPlan:\n{tab}{planText}"

      ///////////////////////////////////////////////////////////
     ///////////////////// Prescription ////////////////////////
    ///////////////////////////////////////////////////////////

    let getPatientOrientations (plan: PlanSetup) =
        let tx = plan.TreatmentOrientation
        let sim = plan.StructureSet.Image.ImagingOrientation

        let tag = 
            if tx = sim
            then pass
            else fail

        stringOutput $"Treatment: \n{tab}{tagString tag tx}\nImaging: \n{tab}{tagString tag sim}"

    let testFunction (plan: PlanSetup) =
        System.Threading.Thread.Sleep(5000)
        let badExample = "BAD"
        let goodExample = "Good"
        stringOutput $"{plan.TotalDose} {tagString fail badExample} dose/{tagString pass goodExample} dose"

