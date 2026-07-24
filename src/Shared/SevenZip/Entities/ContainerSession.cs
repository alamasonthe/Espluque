using SevenZipExtractor;

namespace SevenZip.Entities
{
    public sealed class ContainerSession : IDisposable, IContainerSession
    {
        internal ArchiveFile ArchiveFile { get; }
        internal FileStream? ForcedFormatStream { get; }

        public string FilePath { get; }

        internal ContainerSession(string filePath, ArchiveFile archiveFile, FileStream? forcedFormatStream)
        {
            FilePath = filePath;
            ArchiveFile = archiveFile;
            ForcedFormatStream = forcedFormatStream;
        }

        public void Dispose()
        {
            ArchiveFile.Dispose();
            ForcedFormatStream?.Dispose();
        }
    }
}
