namespace TG275Checklist.EsapiCalls

open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types
open System.Globalization
open TG275Checklist.Model
open TG275Checklist.Sql

module CommonHelpers =

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

    let OARTypes = [ "AVOIDANCE"; "CAVITY"; "ORGAN"; "CONTROL"; "DOSE_REGION"; "IRRAD_VOLUME"; "TREATED_VOLUME" ]
    let targetTypes = [ "GTV"; "CTV"; "PTV" ]

    let getBodyStructure (plan: PlanSetup) = 
        plan.StructureSet.Structures
        |> Seq.filter (fun x -> x.DicomType = "BODY" || x.DicomType = "EXTERNAL")
        |> Seq.tryExactlyOne

    let getTargetStructure (plan: PlanSetup) =
        plan.StructureSet.Structures
        |> Seq.filter (fun x -> x.Id = plan.TargetVolumeID)
        |> Seq.tryExactlyOne

    let getListOfSetupNotes (plan: PlanSetup) =
        let setupNotes = sqlGetSetupNotes plan.Course.Patient.Id plan.Course.Id plan.Id
        match setupNotes with
        | Error error -> $"Error getting Treatment Activities from database: {fail error}"
        | Ok notes ->
            let list = 
                notes
                |> Seq.filter(fun x -> x.setupNote.IsSome)
                |> Seq.map(fun x -> $"{tab}{x.fieldId}:\n{x.setupNote.Value}")
                |> Seq.map(fun x -> x.Replace("\n", $"\n{tab}{tab}"))
            if Seq.length list = 0
            then "No setup notes found"
            else 
                list
                |> Seq.append (Seq.singleton "Setup Notes:")
                |> String.concat "\n"
    


    let testFunction (plan: PlanSetup) =
        System.Threading.Thread.Sleep(3000)
        let badExample = "BAD"
        let goodExample = "Good"
        stringOutput $"{plan.TotalDose} {fail badExample} dose/{pass goodExample} dose"
    let badTestFunction (plan: PlanSetup) =
        failwith "this has been a test of the emergency broadcast system"