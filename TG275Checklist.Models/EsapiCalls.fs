namespace TG275Checklist.Model

open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types
open System.Windows.Controls
open System.Globalization
open System.Text.RegularExpressions

module EsapiCalls =

    let tab = "    "

    let passingTags = "<Pass>", "</Pass>"
    let failingTags = "<Fail>", "</Fail>"
    let warningTags = "<Warn>", "</Warn>"

    let tagString (tag: string * string) text =
        $"{fst tag}{text}{snd tag}"

    let pass text = tagString passingTags text
    let fail text = tagString failingTags text
    let warn text = tagString warningTags text

    let getPassFail condition = if condition then pass else fail
    let getPassWarn condition = if condition then pass else warn

    let stringOutput text =
        { Text = text }

    let toTitleCase (text: string) =
        CultureInfo("en-US").TextInfo.ToTitleCase(text.ToLower())

    type laterality =
    | Right
    | Left
    | NA
    
    let testFunction (plan: PlanSetup) =
        System.Threading.Thread.Sleep(3000)
        let badExample = "BAD"
        let goodExample = "Good"
        stringOutput $"{plan.TotalDose} {fail badExample} dose/{pass goodExample} dose"

      ///////////////////////////////////////////////////////////
     ///////////////////// Prescription ////////////////////////
    ///////////////////////////////////////////////////////////

    let checkForPrescription (rx: RTPrescription) predicate =
        if isNull rx
        then Error "No prescription linked to plan"
        else Ok predicate

    let prescriptionVsPlanOutput rx plan = stringOutput $"Prescription:\n{tab}{rx}\nPlan:\n{tab}{plan}"

    let (|Photon|Electron|Unknown|) (energy: string) =
        if energy.Contains("X") then Photon
        else if energy.ToUpper().Contains("E") then Electron
        else Unknown

    //let (|RxFFF|RxFlat|RxElectron|) (energy: string) = 
    //    let m = Regex.Match(energy)

    let getFullPrescription (plan: PlanSetup) =
        match plan.RTPrescription with
        | null -> stringOutput (fail "No prescription attached to plan")
        | rx ->
            let revision = if rx.RevisionNumber > 0 then fail $"\nRevision number: {rx.RevisionNumber}\nLatest Revision: {rx.LatestRevision.Name}" else pass " - Latest revision"
            let targets =
                rx.Targets
                |> Seq.map (fun targ -> $"{targ.TargetId}: {targ.DosePerFraction * float targ.NumberOfFractions} = {targ.DosePerFraction} x {targ.NumberOfFractions} Fx")
                |> String.concat $"\n{tab}"
            let modes = rx.EnergyModes |> Seq.map (fun x -> toTitleCase x) |> String.concat ", "
            let energies = rx.Energies |> String.concat ", "
            let gating = if rx.Gating = "" then "None" else warn rx.Gating
            let bolus = if rx.BolusThickness = "" then "None" else warn $"{rx.BolusThickness} {rx.BolusFrequency.ToLower()}"
            let otherLinkedPlans = 
                plan.Course.PlanSetups 
                |> Seq.filter (fun p -> p.RTPrescription = rx && p <> plan)
                |> Seq.map (fun p -> p.Id)
            let linkedPlans = if Seq.isEmpty otherLinkedPlans then "None" else warn (otherLinkedPlans |> String.concat ", ")
            let notes =
                match rx.Notes, rx.Comment with 
                 | "", "" ->  ""
                 | "", comment -> comment
                 | notes, "" -> notes
                 | notes, comment -> $"{notes}\n{comment}"

            stringOutput ($"Prescription Name: {rx.Name}{revision}
Site: {rx.Site}
Prescribe To:
{tab}{targets}
Primary/Boost: {toTitleCase rx.PhaseType}
Mode: {modes}
Technique: {rx.Technique}
Energy: {energies}

Gating: {gating}
Bolus: {bolus}

Notes: {warn notes}     
Other Linked Plans: {linkedPlans}")
    
    type private approval =
        {
            Status: string
            DisplayName: string
            Id: string
            DateTime: System.DateTime
        }

    let getPrescriptionVsPlanApprovals (plan: PlanSetup) =
        let rx = plan.RTPrescription
        let rxApproval = 
            checkForPrescription rx { 
                    Status = rx.Status
                    DisplayName = rx.HistoryUserDisplayName
                    Id = rx.HistoryUserName
                    DateTime = rx.HistoryDateTime
                }
        
        let planApproval = 
            match plan.ApprovalHistory |> Seq.filter (fun h -> h.ApprovalStatus = PlanSetupApprovalStatus.Reviewed) |> Seq.tryHead with
            | Some approval -> Ok { 
                    Status = approval.ApprovalStatus.ToString()
                    DisplayName = approval.UserDisplayName
                    Id = approval.UserId
                    DateTime = approval.ApprovalDateTime
                }
            | None -> Error "Plan has not been marked Reviewed by physician"

        match rxApproval, planApproval with
        | Ok rx, Ok plan -> 
            let result = getPassFail (rx.DisplayName = plan.DisplayName)
            prescriptionVsPlanOutput $"{rx.Status} by {result rx.DisplayName} ({rx.Id}) at {rx.DateTime}" $"{plan.Status} by {result plan.DisplayName} ({plan.Id}) at {plan.DateTime}"
        | Ok rx, Error planErr -> 
            prescriptionVsPlanOutput $"{rx.Status} by {rx.DisplayName} ({rx.Id}) at {rx.DateTime}" $"{fail planErr}"
        | Error rxErr, Ok plan -> prescriptionVsPlanOutput $"{fail rxErr}" $"{plan.Status} by {plan.DisplayName} ({plan.Id}) at {plan.DateTime}"
        | Error rxErr, Error planErr -> prescriptionVsPlanOutput $"{fail rxErr}" $"{fail planErr}"

    type private doseInfo =
        {
            Target: string
            TotalDose: DoseValue
            DosePerFraction: DoseValue
            NumberOfFractions: System.Nullable<int>
        }

    let getPrescriptionVsPlanDose (plan: PlanSetup) =
        let rx = plan.RTPrescription
        let rxDoseInfo = 
            checkForPrescription rx (rx.Targets
                    |> Seq.sortByDescending (fun targ -> targ.DosePerFraction.Dose)
                    |> Seq.map (fun targ -> 
                        {
                            Target = targ.TargetId
                            TotalDose = targ.DosePerFraction * float targ.NumberOfFractions
                            DosePerFraction = targ.DosePerFraction
                            NumberOfFractions = new System.Nullable<int>(targ.NumberOfFractions)
                        }))

        let planDoseInfo =
            Ok {
                Target = plan.TargetVolumeID
                TotalDose = plan.TotalDose
                DosePerFraction = plan.DosePerFraction
                NumberOfFractions = plan.NumberOfFractions
            }

        match planDoseInfo, rxDoseInfo with
        | Ok planInfo, Ok rxInfo ->
            let result = getPassFail (planInfo.TotalDose = (rxInfo |> Seq.head).TotalDose)
            prescriptionVsPlanOutput 
                (rxInfo
                |> Seq.mapi(fun i targ -> 
                    if i = 0
                    then $"{targ.Target}:\n{tab}{tab}{result targ.TotalDose} = {targ.DosePerFraction} x {targ.NumberOfFractions} Fx"
                    else $"{targ.Target}:\n{tab}{tab}{targ.TotalDose} = {targ.DosePerFraction} x {targ.NumberOfFractions} Fx")
                |> String.concat $"\n{tab}")
                ($"{planInfo.Target}:\n{tab}{tab}{result planInfo.TotalDose} = {planInfo.DosePerFraction} x {planInfo.NumberOfFractions} Fx")
        | Ok planInfo, Error rxErr ->
            prescriptionVsPlanOutput (fail rxErr) ($"{planInfo.Target}:\n{tab}{tab}{planInfo.TotalDose} = {planInfo.DosePerFraction} x {planInfo.NumberOfFractions} Fx")
        | Error err, _ -> stringOutput "Something went wrong"

    let getPrescriptionVsPlanEnergy (plan: PlanSetup) =
        let rx = plan.RTPrescription
        let rxText = checkForPrescription rx (rx.Energies |> String.concat $"\n{tab}")

        let planText = 
            match plan.Beams |> Seq.filter (fun b -> not b.IsSetupField) with
            | beams when Seq.isEmpty beams -> Error "No treatment beams in plan"
            | beams ->
                Ok(beams 
                |> Seq.map (fun b -> b.EnergyModeDisplayName) 
                |> Seq.distinct
                |> String.concat $"\n{tab}")

        match rxText, planText with
        // TODO: check energies against each other here
        | Ok rx, Ok plan -> prescriptionVsPlanOutput rx plan
        | Ok rx, Error plan -> prescriptionVsPlanOutput rx (fail plan)
        | Error rx, Ok plan -> prescriptionVsPlanOutput (fail rx) plan
        | Error rx, Error plan -> prescriptionVsPlanOutput (fail rx) (fail plan)

    let getPrescriptionVsPlanModality (plan: PlanSetup) =
        let rx = plan.RTPrescription
        let rxText = 
            checkForPrescription 
                rx 
                (rx.EnergyModes |> Seq.map (fun e -> toTitleCase e) |> String.concat $"\n{tab}")

        let planText = 
            match plan.Beams |> Seq.filter (fun b -> not b.IsSetupField) with
            | beams when Seq.isEmpty beams -> Error "No treatment beams in plan"
            | beams ->
                Ok(beams
                |> Seq.map (fun b ->
                    match b.EnergyModeDisplayName with
                    | Photon -> "Photon"
                    | Electron -> "Electron"
                    | Unknown -> "Unknown")
                |> Seq.distinct
                |> String.concat $"\n{tab}")

        let result = getPassFail (rxText = planText)

        match rxText, planText with
        | Ok rx, Ok plan -> prescriptionVsPlanOutput (result rx) (result plan)
        | Ok rx, Error plan -> prescriptionVsPlanOutput rx (fail plan)
        | Error rx, Ok plan -> prescriptionVsPlanOutput (fail rx) plan
        | Error rx, Error plan -> prescriptionVsPlanOutput (fail rx) (fail plan)

    let getPrescriptionVsPlanTechnique (plan: PlanSetup) =
        let rx = plan.RTPrescription
        let rxText = checkForPrescription rx (rx.Technique.Substring(1, rx.Technique.Length - 2))

        let planText = 
            match plan.Beams |> Seq.filter (fun b -> not b.IsSetupField) with
            | beams when Seq.isEmpty beams -> Error "No treatment beams in plan"
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
        | Ok rx, Error plan -> prescriptionVsPlanOutput rx (fail plan)
        | Error rx, Ok plan -> prescriptionVsPlanOutput (fail rx) plan
        | Error rx, Error plan -> prescriptionVsPlanOutput (fail rx) (fail plan)
    
    let getPrescriptionVsPlanBolus (plan: PlanSetup) =
        let rx = plan.RTPrescription

        let rxText = 
            checkForPrescription 
                rx 
                (match rx.BolusThickness with
                | "" -> "No bolus"
                | thickness -> $"{thickness} bolus {rx.BolusFrequency.ToLower()}")

        let planText =
            match plan.Beams |> Seq.filter (fun b -> not b.IsSetupField) with
            | beams when Seq.isEmpty beams -> Error "No treatment beams in plan"
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
            | "No bolus", "No bolus" -> prescriptionVsPlanOutput (pass rx) (pass plan)
            | "No bolus", _ -> prescriptionVsPlanOutput (fail rx) (warn plan)
            | _, "No bolus" -> prescriptionVsPlanOutput (warn rx) (fail plan)
            | _, _ -> prescriptionVsPlanOutput (warn rx) (warn plan)
        | Ok rx, Error plan -> prescriptionVsPlanOutput (id rx) (fail plan)
        | Error rx, Ok plan -> prescriptionVsPlanOutput (fail rx) (id plan)
        | Error rx, Error plan -> prescriptionVsPlanOutput (fail rx) (fail plan)

    let getCoursePlanIntentDiagnosis (plan: PlanSetup) =
        let courseIntent =
            match plan.Course.Intent with
            | "" -> fail "No intent specified"
            | intent -> intent
        let courseDiagnosis =
            match plan.Course.Diagnoses with
            | sequence when Seq.isEmpty sequence -> fail "No diagnoses attached"
            | diagnoses -> 
                diagnoses 
                |> Seq.map (fun diag -> $"{diag.ClinicalDescription.Trim()} ({diag.Code.Trim()})")
                |> String.concat $"\n{tab}{tab}"
        let planIntent =
            match plan.PlanIntent with
            | "" -> warn "No intent specified"
            | intent -> toTitleCase intent

        stringOutput $"Course:\n{tab}Intent:\n{tab}{tab}{courseIntent}\n{tab}Diagnosis:\n{tab}{tab}{courseDiagnosis}\nPlan:\n{tab}Intent:\n{tab}{tab}{planIntent}"

      ///////////////////////////////////////////////////////////
     ////////////////////// Simulation /////////////////////////
    ///////////////////////////////////////////////////////////

    let getPatientOrientations (plan: PlanSetup) =
        let tx = plan.TreatmentOrientation
        let sim = plan.StructureSet.Image.ImagingOrientation

        let result = getPassFail (tx = sim)

        stringOutput $"Treatment: \n{tab}{result tx}\nImaging: \n{tab}{result sim}"
        
      ///////////////////////////////////////////////////////////
     ////////////////////// Contouring /////////////////////////
    ///////////////////////////////////////////////////////////

    type Laterality =
    | Left
    | Right
    | NA

    let OARTypes = [ "AVOIDANCE"; "CAVITY"; "ORGAN"; "CONTROL"; "DOSE_REGION"; "IRRAD_VOLUME"; "TREATED_VOLUME" ]
    let targetTypes = [ "GTV"; "CTV"; "PTV" ]

    let getBodyStructure (plan:PlanSetup) = 
        plan.StructureSet.Structures |>
        Seq.filter (fun x -> x.DicomType = "BODY" || x.DicomType = "EXTERNAL") |>
        Seq.tryExactlyOne

    let getTargetStructure (plan:PlanSetup) =
        plan.StructureSet.Structures |>
        Seq.filter (fun x -> x.Id = plan.TargetVolumeID) |>
        Seq.tryExactlyOne

    let (|ParseLateralityFromInputString|_|) (regex:string) (str:string) =
        let m = Regex(regex).Match(str.ToUpper())
        if m.Success 
        then Some (match m.Groups.[1].Value with | "L" -> Left | "R" -> Right | _ -> NA)
        else None

    let (|GetLateralityFromStructure|_|) (plan:PlanSetup) (body:Structure) (s:Structure) = 
        let offset = s.CenterPoint.x - body.CenterPoint.x

        match plan.TreatmentOrientation with
        | PatientOrientation.HeadFirstSupine 
        | PatientOrientation.FeetFirstProne
        | PatientOrientation.HeadFirstProne 
        | PatientOrientation.FeetFirstSupine -> Some (if offset < 0.0 then Right else Left)
        | _ -> None

    let getTargetInfo (plan:PlanSetup) =
        stringOutput 
            (match getTargetStructure plan with
            | None -> fail "No plan target volume"
            | Some target -> 
                let targetLateralityText =
                    match getBodyStructure plan with
                    | None -> fail "Single body structure not found, calculation not possible"
                    | Some body -> 
                        match plan.Id with
                            | ParseLateralityFromInputString @"^(L|R)T?( |_)" planLaterality -> 
                                    match target with
                                    | GetLateralityFromStructure plan body targetLaterality ->
                                        if planLaterality = targetLaterality
                                        then sprintf "Plan and target %s (%A)" (pass "agree") planLaterality
                                        else sprintf "%s\nPlan is named for %s\nPlan target is on the %s side of %s" (warn "Mismatch") (fail planLaterality) (fail targetLaterality) body.Id
                                    | _ -> sprintf "%s (please tell me and I can add it): %A" (warn "Patient orientation not supported") plan.TreatmentOrientation
                            | _ -> sprintf "None specified"

                sprintf "Target: %s\n%s# of pieces: %s\n%sLaterality: %s\n%sVolume: %0.2f cc"
                    plan.TargetVolumeID
                    tab
                    (match target.GetNumberOfSeparateParts() with
                    | 1 -> pass "1 piece"
                    | n -> warn $"{n} pieces")
                    tab
                    targetLateralityText
                    tab
                    target.Volume)
                        
    let getBodyInfo (plan:PlanSetup) =
        let bodies = 
            plan.StructureSet.Structures |>
            Seq.filter (fun x -> x.DicomType = "BODY" || x.DicomType = "EXTERNAL")

        stringOutput 
            (match bodies |> Seq.length with
            | 0 -> fail "No body/external structures found"
            | 1 -> 
                let body = bodies |> Seq.exactlyOne
                match body.GetNumberOfSeparateParts() with
                | 1 -> sprintf "%s:\n%s# of pieces: %s" body.Id tab (pass "1 piece")
                | num -> sprintf "%s:\n%s# of pieces: %s" body.Id tab (warn $"{num} pieces")
            | num -> warn (sprintf "%i body/external structures found (%s)" num (bodies |> Seq.map(fun x -> x.Id) |> String.concat ", ")))

      ///////////////////////////////////////////////////////////
     ////// Dose Distribution and Overall Plan Quality /////////
    ///////////////////////////////////////////////////////////

    let getReferencePoints (plan: PlanSetup) =
        let primary = plan.PrimaryReferencePoint
        let primaryText = 
            if isNull primary 
            then "" 
            else $"Primary Plan Reference Point:
{tab}{primary.Id}:
{tab}{tab}Session dose limit: {getPassWarn (primary.SessionDoseLimit = plan.DosePerFraction) primary.SessionDoseLimit}
{tab}{tab}Daily dose limit: {primary.DailyDoseLimit}
{tab}{tab}Total dose limit: {getPassWarn (primary.TotalDoseLimit = plan.TotalDose) primary.TotalDoseLimit}
{tab}{tab}Has a location: {primary.HasLocation(plan)}"
        
        let secondaries = 
            plan.ReferencePoints
            |> Seq.filter (fun x -> x <> primary)
            |> Seq.map (fun x -> $"{tab}{x.Id}:
{tab}{tab}Session dose limit: {x.SessionDoseLimit}
{tab}{tab}Daily dose limit: {x.DailyDoseLimit}
{tab}{tab}Total dose limit: {x.TotalDoseLimit}
{tab}{tab}Has a location: {x.HasLocation(plan)}")
        let secondariesText = 
            if Seq.length secondaries > 0
            then sprintf "Secondary Reference Points:\n%s" (secondaries |> String.concat "\n")
            else ""

        stringOutput 
            (match primaryText, secondariesText with
            | "", "" -> fail "No Primary Reference Point selected for plan"
            | p, "" -> p
            | "", s -> sprintf "%s\n\n%s" (fail "No Primary Reference Point selected for plan") s
            | p, s -> $"{p}\n\n{s}")

      ///////////////////////////////////////////////////////////
     ////////////////////// Scheduling /////////////////////////
    ///////////////////////////////////////////////////////////

    let getPlanScheduling (plan:PlanSetup) =
        let overlappingSessions = 
            plan.TreatmentSessions
            |> Seq.map (fun session -> 
                session.TreatmentSession.SessionPlans
                |> Seq.filter (fun sessionPlan -> sessionPlan.PlanSetup.Id <> plan.Id)
                |> Seq.map (fun sessionPlan -> (plan, sessionPlan.PlanSetup)))
            |> Seq.concat
            |> Seq.countBy id

        let numSessions = Seq.length plan.TreatmentSessions

        let overlappingSessionsText =
            if Seq.length overlappingSessions > 0
                then
                    overlappingSessions 
                    |> Seq.map (fun x -> warn $"\n{(fst (fst x)).Id} is scheduled with {(snd (fst x)).Id} for {snd x} sessions.  ")
                    |> Seq.map (fun x -> x + "Please check Plan Scheduling for more info")
                    |> String.concat ""
                else ""

        let numSessionsText = $"{numSessions} sessions"

        stringOutput $"{getPassWarn (numSessions = plan.NumberOfFractions.Value) numSessionsText} scheduled for treatment {overlappingSessionsText}"