module TG275Checklist.Sql

open FSharp.Data
open TG275Checklist.Model

let [<Literal>] private connectionString = DatabaseConnectionString.ariaConnectionString

let sqlGetOncologist patId =
    try
        let cmd = new SqlCommandProvider<const(SqlFile<"SQL Queries\OncologistName.sql">.Text), connectionString>(connectionString)
        let oncologist =
            cmd.Execute(patId = patId)
            |> Seq.map (fun x -> x.RadOncName.Value)
            |> Seq.head
        Ok oncologist
    with ex ->
        Error (ex.Message)

let sqlGetOncologistUserId patId =
    try
        let cmd = new SqlCommandProvider<const(SqlFile<"SQL Queries\OncologistUserId.sql">.Text), connectionString>(connectionString)
        let oncologistId = 
            cmd.Execute(patId = patId)
            |> Seq.map (fun x -> x.app_user_userid.Value)
            |> Seq.head
        Ok oncologistId
    with ex ->
        Error (ex.Message)

let sqlGetRxFrequency patId planId courseId =
    try
        let cmd = new SqlCommandProvider<const(SqlFile<"SQL Queries\PrescribedFrequency.sql">.Text), connectionString>(connectionString)
        let rxFrequency = 
            cmd.Execute(patId = patId, planId = planId, courseId = courseId)
            |> Seq.map (fun x -> x.Frequency)
            |> Seq.head
        Ok rxFrequency
    with ex ->
        Error (ex.Message)

let sqlGetPrescribedImaging patId planId courseId =
    try
        let cmd = new SqlCommandProvider<const(SqlFile<"SQL Queries\PrescribedImaging.sql">.Text), connectionString>(connectionString)

        let rxFrequencyFromSQL = 
            cmd.Execute(patId = patId, planId = planId, courseId = courseId)
            |> Seq.map (fun x -> 
                {|
                    Imaging = x.Imaging
                    SerialNum = x.SerialNum
                    Type =
                        match x.NoteType with
                        | 6s -> TimePoint
                        | 8s -> Frequency
                        | 2s -> Other
                        | _ -> Unknown
                    Value = x.NoteValue
                |})
            |> Seq.toList

        // Find all serial numbers (which equates to individual modality entries)
        let serials = 
            rxFrequencyFromSQL
            |> Seq.distinctBy(fun x -> x.SerialNum)
            |> Seq.map(fun x -> x.SerialNum)
            |> Seq.toList

        // List for results grouped by serial number
        let rxFrequency =
            serials
            |> List.map(fun ser ->
                let imaging = 
                    (rxFrequencyFromSQL 
                    |> List.filter(fun x -> x.SerialNum = ser)
                    |> List.head).Imaging
                let timepoint =
                    rxFrequencyFromSQL 
                    |> List.filter(fun x -> x.SerialNum = ser && x.Type = TimePoint)
                    |> List.tryHead
                    |> Option.bind(fun x -> Some x.Value)
                let frequency =
                    rxFrequencyFromSQL 
                    |> List.filter(fun x -> x.SerialNum = ser && x.Type = Frequency)
                    |> List.tryHead
                    |> Option.bind(fun x -> 
                        match x.Value with
                        | "7" -> "Treatment"
                        | "0" -> "Monday"
                        | "1" -> "Tuesday"
                        | "2" -> "Wednesday"
                        | "3" -> "Thursday"
                        | "4" -> "Friday"
                        | "5" -> "Saturday"
                        | "6" -> "Sunday"
                        | value -> $"Unknown value ({value})"
                        |> Some)
                let other =
                    rxFrequencyFromSQL 
                    |> List.filter(fun x -> x.SerialNum = ser && x.Type = Other)
                    |> List.tryHead
                    |> Option.bind(fun x -> Some x.Value)
                let unknown =
                    rxFrequencyFromSQL 
                    |> List.filter(fun x -> x.SerialNum = ser && x.Type = Unknown)
                    |> List.tryHead
                    |> Option.bind(fun x -> Some x.Value)
                {|
                    Imaging = imaging
                    TimePoint = timepoint
                    Frequency = frequency
                    Other = other
                    Unknown = unknown
                |}
            )

        Ok rxFrequency
    with ex ->
        Error (ex.Message)

let sqlGetScheduledActivities patId =
    try
        let cmd = new SqlCommandProvider<const(SqlFile<"SQL Queries\ScheduledActivities.sql">.Text), connectionString>(connectionString)
        let scheduledActivities =
            cmd.Execute(patId = patId)
            |> Seq.map (fun x -> 
                {
                    ApptTime = x.ScheduledStartTime.Value
                    ApptName = x.ActivityCode
                    ApptColor = TreatmentAppointmentInfo.ConvertFromAriaColor (try x.ForeGroundColor.Value with ex -> ([|byte 255; byte 255; byte 255|]))
                    ApptResource = x.ResourceName
                })
        Ok scheduledActivities
    with ex ->
        Error (ex.Message)

let sqlGetSetupNotes patId courseId planId =
    try
        let cmd = new SqlCommandProvider<const(SqlFile<"SQL Queries\SetupNotes.sql">.Text), connectionString>(connectionString)
        let setupNotes =
            cmd.Execute(patId = patId, courseId = courseId, planId = planId)
            |> Seq.map (fun x -> 
                {|
                    fieldId = x.RadiationId
                    setupNote = x.SetupNote
                |})
        Ok setupNotes
    with ex ->
        Error (ex.Message)
        
let sqlGetElectronBlockCustomCodes patId courseId planId =
    try
        let cmd = new SqlCommandProvider<const(SqlFile<"SQL Queries\ElectronBlockCustomCodes.sql">.Text), connectionString>(connectionString)
        let customCodes =
            cmd.Execute(patId = patId, courseId = courseId, planId = planId)
            |> Seq.map (fun x -> 
                {|
                    fieldId = x.RadiationId
                    blockId = x.BlockId
                    customCode = x.CustomCode
                |})
        Ok customCodes
    with ex ->
        Error (ex.Message)

let sqlGetDrrTemplates patId courseId planId fieldId =
    try
        let cmd = new SqlCommandProvider<const(SqlFile<"SQL Queries\DrrFilter.sql">.Text), connectionString>(connectionString)
        let setupNotes =
            cmd.Execute(patId = patId, courseId = courseId, planId = planId, fieldId = fieldId)
            |> Seq.map (fun x -> 
                {|
                    fieldId = x.RadiationId
                    drrId = x.ImageId
                    drrFilter = x.DRRTemplateFileName
                |})
            |> Seq.head
        Ok setupNotes
    with ex ->
        Error (ex.Message)