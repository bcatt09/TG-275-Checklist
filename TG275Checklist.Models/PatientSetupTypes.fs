namespace TG275Checklist.Model

module PatientSetupTypes =

    // Patient setup checkboxes to display/hide certain checklist items
    type PatientSetupToggleType =
        | PreviousRT of bool
        | Pacemaker of bool
        | Registration of bool
        | FourD of bool
        | DIBH of bool
        | IMRT of bool
        | SRS of bool
        | SBRT of bool

        member this.IsChecked = 
            match this with
            | PreviousRT value -> value
            | Pacemaker value -> value
            | Registration value -> value
            | FourD value -> value
            | DIBH value -> value
            | IMRT value -> value
            | SRS value -> value
            | SBRT value -> value
        member this.Text =
            match this with
            | PreviousRT _ -> "Previous RT"
            | Pacemaker _ -> "Pacemaker/ICD"
            | Registration _ -> "Registration"
            | FourD _ -> "4D Simulation"
            | DIBH _ -> "DIBH"
            | IMRT _ -> "IMRT/VMAT"
            | SRS _ -> "SRS"
            | SBRT _ -> "SBRT"

    // Initial state of list of toggles
    let initToggleList = 
        [ 
            PreviousRT false
            Pacemaker false
            Registration false
            FourD false
            DIBH false
            IMRT false
            SRS false
            SBRT false
        ]
    
    // Plan and toggle list to be displayed on Patient Setup Screen
    type PatientSetupOptions =
        {
            Plans: PlanInfo list
            Toggles: PatientSetupToggleType list
        }

    // Initial empty state of patient setup options
    let initPatientSetupOptions () = 
        { 
            Plans = []
            Toggles = []
        }

    //// Plans to be displayed in Patient Setup Screen
    //type PatientSetupPlan =
    //    {
    //        PlanId: string
    //        CourseId: string
    //        Dose: string
    //        PatientName: string
    //        Oncologist: string
    //        IsChecked: bool     // Is it checked off to be used in checklists?
    //        bindingid: string   // Used for subModel bindings
    //    }
    // Unique plan ID for Binding.subModelSeq (Course/Plan ID combination from Eclipse will be unique)
    let getPlanBindingId courseId planId = courseId + "\\" + planId

    //// Courses to be displayed in Patient Setup Screen
    //type PatientSetupCourse =
    //    {
    //        CourseId: string
    //        Plans: PatientSetupPlan list
    //        IsExpanded: bool    // Is the course expanded (mainly used for initial model)
    //    }

