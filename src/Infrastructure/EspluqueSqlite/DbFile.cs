using Espluque.Contracts.Ports;
using System.Diagnostics;
using System.Reflection;

namespace EspluqueSqlite
{
    internal static class DbFile
    {
        internal static string GetDbFilePath(ISettingsService settingsService)
        {
            string dbFilePath = string.Empty;

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? Process.GetCurrentProcess().ProcessName;
            string appDirectoryPath = Path.Combine(appDataPath, appName);

            string? settingsDbFileName = settingsService.GetSetting("Db").GetAwaiter().GetResult();

            if (string.IsNullOrWhiteSpace(settingsDbFileName))
            {
                settingsDbFileName = $"espluque.db";
            }

            string configuredDbFilePath = Path.IsPathRooted(settingsDbFileName)
                ? settingsDbFileName
                : Path.Combine(appDirectoryPath, settingsDbFileName);

            string applicationDbFilePath = Path.Combine(
                AppContext.BaseDirectory,
                Path.GetFileName(settingsDbFileName));

            if (System.IO.File.Exists(configuredDbFilePath))
            {
                dbFilePath = configuredDbFilePath;
            }
            else if (System.IO.File.Exists(applicationDbFilePath))
            {
                dbFilePath = applicationDbFilePath;
            }
            else
            {
                dbFilePath = configuredDbFilePath;
            }

            string? directoryPath = Path.GetDirectoryName(dbFilePath);

            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            return dbFilePath;
        }

    }
}
