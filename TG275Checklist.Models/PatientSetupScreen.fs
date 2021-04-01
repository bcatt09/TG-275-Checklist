namespace TG275Checklist.Model

open Elmish
open Elmish.WPF

open type System.Windows.Visibility

module PatientSetupScreen =

    open PatientSetupOptions

    //// Courses/Plans to be used in PatientSetup Screen
    //type PlanWithOptions =
    //    {
    //        Id: string
    //        IsChecked: bool     // Is it checked off to be used in checklists?
    //        bindingid: string   // Used for subModel bindings
    //    }
    //let getPlanBindingId courseId planId = courseId + "\\" + planId
    //type CourseWithOptions =
    //    {
    //        Id: string
    //        Plans: PlanWithOptions list
    //        IsExpanded: bool    // Is the course expanded (mainly used for initial model)
    //    }

    //// PatientSetup Model
    //type Model =
    //    {
    //        Courses: CourseWithOptions list
    //        Toggles: ToggleType list
    //        Visibility: System.Windows.Visibility
    //    }

    //// Initial model
    //let init (args:StandaloneApplicationArgs) =
    //    {
    //        Courses = args.Courses |> List.map (fun c -> 
    //            { 
    //                Id = c.Id; 
    //                IsExpanded = c.Id = args.OpenedCourseID; 
    //                Plans = c.Plans |> List.map (fun p ->
    //                    {
    //                        Id = p.Id;
    //                        IsChecked = p.Id = args.OpenedPlanID && c.Id = args.OpenedCourseID;
    //                        bindingid = getPlanBindingId c.Id p.Id 
    //                    })
    //            })
    //        Toggles = toggleList
    //        Visibility = Visible
    //    }
    //let initFromCourses args courses = { init(args) with Courses = courses }

    //// Internal Messages
    //type Msg =
    //| ToggleChanged of ToggleType * bool
    //| UsePlanChanged of string * bool
    //| CourseIsExpandedChanged of string * bool
    //| NextButtonClicked

    //// Messages sent to MainWindow
    //type MainWindowMsg =
    //| NoMainWindowMsg
    //| PatientSetupCompleted of PatientSetupOptions.Model

    //// Handle Messages
    //let update msg (m:Model) =
    //    match msg with
    //    | ToggleChanged  (id, ischecked) -> 
    //        { m with 
    //            Toggles = m.Toggles 
    //            |> List.map(fun toggle -> 
    //                if id = toggle
    //                then 
    //                    match toggle with
    //                    | PreviousRT t -> PreviousRT ischecked
    //                    | Pacemaker t -> Pacemaker ischecked
    //                    | Registration t -> Registration ischecked
    //                    | FourD t -> FourD ischecked
    //                    | DIBH t -> DIBH ischecked
    //                    | IMRT t -> IMRT ischecked
    //                    | SRS t -> SRS ischecked
    //                    | SBRT t -> SBRT ischecked
    //                else toggle)
    //        }, Cmd.none, NoMainWindowMsg
    //    | UsePlanChanged (id, ischecked) -> 
    //        { m with 
    //            Courses = m.Courses 
    //            |> List.map(fun course -> 
    //                { course with 
    //                    Plans = course.Plans 
    //                    |> List.map(fun p -> 
    //                        if p.bindingid = id 
    //                        then { Id = p.Id; IsChecked = ischecked; bindingid = p.bindingid } 
    //                        else p) 
    //                })
    //        }, Cmd.none, NoMainWindowMsg 
    //    | CourseIsExpandedChanged (id, isexpanded) -> 
    //        { m with 
    //            Courses = m.Courses
    //            |> List.map (fun c -> if c.Id = id then { c with IsExpanded = isexpanded } else c)
    //        }, Cmd.none, NoMainWindowMsg 
    //    | NextButtonClicked -> 
    //        { m with Visibility = Collapsed }, 
    //        Cmd.none, 
    //        PatientSetupCompleted 
    //            { 
    //                Plans = 
    //                    [ for c in m.Courses do
    //                        for p in c.Plans do
    //                            if p.IsChecked then 
    //                                yield { PlanId = p.Id; CourseId = c.Id } ]
    //                Toggles = m.Toggles |> List.filter (fun t -> t.IsChecked)          
    //            }

    //// WPF Bindings
    //let planBindings () : Binding<(Model * CourseWithOptions) * PlanWithOptions, Msg> list =
    //    [
    //        "Id" |> Binding.oneWay (fun (_, p) -> p.Id)
    //        "IsChecked" |> Binding.twoWay ((fun (_, p:PlanWithOptions) -> p.IsChecked), (fun value (_, p:PlanWithOptions) -> UsePlanChanged (p.bindingid, value)))
    //    ]
    //let courseBindings () : Binding<Model * CourseWithOptions, Msg> list =
    //    [
    //        "Id" |> Binding.oneWay (fun (_, c) -> c.Id)
    //        "Plans" |> Binding.subModelSeq((fun (_, c) -> c.Plans), (fun (p:PlanWithOptions) -> p.bindingid), planBindings)
    //        "IsExpanded" |> Binding.twoWay ((fun (_, c) -> c.IsExpanded), (fun value (_, c:CourseWithOptions) -> CourseIsExpandedChanged (c.Id, value)))
    //    ]
    //let toggleBindings () : Binding<Model * ToggleType, Msg> list =
    //    [
    //        "Text" |> Binding.oneWay (fun (_, f) -> f.Text)
    //        "IsChecked" |> Binding.twoWay ((fun (_, f:ToggleType) -> f.IsChecked), (fun value (_, f:ToggleType) -> ToggleChanged (f, value)))
    //    ]
    //let bindings () : Binding<Model, Msg> list =
    //    [
    //        "Courses" |> Binding.subModelSeq((fun m -> m.Courses), (fun (c:CourseWithOptions) -> c.Id), courseBindings)
    //        "PatientSetupToggles" |> Binding.subModelSeq((fun m -> m.Toggles), (fun (t:ToggleType) -> t), toggleBindings)
    //        "PatientSetupCompleted" |> Binding.cmd NextButtonClicked
    //        "PatientSetupScreenVisibility" |> Binding.oneWay(fun m -> m.Visibility)
    //    ]