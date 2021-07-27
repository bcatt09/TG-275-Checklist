namespace TG275Checklist.EsapiCalls

open VMS.TPS.Common.Model.API
open CommonHelpers

module ImageGuidance =

    let DrrTest (plan: PlanSetup) =
        plan.Beams
        |> Seq.filter (fun x -> not x.IsSetupField)
        |> Seq.map (fun x -> $"{x.Id} - {x.ReferenceImage.Id}")
        |> String.concat "\n"
        |> stringOutput
