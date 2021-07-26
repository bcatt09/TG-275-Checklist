﻿namespace TG275Checklist.Model

open System

[<AutoOpen>]
module HelperDataTypes =

    // Treatment Appointments which will be displayed on calendar
    type TreatmentAppointmentInfo =
        {
            ApptTime: DateTime
            ApptName: string
            ApptColor: string
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