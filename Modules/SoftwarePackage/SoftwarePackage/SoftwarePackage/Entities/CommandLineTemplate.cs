namespace SoftwarePackage.Entities
{
    internal class CommandLineTemplate
    {
        public string FormatTag { get; set; } = string.Empty;
        public string FavoriteObservedDataList { get; set; } = string.Empty;
        public string InstallCommand { get; set; } = string.Empty;
        public string InstallArguments { get; set; } = string.Empty;
        public string UninstallCommand { get; set; } = string.Empty;
        public string UninstallArguments { get; set; } = string.Empty;
    }
}
