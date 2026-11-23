using System;
using System.IO;
using System.Windows;

namespace WizFileUltra
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                LogAndShowError("AppDomain Exception", args.ExceptionObject as Exception);
            };

            DispatcherUnhandledException += (s, args) =>
            {
                LogAndShowError("Dispatcher Exception", args.Exception);
                args.Handled = true;
            };

            base.OnStartup(e);
        }

        private void LogAndShowError(string type, Exception ex)
        {
            AppLogger.Error($"Unhandled {type}", ex);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{type}: {ex?.Message}");
            Exception inner = ex?.InnerException;
            while (inner != null)
            {
                sb.AppendLine($"--> InnerException: {inner.Message}");
                sb.AppendLine(inner.StackTrace);
                inner = inner.InnerException;
            }
            sb.AppendLine("\nStack:\n" + ex?.StackTrace);
            string msg = sb.ToString();
            try
            {
                File.WriteAllText("error_log.txt", msg);
            }
            catch { }
            MessageBox.Show(msg, "Uygulama Hatası (Debug Kaydedildi)", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
