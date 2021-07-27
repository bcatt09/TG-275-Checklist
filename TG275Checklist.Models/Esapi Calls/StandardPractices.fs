namespace TG275Checklist.EsapiCalls

open System
open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types
open CommonHelpers

module StandardPractices =

    let getCourseAndPlanId (plan: PlanSetup) =   
        stringOutput $"Course: {plan.Course.Id}\nPlan: {plan.Id}"

    let getPlanTechnique (plan: PlanSetup) =
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

    let getDeliverySystem (plan: PlanSetup) =
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

    let getBeamArrangement (plan: PlanSetup) =
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

    let getBeamMUs (plan: PlanSetup) =
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

    let getBeamEnergies (plan: PlanSetup) =
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

    let getBeamDoseRates (plan: PlanSetup) =
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

    let getBolusStructures (plan: PlanSetup) =
        plan.StructureSet.Structures
        |> Seq.filter (fun x -> x.DicomType = "BOLUS")

    let getBeamBolus (plan: PlanSetup) =
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

    let getCalculationLogs (plan: PlanSetup) =
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

    let getBeamToleranceTables (plan: PlanSetup) =
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
