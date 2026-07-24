using SevenZip.Entities;
using SevenZipExtractor;
using Util;

namespace SevenZip.Services
{
    public class ContainerReader
    {
        internal static Result<IContainerSession> OpenSession(string filePath)
        {
            Result<bool> canOpenReadResult = Util.File.CanOpenRead(filePath);
            if (!canOpenReadResult.IsSuccess)
            {
                return Result<IContainerSession>.Failure(canOpenReadResult.Error!.Code, canOpenReadResult.Error.Message);
            }

            try
            {
                var (archiveFile, forcedFormatStream) = ContainerReader.OpenArchiveFile(filePath);
                IContainerSession session = new ContainerSession(filePath, archiveFile, forcedFormatStream);

                return Result<IContainerSession>.Success(session);
            }
            catch (Exception ex)
            {
                return BuildFailureFromException<IContainerSession>(ex, "ARCHIVE_OPEN_SESSION_FAILED", "Failed to open container session");
            }
        }

        internal static (ArchiveFile ArchiveFile, FileStream? ForcedFormatStream) OpenArchiveFile(string filePath)
        {
            string libraryFilePath = Path.Combine( AppContext.BaseDirectory, "native", "sevenzip", "7z.dll");

            try
            {
                return (new ArchiveFile(filePath, libraryFilePath), null);
            }
            catch (SevenZipException) when (IsOle2CompoundFile(filePath))
            {
                FileStream forcedFormatStream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);

                return (new ArchiveFile(forcedFormatStream, SevenZipFormat.Compound), forcedFormatStream);
            }
        }

        private static bool IsOle2CompoundFile(string filePath)
        {
            byte[] expected =
            [
                0xD0, 0xCF, 0x11, 0xE0,
                0xA1, 0xB1, 0x1A, 0xE1
            ];

            byte[] actual = new byte[8];

            using FileStream stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            int read = stream.Read(actual, 0, actual.Length);

            return read == actual.Length && actual.SequenceEqual(expected);
        }

        internal static IReadOnlyList<IArchiveEntryInfo> ReadArchiveEntries(ContainerSession containerSession)
        {
            return containerSession.ArchiveFile.Entries
                .Select(entry => new ArchiveEntryInfo
                {
                    Path = entry.FileName,
                    IsFolder = entry.IsFolder,
                    IsEncrypted = entry.IsEncrypted,
                    Size = entry.Size,
                    PackedSize = entry.PackedSize,
                    CreationTime = entry.CreationTime == DateTime.MinValue ? null : entry.CreationTime,
                    LastAccessTime = entry.LastAccessTime == DateTime.MinValue ? null : entry.LastAccessTime,
                    LastWriteTime = entry.LastWriteTime == DateTime.MinValue ? null : entry.LastWriteTime == new DateTime(1979, 12, 31, 23, 0, 0) ? new DateTime(1980, 1, 1, 0, 0, 0) : entry.LastWriteTime,
                    CRC = entry.CRC,
                    Attributes = entry.Attributes,
                    Method = entry.Method,
                    Comment = entry.Comment,
                    HostOS = entry.HostOS
                })
                .ToList();
        }

        internal static bool EntryExists(ContainerSession containerSession, string entryPath)
        {
            return containerSession.ArchiveFile.Entries.Any(entry =>
                string.Equals(entry.FileName, entryPath, StringComparison.OrdinalIgnoreCase));
        }

        internal static void ExtractEntryToStream(ContainerSession containerSession, string entryPath, Stream outputStream)
        {
            Entry entry = containerSession.ArchiveFile.Entries.First(entry =>
                string.Equals(entry.FileName, entryPath, StringComparison.OrdinalIgnoreCase));

            entry.Extract(outputStream);
        }

        internal static string ExtractAllEntriesToDirectory(ContainerSession containerSession, string outputDirectoryPath)
        {
            string containerName = Path.GetFileNameWithoutExtension(containerSession.FilePath);
            string targetDirectoryPath = Path.Combine(outputDirectoryPath, containerName);

            Directory.CreateDirectory(targetDirectoryPath);

            containerSession.ArchiveFile.Extract(targetDirectoryPath);

            return targetDirectoryPath;
        }

        internal static Result<List<KeyValuePair<string, string>>> CreatePropertiesFromArchiveEntry(IArchiveEntryInfo archiveEntryInfo)
        {
            if (archiveEntryInfo is null)
            {
                return Result<List<KeyValuePair<string, string>>>.Failure("ARCHIVE_ENTRY_INFO_MISSING", "Archive entry info is missing.");
            }

            List<KeyValuePair<string, string>> properties =
            [
                new("Path", archiveEntryInfo.Path),
                new("IsFolder", archiveEntryInfo.IsFolder.ToString()),
                new("IsEncrypted", archiveEntryInfo.IsEncrypted.ToString()),
                new("Size", archiveEntryInfo.Size.ToString()),
                new("PackedSize", archiveEntryInfo.PackedSize.ToString())
            ];

            if (archiveEntryInfo.CreationTime.HasValue)
            {
                properties.Add(new("CreationTime", archiveEntryInfo.CreationTime.Value.ToString("yyyy-MM-dd HH:mm:ss")));
            }

            if (archiveEntryInfo.LastAccessTime.HasValue)
            {
                properties.Add(new("LastAccessTime", archiveEntryInfo.LastAccessTime.Value.ToString("yyyy-MM-dd HH:mm:ss")));
            }

            if (archiveEntryInfo.LastWriteTime.HasValue)
            {
                properties.Add(new("LastWriteTime", archiveEntryInfo.LastWriteTime.Value.ToString("yyyy-MM-dd HH:mm:ss")));
            }

            properties.Add(new("CRC", archiveEntryInfo.CRC.ToString("X8")));
            properties.Add(new("Attributes", archiveEntryInfo.Attributes.ToString()));

            if (!string.IsNullOrWhiteSpace(archiveEntryInfo.Method))
            {
                properties.Add(new("Method", archiveEntryInfo.Method));
            }

            if (!string.IsNullOrWhiteSpace(archiveEntryInfo.Comment))
            {
                properties.Add(new("Comment", archiveEntryInfo.Comment));
            }

            if (!string.IsNullOrWhiteSpace(archiveEntryInfo.HostOS))
            {
                properties.Add(new("HostOS", archiveEntryInfo.HostOS));
            }

            return Result<List<KeyValuePair<string, string>>>.Success(properties);
        }

        internal static Result<T> BuildFailureFromException<T>(Exception exception, string defaultCode, string defaultMessage)
        {
            if (exception is SevenZipException)
            {
                return Result<T>.Failure("ARCHIVE_SEVENZIP_ERROR", $"SevenZipExtractor could not read the container: {exception.Message}");
            }

            return Result<T>.Failure(defaultCode, $"{defaultMessage}: {exception.Message}");
        }

    }
}
