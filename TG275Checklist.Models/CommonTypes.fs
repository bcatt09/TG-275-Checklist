namespace TG275Checklist.Model

// Raw Course/Plan information (Needs to be outside of a module for XAMLs)
type PlanInfo =
    {
        Id: string
    }
type CourseInfo =
    {
        Id: string
        Plans: PlanInfo list
    }

// Common types used throughout the application
[<AutoOpen>]
module CommonTypes = 

    // Arguments passed from the Eclipse plugin
    type StandaloneApplicationArgs =
        {
            PatientID: string
            Courses: CourseInfo list
            OpenedCourseID: string
            OpenedPlanID: string
        } 
    
    // Used wherever plan information is displayed or stored to opening a plan in Eclipse
    type PlanDetails =
        {
            CourseId: string
            PlanId: string
            // Dose
            // Approvals
        }

    // Info displayed by the Main Window
    type MainWindowInfo =
        {
            PatientName: string
            CurrentUser: string
        }

    // Status Bar at bottom of window
    type StatusBar =
    | NoLoadingBar of string
    | Indeterminate of IndeterminateStatusBar
    | Determinate of DeterminateStatusBar
    and IndeterminateStatusBar = { Status: string }
    and DeterminateStatusBar = { Status: string; min: int; max: int }

    // Status bar helpers
    let readyStatus = NoLoadingBar "Ready"
    let indeterminateStatus status = Indeterminate { Status = status }
