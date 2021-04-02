namespace TG275Checklist

open Elmish
open Elmish.WPF
open System
open TG275Checklist.Views
open TG275Checklist.Model
open Model.Model
open Model.Update
open Model.Bindings

module Program =

    [<EntryPoint; STAThread>]
    // args
    // 0 - Patient ID
    // 1 - Currently active plan ("Course/Plan")
    // 2 - List of all plans opened in Eclipse ("Course/Plan//Course/Plan//...")
    let main args =
        // This is required to load the ESAPI API assembly, otherwise an UnauthorizedScriptingAPIAccessException is thrown:
        // Application was not able to load the required Eclipse Scripting API version.
        // The script has possibly been built against a different version of Eclipse Scripting API that is not available in your system.
        // Another reason might be that the script has no reference to Eclipse Scripting API at all.
        let referenceAPI = new VMS.TPS.Common.Model.API.ESAPIActionPackAttribute()
        
        // Build course list
        let courses = 
            args.[2].Split([|"\\\\"|], StringSplitOptions.None) 
                |> Array.toList     // Individual Course/Plan strings
            |> List.groupBy(fun fullId -> fullId.Split('\\').[0]) 
            |> List.map(fun (course, plans) -> (course, plans |> List.map(fun plan -> plan.Split('\\').[1]))) // Grouped by Course ID
            |> List.map (fun (course, plans) -> 
                { 
                    Id = course; 
                    Plans = plans 
                            |> List.map(fun plan -> { Id = plan }) 
                })  // Final Course list

        // Arguments sent from Eclipse to the standalone application in friendly form to be passed to the model
        let standaloneArgs = 
            let tempArgs = if args.Length = 0 then [|"4703528"; "1 SACRUM\\SACRUM_2"|] else args  // Use a known patient if args are blank (launched outside of Eclipse)
            {
                PatientID = tempArgs.[0];
                Courses = courses;
                OpenedCourseID = args.[1].Split('\\').[0];
                OpenedPlanID = args.[1].Split('\\').[1]
            }

        let window = new MainWindow()
        // Dispose of the Esapi service and Application on window close to prevent crashing
        window.Closed.AddHandler(fun _ _ -> EsapiService.esapi.Dispose())

        // Run the Elmish window
        Program.mkProgramWpf (fun () -> init standaloneArgs) update bindings
        |> Program.withConsoleTrace
        |> Program.runWindowWithConfig
            { ElmConfig.Default with LogConsole = true; Measure = true }
            (window)