SELECT		pt.LastName, pt.FirstName, ps.PlanSetupId, ps.CourseId, rxprop.PropertyValue as Imaging, rxprop.PrescriptionPropertySer as SerialNum, rxpropitem.ItemType as NoteType, rxpropitem.ItemValue as NoteValue
FROM		Patient AS pt
INNER JOIN	vv_PlanSetup AS ps ON ps.PatientId = pt.PatientId
INNER JOIN	Prescription AS rx ON rx.PrescriptionSer = ps.PrescriptionSer
INNER JOIN	PrescriptionProperty AS rxprop ON rxprop.PrescriptionSer = rx.PrescriptionSer
INNER JOIN	PrescriptionPropertyItem as rxpropitem on rxpropitem.PrescriptionPropertySer = rxprop.PrescriptionPropertySer
WHERE		(pt.PatientId = @patId)
	AND		(ps.PlanSetupId = @planId)
	AND		(ps.CourseId = @courseId)
	AND		(rxprop.PropertyType = '3')