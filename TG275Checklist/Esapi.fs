module Esapi

open System
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Schedulers
open VMS.TPS.Common.Model.API
open System
open System

type AsyncRunner() =
    let scheduler = new StaTaskScheduler(1)
    member this.runAsync f = Task.Factory.StartNew(f, CancellationToken.None, TaskCreationOptions.None, scheduler)
    member this.Dispose() = scheduler.Dispose()

type EsapiServiceBase() =
    let runner = new AsyncRunner()
    let mutable app : Application option = None
    let mutable patient : Patient option = None

    member this.LogInAsync() = runner.runAsync(fun () -> app <- Some (Application.CreateApplication()))
    member this.OpenPatientAsync(patId:string) = runner.runAsync(fun () -> patient <- Some (app.Value.OpenPatientById(patId)))
    member this.GetPatientNameAsync() = runner.runAsync(fun () -> patient.Value.Name)
    member this.RunAsync f = runner.runAsync (Func<_> (f patient.Value))
    member this.Dispose() = runner.runAsync(fun () -> app.Value.Dispose()) |> Async.AwaitTask |> Async.RunSynchronously; runner.Dispose()        // This is still running synchronously