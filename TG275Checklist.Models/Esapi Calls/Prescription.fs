namespace TG275Checklist.EsapiCalls

open System
open TG275Checklist.Sql
open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types
open CommonHelpers
open TG275Checklist.Model.EsapiService

module Prescription =

    let checkForPrescription (rx: RTPrescription) predicate =
        if isNull rx
        then Error (ValidatedText(Fail "No prescription linked to plan", "Error"))
        else Ok predicate

    let prescriptionVsPlanOutput rx plan = EsapiResults.fromString $"Prescription:\n{tab}{rx}\nPlan:\n{tab}{plan}"

    let (|Photon|Electron|Unknown|) (energy: string) =
        if energy.Contains("X") then Photon
        else if energy.ToUpper().Contains("E") then Electron
        else Unknown

    //let (|RxFFF|RxFlat|RxElectron|) (energy: string) = 
    //    let m = Regex.Match(energy)

    /// <summary>
    /// Gets all Prescription fields from ESAPI and highlights the revision number
    /// </summary>
    let getFullPrescription: EsapiCall = fun plan ->
        match plan.RTPrescription with
        | null -> EsapiResults.fromString (ValidatedText(Fail "No prescription attached to plan", "Error").ToString())
        | rx ->
            let revision = 
                if rx.RevisionNumber > 0 
                then ValidatedText(WarnWithoutExplanation, $"\nRevision number: {rx.RevisionNumber}\nLatest Revision: {rx.LatestRevision.Name}").ToString()
                else $""" - {ValidatedText(Pass, $"Revision number: {rx.RevisionNumber} (Latest)")}"""
            let targets =
                rx.Targets
                |> Seq.map (fun targ -> $"{targ.TargetId}: {targ.DosePerFraction * float targ.NumberOfFractions} = {targ.DosePerFraction} x {targ.NumberOfFractions} Fx")
                |> String.concat $"\n{tab}"
            let modes = rx.EnergyModes |> Seq.map (fun x -> toTitleCase x) |> String.concat ", "
            let energies = rx.Energies |> String.concat ", "
            let gating = ValidatedText(getIdWarnWithoutExplanation (rx.Gating = ""), match rx.Gating with | "" -> "None" | _ -> rx.Gating)
            let bolus = ValidatedText(getIdWarnWithoutExplanation (rx.BolusThickness = ""), match rx.BolusThickness with | "" -> "None" | _ -> $"{rx.BolusThickness} {rx.BolusFrequency.ToLower()}")
            let otherLinkedPlans = 
                plan.Course.PlanSetups 
                |> Seq.filter (fun p -> p.RTPrescription = rx && p <> plan)
                |> Seq.map (fun p -> p.Id)
            let linkedPlans = if Seq.isEmpty otherLinkedPlans then "None" else ValidatedText(WarnWithoutExplanation, (otherLinkedPlans |> String.concat ", ")).ToString()
            let notes =
                match rx.Notes, rx.Comment with 
                 | "", "" ->  ""
                 | "", comment -> $"\n{tab}" + comment.Replace("\n",$"\n{tab}")
                 | notes, "" -> $"\n{tab}" + notes.Replace("\n",$"\n{tab}")
                 | notes, comment -> $"\n{tab}" + notes.Replace("\n",$"\n{tab}") + "\n" + comment.Replace("\n",$"\n{tab}")

            EsapiResults.fromString ($"Prescription Name: {rx.Name}{revision}
Site: {rx.Site}
Prescribe To:
{tab}{targets}
Primary/Boost: {toTitleCase rx.PhaseType}
Mode: {modes}
Technique: {rx.Technique}
Energy: {energies}

Gating: {gating}
Bolus: {bolus}

Notes: {ValidatedText(WarnWithoutExplanation, notes)} 
Other Linked Plans: {linkedPlans}")

    /// <summary>
    /// Gets Prescription Approval and Plan Review status and highlights based on their equivalence
    /// </summary>
    let getPrescriptionVsPlanApprovals: EsapiCall = fun plan ->
        let rx = plan.RTPrescription
        let rxApproval = 
            checkForPrescription 
                rx {|
                    Status = rx.Status
                    DisplayName = rx.HistoryUserDisplayName
                    Id = rx.HistoryUserName
                    DateTime = rx.HistoryDateTime
                |}
        
        let planApproval = 
            match plan.ApprovalHistory |> Seq.filter (fun h -> h.ApprovalStatus = PlanSetupApprovalStatus.Reviewed) |> Seq.tryHead with
            | Some approval -> 
                Ok {|
                    Status = approval.ApprovalStatus.ToString()
                    DisplayName = approval.UserDisplayName
                    Id = approval.UserId
                    DateTime = approval.ApprovalDateTime
                |}
            | None -> Error (ValidatedText(Fail "Plan has not been marked Reviewed by physician", "Error"))

        match rxApproval, planApproval with
        | Ok rx, Ok plan -> 
            let result = getPassWarnWithoutExplanation (rx.DisplayName = plan.DisplayName)
            prescriptionVsPlanOutput $"{rx.Status} by {ValidatedText(result, rx.DisplayName)} ({rx.Id}) at {rx.DateTime}" $"{plan.Status} by {ValidatedText(result, plan.DisplayName)} ({plan.Id}) at {plan.DateTime}"
        | Ok rx, Error planErr -> 
            prescriptionVsPlanOutput $"{rx.Status} by {rx.DisplayName} ({rx.Id}) at {rx.DateTime}" $"{planErr}"
        | Error rxErr, Ok plan -> prescriptionVsPlanOutput $"{rxErr}" $"{plan.Status} by {plan.DisplayName} ({plan.Id}) at {plan.DateTime}"
        | Error rxErr, Error planErr -> prescriptionVsPlanOutput $"{rxErr}" $"{planErr}"

    /// <summary>
    /// Gets Prescription and Plan Dose info and highlights based on their equivalence
    /// </summary>
    let getPrescriptionVsPlanDose: EsapiCall = fun plan ->
        let rx = plan.RTPrescription
        let rxDoseInfo = 
            checkForPrescription rx (rx.Targets
                    |> Seq.sortByDescending (fun targ -> targ.DosePerFraction.Dose)
                    |> Seq.map (fun targ -> 
                        {|
                            Target = targ.TargetId
                            TotalDose = targ.DosePerFraction * float targ.NumberOfFractions
                            DosePerFraction = targ.DosePerFraction
                            NumberOfFractions = new System.Nullable<int>(targ.NumberOfFractions)
                        |}))

        let planDoseInfo =
            Ok {|
                Target = plan.TargetVolumeID
                TotalDose = plan.TotalDose
                DosePerFraction = plan.DosePerFraction
                NumberOfFractions = plan.NumberOfFractions
            |}

        match planDoseInfo, rxDoseInfo with
        | Ok planInfo, Ok rxInfo ->
            let result = getPassWarn "Target dose doesn't match plan dose" (planInfo.TotalDose = (rxInfo |> Seq.head).TotalDose)
            prescriptionVsPlanOutput 
                (rxInfo
                |> Seq.mapi(fun i targ -> 
                    if i = 0
                    then $"{targ.Target}:\n{tab}{tab}{ValidatedText(result, targ.TotalDose)} = {targ.DosePerFraction} x {targ.NumberOfFractions} Fx"
                    else $"{targ.Target}:\n{tab}{tab}{targ.TotalDose} = {targ.DosePerFraction} x {targ.NumberOfFractions} Fx")
                |> String.concat $"\n{tab}")
                ($"{planInfo.Target}:\n{tab}{tab}{ValidatedText(result, planInfo.TotalDose)} = {planInfo.DosePerFraction} x {planInfo.NumberOfFractions} Fx")
        | Ok planInfo, Error rxErr ->
            prescriptionVsPlanOutput (rxErr) ($"{planInfo.Target}:\n{tab}{tab}{planInfo.TotalDose} = {planInfo.DosePerFraction} x {planInfo.NumberOfFractions} Fx")
        | _, _ -> EsapiResults.fromString "Something went wrong"
    
    /// <summary>
    /// Gets Prescription Fractionation Pattern and Machine Treatment Appointments to display on calendar
    /// </summary>
    let getPrescriptionVsPlanFractionationPattern: EsapiCall = fun plan ->
        let rx = plan.RTPrescription
        
        // Prescription fractionation from database
        match sqlGetRxFrequency plan.Course.Patient.Id plan.Id plan.Course.Id with
        | Error error -> $"Error getting Prescription Frequency from database: {ValidatedText(WarnWithoutExplanation, error)}" |> EsapiResults.fromString
        | Ok result -> 
            let rxText = checkForPrescription rx result

            // Patient treatment appointments from database
            match sqlGetScheduledActivities plan.Course.Patient.Id with
            | Error error -> $"Error getting Treatment Activities from database: {ValidatedText(WarnWithoutExplanation, error)}" |> EsapiResults.fromString
            | Ok patientAppts ->
                let numScheduled = (patientAppts |> Seq.distinctBy(fun x -> x.ApptTime) |> Seq.length)

                let planText =
                    if Seq.length patientAppts = 0
                    then ValidatedText(WarnWithoutExplanation, "No machine appointments scheduled").ToString()
                    else 
                        let bidDays = 
                            patientAppts 
                            |> Seq.countBy(fun x -> x.ApptTime.Date)
                            |> Seq.filter(fun x -> snd x > 1)
                        sprintf "%A%s\n%s"
                            (ValidatedText(getPassWarn "Scheduled number of fractions doesn't match plan" (numScheduled = plan.NumberOfFractions.GetValueOrDefault()), $"{numScheduled} machine appointments scheduled between {DateTime.Now.AddMonths(-1).ToShortDateString()} and {DateTime.Now.AddMonths(4).ToShortDateString()}"))
                            (if Seq.length bidDays > 1
                                then 
                                    sprintf "\n%sDays with multiple appointments (Mouse over calendar to the right to check):\n%A" tab (ValidatedText(WarnWithoutExplanation, (bidDays |> Seq.map(fun x -> $"{tab}{tab}{(fst x).ToShortDateString()} - {snd x} appointments") |> String.concat "\n")))
                                else
                                    "")
                            $"{tab}(Machine appointments only, doesn't account primary vs boost, V-sim, or previous courses)"
           
                // Format output
                match rxText with
                | Ok rxInfo -> 
                    { (prescriptionVsPlanOutput rxInfo planText) with 
                        TreatmentAppointments = Some (patientAppts |> Seq.toList) }
                | Error rxError -> $"Prescription:\n{tab}{rxError}\nPlan:\n{tab}{planText}" |> EsapiResults.fromString

    let getPrescriptionVsPlanEnergy: EsapiCall = fun plan ->
        let rx = plan.RTPrescription
        let rxText = checkForPrescription rx (rx.Energies |> String.concat $"\n{tab}")

        let planText = 
            match plan.Beams |> Seq.filter (fun b -> not b.IsSetupField) with
            | beams when Seq.isEmpty beams -> Error (ValidatedText(Fail "No treatment beams in plan", "Error"))
            | beams ->
                Ok(beams 
                |> Seq.map (fun b -> b.EnergyModeDisplayName) 
                |> Seq.distinct
                |> String.concat $"\n{tab}")

        match rxText, planText with
        // TODO: check energies against each other here
        | Ok rx, Ok plan -> prescriptionVsPlanOutput rx plan
        | Ok rx, Error plan -> prescriptionVsPlanOutput rx (plan)
        | Error rx, Ok plan -> prescriptionVsPlanOutput (rx) plan
        | Error rx, Error plan -> prescriptionVsPlanOutput (rx) (plan)

    let getPrescriptionVsPlanModality: EsapiCall = fun plan ->
        let rx = plan.RTPrescription
        let rxText = 
            checkForPrescription 
                rx 
                (rx.EnergyModes |> Seq.map (fun e -> toTitleCase e) |> String.concat $"\n{tab}")

        let planText = 
            match plan.Beams |> Seq.filter (fun b -> not b.IsSetupField) with
            | beams when Seq.isEmpty beams -> Error (ValidatedText(Fail "No treatment beams in plan", "Error"))
            | beams ->
                Ok(beams
                |> Seq.map (fun b ->
                    match b.EnergyModeDisplayName with
                    | Photon -> "Photon"
                    | Electron -> "Electron"
                    | Unknown -> "Unknown")
                |> Seq.distinct
                |> String.concat $"\n{tab}")

        let result = getPassWarnWithoutExplanation (rxText = planText)

        match rxText, planText with
        | Ok rx, Ok plan -> prescriptionVsPlanOutput (ValidatedText(result, rx)) (ValidatedText(result, plan))
        | Ok rx, Error plan -> prescriptionVsPlanOutput rx (plan)
        | Error rx, Ok plan -> prescriptionVsPlanOutput (rx) plan
        | Error rx, Error plan -> prescriptionVsPlanOutput (rx) (plan)

    let getPrescriptionVsPlanTechnique: EsapiCall = fun plan ->
        let rx = plan.RTPrescription
        let rxText = checkForPrescription rx (rx.Technique.Substring(1, rx.Technique.Length - 2))

        let planText = 
            match plan.Beams |> Seq.filter (fun b -> not b.IsSetupField) with
            | beams when Seq.isEmpty beams -> Error (ValidatedText(Fail "No treatment beams in plan", "Error"))
            | beams ->
                Ok(beams
                |> Seq.map (fun b ->
                    match b.MLCPlanType with
                    | MLCPlanType.VMAT -> "VMAT"
                    | MLCPlanType.Static 
                    | MLCPlanType.ArcDynamic -> "3D"
                    | MLCPlanType.DoseDynamic -> 
                        if b.ControlPoints.Count < 25
                        then "FiF"
                        else "IMRT"
                    | _ -> "Unknown")
                |> Seq.distinct
                |> String.concat $"\n{tab}")

        match rxText, planText with
        // TODO: check energies against each other here
        | Ok rx, Ok plan -> prescriptionVsPlanOutput rx plan
        | Ok rx, Error plan -> prescriptionVsPlanOutput rx (plan)
        | Error rx, Ok plan -> prescriptionVsPlanOutput (rx) plan
        | Error rx, Error plan -> prescriptionVsPlanOutput (rx) (plan)
    
    let getPrescriptionVsPlanBolus: EsapiCall = fun plan ->
        let rx = plan.RTPrescription

        let rxText = 
            checkForPrescription 
                rx 
                (match rx.BolusThickness with
                | "" -> "No bolus"
                | thickness -> $"{thickness} bolus {rx.BolusFrequency.ToLower()}")

        let planText =
            match plan.Beams |> Seq.filter (fun b -> not b.IsSetupField) with
            | beams when Seq.isEmpty beams -> Error (ValidatedText(Fail "No treatment beams in plan", "Error"))
            | beams ->
                Ok(beams
                |> Seq.filter (fun b -> not b.IsSetupField)
                |> Seq.map (fun b -> 
                    if Seq.length b.Boluses =  0
                    then  "No bolus"
                    else b.Boluses
                         |> Seq.map (fun bolus -> bolus.Id)
                         |> String.concat ", ")
                |> Seq.distinct
                |> String.concat $"\n{tab}")

        match rxText, planText with
        | Ok rx, Ok plan ->
            match rx, plan with
            | "No bolus", "No bolus" -> prescriptionVsPlanOutput (ValidatedText(Pass, rx)) (ValidatedText(Pass, plan))
            | "No bolus", _ -> prescriptionVsPlanOutput (ValidatedText(Fail "Bolus used in plan was not prescribed", rx)) (ValidatedText(WarnWithoutExplanation, plan))
            | _, "No bolus" -> prescriptionVsPlanOutput (ValidatedText(WarnWithoutExplanation, rx)) (ValidatedText(Warn "Prescribed bolus was not used in plan", plan))
            | _, _ -> prescriptionVsPlanOutput (ValidatedText(WarnWithoutExplanation, rx)) (ValidatedText(WarnWithoutExplanation, plan))
        | Ok rx, Error plan -> prescriptionVsPlanOutput (ValidatedText(WarnWithoutExplanation, rx)) (plan)
        | Error rx, Ok plan -> prescriptionVsPlanOutput (rx) (ValidatedText(WarnWithoutExplanation, plan))
        | Error rx, Error plan -> prescriptionVsPlanOutput (rx) (plan)

    let getCoursePlanIntentDiagnosis: EsapiCall = fun plan ->
        let courseIntent =
            match plan.Course.Intent with
            | "" -> ValidatedText(Fail "No intent specified", "Error").ToString()
            | intent -> intent
        let courseDiagnosis =
            match plan.Course.Diagnoses with
            | sequence when Seq.isEmpty sequence -> ValidatedText(Fail "No diagnoses attached", "Error").ToString()
            | diagnoses -> 
                diagnoses 
                |> Seq.map (fun diag -> $"{diag.ClinicalDescription.Trim()} ({diag.Code.Trim()})")
                |> String.concat $"\n{tab}{tab}"
        let planIntent =
            match plan.PlanIntent with
            | "" -> ValidatedText(WarnWithoutExplanation, "No intent specified").ToString()
            | intent -> toTitleCase intent

        EsapiResults.fromString $"Course:\n{tab}Intent:\n{tab}{tab}{courseIntent}\n{tab}Diagnosis:\n{tab}{tab}{courseDiagnosis}\nPlan:\n{tab}Intent:\n{tab}{tab}{planIntent}"
