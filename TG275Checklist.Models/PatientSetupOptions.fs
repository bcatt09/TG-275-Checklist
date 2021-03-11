namespace TG275Checklist.Model

// Checklist options which are held in the PatientSetup Screen
module PatientSetupOptions = 

    type SelectedPlan =
        {
            PlanId: string
            CourseId: string
        }

    type ToggleType =
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

    let toggleList = [ PreviousRT false; Pacemaker false; Registration false; FourD false; DIBH false; IMRT false; SRS false; SBRT false ]
    
    type Model =
        {
            Plans: SelectedPlan list
            Toggles: ToggleType list
        }
    let init () = { Plans = []; Toggles = [] }