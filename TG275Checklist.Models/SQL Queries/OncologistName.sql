SELECT      pt.LastName, pt.FirstName, gpo.AliasName as RadOncName
FROM        Patient AS pt
INNER JOIN  DWH.vv_GetPatientPrimaryOncologistDetails AS gpo WITH (NoLock) ON pt.PatientSer = gpo.PatientSer
WHERE       (pt.PatientId = @patId)