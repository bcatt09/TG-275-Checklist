namespace TG275Checklist

open Elmish
open Elmish.WPF
open VMS.TPS.Common.Model.API

open TG275Checklist.Views
open System.Threading

module Model =
    
    // Model
    type StandaloneArgs =
        {
            PatientID: string
            CourseID: string
            PlanID: string
        }

    type EsapiContext =
        {
            Patient: Patient
            Course: Course
            Plan: PlanSetup
            CurrentUser: User
        }

    type Model =
        { 
            Test: string
            PatientName: string
            CurrentUser: string
            CourseID: string
            PlanID: string
            MainWindow: TestWindow
            EsapiContext: EsapiContext option
            Args: StandaloneArgs
            InitCtx: SynchronizationContext
        }

    // Messages
    type Msg =
        | Login        
        | LoginSuccess of EsapiContext
        | LoginFailed of exn

    // Initial Model
    let init (ctx:SynchronizationContext) (window:TestWindow) (args:StandaloneArgs) =
        { 
            Test = ""
            PatientName = ""
            CurrentUser = ""
            CourseID = ""
            PlanID = ""
            MainWindow = window
            EsapiContext = None
            Args = args
            InitCtx = ctx
        }, Cmd.ofMsg Login