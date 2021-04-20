namespace TG275Checklist.Model

open Elmish
open Elmish.WPF

open PatientSetupTypes
open ChecklistTypes
open type System.Windows.Visibility

module Model =
    
    type Model =
        { 
            // Main Window

            SharedInfo: MainWindowInfo
            StatusBar: StatusBar
            Args: StandaloneApplicationArgs
            
            // Patient Setup Screen
            
            PatientSetupScreenCourses: CourseInfo list
            PatientSetupScreenToggles: PatientSetupToggleType list
            PatientSetupScreenVisibility: System.Windows.Visibility
            
            // Checklist Screen
            
            ChecklistScreenPlans: PlanChecklist list
            ChecklistScreenVisibility: System.Windows.Visibility
        }
        
    type Msg =
        
        // Main Window

        | EclipseLogin        
        | EclipseLoginSuccess of MainWindowInfo
        | EclipseLoginFailed of exn

        // Patient Setup Screen
        
        | LoadCoursesIntoPatientSetup
        | LoadCoursesSuccess of CourseInfo list
        | LoadCoursesFailed of exn

        | PatientSetupToggleChanged of PatientSetupToggleType * bool
        | PatientSetupUsePlanChanged of string * bool
        | PatientSetupCourseIsExpandedChanged of string * bool

        // Checklist Screen
        
        | DisplayChecklistScreen

        //| LoadFullChecklist
        // TODO: Just use the passed Model for all these?
        | PrepToLoadNextChecklist
        | UpdateLoadingMessage
        | LoadNextChecklist
        | LoadChecklistSuccess of PlanChecklist list
        | LoadChecklistFailure of exn
        | AllChecklistsLoaded


        | Debugton
    
    let init args =
        { 
            SharedInfo =
                {
                    PatientName = ""
                    CurrentUser = ""
                }
            StatusBar = readyStatus
            Args = args

            PatientSetupScreenCourses = args.Courses |> List.map (fun c -> 
                { 
                    CourseId = c.CourseId; 
                    IsExpanded = c.CourseId = args.OpenedCourseID; 
                    Plans = c.Plans |> List.map (fun p ->
                        {
                            PlanId = p.PlanId
                            CourseId = c.CourseId
                            Dose = ""
                            Oncologist = ""
                            PatientName = ""
                            IsChecked = p.PlanId = args.OpenedPlanID && c.CourseId = args.OpenedCourseID;
                        })
                })
            PatientSetupScreenToggles = initToggleList
            PatientSetupScreenVisibility = Visible

            ChecklistScreenPlans = []
            ChecklistScreenVisibility = Collapsed
        }, Cmd.ofMsg EclipseLogin

    let debugPrintChecklist (model: Model) =
        model.ChecklistScreenPlans
        |> List.map (fun plan -> 
            plan.Checklists
            |> List.map (fun catChecklist -> $"{catChecklist.Category}\nLoaded - {catChecklist.Loaded}\nLoading - {catChecklist.Loading}")
            |> String.concat "\n"
            |> (+) $"{plan.PlanDetails.PlanId}\n"
        )
        |> String.concat "\n"