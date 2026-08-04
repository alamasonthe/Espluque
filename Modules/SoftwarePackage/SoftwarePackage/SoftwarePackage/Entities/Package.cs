namespace SoftwarePackage.Entities
{
    internal class Package
    {
        public string? Manufacturer { get; set; } = string.Empty;
        public string? ProductName { get; set; } = string.Empty;
        public string? ProductVersion { get; set; } = string.Empty;
        public string? InstallerType { get; set; } = string.Empty;
        public string? InstallCommand { get; set; } = string.Empty;
        public string? InstallArguments { get; set; } = string.Empty;
        public string? UninstallCommand { get; set; } = string.Empty;
        public string? UninstallArguments { get; set; } = string.Empty;
    }
}
