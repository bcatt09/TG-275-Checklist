namespace TG275Checklist.EsapiCalls

open System.Collections.Generic
open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types
open CommonHelpers
open TG275Checklist.Model.EsapiService
open TG275Checklist.Model.HelperDataTypes
open System.Collections.Generic

module DoseDistributionAndPlanQuality =

    type ConstraintType =
    | DoseToVolume of float
    | VolumeAtDose of DoseValue
    | Min
    | Max

    let coverageStatistics (structure: Structure option) constraintType (plan: PlanSetup) = 
        match structure with
        | None -> ValidatedText(WarnWithoutExplanation, "No plan target").ToString()
        | Some target -> 
            match constraintType with
            | Min -> $"%0.1f{plan.GetDoseAtVolume(target, 100.0, VolumePresentation.Relative, DoseValuePresentation.Relative).Dose}%%"
            | Max -> $"%0.1f{plan.GetDoseAtVolume(target, 0.0, VolumePresentation.Relative, DoseValuePresentation.Relative).Dose}%%"
            | DoseToVolume vol -> $"%0.1f{plan.GetDoseAtVolume(target, vol, VolumePresentation.Relative, DoseValuePresentation.Relative).Dose}%%"
            | VolumeAtDose dose -> $"%0.1f{plan.GetVolumeAtDose(target, dose, VolumePresentation.Relative)}%%"

    let targetDose constraintType (plan: PlanSetup) = 
        coverageStatistics (getTargetStructure plan) constraintType plan

    let getTargetCoverage: EsapiCall = fun plan ->
        let targetTypeFilterList = ["BODY"; "EXTERNAL"; "SUPPORT"; "FIXATION"; "MARKER"; "AVOIDANCE"]
        let strucIds = 
            plan.StructureSet.Structures
            |> Seq.filter(fun s -> not (List.contains s.DicomType targetTypeFilterList))
            |> Seq.map(fun s -> s.Id)
        let resultsDict = 
            strucIds 
            |> Seq.map(fun x -> x, 
                                match getStructureById plan x with 
                                | None -> "No target structure selected" 
                                | target -> 
                                    sprintf "Min = %s\nD95%% = %s\nV95%% = %s\nV100%% = %s\nV105%% = %s\nV110%% = %s\nMax = %s"
                                        (coverageStatistics target Min plan)
                                        (coverageStatistics target (DoseToVolume 95.0) plan)
                                        (coverageStatistics target (VolumeAtDose (new DoseValue(95.0, "%"))) plan)
                                        (coverageStatistics target (VolumeAtDose (new DoseValue(100.0, "%"))) plan)
                                        (coverageStatistics target (VolumeAtDose (new DoseValue(105.0, "%"))) plan)
                                        (coverageStatistics target (VolumeAtDose (new DoseValue(110.0, "%"))) plan)
                                        (coverageStatistics target Max plan) ) 
            |> dict 
            |> Dictionary
        { EsapiResults.init with 
            TargetCoverageDropdown = 
                Some {
                    TargetList = strucIds |> Seq.toList
                    SelectedTarget = plan.TargetVolumeID
                    Results = resultsDict
                    DisplayedResults =
                        match plan.TargetVolumeID with
                        | "" -> "No target structure selected"
                        | target -> resultsDict.[target] } }

    //let getTargetCoverage: EsapiCall = fun plan ->
    //    match getTargetStructure plan with
    //    | None -> "No plan target"
    //    | Some target -> 
    //        sprintf "Target: %s\n%sMin = %s\n%sV95%% = %s\n%sV100%% = %s\n%sV105%% = %s\n%sV110%% = %s\n%sMax = %s"
    //            target.Id
    //            tab
    //            (targetDose Min plan)
    //            tab
    //            (targetDose (VolumeAtDose (new DoseValue(95.0, "%"))) plan)
    //            tab
    //            (targetDose (VolumeAtDose (new DoseValue(100.0, "%"))) plan)
    //            tab
    //            (targetDose (VolumeAtDose (new DoseValue(105.0, "%"))) plan)
    //            tab
    //            (targetDose (VolumeAtDose (new DoseValue(110.0, "%"))) plan)
    //            tab 
    //            (targetDose Max plan)
    //    |> EsapiResults.fromString

    let getOARMaxDoses: EsapiCall = fun plan ->
        let OARTypes = [ "AVOIDANCE"; "CAVITY"; "ORGAN" ]
    
        sprintf "OARs with max dose > 2 Gy:\n%s"
            (plan.StructureSet.Structures
            |> Seq.filter (fun x -> OARTypes |> List.contains x.DicomType && (not x.IsEmpty))
            |> Seq.map (fun x -> (x, plan.GetDoseAtVolume(x, 0.0, VolumePresentation.Relative, DoseValuePresentation.Absolute)))
            |> Seq.filter (fun x -> (snd x).Dose > 200.0)
            |> Seq.map (fun x -> sprintf "%s%s - %A" tab (fst x).Id (snd x))
            |> String.concat "\n")
        |> EsapiResults.fromString

    let getHotspotLocation: EsapiCall = fun plan ->
        let list =
            plan.StructureSet.Structures
            |> Seq.filter (fun x -> x.DicomType <> "BODY" && x.DicomType <> "EXTERNAL" && (not x.IsEmpty) && x.HasSegment)
            |> Seq.filter (fun x -> x.IsPointInsideSegment(plan.Dose.DoseMax3DLocation))
            |> Seq.sortBy (fun x -> x.Volume)
            |> Seq.map (fun x ->  
                let result =
                    if x.Id = plan.TargetVolumeID
                    then Pass 
                    else if OARTypes |> List.contains x.DicomType
                    then WarnWithoutExplanation
                    else Id 
                ValidatedText(result, x.Id))
    
        if Seq.length list = 0
        then ValidatedText(WarnWithoutExplanation, "Hotspot is not within any contoured structures").ToString()
        else $"""Hotspot is within:{'\n'}{list |> Seq.map (fun x -> $"{tab}{x}") |> String.concat "\n"}"""
        |> EsapiResults.fromString

    let getReferencePoints: EsapiCall = fun plan ->
        let primary = plan.PrimaryReferencePoint
        let primaryText = 
            if isNull primary 
            then "" 
            else $"""Primary Plan Reference Point:
{tab}{primary.Id}:
{tab}{tab}Session dose limit: {ValidatedText(getPassWarn "Session dose limit doesn't match planned fraction dose" (primary.SessionDoseLimit = plan.DosePerFraction), primary.SessionDoseLimit)}
{tab}{tab}Daily dose limit: {primary.DailyDoseLimit}
{tab}{tab}Total dose limit: {ValidatedText(getPassWarn "Total dose limit doesn't match plan dose" (primary.TotalDoseLimit = plan.TotalDose), primary.TotalDoseLimit)}
{tab}{tab}Has a location: {primary.HasLocation(plan)}"""
        
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
        | "", "" -> ValidatedText(WarnWithoutExplanation, "No Primary Reference Point selected for plan").ToString()
        | p, "" -> p
        | "", s -> $"""{ValidatedText(WarnWithoutExplanation, "No Primary Reference Point selected for plan")}{"\n\n"}{s}"""
        | p, s -> $"{p}\n\n{s}"
        |> EsapiResults.fromString

    let getPlanNormalization: EsapiCall = fun plan ->
        sprintf "%s%s"
            plan.PlanNormalizationMethod
            (if plan.PlanNormalizationMethod.Contains("Plan Normalization Value:") then "" else sprintf "\nValue: %0.1f%%" plan.PlanNormalizationValue)
        |> EsapiResults.fromString

    let getCI (body:Structure) (struc:Structure) (plan: PlanSetup) dose =
        plan.GetVolumeAtDose(body, (new DoseValue(dose, DoseValue.DoseUnit.Percent)), VolumePresentation.AbsoluteCm3) / struc.Volume

    let getTargetCIs: EsapiCall = fun plan ->
        match getTargetStructure plan with
        | None -> "No plan target"
        | Some target -> 
            match getBodyStructure plan with
            | None -> "Single body structure not found, calculation not possible"
            | Some body -> 
                sprintf "CI100%% = %0.1f\nCI50%% = %0.1f"
                    (getCI body target plan 100.0)
                    (getCI body target plan 50.0)
        |> EsapiResults.fromString

    type beamType =
    | Electron
    | Photon
    | ThreeD
    | IMRT
    | VMAT
    | RapidPlan
    | Unknown

    let getAllCalculationOptions name (algorithm: Dictionary<string,string>) =
        sprintf "%s\n%s"
            name
            (algorithm
            |> Seq.map (fun x -> sprintf "%s%s = %s" tab x.Key x.Value)
            |> String.concat "\n")

    let getCalculationAlgorithmInfo: EsapiCall = fun plan ->
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
        |> EsapiResults.fromString

    let getPlanSums: EsapiCall = fun plan ->
        let otherPlansInCourse =
            plan.Course.PlanSetups
            |> Seq.filter(fun x -> x.Id <> plan.Id)
            |> Seq.map(fun x -> x.Id)

        let planSums = 
            plan.Course.Patient.Courses
            |> Seq.map(fun x -> x.PlanSums)
            |> Seq.concat
            |> Seq.filter(fun sum ->
                sum.PlanSetups
                |> Seq.filter (fun x -> x.Id = plan.Id && x.Course.Id = plan.Course.Id)
                |> Seq.length > 0)
            |> Seq.map(fun sum -> 
                let plans = 
                    sum.PlanSetups
                    |> Seq.map(fun x -> $"{x.Id} ({x.Course.Id})")
                    |> String.concat $"\n{tab}{tab}"
                $"{sum.Id} ({sum.Course.Id}):\n{tab}{tab}{plans}")

        if Seq.length planSums = 0
        then 
            if Seq.length otherPlansInCourse = 0
            then "No plan sums containing this plan"
            else
                sprintf "Other plans in course:\n%s%s\n%A" 
                    tab 
                    (otherPlansInCourse |> String.concat $"\n{tab}") 
                    (ValidatedText(WarnWithoutExplanation, "No plan sums containing this plan"))
        else  
            sprintf "Other plans in course:\n%s%s\nPlan sums containing %s:\n%s%s" 
                tab 
                (otherPlansInCourse |> String.concat $"\n{tab}") 
                plan.Id 
                tab 
                (planSums |> String.concat $"\n{tab}")
        |> EsapiResults.fromString
