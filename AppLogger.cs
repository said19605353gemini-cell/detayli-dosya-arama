using System;
using System.IO;

namespace WizFileUltra
{
    public static class AppLogger
    {
        private static readonly string LogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WizFileUltra");
        
        private static readonly string LogFile = Path.Combine(LogDir, "debug_log.txt");
        private static readonly object _lock = new object();

        public static void Log(string message, string level = "INFO")
        {
            try
            {
                lock (_lock)
                {
                    if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);
                    string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
                    File.AppendAllText(LogFile, line + Environment.NewLine);
                    // Ayrıca uygulama yanına da yaz
                    File.AppendAllText("debug_log.txt", line + Environment.NewLine);
                }
            }
            catch { }
        }

        public static void Error(string message, Exception ex)
        {
            Log($"{message} - Ex: {ex?.Message}\nStackTrace:\n{ex?.StackTrace}", "ERROR");
        }

        public static string GetLogPath() => LogFile;
    }
}
