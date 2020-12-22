namespace TG275Checklist

open Elmish
open Elmish.WPF
open System
open TG275Checklist.Views

open Model
open Update
open System.Threading
open System.Windows

module Program =


    [<EntryPoint; STAThread>]
    let main args =
        async
            {
                let ctx = SynchronizationContext.Current
                MessageBox.Show(Thread.CurrentContext.ContextID.ToString(), "main") |> ignore
                do! Async.SwitchToThreadPool()
                MessageBox.Show(Thread.CurrentContext.ContextID.ToString(), "gui") |> ignore

                //let fixedArgs = String.Join(" ", args).Split('\\')
                let fixedArgs = [|"5050951"; "1 Prostate"; "Prost_1"|]
                let standaloneArgs = 
                    {
                        PatientID = fixedArgs.[0]
                        CourseID = fixedArgs.[1]
                        PlanID = fixedArgs.[2]
                    }

                let window = new TestWindow()

                Program.mkProgramWpf (fun () -> init ctx window standaloneArgs) update bindings
                |> Program.withConsoleTrace
                |> Program.runWindowWithConfig
                    { ElmConfig.Default with LogConsole = true; Measure = true }
                    (window) |> ignore
            } |> Async.StartImmediate

        1