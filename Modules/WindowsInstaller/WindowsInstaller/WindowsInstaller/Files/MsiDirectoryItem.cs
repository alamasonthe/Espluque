namespace WindowsInstaller.Files
{
    internal sealed class MsiDirectoryItem
    {
        public string DirectoryKey { get; set; } = string.Empty;
        public string ParentDirectoryKey { get; set; } = string.Empty;

        public string TargetName { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;

        public List<MsiFileItem> Files { get; } = [];
    }
}
