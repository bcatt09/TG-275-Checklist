using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;

namespace TG275Checklist.Log
{
    public class PVH_Logger
    {
        private static PVH_Logger instance;

        private static string logName = "PVH_Scripts";
        private static string logFilename = logName + ".log";
        private static string logRelativePath = "PVH\\" + logFilename;
        private string logFullPath;

        private static string oldLogFilename = logName + ".old.log";
        private static string oldLogRelativePath = "PVH\\" + oldLogFilename;
        private string oldLogFullPath;

        private string scriptName;
        private string userName;
        private string patientName;
        private string planOrImageName;

        public static PVH_Logger Logger
        {
            get
            {
                if (instance == null)
                {
                    instance = new PVH_Logger();
                }
                return instance;
            }
        }

        private PVH_Logger() { }

        public void Initialize(string scriptName, ScriptContext context, string exePath)
        {
            this.scriptName = scriptName;
            userName = context.CurrentUser.Name;
            patientName = context.Patient.LastName + ", " + context.Patient.FirstName + " (" + context.Patient.Id + ")";
            planOrImageName = (context.PlanSetup != null) ? $"{context.PlanSetup.Id} ({context.Course})" : context.Image.Id;

            logFullPath = Path.Combine(exePath, logRelativePath);
            oldLogFullPath = Path.Combine(GetAssemblyDirectory(), oldLogRelativePath);

            if (File.Exists(logFullPath) && DateTime.Now.Day != File.GetLastWriteTime(logFullPath).Day)
            {
                File.Copy(logFullPath, oldLogFullPath, overwrite: true);
                File.Delete(logFullPath);
            }
        }

        public void Initialize(string scriptName, string userName, string patientName, string planNames, string exePath)
        {
            this.scriptName = scriptName;
            this.userName = userName;
            this.patientName = patientName;
            planOrImageName = planNames;

            logFullPath = Path.Combine(exePath, logRelativePath);
            oldLogFullPath = Path.Combine(GetAssemblyDirectory(), oldLogRelativePath);

            if (File.Exists(logFullPath) && DateTime.Now.Day != File.GetLastWriteTime(logFullPath).Day)
            {
                File.Copy(logFullPath, oldLogFullPath, overwrite: true);
                File.Delete(logFullPath);
            }
        }

        private void WriteToLog(DateTime time, string scriptName, Severity severity, string userName, string patientName, string planOrImageName, string message)
        {
            string text = (string.IsNullOrEmpty(message) ? (message + "\n") : ("\n\t" + message.Replace("   ", "\t\t") + "\n"));
            string contents = $"{time:HH:mm:ss}\t{scriptName,-15}{severity,-10}{userName,-35}{patientName,-35}{planOrImageName,-40}{text}";
            File.AppendAllText(logFullPath, contents);
        }

        public void Log(string message = "", Severity severity = Severity.Info)
        {
            WriteToLog(DateTime.Now, GetAssemblyDirectory(), Severity.Info, logFullPath, File.Exists(logFullPath).ToString(), DateTime.Now.Day.ToString(), File.GetLastWriteTime(logFullPath).Day.ToString());
            WriteToLog(DateTime.Now, instance.scriptName, severity, instance.userName, instance.patientName, instance.planOrImageName, message);
        }

        public void LogWarning(string message)
        {
            WriteToLog(DateTime.Now, instance.scriptName, Severity.Warning, instance.userName, instance.patientName, instance.planOrImageName, message);
        }

        public void LogError(Exception ex)
        {
            string message = ex.ToString();
            WriteToLog(DateTime.Now, instance.scriptName, Severity.Error, instance.userName, instance.patientName, instance.planOrImageName, message);
        }

        public void LogError(Exception ex, string additionalInfo)
        {
            string message = ex.ToString() + $"\t{additionalInfo}";
            WriteToLog(DateTime.Now, instance.scriptName, Severity.Error, instance.userName, instance.patientName, instance.planOrImageName, message);
        }

        private static string GetAssemblyDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }
    }

    public enum Severity
    {
        Info,
        Warning,
        Error
    }
}
