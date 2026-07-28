namespace Espluquer.Entities
{
    internal class SettingsDto
    {
        public string Db { get; set; } = string.Empty;

        public string LogFilePath { get; set; } = string.Empty;

        public string Theme { get; set; } = string.Empty;

        public string RecentFiles { get; set; } = string.Empty;
    }
}
