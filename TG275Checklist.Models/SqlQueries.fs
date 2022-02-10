module TG275Checklist.Sql

open FSharp.Data
open TG275Checklist.Model

let [<Literal>] private connectionString = "Data Source=10.71.248.60;Initial Catalog=VARIAN;User ID=reports;Password=R3p0rtsUs3r"

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