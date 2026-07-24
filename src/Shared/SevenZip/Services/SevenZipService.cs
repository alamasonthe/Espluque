using SevenZip.Entities;
using System.Text;
using Util;

namespace SevenZip.Services
{
    public static class SevenZipService
    {
        public static Result CanOpenContainer(string filePath)
        {
            Result<IContainerSession> sessionResult = ContainerReader.OpenSession(filePath);

            if (!sessionResult.IsSuccess)
            {
                return Result.Failure(sessionResult.Error?.Code ?? "ARCHIVE_OPEN_SESSION_FAILED", sessionResult.Error?.Message ?? "Failed to open container session.");
            }

            if (sessionResult.Value is IDisposable disposableSession)
            {
                disposableSession.Dispose();
            }

            return Result.Success();
        }

        public static Result<List<List<KeyValuePair<string, string>>>> ListEntries(string filePath)
        {
            Result<IContainerSession> sessionResult = ContainerReader.OpenSession(filePath);

            if (!sessionResult.IsSuccess)
            {
                return Result<List<List<KeyValuePair<string, string>>>>.Failure(sessionResult.Error?.Code ?? "ARCHIVE_OPEN_SESSION_FAILED", sessionResult.Error?.Message ?? "Failed to open container session.");
            }

            if (sessionResult.Value is not ContainerSession containerSession)
            {
                if (sessionResult.Value is IDisposable disposableSession)
                {
                    disposableSession.Dispose();
                }

                return Result<List<List<KeyValuePair<string, string>>>>.Failure("INVALID_CONTAINER_SESSION", "Container session was not created by this module.");
            }

            try
            {
                using (containerSession)
                {
                    IReadOnlyList<IArchiveEntryInfo> entries = ContainerReader.ReadArchiveEntries(containerSession);

                    List<List<KeyValuePair<string, string>>> entryPropertiesList = [];

                    foreach (IArchiveEntryInfo entry in entries)
                    {
                        Result<List<KeyValuePair<string, string>>> entryPropertiesResult = ContainerReader.CreatePropertiesFromArchiveEntry(entry);

                        if (!entryPropertiesResult.IsSuccess)
                        {
                            continue;
                        }

                        entryPropertiesList.Add(entryPropertiesResult.Value ?? []);
                    }

                    return Result<List<List<KeyValuePair<string, string>>>>.Success(entryPropertiesList);
                }
            }
            catch (Exception ex)
            {
                return ContainerReader.BuildFailureFromException<List<List<KeyValuePair<string, string>>>>(ex, "ARCHIVE_LIST_ENTRIES_FAILED", "Failed to read container entries");
            }
        }

        public static Result<bool> EntryExists(string filePath, string entryPath)
        {
            if (string.IsNullOrWhiteSpace(entryPath))
            {
                return Result<bool>.Failure("ENTRY_PATH_EMPTY", "Entry path is empty.");
            }

            Result<IContainerSession> sessionResult = ContainerReader.OpenSession(filePath);

            if (!sessionResult.IsSuccess)
            {
                return Result<bool>.Failure(sessionResult.Error?.Code ?? "ARCHIVE_OPEN_SESSION_FAILED", sessionResult.Error?.Message ?? "Failed to open container session.");
            }

            if (sessionResult.Value is not ContainerSession containerSession)
            {
                if (sessionResult.Value is IDisposable disposableSession)
                {
                    disposableSession.Dispose();
                }

                return Result<bool>.Failure("INVALID_CONTAINER_SESSION", "Container session was not created by this module.");
            }

            try
            {
                using (containerSession)
                {
                    bool entryExists = ContainerReader.EntryExists(containerSession, entryPath);

                    return Result<bool>.Success(entryExists);
                }
            }
            catch (Exception ex)
            {
                return ContainerReader.BuildFailureFromException<bool>(ex, "ARCHIVE_ENTRY_EXISTS_FAILED", "Failed to check container entry");
            }
        }

        public static Result<TreeNode<List<KeyValuePair<string, string>>>> GetTree(string filePath)
        {
            Result<List<List<KeyValuePair<string, string>>>> entriesResult = ListEntries(filePath);
            if (!entriesResult.IsSuccess)
            {
                return Result<TreeNode<List<KeyValuePair<string, string>>>>.Failure(entriesResult.Error!.Code, entriesResult.Error.Message);
            }

            try
            {
                var treeItems = entriesResult.Value!
                    .Select(entryProperties =>
                    {
                        string? entryPath = entryProperties.FirstOrDefault(property => property.Key == "Path").Value;
                        string? isFolderValue = entryProperties.FirstOrDefault(property => property.Key == "IsFolder").Value;

                        bool isFolder = bool.TryParse(isFolderValue, out bool parsedIsFolder) && parsedIsFolder;

                        return (
                            Path: entryPath,
                            IsLeaf: !isFolder,
                            Data: entryProperties);
                    })
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.Path))
                    .Select(entry => (
                        Path: entry.Path!,
                        entry.IsLeaf,
                        entry.Data));

                TreeNode<List<KeyValuePair<string, string>>> tree = TreeBuilder.Build(treeItems, ["/", "\\"], Path.GetFileName(filePath));

                return Result<TreeNode<List<KeyValuePair<string, string>>>>.Success(tree);
            }
            catch (Exception ex)
            {
                return ContainerReader.BuildFailureFromException<TreeNode<List<KeyValuePair<string, string>>>>(ex, "ARCHIVE_TREE_BUILD_FAILED", "Failed to build container tree");
            }
        }

        public static Result<byte[]> ExtractEntryToMemory(string filePath, string entryPath)
        {
            if (string.IsNullOrWhiteSpace(entryPath))
            {
                return Result<byte[]>.Failure("ENTRY_PATH_EMPTY", "Entry path is empty.");
            }

            Result<IContainerSession> sessionResult = ContainerReader.OpenSession(filePath);

            if (!sessionResult.IsSuccess)
            {
                return Result<byte[]>.Failure(sessionResult.Error?.Code ?? "ARCHIVE_OPEN_SESSION_FAILED", sessionResult.Error?.Message ?? "Failed to open container session.");
            }

            if (sessionResult.Value is not ContainerSession containerSession)
            {
                if (sessionResult.Value is IDisposable disposableSession)
                {
                    disposableSession.Dispose();
                }

                return Result<byte[]>.Failure("INVALID_CONTAINER_SESSION", "Container session was not created by this module.");
            }

            try
            {
                using (containerSession)
                {
                    if (!ContainerReader.EntryExists(containerSession, entryPath))
                    {
                        return Result<byte[]>.Failure("ENTRY_NOT_FOUND", $"Entry was not found in container: {entryPath}");
                    }

                    using MemoryStream memoryStream = new();

                    ContainerReader.ExtractEntryToStream(containerSession, entryPath, memoryStream);

                    return Result<byte[]>.Success(memoryStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                return ContainerReader.BuildFailureFromException<byte[]>(ex, "ARCHIVE_EXTRACT_ENTRY_TO_MEMORY_FAILED", "Failed to extract container entry to memory");
            }
        }

        public static Result<string> ExtractEntryToString(string filePath, string entryPath)
        {
            if (string.IsNullOrWhiteSpace(entryPath))
            {
                return Result<string>.Failure("ENTRY_PATH_EMPTY", "Entry path is empty.");
            }

            Result<IContainerSession> sessionResult = ContainerReader.OpenSession(filePath);

            if (!sessionResult.IsSuccess)
            {
                return Result<string>.Failure(sessionResult.Error?.Code ?? "ARCHIVE_OPEN_SESSION_FAILED", sessionResult.Error?.Message ?? "Failed to open container session.");
            }

            if (sessionResult.Value is not ContainerSession containerSession)
            {
                if (sessionResult.Value is IDisposable disposableSession)
                {
                    disposableSession.Dispose();
                }

                return Result<string>.Failure("INVALID_CONTAINER_SESSION", "Container session was not created by this module.");
            }

            try
            {
                using (containerSession)
                {
                    if (!ContainerReader.EntryExists(containerSession, entryPath))
                    {
                        return Result<string>.Failure("ENTRY_NOT_FOUND", $"Entry was not found in container: {entryPath}");
                    }

                    using MemoryStream memoryStream = new();

                    ContainerReader.ExtractEntryToStream(containerSession, entryPath, memoryStream);

                    memoryStream.Position = 0;

                    using StreamReader reader = new(
                        memoryStream,
                        Encoding.UTF8,
                        detectEncodingFromByteOrderMarks: true);

                    return Result<string>.Success(reader.ReadToEnd());
                }
            }
            catch (Exception ex)
            {
                return ContainerReader.BuildFailureFromException<string>(
                    ex,
                    "ARCHIVE_EXTRACT_ENTRY_TO_STRING_FAILED",
                    "Failed to extract container entry as string");
            }
        }

        public static Result<bool> ExtractEntryToFile(string filePath, string entryPath, string outputFilePath)
        {
            if (string.IsNullOrWhiteSpace(entryPath))
            {
                return Result<bool>.Failure("ENTRY_PATH_EMPTY", "Entry path is empty.");
            }

            if (string.IsNullOrWhiteSpace(outputFilePath))
            {
                return Result<bool>.Failure("OUTPUT_FILE_PATH_EMPTY", "Output file path is empty.");
            }

            Result<IContainerSession> sessionResult = ContainerReader.OpenSession(filePath);

            if (!sessionResult.IsSuccess)
            {
                return Result<bool>.Failure(sessionResult.Error?.Code ?? "ARCHIVE_OPEN_SESSION_FAILED", sessionResult.Error?.Message ?? "Failed to open container session.");
            }

            if (sessionResult.Value is not ContainerSession containerSession)
            {
                if (sessionResult.Value is IDisposable disposableSession)
                {
                    disposableSession.Dispose();
                }

                return Result<bool>.Failure("INVALID_CONTAINER_SESSION", "Container session was not created by this module.");
            }

            try
            {
                using (containerSession)
                {
                    if (!ContainerReader.EntryExists(containerSession, entryPath))
                    {
                        return Result<bool>.Failure("ENTRY_NOT_FOUND", $"Entry was not found in container: {entryPath}");
                    }

                    using FileStream fileStream = new FileStream(
                        outputFilePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None);

                    ContainerReader.ExtractEntryToStream(containerSession, entryPath, fileStream);

                    return Result<bool>.Success(true);
                }
            }
            catch (Exception ex)
            {
                return ContainerReader.BuildFailureFromException<bool>(ex, "ARCHIVE_EXTRACT_ENTRY_TO_FILE_FAILED", $"Failed to extract container entry to file: {entryPath}");
            }
        }

        public static Result<string> ExtractAllEntriesToDirectory(string filePath, string outputDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(outputDirectoryPath))
            {
                return Result<string>.Failure("OUTPUT_DIRECTORY_PATH_EMPTY", "Output directory path is empty.");
            }

            Result<IContainerSession> sessionResult = ContainerReader.OpenSession(filePath);

            if (!sessionResult.IsSuccess)
            {
                return Result<string>.Failure(sessionResult.Error?.Code ?? "ARCHIVE_OPEN_SESSION_FAILED", sessionResult.Error?.Message ?? "Failed to open container session.");
            }

            if (sessionResult.Value is not ContainerSession containerSession)
            {
                if (sessionResult.Value is IDisposable disposableSession)
                {
                    disposableSession.Dispose();
                }

                return Result<string>.Failure("INVALID_CONTAINER_SESSION", "Container session was not created by this module.");
            }

            try
            {
                using (containerSession)
                {
                    string targetDirectoryPath = ContainerReader.ExtractAllEntriesToDirectory(containerSession, outputDirectoryPath);

                    return Result<string>.Success(targetDirectoryPath);
                }
            }
            catch (Exception ex)
            {
                return ContainerReader.BuildFailureFromException<string>(ex, "ARCHIVE_EXTRACT_ALL_ENTRIES_FAILED", "Failed to extract all container entries");
            }
        }
    }
}
