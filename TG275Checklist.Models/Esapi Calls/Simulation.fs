namespace TG275Checklist.EsapiCalls

open System
open CommonHelpers
open TG275Checklist.Model.EsapiService

module Simulation =

    let getSetupNotes: EsapiCall = fun plan ->
        getSetupNotesAsString plan
        |> EsapiResults.fromString

    let getPatientOrientations: EsapiCall = fun plan ->
        let tx = plan.TreatmentOrientation
        let sim = plan.StructureSet.Image.ImagingOrientation

        let result = getPassFail "treatment orientation doesn't match simulation orientation" (tx = sim)

        $"Treatment: \n{tab}{ValidatedText(result, tx)}\nImaging: \n{tab}{ValidatedText(result, sim)}" |> EsapiResults.fromString
    
    let getCTInfo: EsapiCall = fun plan ->
        match plan.StructureSet.Image with
        | null -> ValidatedText(Fail "No image loaded", "Error").ToString()
        | ct -> 
            let creationDate =
                match ct.CreationDateTime.HasValue with
                | true -> ct.CreationDateTime.Value.ToShortDateString()
                | false -> ValidatedText(Warn "No creation date on CT", "Error").ToString()
            let tryGetDate (id: string) =
                if id.Length < 8
                then $"%8s{id}"
                else id.Substring(0,8)
            let isDateFunction = // If the CT ID is a date we'll highlight is red if it doesn't match, otherwise we'll use yellow
                if fst (DateTime.TryParse(tryGetDate(ct.Id)))
                then getPassFail "CT date does not match ID"
                else getPassWarn "CT ID couldn't be checked against the date because it doesn't follow naming conventions"
            let dateResult = isDateFunction (tryGetDate(ct.Id) = DateTime.Parse(creationDate).ToString("yyyyMMdd"))

            $"Imaging Device for HU Curve: {ValidatedText(Highlight, ct.Series.ImagingDeviceId)}\nCT Name: {ValidatedText(dateResult, ct.Id)}\nCreation date: {ValidatedText(dateResult, creationDate)}\n# of slices: {ct.ZSize}\nSlice thickness: %0.1f{ct.ZRes} mm"
        |> EsapiResults.fromString
