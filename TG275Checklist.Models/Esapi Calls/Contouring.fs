namespace TG275Checklist.EsapiCalls

open System.Text.RegularExpressions
open TG275Checklist.Sql
open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types
open CommonHelpers

module Contouring =

    let (|ParseLateralityFromInputString|_|) (regex: string) (str: string) =
        let m = Regex(regex).Match(str.ToUpper())
        if m.Success 
        then Some (match m.Groups.[1].Value with | "L" -> Left | "R" -> Right | _ -> NA)
        else None

    let (|GetLateralityFromStructure|_|) (plan: PlanSetup) (body: Structure) (s: Structure) = 
        let offset = s.CenterPoint.x - body.CenterPoint.x

        match plan.TreatmentOrientation with
        | PatientOrientation.HeadFirstSupine 
        | PatientOrientation.FeetFirstProne
        | PatientOrientation.HeadFirstProne 
        | PatientOrientation.FeetFirstSupine -> Some (if offset < 0.0 then Right else Left)
        | _ -> None

    let getTargetInfo (plan: PlanSetup) = 
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

    let checkStructureLaterality (plan: PlanSetup) (name: string) (struc: Structure) =
        match getBodyStructure plan with
        | None -> None, None, Some "Single body structure not found, calculation not possible"
        | Some body ->
            match name with
                | ParseLateralityFromInputString @"_(R|L)T?( |_)?" nameLaterality -> 
                        match struc with
                        | GetLateralityFromStructure plan body structureLaterality -> (Some nameLaterality, Some structureLaterality, None)
                        | _ -> (None, None, None)
                | _ -> (None, None, None)

    let getOARInfo (plan: PlanSetup) =
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
                        
    let getBodyInfo (plan: PlanSetup) =
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
            
    let getHUOverrides (plan: PlanSetup) =
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

    let getCouchStructures (plan: PlanSetup) =
        plan.StructureSet.Structures
        |> Seq.filter (fun x -> x.DicomType = "SUPPORT")
        |> Seq.map (fun x -> (x, x.GetAssignedHU()))
        |> Seq.map (fun x -> sprintf "%s (%0.1f HU)" (fst x).Id (snd(snd x)))
        |> String.concat "\n"
        |> stringOutput

    let getContourApprovals (plan: PlanSetup) =
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
            let result =
                // Primary oncologist user ID from database
                match sqlGetOncologistUserId plan.Course.Patient.Id with
                | Error _ -> id
                | Ok oncologistUserId -> getPassWarn (approval.UserId = oncologistUserId)    // Check if each structure has been approved by the primary oncologist
            $"All contours approved by {result approval.UserDisplayName} ({approval.UserId})"
        else
            approvals
            |> Seq.map (fun x -> 
                let result = 
                    // Primary oncologist user ID from database
                    match sqlGetOncologistUserId plan.Course.Patient.Id with
                    | Error _ -> id
                    | Ok oncologistUserId -> getIdWarn ((snd x).UserId = oncologistUserId)   // Check if structures have been approved by the primary oncologist
                sprintf "%s approved by %s (%s) at %A" (fst x) (result (snd x).UserDisplayName) (snd x).UserId (snd x).ApprovalDateTime)
            |> String.concat "\n"
        |> stringOutput
