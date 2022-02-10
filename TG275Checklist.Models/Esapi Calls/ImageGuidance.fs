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
