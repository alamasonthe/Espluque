namespace RegViewer
{
    internal class RegistryValue
    {
        internal string KeyPath { get; set; } = string.Empty;
        internal string Name { get; set; } = string.Empty;
        internal Microsoft.Win32.RegistryValueKind Type { get; set; }
        internal string RawData { get; set; } = string.Empty;

        internal string DisplayType => Type switch
        {
            Microsoft.Win32.RegistryValueKind.String => "REG_SZ",
            Microsoft.Win32.RegistryValueKind.ExpandString => "REG_EXPAND_SZ",
            Microsoft.Win32.RegistryValueKind.Binary => "REG_BINARY",
            Microsoft.Win32.RegistryValueKind.DWord => "REG_DWORD",
            Microsoft.Win32.RegistryValueKind.MultiString => "REG_MULTI_SZ",
            Microsoft.Win32.RegistryValueKind.QWord => "REG_QWORD",
            Microsoft.Win32.RegistryValueKind.None => "REG_NONE",
            _ => "Unknown"
        };
    }
}
