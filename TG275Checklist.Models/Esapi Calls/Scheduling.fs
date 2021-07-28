namespace TG275Checklist.EsapiCalls

open VMS.TPS.Common.Model.API
open CommonHelpers
open TG275Checklist.Model.EsapiService

module Scheduling =

    let getPlanScheduling: EsapiCall = fun plan ->
        let overlappingSessions = 
            plan.TreatmentSessions
            |> Seq.map (fun session -> 
                session.TreatmentSession.SessionPlans
                |> Seq.filter (fun sessionPlan -> sessionPlan.PlanSetup.Id <> plan.Id)
                |> Seq.map (fun sessionPlan -> (plan, sessionPlan.PlanSetup)))
            |> Seq.concat
            |> Seq.countBy id

        let numSessions = Seq.length plan.TreatmentSessions

        let overlappingSessionsText =
            if Seq.length overlappingSessions > 0
                then
                    overlappingSessions 
                    |> Seq.map (fun x -> ValidatedText(WarnWithoutExplanation, $"\n{(fst (fst x)).Id} is scheduled with {(snd (fst x)).Id} for {snd x} sessions.  ").ToString())
                    |> Seq.map (fun x -> x + "Please check Plan Scheduling for more info")
                    |> String.concat ""
                else ""

        let numSessionsText = $"{numSessions} sessions"

        let numSessionsResult = getPassWarn "Scheduled activities doesn't equal planned fractions" (numSessions = plan.NumberOfFractions.GetValueOrDefault())

        EsapiResults.fromString $"{ValidatedText(numSessionsResult, numSessionsText)} scheduled for treatment {overlappingSessionsText}"

