namespace TG275Checklist.Model

open VMS.TPS.Common.Model.API

module EsapiCalls =

    let testFunction (plan:PlanSetup) =
        System.Threading.Thread.Sleep(5000)
        { Text = plan.TotalDose.ToString() }

