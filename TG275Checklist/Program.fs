namespace TG275Checklist

open Elmish
open Elmish.WPF
open System
open TG275Checklist.Views

open App
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
        let standaloneArgs = 
            let tempArgs = if args.Length = 0 then [|"4703528"; "1 SACRUM"; "SACRUM_2"|] else args  // Use a known patient if args are blank (launched outside of Eclipse)
            {
                PatientID = tempArgs.[0]
                CourseID = tempArgs.[1]
                PlanID = tempArgs.[2]
            }

        let window = new MainWindow()

        Program.mkProgramWpf (fun () -> init window standaloneArgs) update bindings
        |> Program.withConsoleTrace
        |> Program.runWindowWithConfig
            { ElmConfig.Default with LogConsole = true; Measure = true }
            (window) |> ignore

        1