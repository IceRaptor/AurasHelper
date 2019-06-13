using System;
using System.IO;

namespace AurasHelper {

    public class Logger {

        private static StreamWriter LogStream;
        private static string LogFile;

        public Logger(string modDir, string logName) {
            if (LogFile == null) {
                LogFile = Path.Combine(modDir, $"{logName}.log");
            }
            if (File.Exists(LogFile)) {
                File.Delete(LogFile);
            }

            LogStream = File.AppendText(LogFile);
        }

        public void Info(string message) { Log(message); }
        public void Debug(string message) { if (Mod.Config.Debug) { Log(message); } }
        public void Trace(string message) { if (Mod.Config.Trace) { Log(message); } }

        private void Log(string message) {
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
            LogStream.WriteLine($"{now} - {message}");
            LogStream.Flush();
        }

    }
}
