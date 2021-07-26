SELECT		Top(1) pt.LastName, pt.FirstName, gpo.AliasName as RadOnc, usr.app_user_userid
FROM		Patient AS pt
INNER JOIN	DWH.vv_GetPatientPrimaryOncologistDetails AS gpo ON pt.PatientSer = gpo.PatientSer
INNER JOIN	DoctorMH AS doc ON doc.ResourceSer = gpo.PrimaryOncologistSer
INNER JOIN	userid AS usr ON usr.dsp_name = doc.AliasName
INNER JOIN	prof_typ AS prof ON prof.prof_typ = usr.prof_typ
WHERE		prof.prof_desc = 'Physician'
AND			usr.prof_flag = 'P' 
AND			pt.PatientId = @patId