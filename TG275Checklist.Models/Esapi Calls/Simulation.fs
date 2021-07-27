namespace TG275Checklist.EsapiCalls

open System
open VMS.TPS.Common.Model.API
open CommonHelpers

module Simulation =

    let getSetupNotes (plan: PlanSetup) =
        getListOfSetupNotes plan
        |> stringOutput

    let getPatientOrientations (plan: PlanSetup) =
        let tx = plan.TreatmentOrientation
        let sim = plan.StructureSet.Image.ImagingOrientation

        let result = getPassFail (tx = sim)

        stringOutput $"Treatment: \n{tab}{result tx}\nImaging: \n{tab}{result sim}"
    
    let getCTInfo (plan: PlanSetup) =
        match plan.StructureSet.Image with
        | null -> fail "No image loaded"
        | ct -> 
            let creationDate =
                match ct.CreationDateTime.HasValue with
                | true -> ct.CreationDateTime.Value.ToShortDateString()
                | false -> warn "No creation date"
            let tryGetDate (id: string) =
                if id.Length < 8
                then sprintf "%8s" id
                else id.Substring(0,8)
            let isDateFunction = // If the CT ID is a date we'll highlight is red if it doesn't match, otherwise we'll use yellow
                if fst (DateTime.TryParse(tryGetDate(ct.Id)))
                then getPassFail
                else getPassWarn
            let dateResult = isDateFunction (tryGetDate(ct.Id) = DateTime.Parse(creationDate).ToString("yyyyMMdd"))

            $"Imaging Device for HU Curve: {ct.Series.ImagingDeviceId}\nCT Name: {dateResult ct.Id}\nCreation date: {dateResult creationDate}\n# of slices: {ct.ZSize}"
        |> stringOutput
