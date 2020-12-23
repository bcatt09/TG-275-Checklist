namespace TG275Checklist

open Elmish
open Elmish.WPF
open System
open TG275Checklist.Views

open Model
open Update
open System.Threading
open System.Windows
open System.Windows.Threading

module Program =


    [<EntryPoint; STAThread>]
    let main args =
        async{
            let ctx = SynchronizationContext.Current
            MessageBox.Show($"Thread: {Thread.CurrentThread.ManagedThreadId}", "Initial (ESAPI)") |> ignore

            //let fixedArgs = String.Join(" ", args).Split('\\')
            let fixedArgs = [|"5050951"; "1 Prostate"; "Prost_1"|]
            let standaloneArgs = 
                {
                    PatientID = fixedArgs.[0]
                    CourseID = fixedArgs.[1]
                    PlanID = fixedArgs.[2]
                }

            let startElmish ctx =
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher))
                let window = new TestWindow()

                Program.mkProgramWpf (fun () -> init ctx window standaloneArgs) update bindings
                |> Program.withConsoleTrace
                |> Program.runWindowWithConfig
                    { ElmConfig.Default with LogConsole = true; Measure = true }
                    (window) |> ignore

            let start() = startElmish ctx

            //do! Async.SwitchToNewThread()
            //Thread.CurrentThread.SetApartmentState(ApartmentState.STA)
            let uiThread = new Thread(start)
            uiThread.SetApartmentState(ApartmentState.STA)
            uiThread.Start()
            //MessageBox.Show($"Thread: {Thread.CurrentThread.ManagedThreadId}", "New context (UI)") |> ignore
        } |> Async.RunSynchronously

        1