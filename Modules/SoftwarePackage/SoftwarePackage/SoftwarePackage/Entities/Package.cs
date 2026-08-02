namespace SoftwarePackage.Entities
{
    internal class Package
    {
        public string? Manufacturer { get; set; } = string.Empty;
        public string? ProductName { get; set; } = string.Empty;
        public string? ProductVersion { get; set; } = string.Empty;
        public string? InstallerType { get; set; } = string.Empty;
        public string? InstallerCommand { get; set; } = string.Empty;
        public string? InstallerParameters { get; set; } = string.Empty;
        public string? UninstallerCommand { get; set; } = string.Empty;
        public string? UninstallerParameters { get; set; } = string.Empty;
    }
}
