module MetroProgressDialog

open MahApps.Metro.Controls.Dialogs
open TG275Checklist.Views
open System.Threading

let showDialog (window:MainWindow) =

    // Register DataContext for displaying Metro Dialogs
    DialogParticipation.SetRegister(window, window.DataContext)

    async {
        // Dialog Settings
        let mySettings = 
            MetroDialogSettings(
                NegativeButtonText = "Cancel",
                AnimateShow = false,
                AnimateHide = false,
                ColorScheme = MetroDialogColorScheme.Accented)
            
        // Display ProgressDialog
        let! controller = DialogCoordinator.Instance.ShowProgressAsync (context = window.DataContext, title = "Logging in to Eclipse", message = "Please wait...", settings = mySettings) |> Async.AwaitTask
        controller.SetIndeterminate()

        // Do long calculation
        do Thread.Sleep(5000)

        // Close Dialog
        do! controller.CloseAsync() |> Async.AwaitTask
    }