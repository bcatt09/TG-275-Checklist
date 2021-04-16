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
            
            PatientSetupScreenCourses: PatientSetupCourse list
            PatientSetupScreenToggles: PatientSetupToggleType list
            PatientSetupScreenVisibility: System.Windows.Visibility
            
            // Checklist Screen
            
            ChecklistScreenPlans: FullChecklist list
            ChecklistScreenVisibility: System.Windows.Visibility
        }
        
    type Msg =
        
        // Main Window

        | EclipseLogin        
        | EclipseLoginSuccess of MainWindowInfo
        | EclipseLoginFailed of exn
        | Refresh

        // Patient Setup Screen
        
        | LoadCoursesIntoPatientSetup
        | LoadCoursesSuccess of PatientSetupCourse list
        | LoadCoursesFailed of exn

        | PatientSetupToggleChanged of PatientSetupToggleType * bool
        | PatientSetupUsePlanChanged of string * bool
        | PatientSetupCourseIsExpandedChanged of string * bool

        // Checklist Screen
        
        | DisplayChecklistScreen
        | PrepToLoadNextChecklist
        | UpdateLoadingMessage
        | LoadNextChecklist
        | LoadChecklistSuccess of FullChecklist list
        | LoadChecklistFailure of exn
        | AllChecklistsLoaded
    
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
                    Id = c.Id; 
                    IsExpanded = c.Id = args.OpenedCourseID; 
                    Plans = c.Plans |> List.map (fun p ->
                        {
                            Id = p.Id;
                            IsChecked = p.Id = args.OpenedPlanID && c.Id = args.OpenedCourseID;
                            bindingid = getPlanBindingId c.Id p.Id 
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