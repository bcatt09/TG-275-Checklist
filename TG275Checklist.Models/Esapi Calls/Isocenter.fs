namespace TG275Checklist.EsapiCalls

open VMS.TPS.Common.Model.API
open CommonHelpers

module Isocenter =

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

        let isocenterShifts =
            shiftFromList
            |> Seq.sortBy (fun x -> if (fst x) = "User Origin" then "zzzzzzz" else (fst x)) // If User Origin is in same location as a marker we want to chose the marker
            |> Seq.distinctBy (fun x -> snd x)
            |> Seq.map(fun pt -> 
                // Point to calc shifts from
                sprintf "Shifts from %s: (%.2f, %.2f, %.2f)%s" 
                    (warn (fst pt)) 
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
                                (if shift.x > 0.1 then sprintf "\n%sPatient left: %.1f cm" tab shift.x      else if shift.x < -0.1 then sprintf "\n%sPatient right: %.1f cm" tab -shift.x    else "")
                                (if shift.z > 0.1 then sprintf "\n%sPatient superior: %.1f cm" tab shift.z  else if shift.z < -0.1 then sprintf "\n%sPatient inferior: %.1f cm" tab -shift.z else "")
                                (if shift.y > 0.1 then sprintf "\n%sPatient posterior: %.1f cm" tab shift.y else if shift.y < -0.1 then sprintf "\n%sPatient anterior: %.1f cm" tab -shift.y else ""))
                    |> String.concat "\n"))
            |> String.concat "\n"
        
        let setupShifts = getListOfSetupNotes plan

        $"{isocenterShifts}\n\n{setupShifts}" |> stringOutput

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
