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

    let oncologistLookup id =
        match id with
        | "" -> "No Assigned Primary Oncologist"
        | "1700882685A" -> "Faheem Ahmad, MD"
        | "1013302397A" -> "David Bergman, MD"
        | "1467716969A" -> "Hassan Beydoun, MD"
        | "1144410317A" -> "Amit Bhatt, MD"
        | "1699970525A" -> "Boike, Thomas"
        | "1144403205" -> "Erin Burke, PA-C"
        | "1164423216A" -> "Canto, Cheryl"
        | "1740265834A" -> "Connolly, Irene"
        | "1760689665A" -> "Coppola, Elena ."
        | "1093078834A" -> "John Cramer, MD"
        | "1790952059A" -> "Kiran Devisetty, MD"
        | "1003135500A" -> "Michael Dominello, DO"
        | "1720061534A" -> "Stephen Franklin, MD"
        | "1609899640A" -> "Arthur Frazier, MD"
        | "1386628626A" -> "Galloway, Lisa ."
        | "1083903256A" -> "Omar Gayar, MD"
        | "1962402370A" -> "Hesham Gayar, MD"
        | "1174584817A" -> "Gun, Samuel."
        | "1164613931A" -> "Rabbie Hanna, MD"
        | "1033159900A" -> "Hart, Kimberly"
        | "1205861853A" -> "Hire, Ervin ."
        | "1649596800A" -> "Jeffrey Hotaling, MD"
        | "1316972086A" -> "Christian Hyde, MD"
        | "1558658872A" -> "Matthew Johnson, MD"
        | "1275537144A" -> "Nathan Kaufman, MD"
        | "1821230921A" -> "Isaac Kaufman, MD"
        | "1639198120A" -> "Harold Kim, MD"
        | "1578515490A" -> "Jordan Maier, MD"
        | "1417927625A" -> "Steven Miller, MD"
        | "1174523575A" -> "Jack Nettleton, MD"
        | "14992" -> "Tolutope Oyasiji, M.D. MRCS, MHSA, FACS"
        | "1215149117A" -> "Spencer, Brooke"
        | "1427379718A" -> "Stenz, Justin"
        | "14531" -> "Tari Stull, M.D."
        | "1013954007" -> "Andrew Turrisi, M.D."
        | "1861466963A" -> "Nitin Vaishampayan, MD"
        | "1083027346A" -> "Dustin Wasylik (Profile Not Attached)"
        | "1669697629A" -> "Salam Yanek, MD"
        | "1396783460A" -> "George Yoo, MD"
        | _ -> $"Unknown (new?) Doctor ({id})"
