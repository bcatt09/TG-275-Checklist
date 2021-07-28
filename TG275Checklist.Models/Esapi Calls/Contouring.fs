namespace TG275Checklist.EsapiCalls

open System.Text.RegularExpressions
open TG275Checklist.Sql
open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types
open CommonHelpers
open TG275Checklist.Model.EsapiService

module Contouring =

    let (|ParseLateralityFromInputString|_|) (regex: string) (str: string) =
        let m = Regex(regex).Match(str.ToUpper())
        if m.Success 
        then Some (match m.Groups.[1].Value with | "L" -> Left | "R" -> Right | _ -> NoLaterality)
        else None

    let (|GetLateralityFromStructure|_|) (plan: PlanSetup) (body: Structure) (s: Structure) = 
        let offset = s.CenterPoint.x - body.CenterPoint.x

        match plan.TreatmentOrientation with
        | PatientOrientation.HeadFirstSupine 
        | PatientOrientation.FeetFirstProne
        | PatientOrientation.HeadFirstProne 
        | PatientOrientation.FeetFirstSupine -> Some (if offset < 0.0 then Right else Left)
        | _ -> None

    let getTargetInfo: EsapiCall = fun plan ->
        match getTargetStructure plan with
        | None -> ValidatedText(WarnWithoutExplanation, "No plan target volume").ToString()
        | Some target -> 
            let targetLateralityText =
                match getBodyStructure plan with
                | None -> ValidatedText(Warn "Lateralitty calculation not possible", "Single body structure not found").ToString()
                | Some body -> 
                    match plan.Id with
                        | ParseLateralityFromInputString @"^(L|R)T?( |_)" planLaterality -> 
                                match target with
                                | GetLateralityFromStructure plan body targetLaterality ->
                                    if planLaterality = targetLaterality
                                    then $"Plan and target agree: ({ValidatedText(Pass, planLaterality.ToString())})"
                                    else $"""{ValidatedText(WarnWithoutExplanation, "Mismatch")}{'\n'}Plan is named for {ValidatedText(WarnWithoutExplanation, planLaterality)}{'\n'}Plan target is on the {ValidatedText(WarnWithoutExplanation, targetLaterality)} side of {body.Id}"""
                                | _ -> ValidatedText(Warn "Patient orientation not supported", plan.TreatmentOrientation).ToString()
                        | _ -> sprintf "None specified"

            sprintf "Target: %s\n%s# of pieces: %A\n%sLaterality: %s\n%sVolume: %0.2f cc"
                plan.TargetVolumeID
                tab
                (match target.GetNumberOfSeparateParts() with
                | 1 -> ValidatedText(Pass, "1 piece")
                | n -> ValidatedText(WarnWithoutExplanation, $"{n} pieces"))
                tab
                targetLateralityText
                tab
                target.Volume
        |> EsapiResults.fromString

    let checkStructureLaterality (plan: PlanSetup) (name: string) (struc: Structure) =
        match getBodyStructure plan with
        | None -> None, None, Some (ValidatedText(Warn "Single body structure not found", "Structure laterality calculation not possible"))
        | Some body ->
            match name with
                | ParseLateralityFromInputString @"_(R|L)T?( |_)?" nameLaterality -> 
                        match struc with
                        | GetLateralityFromStructure plan body structureLaterality -> (Some nameLaterality, Some structureLaterality, None)
                        | _ -> (None, None, None)
                | _ -> (None, None, None)

    let getOARInfo: EsapiCall = fun plan ->
        let OARTypes = [ "AVOIDANCE"; "CAVITY"; "ORGAN"; "CONTROL"; "DOSE_REGION"; "IRRAD_VOLUME"; "TREATED_VOLUME" ]
    
        plan.StructureSet.Structures |>
        Seq.filter (fun x -> OARTypes |> List.contains x.DicomType && (not x.IsEmpty)) |>
        Seq.map (fun x -> 
            sprintf "%s - %0.2f cc%s %s" 
                x.Id
                x.Volume
                (match x.GetNumberOfSeparateParts() with
                    | 1 -> ""
                    | pieces -> $"""{'\n'}{tab} {ValidatedText(WarnWithoutExplanation, $"{pieces} pieces")}""")
                (match checkStructureLaterality plan x.Id x with
                    | _, _, Some error -> error.ToString()
                    | None, _, None -> ""
                    | Some namedLat, Some strucLat, None -> 
                        if namedLat = strucLat
                        then $"\n{tab}Laterality matches ({ValidatedText(Pass, namedLat)})"
                        else $"""{'\n'}{tab}{ValidatedText(Warn "laterality mismatch", $"{x.Id} is on the {strucLat}")}"""
                    | _ -> $"""{'\n'}{tab}{ValidatedText(Warn "hopefully due to patient orientation", "Error calculating structure laterality")}""")) 
        |> String.concat "\n"
        |> EsapiResults.fromString
                        
    let getBodyInfo: EsapiCall = fun plan ->
        let bodies = 
            plan.StructureSet.Structures
            |> Seq.filter (fun x -> x.DicomType = "BODY" || x.DicomType = "EXTERNAL")

        match bodies |> Seq.length with
        | 0 -> ValidatedText(Fail "No body/external structures found", "Error").ToString()
        | 1 -> 
            let body = bodies |> Seq.exactlyOne
            match body.GetNumberOfSeparateParts() with
            | 1 -> $"""{body.Id}:{'\n'}{tab}# of pieces: {ValidatedText(Pass, "1 piece")}"""
            | num -> $"""{body.Id}:{'\n'}{tab}# of pieces: {ValidatedText(WarnWithoutExplanation, $"{num} piece")}"""
        | num -> ValidatedText(WarnWithoutExplanation, (sprintf "%i body/external structures found (%s)" num (bodies |> Seq.map(fun x -> x.Id) |> String.concat ", "))).ToString()
        |> EsapiResults.fromString
            
    let getHUOverrides: EsapiCall = fun plan ->
        let huOverrides = 
            plan.StructureSet.Structures
            |> Seq.filter (fun x -> x.DicomType <> "SUPPORT")
            |> Seq.map (fun x -> (x, x.GetAssignedHU()))
            |> Seq.filter (fun x ->  fst(snd x))

        if Seq.isEmpty huOverrides
        then "No HU overrides"
        else
            ValidatedText(WarnWithoutExplanation,
                (huOverrides
                |> Seq.map (fun x -> sprintf "%s (%0.1f HU)" (fst x).Id (snd(snd x)))
                |> String.concat "\n")).ToString()
        |> EsapiResults.fromString

    let getCouchStructures: EsapiCall = fun plan ->
        plan.StructureSet.Structures
        |> Seq.filter (fun x -> x.DicomType = "SUPPORT")
        |> Seq.map (fun x -> (x, x.GetAssignedHU()))
        |> Seq.map (fun x -> sprintf "%s (%0.1f HU)" (fst x).Id (snd(snd x)))
        |> String.concat "\n"
        |> EsapiResults.fromString

    let getContourApprovals: EsapiCall = fun plan ->
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
                | Error error -> Id
                | Ok oncologistUserId -> getPassWarn "Structure not approved by primary oncologist" (approval.UserId = oncologistUserId)    // Check if each structure has been approved by the primary oncologist
            $"All contours approved by {ValidatedText(result, approval.UserDisplayName)} ({approval.UserId})"
        else
            approvals
            |> Seq.map (fun x -> 
                let result = 
                    // Primary oncologist user ID from database
                    match sqlGetOncologistUserId plan.Course.Patient.Id with
                    | Error _ -> Id
                    | Ok oncologistUserId -> getIdWarn "Structure not approves by primary oncologist" ((snd x).UserId = oncologistUserId)   // Check if structures have been approved by the primary oncologist
                sprintf "%s approved by %A (%s) at %A" (fst x) (ValidatedText(result, (snd x).UserDisplayName)) (snd x).UserId (snd x).ApprovalDateTime)
            |> String.concat "\n"
        |> EsapiResults.fromString
