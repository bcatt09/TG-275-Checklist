namespace TG275Checklist

//open NLog

//open System
//open System.IO
//open System.Reflection

//module Log =

//    let LogName = "ESAPIScripts"
//    let DefaultLogFileName = LogName + ".log"
//    let layoutString = "${date:format=HH\\:mm\\:ss:padding=-10:fixedlength=true} ${gdc:item=Script:padding=-20:fixedlength=true} ${level:uppercase=true:padding=-10:fixedlength=true} ${gdc:item=User:padding=-35:fixedlength=true} ${gdc:item=Patient:padding=-35:fixedlength=true} ${gdc:item=Plan:padding=-35:fixedlength=true} ${message}${onexception:${newline}  ${exception:format=Message,StackTrace:separator=\r\n}}"

//    let GetAssemblyDirectory () =
//        Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
    
//    let GetDefaultLogPath () = 
//        Path.Combine(GetAssemblyDirectory(), DefaultLogFileName)
    
//    let GetOldLogPath () =
//        Path.Combine(GetAssemblyDirectory(), LogName + ".old.log")

//    type Log public () =
    
//        // Initialize log file and layout for use
//        static member public Initialize(user, patientName, plans) = 
//            let config = new Config.LoggingConfiguration()
//            let logFile = new Targets.FileTarget("logfile", 
//                            FileName = Layouts.Layout.FromString(GetDefaultLogPath()),
//                            Layout = Layouts.Layout.FromString layoutString)
     
//            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logFile)

//            LogManager.Configuration <- config
        
//            // Globals which will be added to each log entry
//            GlobalDiagnosticsContext.Set("Script", "Checklist")
//            GlobalDiagnosticsContext.Set("User", user)
//            GlobalDiagnosticsContext.Set("Patient", patientName)
//            GlobalDiagnosticsContext.Set("Plan", plans |> String.concat (", "))

//            let copyOldLogAndDelete () =         
//                if File.Exists(GetOldLogPath())
//                then File.Delete(GetOldLogPath())
                
//                if File.Exists(GetDefaultLogPath())
//                then
//                    File.Copy(GetDefaultLogPath(), GetOldLogPath())
//                    File.Delete(GetDefaultLogPath())

//            // Clear the log every day and save yesterday's log in case there were errors that need to be looked into
//            match File.GetLastWriteTime(GetDefaultLogPath()).Month = DateTime.Now.Month with
//            | true -> ()
//            | false -> copyOldLogAndDelete()