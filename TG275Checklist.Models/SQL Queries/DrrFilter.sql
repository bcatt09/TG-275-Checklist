SELECT	field.RadiationId, image.ImageId, field.DRRTemplateFileName
FROM	vv_ExternalField as field
	INNER JOIN PlanSetup as planSetup ON planSetup.PlanSetupSer = field.PlanSetupSer
    INNER JOIN Course as course ON course.CourseSer = field.CourseSer
	INNER JOIN Patient as pat ON course.PatientSer = pat.PatientSer
	INNER JOIN Radiation as rad on rad.RadiationSer = field.RadiationSer
	INNER JOIN Image as image on image.ImageSer = rad.RefImageSer
WHERE pat.PatientId = @patId
    AND course.CourseId = @courseId
	AND planSetup.PlanSetupId = @planId
	AND field.RadiationId = @fieldId