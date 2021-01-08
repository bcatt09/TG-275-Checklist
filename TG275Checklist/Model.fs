namespace TG275Checklist

open Elmish
open Elmish.WPF

open Esapi
open TG275Checklist.Views
open VMS.TPS.Common.Model.API
open MahApps.Metro.Controls.Dialogs

module PatientSetup =
    type FlagType =
        | PreviousRT
        | Pacemaker
        | Registration
        | FourD
        | DIBH
        | IMRT
        | SRS
        | SBRT
    type Flag =
        {
            Type: FlagType
            Text: string
            Checked: bool
            Id: int
        }
    let private getText flagType =
        match flagType with
        | PreviousRT _ -> "Previous RT"
        | Pacemaker _ -> "Pacemaker/ICD"
        | Registration _ -> "Registration"
        | FourD _ -> "4D Simulation"
        | DIBH _ -> "DIBH"
        | IMRT _ -> "IMRT/VMAT"
        | SRS _ -> "SRS"
        | SBRT _ -> "SBRT"
    let flagList =  
        [
            {
                Type = PreviousRT;
                Text = getText PreviousRT;
                Checked = false; 
                Id = 1
            };
            {
                Type = Pacemaker;
                Text = getText Pacemaker;
                Checked = false; 
                Id = 2
            };
            {
                Type = Registration;
                Text = getText Registration;
                Checked = false; 
                Id = 3
            };
            {
                Type = FourD;
                Text = getText FourD;
                Checked = false; 
                Id = 4
            };
            {
                Type = DIBH;
                Text = getText DIBH;
                Checked = false; 
                Id = 5
            };
            {
                Type = IMRT;
                Text = getText IMRT;
                Checked = false; 
                Id = 6
            };
            {
                Type = SRS;
                Text = getText SRS;
                Checked = false; 
                Id = 7
            };
            {
                Type = SBRT;
                Text = getText SBRT;
                Checked = false; 
                Id = 8
            };
        ]
    type Plan =
        {
            Id: string
        }
    type Course =
        {
            Id: string
            Plans: Plan list
        }
    type Model =
        {
            Courses: Course list
            Flags: Flag list
        }
    let init () =
        {
            Courses = []
            Flags = flagList
        }

    type Msg =
    | FlagChanged of int * bool

    let update msg m =
        match msg with
        | FlagChanged (id, ischecked) -> { m with Flags = m.Flags |> List.map(fun flag -> if flag.Id = id then { flag with Checked = ischecked } else flag) }

    let flagBindings () : Binding<Model * Flag, Msg> list =
        [
            "Text" |> Binding.oneWay (fun (_, f) -> f.Text);
            "Flag" |> Binding.twoWay ((fun (_, f:Flag) -> f.Checked), (fun value (_, f:Flag) -> FlagChanged (f.Id, value)))
        ]

    let bindings () : Binding<Model, Msg> list =
        [
            "PatientSetupFlags" |> Binding.subModelSeq((fun m -> m.Flags), (fun (f:Flag) -> f.Id), flagBindings)
        ]

module Checklist =
    type ChecklistCategory =
        | Prescription
        | Simulation
        | Contouring
        | StandardProcedure
        | DoseDistribution
        | Verification
        | Isocenter
        | ImageGuidanceSetup
        | Scheduling
        | Replan
        | Deviations
    type ChecklistItem =
        {
            Description: string
            EsapiText: string option
            //OtherThingsToDisplay1: 'a option
            //OtherThingsToDisplay2: 'a option
        }
    type Model =
        {
            Category: ChecklistCategory
            Checklist: ChecklistItem list
        }

module App =
    type StandaloneApplicationArgs =
        {
            PatientID: string
            CourseID: string
            PlanID: string
        }

    type SharedInfo =
        {
            PatientName: string
            CourseID: string
            PlanID: string
            CurrentUser: string
        }

    type Model =
        { 
            PatientSetupScreen: PatientSetup.Model
            //ChecklistList: Checklist.Model list
            SharedInfo: SharedInfo
            MainWindow: MainWindow
            Status: string
            Args: StandaloneApplicationArgs
        }

    type Msg =
        // Eclipse login
        | Login        
        | LoginSuccess of SharedInfo
        | LoginFailed of exn
        | PatientSetupMsg of PatientSetup.Msg
        | Debugton

    // Initial empty model
    let init (window:MainWindow) (args:StandaloneApplicationArgs) =
        { 
            PatientSetupScreen = PatientSetup.init()
            SharedInfo =
                {
                    PatientName = ""
                    CurrentUser = ""
                    CourseID = ""
                    PlanID = ""
                }
            MainWindow = window
            Status = ""
            Args = args
        }, Cmd.ofMsg Login
    
    // Initial Eclipse login function
    let login (window:MainWindow, args:StandaloneApplicationArgs) =

        // Register DataContext for displaying Metro Dialogs
        DialogParticipation.SetRegister(window, window.DataContext)

        async {
            // Dialog Settings
            let mySettings = 
                MetroDialogSettings(
                    NegativeButtonText = "Cancel",
                    AnimateShow = false,
                    AnimateHide = false,
                    ColorScheme = MetroDialogColorScheme.Accented)
            
            // Display ProgressDialog
            let! controller = DialogCoordinator.Instance.ShowProgressAsync (context = window.DataContext, title = "Logging in to Eclipse", message = "Please wait...", settings = mySettings) |> Async.AwaitTask
            controller.SetIndeterminate()

            // Log in to Eclipse and get initial patient info
            try
                do! esapi.LogInAsync()
                do! esapi.OpenPatientAsync(args.PatientID)
                let! patientInfo = esapi.Run(fun (pat : Patient, app : Application) ->
                        let course = 
                            pat.Courses 
                            |> Seq.filter (fun x -> x.Id = args.CourseID) 
                            |> Seq.exactlyOne
                        let plan = 
                            course.PlanSetups
                            |> Seq.filter (fun x -> x.Id = args.PlanID)
                            |> Seq.exactlyOne
                        {
                            PatientName = pat.Name
                            CourseID = course.Id
                            PlanID = plan.Id
                            CurrentUser = app.CurrentUser.Name
                        })

                // Close Dialog
                do! controller.CloseAsync() |> Async.AwaitTask

                // Dispose of the Esapi service and Application to prevent crashing
                window.Closed.AddHandler(fun _ _ -> esapi.Dispose())
                
                return LoginSuccess patientInfo
            with ex ->
                do! controller.CloseAsync() |> Async.AwaitTask

                return LoginFailed ex
        }
         

    // Handle any messages
    let update msg m =
        match msg with
        | Login -> { m with Status = "Logging in to Eclipse" }, Cmd.OfAsync.either login (m.MainWindow, m.Args) id LoginFailed
        | LoginFailed x -> 
            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Login to Eclipse") |> ignore
            { m with Status = "Failed to log in to Eclipse" }, Cmd.none
        | LoginSuccess patientInfo -> { m with SharedInfo = patientInfo; Status = "Ready" }, Cmd.none
        | PatientSetupMsg msg -> { m with PatientSetupScreen = PatientSetup.update msg m.PatientSetupScreen }, Cmd.none
        | Debugton -> System.Windows.MessageBox.Show(m.PatientSetupScreen.Flags |> List.map(fun f -> f.Checked.ToString()) |> String.concat "\n") |> ignore; m, Cmd.none

    // WPF bindings
    let bindings () : Binding<Model, Msg> list = 
        [
            // MainWindow Info
            "PatientName" |> Binding.oneWay (fun m -> m.SharedInfo.PatientName)
            "CurrentUser" |> Binding.oneWay (fun m -> m.SharedInfo.CurrentUser)
            "PlanID" |> Binding.oneWay (fun m -> m.SharedInfo.PlanID)
            "CourseID" |> Binding.oneWay (fun m -> m.SharedInfo.CourseID)
            "Status" |> Binding.oneWay (fun m -> m.Status)

            // Patient Setup
            "PatientSetup" |> Binding.subModel((fun m -> m.PatientSetupScreen), snd, PatientSetupMsg, PatientSetup.bindings)

            "Debugton" |> Binding.cmd Debugton
        ]