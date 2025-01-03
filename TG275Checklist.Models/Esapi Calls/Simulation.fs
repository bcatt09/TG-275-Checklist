namespace TG275Checklist.EsapiCalls

open System
open CommonHelpers
open TG275Checklist.Model.EsapiService
open System.Text.RegularExpressions

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
                if id.Length < 4
                then $"%4s{id}"
                else id.[id.Length-4..]
            let isDateFunction = 
                if (Regex.Match(ct.Id, "\\d{4}$").Success)
                then getPassFail "CT date does not match ID"
                else getPassWarn "CT ID couldn't be checked against the date because it doesn't follow naming conventions"
            let dateResult = isDateFunction (tryGetDate(ct.Id) = DateTime.Parse(creationDate).ToString("MMyy"))

            $"Imaging Device for HU Curve: {ValidatedText(Highlight, ct.Series.ImagingDeviceId)}\nCT Name: {ValidatedText(dateResult, ct.Id)}\nCreation date: {ValidatedText(dateResult, creationDate)}\n# of slices: {ct.ZSize}\nSlice thickness: %0.1f{ct.ZRes} mm"
        |> EsapiResults.fromString
