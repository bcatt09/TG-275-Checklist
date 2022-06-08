namespace TG275Checklist.EsapiCalls

open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types
open System.Globalization
open TG275Checklist.Model
open TG275Checklist.Sql

[<AutoOpen>]
module CommonHelpers =
    
    type EsapiCall = PlanSetup -> EsapiResults

    let tab = "    "

    // Validated Text
    let private passingTags = "<Pass>", "</Pass>"
    let private failingTags = "<Fail>", "</Fail>"
    let private warningTags = "<Warn>", "</Warn>"
    let private highlightTags = "<Highlight>", "</Highlight>"

    let private tagString (tag: string * string) text =
        $"{fst tag}{text}{snd tag}"

    let private pass text = tagString passingTags text
    let private fail text = tagString failingTags text
    let private warn text = tagString warningTags text
    let private highlight text = tagString highlightTags text

    type ValidationResult =
        | Pass
        | Warn of string
        | WarnWithoutExplanation
        | Fail of string
        | Highlight
        | Id

    type ValidatedText(result: ValidationResult, object) =
        member this.Result = result
        member this.Object = object
        override this.ToString() =
                match this.Result with
                | Pass -> pass this.Object
                | Warn reason -> warn $"{this.Object} ({reason})"
                | WarnWithoutExplanation -> warn this.Object
                | Fail reason -> fail $"{this.Object} ({reason})"
                | Highlight -> highlight this.Object
                | Id -> this.Object.ToString()

    let getPassFail failureReason condition = if condition then Pass else Fail failureReason
    let getPassWarn warningReason condition = if condition then Pass else Warn warningReason
    let getPassWarnWithoutExplanation condition = if condition then Pass else WarnWithoutExplanation
    let getPassId condition = if condition then Pass else Id
    let getIdWarn warningReason condition = if condition then Id else Warn warningReason
    let getIdWarnWithoutExplanation  condition = if condition then Id else WarnWithoutExplanation

    let toTitleCase (text: string) =
        CultureInfo("en-US").TextInfo.ToTitleCase(text.ToLower())

    type laterality =
    | Right
    | Left
    | NoLaterality

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

    let getStructureById (plan: PlanSetup) structureId =
        plan.StructureSet.Structures
        |> Seq.filter (fun x -> x.Id = structureId)
        |> Seq.tryExactlyOne

    let getSetupNotesAsList (plan: PlanSetup) =
        let setupNotes = sqlGetSetupNotes plan.Course.Patient.Id plan.Course.Id plan.Id
        match setupNotes with
        | Error error -> Error error
        | Ok notes ->
            Ok (notes
            |> Seq.filter(fun x -> x.setupNote.IsSome)
            |> Seq.map(fun x -> $"{tab}{x.fieldId}:\n{x.setupNote.Value}")
            |> Seq.map(fun x -> x.Replace("\n", $"\n{tab}{tab}")))

    let getSetupNotesAsString (plan: PlanSetup) =
        match getSetupNotesAsList plan with
        | Error error -> $"Error getting Se from database: {fail error}"
        | Ok list ->
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
        EsapiResults.fromString $"{plan.TotalDose} {fail badExample} dose/{pass goodExample} dose"
    let badTestFunction (plan: PlanSetup) =
        failwith "this has been a test of the emergency broadcast system"