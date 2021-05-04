namespace TG275Checklist.Sql

module SqlQueries =

    let [<Literal>] connectionString = "Data Source=10.71.248.60;Initial Catalog=VARIAN;Integrated Security=True"

    let [<Literal>] sqlGetOncologist =
        "SELECT     pt.LastName, pt.FirstName, gpo.AliasName as RadOncName
        FROM        Patient AS pt
        INNER JOIN  DWH.vv_GetPatientPrimaryOncologistDetails AS gpo WITH (NoLock) ON pt.PatientSer = gpo.PatientSer
        WHERE       (pt.PatientId = @patId)"
    
    let [<Literal>] sqlGetRxFrequency  = 
        "SELECT	    pt.LastName, pt.FirstName, ps.PlanSetupId, ps.CourseId, rxprop.PropertyValue as Frequency
        FROM		Patient AS pt
        INNER JOIN	vv_PlanSetup AS ps ON ps.PatientId = pt.PatientId
        INNER JOIN	Prescription AS rx ON rx.PrescriptionSer = ps.PrescriptionSer
        INNER JOIN	PrescriptionProperty AS rxprop ON rxprop.PrescriptionSer = rx.PrescriptionSer
        WHERE		(pt.PatientId = @patId)
                AND		(ps.PlanSetupId = @planId)
                AND		(ps.CourseId = @courseId)
                AND		(rxprop.PropertyType = '7')"

    let [<Literal>] sqlGetScheduledActivities =
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

    let [<Literal>] sqlGetOncologistUserId =
        "SELECT     Top(1) pt.LastName, pt.FirstName, gpo.AliasName as RadOnc, usr.app_user_userid
        FROM        Patient AS pt
        INNER JOIN  DWH.vv_GetPatientPrimaryOncologistDetails AS gpo ON pt.PatientSer = gpo.PatientSer
        INNER JOIN	DoctorMH AS doc ON doc.ResourceSer = gpo.PrimaryOncologistSer
        INNER JOIN	userid AS usr ON usr.dsp_name = doc.AliasName
        INNER JOIN	prof_typ AS prof ON prof.prof_typ = usr.prof_typ
        WHERE       prof.prof_desc = 'Physician'
        AND			usr.prof_flag = 'P' 
        AND			pt.PatientId = @patId"

