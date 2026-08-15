using Espluque.Contracts.CrossCutting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

namespace MiniFileLogger
{
    public class Logger : Espluque.Contracts.CrossCutting.ILogger
    {
        public event Action<string>? LineLogged;

        private string _logFilePath;

        public Logger(ISettingsService settingsService)
        {
            GetLogFilePath(settingsService);
        }

        private void GetLogFilePath(ISettingsService settingsService)
        {
            var settingsLogFilePath = settingsService.GetSetting("LogFilePath").GetAwaiter().GetResult();

            if (!string.IsNullOrWhiteSpace(settingsLogFilePath))
            {
                _logFilePath = settingsLogFilePath;
            }
            else
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? Process.GetCurrentProcess().ProcessName;

                _logFilePath = Path.Combine(appDataPath, appName, $"{appName}.log");
            }

            string? directoryPath = Path.GetDirectoryName(_logFilePath);

            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public void Log(LogLevel logLevel, string message)
        {
            string line = BuildLine(logLevel, message);

            TryWriteLine(line);

            LineLogged?.Invoke(line);
        }

        private static string BuildLine(LogLevel logLevel, string message)
        {
            return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\t{logLevel,-12}\t{message}";
        }

        private void TryWriteLine(string line)
        {
            try
            {
                File.AppendAllText(
                    _logFilePath,
                    line + Environment.NewLine);
            }
            catch
            {
                // no exception to prevent app crash
            }
        }

    }
}
