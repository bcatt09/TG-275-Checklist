SELECT		pt.LastName, pt.FirstName, ps.PlanSetupId, ps.CourseId, rxprop.PropertyValue as Frequency
FROM		Patient AS pt
INNER JOIN	vv_PlanSetup AS ps ON ps.PatientId = pt.PatientId
INNER JOIN	Prescription AS rx ON rx.PrescriptionSer = ps.PrescriptionSer
INNER JOIN	PrescriptionProperty AS rxprop ON rxprop.PrescriptionSer = rx.PrescriptionSer
WHERE		(pt.PatientId = @patId)
	AND		(ps.PlanSetupId = @planId)
	AND		(ps.CourseId = @courseId)
	AND		(rxprop.PropertyType = '7')