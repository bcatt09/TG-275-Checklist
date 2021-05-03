namespace TG275Checklist.Sql

module SqlQueries =

    let [<Literal>] connectionString = "Data Source=10.71.248.60;Initial Catalog=VARIAN;Integrated Security=True"

    let [<Literal>] sqlGetOncologist =
        "SELECT      pt.LastName, pt.FirstName, gpo.AliasName as RadOncName
        FROM        Patient AS pt
        inner join  DWH.vv_GetPatientPrimaryOncologistDetails AS gpo WITH (NoLock) ON pt.PatientSer = gpo.PatientSer
        WHERE       (pt.PatientId = @patId)"
    
    let [<Literal>] sqlGetRxFrequency  = 
        "select	pt.LastName, pt.FirstName, ps.PlanSetupId, ps.CourseId, rxprop.PropertyValue as Frequency
        from		Patient as pt
        inner join	vv_PlanSetup as ps on ps.PatientId = pt.PatientId
        inner join	Prescription as rx on rx.PrescriptionSer = ps.PrescriptionSer
        inner join	PrescriptionProperty as rxprop on rxprop.PrescriptionSer = rx.PrescriptionSer
        where		(pt.PatientId = @patId)
                AND		(ps.PlanSetupId = @planId)
                AND		(ps.CourseId = @courseId)
                AND		(rxprop.PropertyType = '7')"

    let [<Literal>] sqlGetScheduledActivities =
        "select	pt.LastName, pt.FirstName, ps.PlanSetupId, ps.CourseId, act.ActivityCode, sa.ScheduledStartTime
        from		Patient as pt
        inner join	vv_PlanSetup as ps on ps.PatientId = pt.PatientId
        inner join	ScheduledActivity as sa on pt.PatientSer = sa.PatientSer
        inner join	ActivityInstance as ai on sa.ActivityInstanceSer = ai.ActivityInstanceSer
        inner join	Activity as act on ai.ActivitySer = act.ActivitySer
        where		(pt.PatientId = @patId)
                AND		(ps.PlanSetupId = @planId)
                AND		(ps.CourseId = @courseId)
                AND		(act.ActivityCategorySer = '173')
                AND		(sa.ScheduledStartTime BETWEEN DATEADD(MONTH, - 3, GETDATE()) AND DATEADD(MONTH, 4, GETDATE()))"

    let [<Literal>] sqlGetOncologistUserId =
        "SELECT      Top(1) pt.LastName, pt.FirstName, gpo.AliasName as RadOnc, usr.app_user_userid
        FROM        Patient AS pt
        inner join  DWH.vv_GetPatientPrimaryOncologistDetails AS gpo ON pt.PatientSer = gpo.PatientSer
        inner join	DoctorMH as doc on doc.ResourceSer = gpo.PrimaryOncologistSer
        inner join	userid as usr on usr.dsp_name = doc.AliasName
        inner join	prof_typ as prof on prof.prof_typ = usr.prof_typ
        WHERE       prof.prof_desc = 'Physician'
        and			usr.prof_flag = 'P' 
        and			pt.PatientId = @patId"

