﻿SELECT		pt.LastName, pt.FirstName, act.ActivityCode, sa.ScheduledStartTime, act.ForeGroundColor
FROM		Patient AS pt
INNER JOIN	ScheduledActivity AS sa ON pt.PatientSer = sa.PatientSer
INNER JOIN	ActivityInstance AS ai ON sa.ActivityInstanceSer = ai.ActivityInstanceSer
INNER JOIN	Activity AS act ON ai.ActivitySer = act.ActivitySer
INNER JOIN	ActivityCategory as actCat on actCat.ActivityCategorySer = act.ActivityCategorySer
WHERE		(pt.PatientId = @patId)
	AND		(actCat.ActivityCategoryCode = 'Treatment')
	AND		(sa.ObjectStatus <> 'Deleted')
	AND		(sa.ScheduledStartTime BETWEEN DATEADD(MONTH, - 1, GETDATE()) AND DATEADD(MONTH, 4, GETDATE()))
ORDER BY	sa.ScheduledStartTime