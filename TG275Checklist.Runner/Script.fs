namespace VMS.TPS

open System.Diagnostics
open System.Reflection
open System.Windows
open VMS.TPS.Common.Model.API

type Script () =

    member this.Execute (context:ScriptContext) =

        let fullAssemblyName = Assembly.GetExecutingAssembly().Location
        let minusExtension = fullAssemblyName.[0..fullAssemblyName.Length-11]
        let exePath = minusExtension + ".exe"

        let openedPlans = // Starting with the currently open plan
            [$"{context.Course.Id}\\{context.PlanSetup.Id}";
            context.ExternalPlansInScope
            |> Seq.filter (fun plan -> context.PlanSetup.Id <> plan.Id && context.Course.Id <> plan.Course.Id)
            |> Seq.sortBy (fun plan -> plan.Course.Id)
            |> Seq.map (fun plan -> $"{plan.Course.Id}\\{plan.Id}")
            |> String.concat "\\\\"]
            |> String.concat "\\\\"

        try
            if context.Patient = null 
            then MessageBox.Show("Please open a patient before running this script", "No Patient Loaded") |> ignore
            else Process.Start(exePath, $"\"{context.Patient.Id}\" \"{openedPlans}\"") |> ignore
        with ex ->
            MessageBox.Show($"{ex.Message}\n\n{ex.InnerException}\n\n{ex.StackTrace}", "Failed to start application.") |> ignore
