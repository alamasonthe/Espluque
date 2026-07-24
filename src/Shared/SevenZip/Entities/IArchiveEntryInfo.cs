namespace SevenZip.Entities
{
    public interface IArchiveEntryInfo
    {
        string Path { get; init; }

        bool IsFolder { get; init; }
        bool IsEncrypted { get; init; }

        ulong Size { get; init; }
        ulong PackedSize { get; init; }

        DateTime? CreationTime { get; init; }
        DateTime? LastAccessTime { get; init; }
        DateTime? LastWriteTime { get; init; }

        uint CRC { get; init; }
        uint Attributes { get; init; }

        string? Method { get; init; }
        string? Comment { get; init; }
        string? HostOS { get; init; }
    }
}