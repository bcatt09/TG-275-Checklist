namespace TG275Checklist.Model

// Raw Course/Plan information loaded from Eclipse (Needs to be outside of a module for XAMLs)
type PlanInfo =
    {
        PlanId: string
        CourseId: string
        PatientName: string
        PlanDose: string
        Oncologist: string
        IsChecked: bool     // Is it checked off to be used in checklists?
    }
    member this.bindingId = this.CourseId + "\\" + this.PlanId
    static member init =
        {
            PlanId = ""
            CourseId = ""
            PatientName = ""
            PlanDose = ""
            Oncologist = ""
            IsChecked = false
        }

type CourseInfo =
    {
        CourseId: string
        Plans: PlanInfo list
        IsExpanded: bool    // Is the course expanded (mainly used for initial model)
    }
    static member init =
        {
            CourseId = ""
            Plans = []
            IsExpanded = false
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
