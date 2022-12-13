SELECT		pt.LastName, pt.FirstName, act.ActivityCode, act.Description, sa.ScheduledStartTime, act.ForeGroundColor, res.ResourceName
FROM		Patient AS pt
INNER JOIN	ScheduledActivity AS sa ON pt.PatientSer = sa.PatientSer
INNER JOIN	ActivityInstance AS ai ON sa.ActivityInstanceSer = ai.ActivityInstanceSer
INNER JOIN	Activity AS act ON ai.ActivitySer = act.ActivitySer
INNER JOIN	ActivityCategory as actCat on actCat.ActivityCategorySer = act.ActivityCategorySer
INNER JOIN	ResourceActivity as ra on ra.ScheduledActivitySer = sa.ScheduledActivitySer
INNER JOIN	vv_ResourceName as res on res.ResourceSer = ra.ResourceSer
INNER JOIN	Resource as resource on resource.ResourceSer = ra.ResourceSer
WHERE		(pt.PatientId = @patId)
	AND		(actCat.ActivityCategoryCode = 'Treatment')
	AND		(resource.ResourceType = 'Machine')
	AND		(sa.ObjectStatus <> 'Deleted')
	AND		(sa.ScheduledStartTime BETWEEN DATEADD(MONTH, - 1, GETDATE()) AND DATEADD(MONTH, 4, GETDATE()))
ORDER BY	sa.ScheduledStartTime