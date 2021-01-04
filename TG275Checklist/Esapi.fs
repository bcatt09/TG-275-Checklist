module Esapi

open System
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Schedulers
open VMS.TPS.Common.Model.API

exception PatientNotLoadedException of string
exception UserNotLoggedInException of string

//type AsyncRunner() =
//    let scheduler = new StaTaskScheduler(1)
//    member this.runAsync f = Task.Factory.StartNew(f, CancellationToken.None, TaskCreationOptions.None, scheduler)
//    member this.Dispose() = scheduler.Dispose()

//type EsapiServiceBase() =
//    let runner = new AsyncRunner()
//    let mutable app : Application option = None
//    let mutable patient : Patient option = None

//    member this.LogInAsync() = runner.runAsync(fun () -> app <- Some (Application.CreateApplication()))
//    member this.OpenPatientAsync(patId:string) = 
//        runner.runAsync(fun () -> 
            
//            patient <- Some (app.Value.OpenPatientById(patId)
//        ))
//    member this.RunAsync f = 
//        runner.runAsync (Func<_> (f patient.Value))
//    member this.Dispose() = 
//        runner.runAsync(fun () -> app.Value.Dispose()) |> Async.AwaitTask |> Async.Start
//        runner.Dispose()

type EsapiBuilder() =
    let scheduler = new StaTaskScheduler(1)
    let mutable app : Application option = None
    let mutable patient : Patient option = None
    let RunAsync f = Task.Factory.StartNew(Func<_> f, CancellationToken.None, TaskCreationOptions.None, scheduler) |> Async.AwaitTask

    member this.LogInAsync() = RunAsync(fun () -> app <- Some (Application.CreateApplication()))
    member this.OpenPatientAsync (patientId : string) = RunAsync(fun () -> patient <- Some (app.Value.OpenPatientById(patientId)))
    member this.Dispose() = RunAsync(fun () -> app.Value.Dispose()) |> Async.Start; scheduler.Dispose()

    member this.Zero () = ()
    
    member this.Return (f : unit -> 'a) = RunAsync f
    member this.Return (f : Application -> 'a) = RunAsync (fun () -> f(app.Value))
    member this.Return (f : Patient -> 'a) = RunAsync (fun () -> f(patient.Value))
    member this.Return (f : Application * Patient -> 'a) = RunAsync (fun () -> f(app.Value, patient.Value))
    member this.Return (f : Patient * Application -> 'a) = RunAsync (fun () -> f(patient.Value, app.Value))

    //[<CustomOperation("esapi", MaintainsVariableSpaceUsingBind = true)>]
    //member this.Esapi (f : unit -> 'a) = RunAsync f
    //member this.Esapi (f : Patient -> 'a) = RunAsync (fun () -> f(patient.Value))
    //member this.Esapi (f : Application -> 'a) = RunAsync (fun () -> f(app.Value))

    //member this.Run (f : unit -> 'a) = RunAsync f
    //member this.Run (f : Patient -> 'a) = RunAsync (fun () -> f(patient.Value))
    //member this.Run (f : Application -> 'a) = RunAsync (fun () -> f(app.Value))

let esapiAsync = new EsapiBuilder()




//type Microsoft.FSharp.Control.AsyncBuilder with
//    member x.Bind(t:Task<'T>, f:'T -> Async<'R>) : Async<'R> = async.Bind(Async.AwaitTask t, f)   

//    member x.Bind(t:Task, f:unit -> Async<'R>) : Async<'R> = async.Bind(Async.AwaitTask t, f)   

//    member x.Bind (m:'a IObservable, f:'a -> 'b Async) = async.Bind(Async.AwaitObservable m, f)   

//    member x.ReturnFrom(computation:Task<'T>) x.ReturnFrom(Async.AwaitTask computation)