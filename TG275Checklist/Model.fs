namespace TG275Checklist

open Elmish
open Elmish.WPF

open TG275Checklist.Views

module Model =
    
    // Model
    type StandaloneArgs =
        {
            PatientID: string
            CourseID: string
            PlanID: string
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
            Args: StandaloneArgs
        }

    // Messages
    type Msg =
        | Login        
        | LoginSuccess of PatientInfo
        | LoginFailed of exn

    // Initial Model
    let init (window:TestWindow) (args:StandaloneArgs) =
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
            Args = args
        }, Cmd.ofMsg Login