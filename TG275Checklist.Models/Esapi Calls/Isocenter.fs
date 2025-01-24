namespace TG275Checklist.EsapiCalls

open VMS.TPS.Common.Model.API
open CommonHelpers
open TG275Checklist.Model.EsapiService

module Isocenter =

    let getIsocenters (plan:PlanSetup) = 
            plan.Beams
            |> Seq.map(fun x -> x.IsocenterPosition)
            |> Seq.distinct

    let getIsocenterPlacement: EsapiCall = fun plan ->
        let isos = getIsocenters plan

        match Seq.length isos with
        | n when n > 1 -> $"""{ValidatedText(WarnWithoutExplanation, "Warning")}: {ValidatedText(WarnWithoutExplanation, n.ToString())} isocenters detected"""
        | 1 -> 
            match getTargetStructure plan with
            | None -> $"""{ValidatedText(Pass, "1")} Isocenter"""
            | Some target ->
                if target.IsPointInsideSegment(isos |> Seq.exactlyOne)
                then $"""Isocenter is {ValidatedText(Pass, "inside")} target ({target.Id})"""
                else $"""Isocenter is positioned {ValidatedText(WarnWithoutExplanation, "outside")} of the target: {target.Id}"""
        | 0 -> ValidatedText(Warn "No beams in the plan?", "No isocenters found").ToString()
        | _ -> ValidatedText(Fail "Somehow got a negative number of isocenters", "Error").ToString()
        |> EsapiResults.fromString

    let getShifts: EsapiCall = fun plan ->
        // User origin and markers to calculate shifts from
        let shiftFromList = 
            ("User Origin", plan.StructureSet.Image.UserOrigin)
            |> Seq.singleton
            |> Seq.append (plan.StructureSet.Structures |>Seq.filter (fun x -> x.DicomType = "MARKER") |> Seq.map (fun x -> (x.Id, x.CenterPoint)))

        let isos = getIsocenters plan

        let isocenterShifts =
            shiftFromList
            |> Seq.sortBy (fun x -> if (fst x) = "User Origin" then "zzzzzzz" else (fst x)) // If User Origin is in same location as a marker we want to chose the marker
            |> Seq.distinctBy (fun x -> snd x)
            |> Seq.map(fun pt -> 
                // Point to calc shifts from
                sprintf "Shifts from %A: (%.2f, %.2f, %.2f)%s" 
                    (ValidatedText(Highlight, (fst pt))) 
                    (((snd pt).x - plan.StructureSet.Image.UserOrigin.x) / 10.0)
                    (((snd pt).y - plan.StructureSet.Image.UserOrigin.y) / 10.0)
                    (((snd pt).z - plan.StructureSet.Image.UserOrigin.z) / 10.0)
                    (isos
                    |> Seq.map(fun iso -> 
                        let shift = (iso - (snd pt)) / 10.0
                        if shift.Length < 0.05
                        then sprintf "\n%sNo shifts" tab
                        else
                            sprintf "%s%s%s" 
                                (if shift.x > 0.1 then sprintf "\n%sLeft: %.1f cm" tab shift.x      else if shift.x < -0.1 then sprintf "\n%sRight: %.1f cm" tab -shift.x    else "")
                                (if shift.y > 0.1 then sprintf "\n%sUp: %.1f cm" tab shift.y else if shift.y < -0.1 then sprintf "\n%sDown: %.1f cm" tab -shift.y else "")
                                (if shift.z > 0.1 then sprintf "\n%sOut: %.1f cm" tab shift.z  else if shift.z < -0.1 then sprintf "\n%sIn: %.1f cm" tab -shift.z else ""))
                    |> String.concat "\n"))
            |> String.concat "\n"
        
        let setupShifts = getSetupNotesAsString plan

        $"{isocenterShifts}\n\n{setupShifts}" |> EsapiResults.fromString

    let getNumberOfIsocenters: EsapiCall = fun plan ->
        let num = 
            plan.Beams
            |> Seq.map(fun x -> x.IsocenterPosition)
            |> Seq.distinct
            |> Seq.length

        (match num with
        | n when n > 1 -> ValidatedText(WarnWithoutExplanation, $"{n} isocenters detected")
        | 1 -> ValidatedText(Pass, "1 isocenter")
        | 0 -> ValidatedText(Warn "No beams in the plan?", "No isocenters found")
        | _ -> ValidatedText(Fail "Somehow got a negative number of isocenters", "Error")).ToString()
        |> EsapiResults.fromString
