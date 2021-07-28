namespace TG275Checklist.EsapiCalls

open VMS.TPS.Common.Model.API
open CommonHelpers
open TG275Checklist.Model.EsapiService

module DoseVerification =

    let getQaPlans: EsapiCall = fun plan ->
        let qas = 
            plan.Course.Patient.Courses
            |> Seq.map(fun x -> x.PlanSetups)
            |> Seq.concat
            |> Seq.filter(fun p -> try p.VerifiedPlan = plan with _ -> false)   // PlanSetup.get_VerifiedPlan() could throw exception if the Verified Plan has been deleted
            |> Seq.map(fun x -> $"{x.Id} ({x.Course.Id})")

        if Seq.length qas = 0
        then "No verification plans have been created for this plan"
        else sprintf "Verification Plans:\n%s%s" tab (qas |> String.concat $"\n{tab}")
        |> EsapiResults.fromString
