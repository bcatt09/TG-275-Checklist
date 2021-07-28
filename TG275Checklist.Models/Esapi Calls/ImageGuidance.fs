namespace TG275Checklist.EsapiCalls

open VMS.TPS.Common.Model.API
open CommonHelpers
open TG275Checklist.Model.EsapiService

module ImageGuidance =

    let DrrTest: EsapiCall = fun plan ->
        plan.Beams
        |> Seq.filter (fun x -> not x.IsSetupField)
        |> Seq.map (fun x -> $"{x.Id} - {x.ReferenceImage.Id}")
        |> String.concat "\n"
        |> EsapiResults.fromString
