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

        // Patient Setup Screen
        
        | LoadCoursesIntoPatientSetup
        | LoadCoursesSuccess of PatientSetupCourse list
        | LoadCoursesFailed of exn

        | PatientSetupToggleChanged of PatientSetupToggleType * bool
        | PatientSetupUsePlanChanged of string * bool
        | PatientSetupCourseIsExpandedChanged of string * bool

        // Checklist Screen
        
        | DisplayChecklistScreen

        | LoadChecklists
        | LoadChecklistsSuccess of FullChecklist list
        | LoadChecklistsFailure of exn


        | Debugton
    
    let init args =
        { 
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
            SharedInfo =
                {
                    PatientName = ""
                    CurrentUser = ""
                }
            ChecklistScreenPlans = []
            ChecklistScreenVisibility = Collapsed
            StatusBar = readyStatus
            Args = args
        }, Cmd.ofMsg EclipseLogin