SELECT field.RadiationId, rad.SetupNote
FROM vv_ExternalFieldCommon as field
	INNER JOIN PlanSetup as planSetup ON planSetup.PlanSetupSer = field.PlanSetupSer
    INNER JOIN Course as course ON course.CourseSer = field.CourseSer
	INNER JOIN Patient as pat ON course.PatientSer = pat.PatientSer
	INNER JOIN Radiation as rad on rad.RadiationSer = field.RadiationSer
WHERE pat.PatientId = @patId
    AND course.CourseId = @courseId
	AND planSetup.PlanSetupId = @planId