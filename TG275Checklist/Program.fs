namespace TG275Checklist

open Elmish
open Elmish.WPF
open System
open TG275Checklist.Views
open TG275Checklist.Model
open TG275Checklist.Model.App

module Program =


    [<EntryPoint; STAThread>]
    let main args =
        // This is required to load the ESAPI API assembly, otherwise an UnauthorizedScriptingAPIAccessException is thrown:
        // Application was not able to load the required Eclipse Scripting API version.
        // The script has possibly been built against a different version of Eclipse Scripting API that is not available in your system.
        // Another reason might be that the script has no reference to Eclipse Scripting API at all.
        let referenceAPI = new VMS.TPS.Common.Model.API.ESAPIActionPackAttribute()

        let courses = 
            args.[1].Split([|"\\\\"|], StringSplitOptions.None) 
            |> Array.toList     // Individual Course/Plan strings
            |> List.groupBy(fun fullId -> fullId.Split('\\').[0]) 
            |> List.map(fun (course, plans) -> (course, plans |> List.map(fun plan -> plan.Split('\\').[1]))) // Grouped by Course ID
            |> List.map (fun (course, plans) -> 
                { 
                    Id = course; 
                    Plans = plans 
                            |> List.map(fun plan -> { Id = plan }) 
                })

        let standaloneArgs = 
            let tempArgs = if args.Length = 0 then [|"4703528"; "1 SACRUM\\SACRUM_2"|] else args  // Use a known patient if args are blank (launched outside of Eclipse)
            {
                PatientID = tempArgs.[0];
                Courses = courses;
                OpenedCourseID = args.[1].Split('\\').[0];
                OpenedPlanID = args.[1].Split('\\').[1]
            }

        let window = new MainWindow()
        
        // Dispose of the Esapi service and Application to prevent crashing
        window.Closed.AddHandler(fun _ _ -> Esapi.esapi.Dispose())

        Program.mkProgramWpf (fun () -> init standaloneArgs) update bindings
        |> Program.withConsoleTrace
        |> Program.runWindowWithConfig
            { ElmConfig.Default with LogConsole = true; Measure = true }
            (window) |> ignore

        1