namespace TG275Checklist.EsapiCalls

open System
open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types
open CommonHelpers
open TG275Checklist.Model.EsapiService

module StandardPractices =

    let getCourseAndPlanId: EsapiCall = fun plan ->
        EsapiResults.fromString $"Course: {plan.Course.Id}\nPlan: {plan.Id}"

    let getPlanTechnique: EsapiCall = fun plan ->
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
        |> EsapiResults.fromString

    let getDeliverySystem: EsapiCall = fun plan ->
        let list =
            plan.Beams
            |> Seq.map (fun x -> x.TreatmentUnit)
            |> Seq.distinctBy (fun x -> x.Id)

        match Seq.length list with
            | 0 -> ValidatedText(WarnWithoutExplanation, "No fields in plan").ToString()
            | 1 -> $"{ValidatedText(Pass, (list |> Seq.exactlyOne).Id)} for all fields"
            | _ -> $"""{ValidatedText(Fail "Multiple machines", "Error")}:{'\n'}{(plan.Beams |> Seq.map (fun x -> $"{x.Id} - {x.TreatmentUnit.Id}") |> String.concat "\n")}"""
        |> EsapiResults.fromString

    let getBeamArrangement: EsapiCall = fun plan ->
        match Seq.length plan.Beams with
        | 0 -> ValidatedText(WarnWithoutExplanation, "No fields in plan").ToString()
        | _ -> 
            plan.Beams
            |> Seq.filter (fun x -> not x.IsSetupField)
            |> Seq.map (fun x -> 
                sprintf "%s:\n%sGantry: %s\n%sCollimator: %0.1f%s"
                    x.Id
                    tab
                    (match x.GantryDirection with 
                        | GantryDirection.None -> sprintf "%0.1f%s" (x.GantryAngleToUser((x.ControlPoints |> Seq.head).GantryAngle)) (if x.IsGantryExtended then "E" else "")
                        | dir -> sprintf "%0.1f -> %0.1f (%A)" (x.ControlPoints |> Seq.head).GantryAngle (x.ControlPoints |> Seq.last).GantryAngle dir)
                    tab
                    (x.CollimatorAngleToUser((x.ControlPoints |> Seq.head).CollimatorAngle))
                    (if (x.ControlPoints |> Seq.head).PatientSupportAngle <> 0.0
                    then 
                        sprintf "\n%sCouch: %0.1f" tab (x.PatientSupportAngleToUser((x.ControlPoints |> Seq.head).PatientSupportAngle))
                    else ""))
            |> String.concat "\n"
        |> EsapiResults.fromString

    let getBeamMUs: EsapiCall = fun plan ->
        match Seq.length plan.Beams with
        | 0 -> ValidatedText(WarnWithoutExplanation, "No fields in plan").ToString()
        | _ -> sprintf "%s\n\nTotal = %0.1f MU\nMU factor = %0.1f"
                (plan.Beams
                |> Seq.filter (fun x -> not x.IsSetupField)
                |> Seq.map (fun x -> $"{x.Id} - %0.1f{x.Meterset.Value} MU")
                |> String.concat "\n")
                (plan.Beams 
                |> Seq.filter(fun x -> not x.IsSetupField)
                |> Seq.sumBy (fun x -> x.Meterset.Value))
                ((plan.Beams
                |> Seq.filter(fun x -> not x.IsSetupField)
                |> Seq.sumBy (fun x -> x.Meterset.Value)) / plan.DosePerFraction.Dose)
        |> EsapiResults.fromString

    let getBeamEnergies: EsapiCall = fun plan ->
        match Seq.length plan.Beams with
        | 0 -> ValidatedText(WarnWithoutExplanation, "No fields in plan").ToString()
        | _ -> 
            let energies =
                plan.Beams
                |> Seq.filter (fun x -> not x.IsSetupField)
                |> Seq.map (fun x -> x.EnergyModeDisplayName)
                |> Seq.distinct
        
            if Seq.length energies = 1
            then 
                $"{energies |> Seq.exactlyOne} for all fields"
            else 
                plan.Beams
                |> Seq.filter (fun x -> not x.IsSetupField)
                |> Seq.map (fun x -> $"{x.Id} - {x.EnergyModeDisplayName}")
                |> String.concat "\n"
        |> EsapiResults.fromString

    let getBeamDoseRates: EsapiCall = fun plan ->
        let list =
            plan.Beams
            |> Seq.filter (fun x -> not x.IsSetupField)
            |> Seq.map (fun x -> x.DoseRate)
            |> Seq.distinct

        match Seq.length list with
        | 0 -> ValidatedText(WarnWithoutExplanation, "No fields in plan").ToString()
        | 1 -> sprintf "%i MU/min for all fields" (list |> Seq.exactlyOne)
        | _ -> $"""{ValidatedText(Warn "Multiple dose rates", "Error")}:{'\n'}{(plan.Beams |> Seq.filter (fun x -> not x.IsSetupField) |> Seq.map (fun x -> $"{x.Id} - {x.DoseRate}") |> String.concat "\n")}"""
        |> EsapiResults.fromString

    let getBolusStructures (plan: PlanSetup) =
        plan.StructureSet.Structures
        |> Seq.filter (fun x -> x.DicomType = "BOLUS")

    let getBeamBolus: EsapiCall = fun plan ->
        let bolusStructures = plan.StructureSet.Structures |> Seq.filter (fun x -> x.DicomType = "BOLUS")
        let numAttachedToBeams = plan.Beams |> Seq.sumBy(fun x -> Seq.length x.Boluses)

        match Seq.length plan.Beams with
        | 0 -> ValidatedText(WarnWithoutExplanation, "No fields in plan").ToString()
        | _ -> 
            match Seq.length bolusStructures, numAttachedToBeams with
            | 0, _ -> "No bolus structures"
            | numStrucs, numAttached ->
                sprintf "Bolus structure%s:\n%s\nBolus attached to fields:\n%s"
                    (if numStrucs = 0 then "" else "s")
                    // Bolus structures
                    (match numStrucs with
                    | 1 -> $"{tab}{(bolusStructures |> Seq.head).Id} (%0.0f{snd ((bolusStructures |> Seq.head).GetAssignedHU())} HU)"
                    | num -> $"""({ValidatedText(WarnWithoutExplanation, $"{tab}{num} bolus structures in plan\n")} {bolusStructures |> Seq.map (fun x -> x.Id) |> String.concat($"\n{tab}")}""")
                    // Boluses attached to fields
                    (match numAttached with
                    | 0 -> $"""{tab}{ValidatedText(WarnWithoutExplanation, "None")}"""
                    | _ -> 
                        plan.Beams
                        |> Seq.filter (fun x -> not x.IsSetupField)
                        |> Seq.map (fun x ->
                            sprintf "%s%s:\n%s" 
                                tab
                                x.Id 
                                (match Seq.length x.Boluses with
                                | 0 -> $"""{tab}{tab}{ValidatedText(WarnWithoutExplanation, "None")}"""
                                | 1 -> 
                                    x.Boluses
                                    |> Seq.map (fun bol -> sprintf "%s%s%s (%0.0f HU)" tab tab bol.Id bol.MaterialCTValue)
                                    |> Seq.head
                                | num ->
                                    sprintf "%s\n%s"
                                        ($"{ValidatedText(WarnWithoutExplanation, num.ToString())} boluses attached")
                                        (x.Boluses
                                        |> Seq.map (fun bol -> sprintf "%s%s%s (%0.0f HU)" tab tab bol.Id bol.MaterialCTValue)
                                        |> String.concat "\n")))
                        |> String.concat "\n")
        |> EsapiResults.fromString

    let getCalculationLogs: EsapiCall = fun plan ->
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
        then ValidatedText(Pass, "No errors or warnings during calculation").ToString()
        else ValidatedText(WarnWithoutExplanation, logs).ToString()
        |> EsapiResults.fromString

    let getBeamToleranceTables: EsapiCall = fun plan ->
        let list =
            plan.Beams
            |> Seq.map (fun x -> x.ToleranceTableLabel)
            |> Seq.distinct

        match Seq.length list with
        | 0 -> ValidatedText(WarnWithoutExplanation, "No fields in plan").ToString()
        | 1 -> $"{ValidatedText(Pass, list |> Seq.exactlyOne)} for all fields"
        | _ -> $"""{ValidatedText(Warn "Multiple tolerance tables", "Warning")}:{'\n'}{plan.Beams |> Seq.map (fun x -> $"{x.Id} - {x.ToleranceTableLabel}") |> String.concat "\n"}"""
        |> EsapiResults.fromString
