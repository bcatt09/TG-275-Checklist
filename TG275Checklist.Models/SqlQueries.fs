module TG275Checklist.Sql

open FSharp.Data
open TG275Checklist.Model.EsapiService

let [<Literal>] private connectionString = "Data Source=10.71.248.60;Initial Catalog=VARIAN;User ID=reports;Password=R3p0rtsUs3r"

let [<Literal>] private sqlCmdGetOncologist =
    "SELECT     pt.LastName, pt.FirstName, gpo.AliasName as RadOncName
    FROM        Patient AS pt
    INNER JOIN  DWH.vv_GetPatientPrimaryOncologistDetails AS gpo WITH (NoLock) ON pt.PatientSer = gpo.PatientSer
    WHERE       (pt.PatientId = @patId)"

let [<Literal>] private sqlCmdGetOncologistUserId =
    "SELECT     Top(1) pt.LastName, pt.FirstName, gpo.AliasName as RadOnc, usr.app_user_userid
    FROM        Patient AS pt
    INNER JOIN  DWH.vv_GetPatientPrimaryOncologistDetails AS gpo ON pt.PatientSer = gpo.PatientSer
    INNER JOIN	DoctorMH AS doc ON doc.ResourceSer = gpo.PrimaryOncologistSer
    INNER JOIN	userid AS usr ON usr.dsp_name = doc.AliasName
    INNER JOIN	prof_typ AS prof ON prof.prof_typ = usr.prof_typ
    WHERE       prof.prof_desc = 'Physician'
    AND			usr.prof_flag = 'P' 
    AND			pt.PatientId = @patId"
    
let [<Literal>] private sqlCmdGetRxFrequency  = 
    "SELECT	    pt.LastName, pt.FirstName, ps.PlanSetupId, ps.CourseId, rxprop.PropertyValue as Frequency
    FROM		Patient AS pt
    INNER JOIN	vv_PlanSetup AS ps ON ps.PatientId = pt.PatientId
    INNER JOIN	Prescription AS rx ON rx.PrescriptionSer = ps.PrescriptionSer
    INNER JOIN	PrescriptionProperty AS rxprop ON rxprop.PrescriptionSer = rx.PrescriptionSer
    WHERE		(pt.PatientId = @patId)
            AND		(ps.PlanSetupId = @planId)
            AND		(ps.CourseId = @courseId)
            AND		(rxprop.PropertyType = '7')"

let [<Literal>] private sqlCmdGetScheduledActivities =
    "SELECT	    pt.LastName, pt.FirstName, act.ActivityCode, sa.ScheduledStartTime, act.ForeGroundColor
    FROM		Patient AS pt
    INNER JOIN	ScheduledActivity AS sa ON pt.PatientSer = sa.PatientSer
    INNER JOIN	ActivityInstance AS ai ON sa.ActivityInstanceSer = ai.ActivityInstanceSer
    INNER JOIN	Activity AS act ON ai.ActivitySer = act.ActivitySer
    WHERE		(pt.PatientId = @patId)
    AND		    (act.ActivityCategorySer = '173')
    AND         (sa.ObjectStatus <> 'Deleted')
    AND		    (sa.ScheduledStartTime BETWEEN DATEADD(MONTH, - 1, GETDATE()) AND DATEADD(MONTH, 4, GETDATE()))
    ORDER BY	sa.ScheduledStartTime"

let sqlGetOncologist patId =
    try
        let planCmd = new SqlCommandProvider<const(sqlCmdGetOncologist), connectionString>(connectionString)
        let oncologist =
            planCmd.Execute(patId = patId)
            |> Seq.map (fun x -> x.RadOncName.Value)
            |> Seq.head
        Ok oncologist
    with ex ->
        Error (ex.Message)

let sqlGetOncologistUserId patId =
    try
        let planCmd = new SqlCommandProvider<const(sqlCmdGetOncologistUserId), connectionString>(connectionString)
        let oncologistId = 
            planCmd.Execute(patId = patId)
            |> Seq.map (fun x -> x.app_user_userid.Value)
            |> Seq.head
        Ok oncologistId
    with ex ->
        Error (ex.Message)

let sqlGetRxFrequency patId planId courseId =
    try
        let planCmd = new SqlCommandProvider<const(sqlCmdGetRxFrequency), connectionString>(connectionString)
        let rxFrequency = 
            planCmd.Execute(patId = patId, planId = planId, courseId = courseId)
            |> Seq.map (fun x -> x.Frequency)
            |> Seq.head
        Ok rxFrequency
    with ex ->
        Error (ex.Message)

let sqlGetScheduledActivities patId =
    try
        let planCmd = new SqlCommandProvider<const(sqlCmdGetScheduledActivities), connectionString>(connectionString)
        let scheduledActivities =
            planCmd.Execute(patId = patId)
            |> Seq.map (fun x -> 
                {
                    ApptTime = x.ScheduledStartTime.Value
                    ApptName = x.ActivityCode
                    ApptColor = TreatmentAppointmentInfo.ConvertFromAriaColor (try x.ForeGroundColor.Value with ex -> ([|byte 255; byte 255; byte 255|]))
                })
        Ok scheduledActivities
    with ex ->
        Error (ex.Message)