namespace TG275Checklist

open Elmish
open Elmish.WPF
open MahApps.Metro.Controls.Dialogs
open VMS.TPS.Common.Model.API

open Esapi
open Model
open TG275Checklist.Views

open System.Threading

module Update =
    
    // Initial Eclipse login function
    let login (window:TestWindow, args:StandaloneArgs) =

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
                do! esapiAsync.LogInAsync()
                do! esapiAsync.OpenPatientAsync(args.PatientID)
                let! patientInfo = 
                    esapiAsync { 
                        return fun (app:Application, pat:Patient) ->
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
                            }
                        }

                // Close Dialog
                do! controller.CloseAsync() |> Async.AwaitTask

                // Dispose of the Esapi service and Application to prevent crashing
                window.Closed.AddHandler(fun _ _ -> esapiAsync.Dispose())
                
                return LoginSuccess patientInfo
            with ex ->
                do! controller.CloseAsync() |> Async.AwaitTask

                return LoginFailed ex
        }
         

    // Handle any messages
    let update msg m =
        match msg with
        | Login -> m, Cmd.OfAsync.either login (m.MainWindow, m.Args) id LoginFailed
        | LoginFailed x -> 
            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.InnerException}\n\n{x.StackTrace}", "Unable to Login to Eclipse") |> ignore
            m, Cmd.none
        | LoginSuccess patientInfo -> { m with PatientInfo = patientInfo}, Cmd.none

    // WPF bindings
    let bindings () : Binding<Model, Msg> list = 
        [
            "PatientName" |> Binding.oneWay (fun m -> m.PatientInfo.PatientName)
            "CurrentUser" |> Binding.oneWay (fun m -> m.PatientInfo.CurrentUser)
            "PlanID" |> Binding.oneWay (fun m -> m.PatientInfo.PlanID)
            "CourseID" |> Binding.oneWay (fun m -> m.PatientInfo.CourseID)
        ]

