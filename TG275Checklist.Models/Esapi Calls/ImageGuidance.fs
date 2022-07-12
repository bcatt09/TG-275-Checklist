namespace TG275Checklist.EsapiCalls

open VMS.TPS.Common.Model.API
open CommonHelpers
open TG275Checklist.Model.EsapiService
open TG275Checklist.Sql
open TG275Checklist

module ImageGuidance =

    let DrrTemplates: EsapiCall = fun plan ->
        plan.Beams
        |> Seq.map (fun x -> 
            let drrInfo = sqlGetDrrTemplates plan.Course.Patient.Id plan.Course.Id plan.Id x.Id
            
            match drrInfo with
            | Error error -> error
            | Ok info ->
                $"""{x.Id}: {info.drrId}
{tab}{match info.drrFilter with | None -> "None" | Some filter -> filter}""")
        |> String.concat "\n"
        |> EsapiResults.fromString

    let PrescribedImaging: EsapiCall = fun plan ->
        let rxImaging = sqlGetPrescribedImaging plan.Course.Patient.Id plan.Id plan.Course.Id

        match rxImaging with
        | Error error -> error
        | Ok imaging ->
            imaging
            |> Seq.map(fun x -> $"{x.Imaging}" +
                                (match x.TimePoint with
                                | None -> ""
                                | Some value -> $"\n{tab}{value}") +
                                (match x.Frequency with
                                | None -> ""
                                | Some value -> $"\n{tab}Every {value}") +
                                (match x.Other with
                                | None -> ""
                                | Some value -> $"\n{tab}Other: {value}") +
                                (match x.Unknown with
                                | None -> ""
                                | Some value -> $"\n{tab}Unknown value found:\n{tab}{tab}{value}"))

            |> String.concat "\n"
        |> EsapiResults.fromString
