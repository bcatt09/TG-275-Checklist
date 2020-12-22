namespace TG275Checklist

open Elmish
open Elmish.WPF
open MahApps.Metro.Controls.Dialogs
open VMS.TPS.Common.Model.API

open Model
open TG275Checklist.Views

open System.Windows
open System.Threading
open System.Threading.Tasks
open VMS.TPS.Common.Model.API


module Update =

    let login (initCtx:SynchronizationContext, window:TestWindow, args:StandaloneArgs) =
        // Register DataContext for displaying Metro Dialogs
        //DialogParticipation.SetRegister(window, window.DataContext)

        //Application.Current.Dispatcher.Invoke(fun () ->
        MessageBox.Show(Thread.CurrentContext.ContextID.ToString(), "in login") |> ignore
        let guiCtx = SynchronizationContext.Current
        MessageBox.Show(Thread.CurrentContext.ContextID.ToString(), "on gui") |> ignore
        async {
            do! Async.SwitchToContext guiCtx

            // Dialog Settings
            let mySettings = 
                MetroDialogSettings(
                    NegativeButtonText = "Cancel",
                    AnimateShow = false,
                    AnimateHide = false,
                    ColorScheme = MetroDialogColorScheme.Accented)
                
            // Display ProgressDialog
            //let! controller = DialogCoordinator.Instance.ShowProgressAsync (context = window.DataContext, title = "Logging in to Eclipse", message = "Please wait...", settings = mySettings) |> Async.AwaitTask
            //controller.SetIndeterminate()

            let ctx = SynchronizationContext.Current
            do! Async.SwitchToThreadPool()
            // Log in to Eclipse
            let! esapiApp = async { return Application.CreateApplication() }
            do! Async.SwitchToContext ctx

            // Create the Context
            let context = 
                let patient = esapiApp.OpenPatientById(args.PatientID)
                let course = 
                    patient.Courses 
                    |> Seq.filter (fun x -> x.Id = args.CourseID) 
                    |> Seq.exactlyOne
                let plan = 
                    course.PlanSetups
                    |> Seq.filter (fun x -> x.Id = args.PlanID)
                    |> Seq.exactlyOne
                {
                    Patient = patient
                    Course = course
                    Plan = plan
                    CurrentUser = esapiApp.CurrentUser
                }

            // Close Dialog
            //do! controller.CloseAsync() |> Async.AwaitTask

            // Dispose Application to prevent crashing
            window.Closed.AddHandler(fun _ _ -> esapiApp.Dispose())

            return LoginSuccess context
        }
        //)

    //let populateData (context:EsapiContext) =
         


    let update msg m =
        match msg with
        | Login -> m, Cmd.OfAsync.either login (m.InitCtx, m.MainWindow, m.Args) id LoginFailed
        | LoginFailed x -> 
            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.StackTrace}", "Unable to Login to Eclipse") |> ignore
            m, Cmd.none
        | LoginSuccess context -> { m with EsapiContext = Some context}, Cmd.none//ofMsg populateData context id
        //| LoginSuccess test -> { m with Test = test}, Cmd.none

    let bindings () : Binding<Model, Msg> list = 
        [
            //"CurrentUser" |> Binding.oneWay (fun m -> m.Test)
            "PatientName" |> Binding.oneWay (fun m -> match m.EsapiContext with | None -> "" | Some context -> context.Patient.Name)
            "CurrentUser" |> Binding.oneWay (fun m -> match m.EsapiContext with | None -> "" | Some context -> context.CurrentUser.Name)
            "PlanID" |> Binding.oneWay (fun m -> match m.EsapiContext with | None -> "" | Some context -> context.Plan.Id)
            "CourseID" |> Binding.oneWay (fun m -> match m.EsapiContext with | None -> "" | Some context -> context.Course.Id)
            "Login" |> Binding.cmd Login
        ]

