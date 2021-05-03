namespace TG275Checklist.Model

open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types

open FSharp.Data

open System
open System.Globalization
open System.Text.RegularExpressions
open System.Collections.Generic
open TG275Checklist.Sql
open System.Windows.Controls

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
    let getPassId condition = if condition then pass else id
    let getIdWarn condition = if condition then id else warn

    let stringOutput text = { EsapiResults.init with Text = text }

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
    let badTestFunction (plan: PlanSetup) =
        failwith "this has been a test of the emergency broadcast system"


        
        ///////////////////////////////////////////////////////////
       ///////////////////////////////////////////////////////////
      ///////////////////// Prescription ////////////////////////
     ///////////////////////////////////////////////////////////
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
            let revision = 
                if rx.RevisionNumber > 0 
                then fail $"\nRevision number: {rx.RevisionNumber}\nLatest Revision: {rx.LatestRevision.Name}" 
                else " - " + pass "Latest revision"
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

    let getPrescriptionVsPlanFractionationPattern (plan: PlanSetup) =
        let rx = plan.RTPrescription
        
        // Prescription fractionation from database
        use rxCmd = new SqlCommandProvider<SqlQueries.sqlGetRxFrequency, SqlQueries.connectionString>(SqlQueries.connectionString)
        let rxText = 
            checkForPrescription 
                rx 
                (rxCmd.Execute(patId = plan.Course.Patient.Id, planId = plan.Id, courseId = plan.Course.Id)
                |> Seq.map (fun x -> x.Frequency)
                |> Seq.head)

        // Patient treatment appointments from database
        use planCmd = new SqlCommandProvider<const(SqlQueries.sqlGetScheduledActivities), SqlQueries.connectionString>(SqlQueries.connectionString)
        let planCmdResultDates = 
            planCmd.Execute(patId = plan.Course.Patient.Id, planId = plan.Id, courseId = plan.Course.Id)
            |> Seq.map (fun x -> x.ScheduledStartTime.Value)
        let planText =
            planCmdResultDates
            |> Seq.map (fun time -> sprintf "%s - %s" (time.ToString("dddd")) (time.ToShortDateString()))
            |> String.concat $"\n{tab}"
        let numScheduled = (planText.ToCharArray() |> Array.filter ((=) '\n') |> Seq.length) + 1
        let planTextOutput = 
            if planText = "" 
            then warn "No machine appointments scheduled" 
            else 
                getPassWarn (numScheduled = plan.NumberOfFractions.GetValueOrDefault()) $"{numScheduled} machine appointments scheduled between {DateTime.Now.AddMonths(-3).ToShortDateString()} and {DateTime.Now.AddMonths(4).ToShortDateString()}\n{tab}(Machine appointments only, doesn't account primary vs boost, V-sim, or previous courses)"
        
        // Format output
        match rxText with
        | Ok rxInfo -> 
            { (prescriptionVsPlanOutput rxInfo planTextOutput) with 
                TreatmentAppointments = Some (planCmdResultDates |> Seq.toList) }
        | Error rxError -> $"Prescription:\n{tab}{fail rxError}\nPlan:\n{tab}{planTextOutput}" |> stringOutput

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
       ///////////////////////////////////////////////////////////
      ////////////////////// Simulation /////////////////////////
     ///////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////

    let getPatientOrientations (plan: PlanSetup) =
        let tx = plan.TreatmentOrientation
        let sim = plan.StructureSet.Image.ImagingOrientation

        let result = getPassFail (tx = sim)

        stringOutput $"Treatment: \n{tab}{result tx}\nImaging: \n{tab}{result sim}"
    
    let getCTInfo (plan: PlanSetup) =
        match plan.StructureSet.Image with
        | null -> fail "No image loaded"
        | ct -> 
            let creationDate =
                match ct.CreationDateTime.HasValue with
                | true -> ct.CreationDateTime.Value.ToShortDateString()
                | false -> warn "No creation date"
            let isDateFunction = // If the CT ID is a date we'll highlight is red if it doesn't match, otherwise we'll use yellow
                if fst (DateTime.TryParse(ct.Id.Substring(0,8)))
                then getPassFail
                else getPassWarn
            let dateResult = isDateFunction (ct.Id.Substring(0,8) = DateTime.Parse(creationDate).ToString("yyyyMMdd"))

            $"Imaging Device for HU Curve: {ct.Series.ImagingDeviceId}\nCT Name: {dateResult ct.Id}\nCreation date: {dateResult creationDate}\n# of slices: {ct.ZSize}"
        |> stringOutput
        
        ///////////////////////////////////////////////////////////
       ///////////////////////////////////////////////////////////
      ////////////////////// Contouring /////////////////////////
     ///////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////

    type Laterality =
    | Left
    | Right
    | NA

    let OARTypes = [ "AVOIDANCE"; "CAVITY"; "ORGAN"; "CONTROL"; "DOSE_REGION"; "IRRAD_VOLUME"; "TREATED_VOLUME" ]
    let targetTypes = [ "GTV"; "CTV"; "PTV" ]

    let getBodyStructure (plan:PlanSetup) = 
        plan.StructureSet.Structures
        |> Seq.filter (fun x -> x.DicomType = "BODY" || x.DicomType = "EXTERNAL")
        |> Seq.tryExactlyOne

    let getTargetStructure (plan:PlanSetup) =
        plan.StructureSet.Structures
        |> Seq.filter (fun x -> x.Id = plan.TargetVolumeID)
        |> Seq.tryExactlyOne

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
        match getTargetStructure plan with
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
                                    then sprintf "Plan and target agree (%s)" (pass planLaterality)
                                    else sprintf "%s\nPlan is named for %s\nPlan target is on the %s side of %s" (warn "Mismatch") (fail planLaterality) (fail targetLaterality) body.Id
                                | _ -> sprintf "%s: %A" (warn "Patient orientation not supported") plan.TreatmentOrientation
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
                target.Volume
        |> stringOutput

    let checkStructureLaterality (plan:PlanSetup) (name:string) (struc:Structure) =
        match getBodyStructure plan with
        | None -> None, None, Some "Single body structure not found, calculation not possible"
        | Some body ->
            match name with
                | ParseLateralityFromInputString @"_(R|L)T?( |_)?" nameLaterality -> 
                        match struc with
                        | GetLateralityFromStructure plan body structureLaterality -> (Some nameLaterality, Some structureLaterality, None)
                        | _ -> (None, None, None)
                | _ -> (None, None, None)

    let getOARInfo (plan:PlanSetup) =
        let OARTypes = [ "AVOIDANCE"; "CAVITY"; "ORGAN"; "CONTROL"; "DOSE_REGION"; "IRRAD_VOLUME"; "TREATED_VOLUME" ]
    
        plan.StructureSet.Structures |>
        Seq.filter (fun x -> OARTypes |> List.contains x.DicomType && (not x.IsEmpty)) |>
        Seq.map (fun x -> 
            sprintf "%s - %0.2f cc%s %s" 
                x.Id
                x.Volume
                (match x.GetNumberOfSeparateParts() with
                    | 1 -> ""
                    | pieces -> sprintf "\n%s %s" tab (warn $"{pieces} pieces"))
                (match checkStructureLaterality plan x.Id x with
                    | _, _, Some error -> warn error
                    | None, _, None -> ""
                    | Some namedLat, Some strucLat, None -> 
                        if namedLat = strucLat
                        then sprintf "\n%sLaterality matches (%s)" tab (pass namedLat)
                        else sprintf "\n%s%s - %s is on the %A" tab (warn "Mismatch") x.Id strucLat
                    | _ -> sprintf "\n%s%s" tab (fail "Error calculating structure laterality (hopefully due to patient orientation)"))) 
        |> String.concat "\n"
        |> stringOutput
                        
    let getBodyInfo (plan:PlanSetup) =
        let bodies = 
            plan.StructureSet.Structures
            |> Seq.filter (fun x -> x.DicomType = "BODY" || x.DicomType = "EXTERNAL")

        match bodies |> Seq.length with
        | 0 -> fail "No body/external structures found"
        | 1 -> 
            let body = bodies |> Seq.exactlyOne
            match body.GetNumberOfSeparateParts() with
            | 1 -> sprintf "%s:\n%s# of pieces: %s" body.Id tab (pass "1 piece")
            | num -> sprintf "%s:\n%s# of pieces: %s" body.Id tab (warn $"{num} pieces")
        | num -> warn (sprintf "%i body/external structures found (%s)" num (bodies |> Seq.map(fun x -> x.Id) |> String.concat ", "))
        |> stringOutput
            
    let getHUOverrides (plan:PlanSetup) =
        let huOverrides = 
            plan.StructureSet.Structures
            |> Seq.filter (fun x -> x.DicomType <> "SUPPORT")
            |> Seq.map (fun x -> (x, x.GetAssignedHU()))
            |> Seq.filter (fun x ->  fst(snd x))

        if Seq.isEmpty huOverrides
        then "No HU overrides"
        else
            warn
                (huOverrides
                |> Seq.map (fun x -> sprintf "%s (%0.1f HU)" (fst x).Id (snd(snd x)))
                |> String.concat "\n")
        |> stringOutput

    let getCouchStructures (plan:PlanSetup) =
        plan.StructureSet.Structures
        |> Seq.filter (fun x -> x.DicomType = "SUPPORT")
        |> Seq.map (fun x -> (x, x.GetAssignedHU()))
        |> Seq.map (fun x -> sprintf "%s (%0.1f HU)" (fst x).Id (snd(snd x)))
        |> String.concat "\n"
        |> stringOutput

    let getContourApprovals (plan:PlanSetup) =
        // Primary oncologist user ID from database
        use oncoCmd = new SqlCommandProvider<SqlQueries.sqlGetOncologistUserId, SqlQueries.connectionString>(SqlQueries.connectionString)
        let oncologistUserId = 
            oncoCmd.Execute(patId = plan.Course.Patient.Id)
            |> Seq.map (fun x -> x.app_user_userid.Value)
            |> Seq.head

        let approvals =
            plan.StructureSet.Structures
            |> Seq.map (fun x -> 
                let approval = 
                    x.ApprovalHistory
                    |> Seq.sortByDescending(fun y ->  y.ApprovalDateTime)
                    |> Seq.head
                (x.Id, approval)
            )

        let distinctApprovals = 
            approvals
            |> Seq.distinctBy (fun x -> (snd x).UserId)
            |> Seq.map (fun x -> snd x)

        if Seq.length distinctApprovals = 1
        then 
            let approval = distinctApprovals |> Seq.exactlyOne
            let result = getPassWarn (approval.UserId = oncologistUserId)
            $"All contours approved by {result approval.UserDisplayName} ({approval.UserId})"
        else
            approvals
            |> Seq.map (fun x -> 
                let result = getIdWarn ((snd x).UserId = oncologistUserId)
                sprintf "%s approved by %s (%s) at %A" (fst x) (result (snd x).UserDisplayName) (snd x).UserId (snd x).ApprovalDateTime)
            |> String.concat "\n"
        |> stringOutput
        
        ///////////////////////////////////////////////////////////
       ///////////////////////////////////////////////////////////
      /////////// Standard Practices and Procedures /////////////
     ///////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////

    let getCourseAndPlanId (plan:PlanSetup) =   
        stringOutput $"Course: {plan.Course.Id}\nPlan: {plan.Id}"

    let getPlanTechnique (plan:PlanSetup) =
        let list =
            plan.Beams
            |> Seq.filter (fun x -> not x.IsSetupField)
            |> Seq.map (fun x -> x.Technique, x.MLCPlanType)
            |> Seq.distinct
    
        if Seq.length list = 1
        then sprintf "%A / %A for all fields" (fst (list |> Seq.exactlyOne)) (snd (list |> Seq.exactlyOne))
        else
            list
            |> Seq.map (fun pair ->
                plan.Beams
                |> Seq.filter (fun beam -> (beam.Technique = fst pair) && (beam.MLCPlanType = snd pair))
                |> Seq.map (fun x -> x.Id)
                |> String.concat ", "
                |> sprintf "%A / %A: %s" (fst pair) (snd pair)) 
            |> String.concat "\n"
        |> stringOutput

    let getDeliverySystem (plan:PlanSetup) =
        let list =
            plan.Beams
            |> Seq.map (fun x -> x.TreatmentUnit)
            |> Seq.distinctBy (fun x -> x.Id)

        match Seq.length list with
            | 0 -> fail "No fields in plan"
            | 1 -> sprintf "%s for all fields" (pass (list |> Seq.exactlyOne).Id)
            | _ -> sprintf "%s - Multiple machines:\n%s"
                    (fail "Error")
                    (plan.Beams
                    |> Seq.map (fun x -> sprintf "%s - %s" x.Id x.TreatmentUnit.Id)
                    |> String.concat "\n")
        |> stringOutput

    let getBeamArrangement (plan:PlanSetup) =
        match Seq.length plan.Beams with
        | 0 -> fail "No fields in plan"
        | _ -> 
            plan.Beams
            |> Seq.filter (fun x -> not x.IsSetupField)
            |> Seq.map (fun x -> 
                sprintf "%s:\n%sGantry: %s\n%sCollimator: %0.1f%s"
                    x.Id
                    tab
                    (match x.GantryDirection with 
                        | GantryDirection.None -> sprintf "%0.1f" (x.GantryAngleToUser((x.ControlPoints |> Seq.head).GantryAngle))
                        | dir -> sprintf "%0.1f -> %0.1f (%A)" (x.ControlPoints |> Seq.head).GantryAngle (x.ControlPoints |> Seq.last).GantryAngle dir)
                    tab
                    (x.CollimatorAngleToUser((x.ControlPoints |> Seq.head).CollimatorAngle))
                    (if (x.ControlPoints |> Seq.head).PatientSupportAngle <> 0.0
                    then 
                        sprintf "\n%sCouch: %0.1f" tab (x.PatientSupportAngleToUser((x.ControlPoints |> Seq.head).PatientSupportAngle))
                    else ""))
            |> String.concat "\n"
        |> stringOutput

    let getBeamMUs (plan:PlanSetup) =
        match Seq.length plan.Beams with
        | 0 -> fail "No fields in plan"
        | _ -> sprintf "%s\n\nTotal = %0.1f MU\nMU factor = %0.1f"
                (plan.Beams
                |> Seq.filter (fun x -> not x.IsSetupField)
                |> Seq.map (fun x -> sprintf "%s - %0.1f MU" x.Id x.Meterset.Value)
                |> String.concat "\n")
                (plan.Beams 
                |> Seq.filter(fun x -> not x.IsSetupField)
                |> Seq.sumBy (fun x -> x.Meterset.Value))
                ((plan.Beams
                |> Seq.filter(fun x -> not x.IsSetupField)
                |> Seq.sumBy (fun x -> x.Meterset.Value)) / plan.DosePerFraction.Dose)
        |> stringOutput

    let getBeamEnergies (plan:PlanSetup) =
        match Seq.length plan.Beams with
        | 0 -> fail "No fields in plan"
        | _ -> 
            let energies =
                plan.Beams
                |> Seq.filter (fun x -> not x.IsSetupField)
                |> Seq.map (fun x -> x.EnergyModeDisplayName)
                |> Seq.distinct
        
            if Seq.length energies = 1
            then 
                sprintf "%s for all fields" (energies |> Seq.exactlyOne)
            else 
                plan.Beams
                |> Seq.filter (fun x -> not x.IsSetupField)
                |> Seq.map (fun x -> sprintf "%s - %s" x.Id x.EnergyModeDisplayName)
                |> String.concat "\n"
        |> stringOutput

    let getBeamDoseRates (plan:PlanSetup) =
        let list =
            plan.Beams
            |> Seq.filter (fun x -> not x.IsSetupField)
            |> Seq.map (fun x -> x.DoseRate)
            |> Seq.distinct

        match Seq.length list with
        | 0 -> fail "No fields in plan"
        | 1 -> sprintf "%i MU/min for all fields" (list |> Seq.exactlyOne)
        | _ -> 
            sprintf "%s - Multiple dose rates:\n%s"
                (warn "warning")
                (plan.Beams
                |> Seq.filter (fun x -> not x.IsSetupField)
                |> Seq.map (fun x -> sprintf "%s - %i" x.Id x.DoseRate)
                |> String.concat "\n")
        |> stringOutput

    let getBolusStructures (plan:PlanSetup) =
        plan.StructureSet.Structures
        |> Seq.filter (fun x -> x.DicomType = "BOLUS")

    let getBeamBolus (plan:PlanSetup) =
        let bolusStructures = plan.StructureSet.Structures |> Seq.filter (fun x -> x.DicomType = "BOLUS")
        let numAttachedToBeams = plan.Beams |> Seq.sumBy(fun x -> Seq.length x.Boluses)

        match Seq.length plan.Beams with
        | 0 -> fail "No fields in plan"
        | _ -> 
            match Seq.length bolusStructures, numAttachedToBeams with
            | 0, _ -> "No bolus structures"
            | numStrucs, numAttached ->
                sprintf "Bolus structure%s:\n%s\nBolus attached to fields:\n%s"
                    (if numStrucs = 0 then "" else "s")
                    // Bolus structures
                    (match numStrucs with
                    | 1 -> sprintf "%s%s (%0.0f HU)" tab (bolusStructures |> Seq.head).Id (snd ((bolusStructures |> Seq.head).GetAssignedHU()))
                    | num ->
                        sprintf "%s%s"
                            (warn $"{tab}{num} bolus structures in plan\n")
                            (bolusStructures
                            |> Seq.map (fun x -> x.Id)
                            |> String.concat($"\n{tab}")))
                    // Boluses attached to fields
                    (match numAttached with
                    | 0 -> warn $"{tab}None"
                    | _ -> 
                        plan.Beams
                        |> Seq.filter (fun x -> not x.IsSetupField)
                        |> Seq.map (fun x ->
                            sprintf "%s%s:\n%s" 
                                tab
                                x.Id 
                                (match Seq.length x.Boluses with
                                | 0 -> warn $"{tab}{tab}None"
                                | 1 -> 
                                    x.Boluses
                                    |> Seq.map (fun bol -> sprintf "%s%s%s (%0.0f HU)" tab tab bol.Id bol.MaterialCTValue)
                                    |> Seq.head
                                | num ->
                                    sprintf "%s\n%s"
                                        (warn $"{num} boluses attached")
                                        (x.Boluses
                                        |> Seq.map (fun bol -> sprintf "%s%s%s (%0.0f HU)" tab tab bol.Id bol.MaterialCTValue)
                                        |> String.concat "\n")))
                        |> String.concat "\n")
        |> stringOutput

    let getCalculationLogs (plan:PlanSetup) =
        let logs = 
            plan.Beams
            |> Seq.filter (fun x -> not x.IsSetupField)
            |> Seq.map (fun x -> 
                x.CalculationLogs
                |> Seq.map (fun y -> 
                    sprintf "%s (%s)\n%s"
                        y.Beam.Id
                        y.Category
                        (y.MessageLines
                        |> Seq.map (fun z -> sprintf "%s%s" tab z)
                        |> Seq.filter (fun z -> z.ToLower().Contains("error") || z.ToLower().Contains("warn"))
                        |> String.concat "\n"))
                |> Seq.filter (fun y -> y.ToLower().Contains("error") || y.ToLower().Contains("warn"))
                |> String.concat "\n")
            |> String.concat "\n"

        if String.IsNullOrWhiteSpace logs
        then pass "No errors or warnings during calculation"
        else warn logs
        |> stringOutput

    let getBeamToleranceTables (plan:PlanSetup) =
        let list =
            plan.Beams
            |> Seq.map (fun x -> x.ToleranceTableLabel)
            |> Seq.distinct

        match Seq.length list with
        | 0 -> fail "No fields in plan"
        | 1 -> sprintf "%s for all fields" (pass (list |> Seq.exactlyOne))
        | _ -> sprintf "%s - Multiple tolerance tables:\n%s"
                (warn "Warning")
                (plan.Beams
                |> Seq.map (fun x -> sprintf "%s - %s" x.Id x.ToleranceTableLabel)
                |> String.concat "\n")
        |> stringOutput
        
        ///////////////////////////////////////////////////////////
       ///////////////////////////////////////////////////////////
      ////// Dose Distribution and Overall Plan Quality /////////
     ///////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////

    type ConstraintType =
    | DoseToVolume of float
    | VolumeAtDose of DoseValue
    | Min
    | Max

    let targetDose constraintType (plan:PlanSetup) = 
        match getTargetStructure plan with
        | None -> warn "No plan target"
        | Some target -> 
            match constraintType with
            | Min -> sprintf "%0.1f%%" (plan.GetDoseAtVolume(target, 100.0, VolumePresentation.Relative, DoseValuePresentation.Relative).Dose)
            | Max -> sprintf "%0.1f%%" (plan.GetDoseAtVolume(target, 0.0, VolumePresentation.Relative, DoseValuePresentation.Relative).Dose)
            | DoseToVolume vol -> sprintf "%0.1f%%" (plan.GetDoseAtVolume(target, vol, VolumePresentation.Relative, DoseValuePresentation.Relative).Dose)
            | VolumeAtDose dose -> sprintf "%0.1f%%" (plan.GetVolumeAtDose(target, dose, VolumePresentation.Relative))
    

    let getTargetCoverage (plan:PlanSetup) =
        match getTargetStructure plan with
        | None -> "No plan target"
        | Some target -> 
            sprintf "Target: %s\n%sMin = %s\n%sV95%% = %s\n%sV100%% = %s\n%sV105%% = %s\n%sV110%% = %s\n%sMax = %s"
                target.Id
                tab
                (targetDose Min plan)
                tab
                (targetDose (VolumeAtDose (new DoseValue(95.0, "%"))) plan)
                tab
                (targetDose (VolumeAtDose (new DoseValue(100.0, "%"))) plan)
                tab
                (targetDose (VolumeAtDose (new DoseValue(105.0, "%"))) plan)
                tab
                (targetDose (VolumeAtDose (new DoseValue(110.0, "%"))) plan)
                tab 
                (targetDose Max plan)
        |> stringOutput

    let getOARMaxDoses (plan:PlanSetup) =
        let OARTypes = [ "AVOIDANCE"; "CAVITY"; "ORGAN" ]
    
        sprintf "OARs with max dose > 2 Gy:\n%s"
            (plan.StructureSet.Structures
            |> Seq.filter (fun x -> OARTypes |> List.contains x.DicomType && (not x.IsEmpty))
            |> Seq.map (fun x -> (x, plan.GetDoseAtVolume(x, 0.0, VolumePresentation.Relative, DoseValuePresentation.Absolute)))
            |> Seq.filter (fun x -> (snd x).Dose > 200.0)
            |> Seq.map (fun x -> sprintf "%s%s - %A" tab (fst x).Id (snd x))
            |> String.concat "\n")
        |> stringOutput

    let getHotspotLocation (plan:PlanSetup) =
        let list =
            plan.StructureSet.Structures
            |> Seq.filter (fun x -> x.DicomType <> "BODY" && x.DicomType <> "EXTERNAL" && (not x.IsEmpty) && x.HasSegment)
            |> Seq.filter (fun x -> x.IsPointInsideSegment(plan.Dose.DoseMax3DLocation))
            |> Seq.sortBy (fun x -> x.Volume)
            |> Seq.map (fun x -> 
                (if x.Id = plan.TargetVolumeID
                then pass 
                else if OARTypes |> List.contains x.DicomType
                then fail
                else id) x.Id)
    
        if Seq.length list = 0
        then warn "Hotspot is not within any contoured structures"
        else 
            sprintf "Hotspot is within:\n%s"
                (list
                |> Seq.map (fun x -> sprintf "%s%s" tab x)
                |> String.concat "\n")
        |> stringOutput

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

        match primaryText, secondariesText with
        | "", "" -> fail "No Primary Reference Point selected for plan"
        | p, "" -> p
        | "", s -> sprintf "%s\n\n%s" (fail "No Primary Reference Point selected for plan") s
        | p, s -> $"{p}\n\n{s}"
        |> stringOutput

    let getPlanNormalization (plan:PlanSetup) =
        sprintf "%s%s"
            plan.PlanNormalizationMethod
            (if plan.PlanNormalizationMethod.Contains("Plan Normalization Value:") then "" else sprintf "\nValue: %0.1f%%" plan.PlanNormalizationValue)
        |> stringOutput

    let getCI (body:Structure) (struc:Structure) (plan:PlanSetup) dose =
        plan.GetVolumeAtDose(body, (new DoseValue(dose, DoseValue.DoseUnit.Percent)), VolumePresentation.AbsoluteCm3) / struc.Volume

    let getTargetCIs (plan:PlanSetup) =
        match getTargetStructure plan with
        | None -> "No plan target"
        | Some target -> 
            match getBodyStructure plan with
            | None -> "Single body structure not found, calculation not possible"
            | Some body -> 
                sprintf "CI100%% = %0.1f\nCI50%% = %0.1f"
                    (getCI body target plan 100.0)
                    (getCI body target plan 50.0)
        |> stringOutput

    type beamType =
    | Electron
    | Photon
    | ThreeD
    | IMRT
    | VMAT
    | RapidPlan
    | Unknown

    let getAllCalculationOptions name (algorithm:Dictionary<string,string>) =
        sprintf "%s\n%s"
            name
            (algorithm
            |> Seq.map (fun x -> sprintf "%s%s = %s" tab x.Key x.Value)
            |> String.concat "\n")

    let getCalculationAlgorithmInfo (plan:PlanSetup) =
        let techniques = 
            plan.Beams
            |> Seq.filter (fun x -> not x.IsSetupField)
            |> Seq.map (fun x -> 
                if x.EnergyModeDisplayName.ToUpper().Contains("E")
                then [Electron]
                else
                    match x.MLCPlanType with
                    | MLCPlanType.VMAT -> [Photon; VMAT]
                    | MLCPlanType.Static 
                    | MLCPlanType.ArcDynamic -> [Photon; ThreeD]
                    | MLCPlanType.DoseDynamic -> 
                        if x.ControlPoints.Count < 25
                        then [Photon; ThreeD]
                        else [Photon; IMRT]
                    | _ -> [Unknown]
            )
            |> Seq.concat
            |> Seq.distinct 
        let techniquesWithRapidPlan = 
            Seq.append techniques (if Seq.length plan.DVHEstimates > 0
                                   then [RapidPlan] |> List.toSeq
                                   else Seq.empty)

        techniquesWithRapidPlan
        |> Seq.map (fun x ->
            match x with
            | Electron -> [getAllCalculationOptions plan.ElectronCalculationModel plan.ElectronCalculationOptions]
            | Photon -> [getAllCalculationOptions plan.PhotonCalculationModel plan.PhotonCalculationOptions]
            | ThreeD -> [""]
            | IMRT -> 
                [ plan.GetCalculationModel(CalculationType.PhotonLeafMotions);
                  plan.GetCalculationModel(CalculationType.PhotonIMRTOptimization) ] |>
                List.map (fun algName -> getAllCalculationOptions algName (plan.GetCalculationOptions(algName)))
            | VMAT -> 
                [ plan.GetCalculationModel(CalculationType.PhotonVMATOptimization) ] |>
                List.map (fun algName -> getAllCalculationOptions algName (plan.GetCalculationOptions(algName)))
            | RapidPlan -> 
                [ plan.GetCalculationModel(CalculationType.DVHEstimation) ] |>
                List.map (fun algName -> getAllCalculationOptions algName (plan.GetCalculationOptions(algName)))
            | Unknown -> [""]
        )
        |> Seq.concat
        |> String.concat "\n\n"
        |> stringOutput
        
        ///////////////////////////////////////////////////////////
       ///////////////////////////////////////////////////////////
      /////////////////// Isocenter Checks //////////////////////
     ///////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////

    let getIsocenters (plan:PlanSetup) = 
            plan.Beams
            |> Seq.map(fun x -> x.IsocenterPosition)
            |> Seq.distinct

    let getIsocenterPlacement (plan:PlanSetup) =
        let isos = getIsocenters plan

        match Seq.length isos with
        | n when n > 1 -> sprintf "%s: %i isocenters detected, please check their positioning manually" (warn "Warning") n
        | 1 -> 
            match getTargetStructure plan with
            | None -> pass "1 isocenter"
            | Some target ->
                if target.IsPointInsideSegment(isos |> Seq.exactlyOne)
                then sprintf "Isocenter is %s target (%s)" (pass "inside") target.Id
                else sprintf "Isocenter is positioned %s of the target (%s)" (warn "outside") target.Id
        | 0 -> warn "No isocenters found (No beams in the plan?)"
        | _ -> fail "Error"
        |> stringOutput

    let getShifts (plan:PlanSetup) =
        // User origin and markers to calculate shifts from
        let shiftFromList = 
            ("User Origin", plan.StructureSet.Image.UserOrigin)
            |> Seq.singleton
            |> Seq.append (plan.StructureSet.Structures |>Seq.filter (fun x -> x.DicomType = "MARKER") |> Seq.map (fun x -> (x.Id, x.CenterPoint)))

        let isos = getIsocenters plan

        shiftFromList
        |> Seq.sortBy (fun x -> if (fst x) = "User Origin" then "zzzzzzz" else (fst x)) // If User Origin is in same location as a marker we want to chose the marker
        |> Seq.distinctBy (fun x -> snd x)
        |> Seq.map(fun pt -> 
            // Point to calc shifts from
            sprintf "Shifts from %s: (%.2f, %.2f, %.2f)%s" 
                (warn (fst pt)) (snd pt).x (snd pt).y (snd pt).z
                (isos
                |> Seq.map(fun iso -> 
                    let shift = (iso - (snd pt)) / 10.0
                    if shift.Length < 0.05
                    then sprintf "\n%sNo shifts" tab
                    else
                        sprintf "%s%s%s" 
                            (if shift.x > 0.1 then sprintf "\n%sPatient left: %.1f cm" tab shift.x      else if shift.x < -0.1 then sprintf "\n%sPatient right: %.1f cm" tab -shift.x    else "")
                            (if shift.z > 0.1 then sprintf "\n%sPatient superior: %.1f cm" tab shift.z  else if shift.z < -0.1 then sprintf "\n%sPatient inferior: %.1f cm" tab -shift.z else "")
                            (if shift.y > 0.1 then sprintf "\n%sPatient posterior: %.1f cm" tab shift.y else if shift.y < -0.1 then sprintf "\n%sPatient anterior: %.1f cm" tab -shift.y else "")
                )
                |> String.concat "\n") 
        )
        |> String.concat "\n"
        |> stringOutput

    let getNumberOfIsocenters (plan:PlanSetup) =
        let num = 
            plan.Beams
            |> Seq.map(fun x -> x.IsocenterPosition)
            |> Seq.distinct
            |> Seq.length

        match num with
        | n when n > 1 -> sprintf "%s: %i isocenters detected" (warn "Warning") n
        | 1 -> pass "1 isocenter"
        | 0 -> warn "No isocenters found (No beams in the plan?)"
        | _ -> fail "Error"
        |> stringOutput
        
        ///////////////////////////////////////////////////////////
       ///////////////////////////////////////////////////////////
      ////////////////////// Scheduling /////////////////////////
     ///////////////////////////////////////////////////////////
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

        stringOutput $"{getPassWarn (numSessions = plan.NumberOfFractions.GetValueOrDefault()) numSessionsText} scheduled for treatment {overlappingSessionsText}"