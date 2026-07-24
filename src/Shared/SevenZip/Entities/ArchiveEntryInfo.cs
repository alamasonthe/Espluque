using SevenZip.Entities;

namespace SevenZip.Entities
{
    public sealed class ArchiveEntryInfo : IArchiveEntryInfo
    {
        public string Path { get; init; } = string.Empty;

        public bool IsFolder { get; init; }
        public bool IsEncrypted { get; init; }

        public ulong Size { get; init; }
        public ulong PackedSize { get; init; }

        public DateTime? CreationTime { get; init; }
        public DateTime? LastAccessTime { get; init; }
        public DateTime? LastWriteTime { get; init; }

        public uint CRC { get; init; }
        public uint Attributes { get; init; }

        public string? Method { get; init; }
        public string? Comment { get; init; }
        public string? HostOS { get; init; }
    }
}