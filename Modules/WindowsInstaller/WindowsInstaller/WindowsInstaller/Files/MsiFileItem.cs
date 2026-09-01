namespace WindowsInstaller.Files
{
    public sealed class MsiFileItem
    {
        public string FileKey { get; set; } = string.Empty;
        public int Attributes { get; set; }

        public string TargetName { get; set; } = string.Empty;
        public string TargetPath { get; set; } = string.Empty;

        public string SourceName { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;

        public string ComponentKey { get; set; } = string.Empty;
        public string DirectoryKey { get; set; } = string.Empty;

        public string FileVersion { get; set; } = string.Empty;
        public int Sequence { get; set; }
        public long FileSize { get; set; }
    }
}
