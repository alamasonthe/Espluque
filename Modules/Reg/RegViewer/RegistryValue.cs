namespace RegViewer
{
    internal class RegistryValue
    {
        internal string KeyPath { get; set; } = string.Empty;
        internal string Name { get; set; } = string.Empty;
        internal Microsoft.Win32.RegistryValueKind Type { get; set; }
        internal string RawData { get; set; } = string.Empty;
    }
}
