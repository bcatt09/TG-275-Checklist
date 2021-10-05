--DECLARE	@patId AS VARCHAR(50)='6984813'
--DECLARE	@courseId AS VARCHAR(50)='1 Skin'
--DECLARE	@planId AS VARCHAR(50)='R Foot_2'

SELECT	pat.LastName, pat.FirstName, planSetup.PlanSetupId, rad.RadiationId, block.BlockId, fAddOn.CustomCode
FROM	vv_ExternalFieldCommon AS field
	INNER JOIN	PlanSetup AS planSetup ON planSetup.PlanSetupSer = field.PlanSetupSer
	INNER JOIN	Course AS course ON course.CourseSer = field.CourseSer
	INNER JOIN	Patient AS pat ON course.PatientSer = pat.PatientSer
	INNER JOIN	Radiation AS rad ON rad.RadiationSer = field.RadiationSer
	INNER JOIN	Block AS block ON block.RadiationSer = rad.RadiationSer
	INNER JOIN FieldAddOn AS fAddOn ON fAddOn.RadiationSer = rad.RadiationSer
WHERE	pat.PatientId = @patId
	AND	course.CourseId = @courseId
	AND	planSetup.PlanSetupId = @planId
	AND fAddOn.CustomCode <> 'NULL'