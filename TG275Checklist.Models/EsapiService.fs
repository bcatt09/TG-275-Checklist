namespace TG275Checklist.Model

open System
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Schedulers
open VMS.TPS.Common.Model.API

open HelperDataTypes

[<AutoOpen>]
module EsapiService =

    // TODO: Implement these?
    //exception PatientNotLoadedException of string
    //exception UserNotLoggedInException of string

    type EsapiAsyncService() =
        // To run any Esapi calls
        let scheduler = new StaTaskScheduler(1)

        // Holds Application and Patient data once they are opened
        let mutable app : Application option = None
        let mutable patient : Patient option = None

        // Run a function on the Esapi thread
        let RunAsync f = Task.Factory.StartNew(Func<_> f, CancellationToken.None, TaskCreationOptions.None, scheduler) |> Async.AwaitTask
    
        // Opening/Closing
        member this.LogInAsync() = RunAsync(fun () -> app <- Some (Application.CreateApplication()))
        member this.OpenPatientAsync (patientId : string) = RunAsync(fun () -> 
            patient <- 
                match app with
                | Some app -> Some (app.OpenPatientById(patientId))
                | None -> None)
        member this.Dispose() = RunAsync(fun () -> app.Value.Dispose()) |> Async.Start; scheduler.Dispose()

        // Run Overloads
        member this.Run (f: Application -> 'a) = RunAsync (fun () -> f(app.Value))
        member this.Run (f: Patient -> 'a) = RunAsync (fun () -> f(patient.Value))
        member this.Run (f: Application * Patient -> 'a) = RunAsync (fun () -> f(app.Value, patient.Value))
        member this.Run (f: Patient * Application -> 'a) = RunAsync (fun () -> f(patient.Value, app.Value))

    // The service instance that will be used through the application
    let esapi = new EsapiAsyncService()
                
    // Any results that will be returned to display in the checklist
    type EsapiResults =
        {
            Text: string
            TreatmentAppointments: List<TreatmentAppointmentInfo> option
            TargetCoverageDropdown: TargetCoverageDropdown option
            //OtherThingsToDisplay1: 'a option
            //OtherThingsToDisplay2: 'a option
        }
        static member init = 
            { 
                Text = ""
                TreatmentAppointments = None
                TargetCoverageDropdown = None
            }
        static member fromString str = { EsapiResults.init with Text = str }

    type PureEsapiFunction = PlanSetup -> EsapiResults

    let runEsapiFunction planDetails (esapiFunc: PureEsapiFunction option) =
        async {
            match esapiFunc with
            | Some func ->
                let! result = esapi.Run(fun (pat: Patient) ->
                    let plan = 
                        pat.Courses
                        |> Seq.map(fun c -> c.PlanSetups |> Seq.cast<PlanSetup>)
                        |> Seq.concat
                        |> Seq.filter(fun p -> p.Id = planDetails.PlanId && p.Course.Id = planDetails.CourseId)
                        |> Seq.exactlyOne
                    func plan)

                return Some result

            | None -> return None
        }

    type MiscEsapiFunction = PlanSetup -> string -> string

    let getMiscEsapiResults planDetails input (esapiFunc: MiscEsapiFunction) =
        async {
            let! result = esapi.Run(fun (pat: Patient) ->
                let plan = 
                    pat.Courses
                    |> Seq.map(fun c -> c.PlanSetups |> Seq.cast<PlanSetup>)
                    |> Seq.concat
                    |> Seq.filter(fun p -> p.Id = planDetails.PlanId && p.Course.Id = planDetails.CourseId)
                    |> Seq.exactlyOne
                esapiFunc plan input)

            return result
        }