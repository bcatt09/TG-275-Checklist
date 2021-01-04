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
open System
open System.Threading.Tasks.Schedulers
open System.Windows.Input
open Esapi

module Update =
    let printThread msg = MessageBox.Show($"[{Thread.CurrentThread.ManagedThreadId}] - {msg}") |> ignore

    let login ((*initCtx:SynchronizationContext,*) window:TestWindow, args:StandaloneArgs) =
        // Register DataContext for displaying Metro Dialogs
        DialogParticipation.SetRegister(window, window.DataContext)

        let service = new EsapiServiceBase()

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

            // Log in to Eclipse
            try
                //let esapiApp = async { return Application.CreateApplication() } |> Async.RunSynchronously
                do! service.LogInAsync() |> Async.AwaitTask
                do! service.OpenPatientAsync(args.PatientID) |> Async.AwaitTask
                //// Create the Context
                //let! context = service.RunAsync(fun () ->
                //    let patient = esapiApp.OpenPatientById(args.PatientID)
                //    let course = 
                //        patient.Courses 
                //        |> Seq.filter (fun x -> x.Id = args.CourseID) 
                //        |> Seq.exactlyOne
                //    let plan = 
                //        course.PlanSetups
                //        |> Seq.filter (fun x -> x.Id = args.PlanID)
                //        |> Seq.exactlyOne
                //    {
                //        Patient = patient
                //        Course = course
                //        Plan = plan
                //        CurrentUser = esapiApp.CurrentUser
                //    }
                //)

                let! pInfo = 
                    let b = fun (pat:Patient) () ->
                        let course = 
                            pat.Courses 
                            |> Seq.filter (fun x -> x.Id = args.CourseID) 
                            |> Seq.exactlyOne
                        let plan = 
                            course.PlanSetups
                            |> Seq.filter (fun x -> x.Id = args.PlanID)
                            |> Seq.exactlyOne
                        let a ={
                            PatientName = pat.Id
                            CourseID = course.Id
                            PlanID = plan.Id
                            CurrentUser = pat.Name
                        }
                        a
                    service.RunAsync(b) |> Async.AwaitTask

                // Close Dialog
                do! controller.CloseAsync() |> Async.AwaitTask

                // Dispose Application to prevent crashing
                window.Closed.AddHandler(fun _ _ -> service.Dispose())
                
                printThread "returning context"
                return LoginSuccess pInfo
            with ex ->
                do! controller.CloseAsync() |> Async.AwaitTask

                return LoginFailed ex
        }

    //let populateData (context:EsapiContext) =
    //    printThread "populating data1"
    //    async{
    //        printThread "populating data"
    //        let! pData = service.RunAsync(fun () ->
    //            {
    //                PatientName = context.Patient.Id
    //                CourseID = context.Course.Id
    //                PlanID = context.Plan.Id
    //                CurrentUser = context.CurrentUser.Name
    //            }
    //        )
            
    //        printThread "returning data"
    //        return GetPatientInfoSuccess pData
    //    }
         


    let update msg m =
        match msg with
        | Login -> m, Cmd.OfAsync.either login (m.MainWindow, m.Args) id LoginFailed
        | LoginFailed x -> 
            System.Windows.MessageBox.Show($"{x.Message}\n\n{x.StackTrace}", "Unable to Login to Eclipse") |> ignore
            m, Cmd.none
        | LoginSuccess context -> { m with PatientInfo = context}, Cmd.none//Cmd.OfAsync.either populateData context id GetPatientInfoFailed
        //| GetPatientInfoSuccess patInfo -> { m with PatientInfo = patInfo}, Cmd.none
        //| GetPatientInfoFailed x -> 
        //    System.Windows.MessageBox.Show($"{x.Message}\n\n{x.StackTrace}", "Unable to Get Patient Info") |> ignore
        //    m, Cmd.none

    let bindings () : Binding<Model, Msg> list = 
        [
            //"CurrentUser" |> Binding.oneWay (fun m -> m.Test)
            "PatientName" |> Binding.oneWay (fun m -> m.PatientInfo.PatientName)
            "CurrentUser" |> Binding.oneWay (fun m -> m.PatientInfo.CurrentUser)
            "PlanID" |> Binding.oneWay (fun m -> m.PatientInfo.PlanID)
            "CourseID" |> Binding.oneWay (fun m -> m.PatientInfo.CourseID)
            "Login" |> Binding.cmd Login
        ]

