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
open System.Windows.Controls
open System.Threading.Tasks.Schedulers
open System.Threading.Tasks
open VMS.TPS.Common.Model.API

module Program =


    [<EntryPoint; STAThread>]
    let main args =
        let printThread msg = MessageBox.Show($"[{Thread.CurrentThread.ManagedThreadId}] - {msg}") |> ignore
        //let fixedArgs = String.Join(" ", args).Split('\\')
        let fixedArgs = [|"4703528"; "1 SACRUM"; "SACRUM_2"|]
        let standaloneArgs = 
            {
                PatientID = fixedArgs.[0]
                CourseID = fixedArgs.[1]
                PlanID = fixedArgs.[2]
            }

        let window = new TestWindow()
        

        Program.mkProgramWpf (fun () -> init window standaloneArgs) update bindings
        |> Program.withConsoleTrace
        |> Program.runWindowWithConfig
            { ElmConfig.Default with LogConsole = true; Measure = true }
            (window) |> ignore

        1