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

    type PatientInfo =
        {
            PatientName: string
            CourseID: string
            PlanID: string
            CurrentUser: string
        }

    type Model =
        { 
            Test: string
            PatientInfo: PatientInfo
            MainWindow: TestWindow
            EsapiContext: PatientInfo option//EsapiContext option
            //EsapiContext: string option
            Args: StandaloneArgs
            //InitCtx: SynchronizationContext
        }

    // Messages
    type Msg =
        | Login        
        | LoginSuccess of PatientInfo
        | LoginFailed of exn
        //| GetPatientInfoSuccess of PatientInfo
        //| GetPatientInfoFailed of exn

    // Initial Model
    let init (*ctx:SynchronizationContext*) (window:TestWindow) (args:StandaloneArgs) =
        { 
            Test = ""
            PatientInfo =
                {
                    PatientName = ""
                    CurrentUser = ""
                    CourseID = ""
                    PlanID = ""
                }
            MainWindow = window
            EsapiContext = None
            Args = args
            //InitCtx = ctx
        }, Cmd.none//Cmd.ofMsg Login