using Microsoft.Extensions.Logging;
using System.IO;
using Espluque.Contracts.Ports;

namespace MiniFileLogger
{
    public class Logger : Espluque.Contracts.Ports.ILogger
    {
        private readonly string _logFilePath =
           Path.Combine(AppContext.BaseDirectory, "mapvoye.log");

        public event Action<string>? LineLogged;

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
                // Logger volontairement non bloquant.
                // L'affichage UI continue même si le fichier est inaccessible.
            }
        }
    }
}
