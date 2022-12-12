namespace TG275Checklist.Model

open System
open VMS.TPS.Common.Model.API
open System.Collections.Generic

[<AutoOpen>]
module HelperDataTypes =

    // Treatment Appointments which will be displayed on calendar
    type TreatmentAppointmentInfo =
        {
            ApptTime: DateTime
            ApptName: string
            ApptColor: string
            ApptResource: string
        }
        static member ConvertFromAriaColor (color: byte[]) =
            let byteString = BitConverter.ToString(color).Replace("-", "")
            // For some reason Aria appears to store colors as 
            // "BBGGRRAA"
            //  01234567
            if(byteString.Length < 6)
            then "#FFFFFF"
            else
                let R = byteString.Substring(4, 2)
                let G = byteString.Substring(2, 2)
                let B = byteString.Substring(0, 2)
                $"#{R}{G}{B}"

    // Target coverage statistics checklist item
    type TargetCoverageDropdown =
        {
            TargetList: string list
            SelectedTarget: string
            Results: Dictionary<string, string>
            DisplayedResults: string
        }

    // Available properties for prescribed imaging modalities
    type PrescribedImagingProperties = 
        | TimePoint
        | Frequency
        | Other
        | Unknown