using Espluque.Application.Entities;
using System.IO.Compression;
using Util;
using Espluque.Contracts.Interfaces;

namespace Espluque.Application.Services
{
    public class NodeBinaryReader
    {
        public Result<byte[]> ReadBytes(
            IAnalysisNode node,
            long offset,
            int size)
        {
            if (node is null)
            {
                return Result<byte[]>.Failure(
                    "NODE_BINARY_READER_NODE_MISSING",
                    "NodeBinaryReader.ReadBytes: analysis node is missing.");
            }

            if (node.TargetInternalPath is null || node.TargetInternalPath.Count == 0)
            {
                return Bin.ReadBytesFromFile(
                    node.TargetRootFilePath,
                    offset,
                    size);
            }

            return ReadBytesFromInternalPath(
                node.TargetRootFilePath,
                node.TargetInternalPath,
                offset,
                size);
        }

        private Result<byte[]> ReadBytesFromInternalPath(
            string rootFilepath,
            List<(string Handler, string Value)> internalPath,
            long offset,
            int size)
        {
            if (string.IsNullOrWhiteSpace(rootFilepath))
            {
                return Result<byte[]>.Failure(
                    "NODE_BINARY_READER_ROOT_FILEPATH_EMPTY",
                    "NodeBinaryReader.ReadBytesFromInternalPath: root filepath is empty.");
            }

            if (offset < 0)
            {
                return Result<byte[]>.Failure(
                    "OFFSET_INVALID",
                    "Offset cannot be negative.");
            }

            if (size < 0)
            {
                return Result<byte[]>.Failure(
                    "SIZE_INVALID",
                    "Size cannot be negative.");
            }

            try
            {
                byte[] currentContainerBytes = System.IO.File.ReadAllBytes(rootFilepath);

                for (int i = 0; i < internalPath.Count; i++)
                {
                    (string Handler, string Value) currentTarget = internalPath[i];

                    if (string.IsNullOrWhiteSpace(currentTarget.Handler))
                    {
                        return Result<byte[]>.Failure(
                            "NODE_BINARY_READER_INTERNAL_HANDLER_EMPTY",
                            "NodeBinaryReader.ReadBytesFromInternalPath: internal target handler is empty.");
                    }

                    if (string.IsNullOrWhiteSpace(currentTarget.Value))
                    {
                        return Result<byte[]>.Failure(
                            "NODE_BINARY_READER_INTERNAL_VALUE_EMPTY",
                            "NodeBinaryReader.ReadBytesFromInternalPath: internal target value is empty.");
                    }

                    if (!string.Equals(currentTarget.Handler, "zip", StringComparison.OrdinalIgnoreCase))
                    {
                        return Result<byte[]>.Failure(
                            "NODE_BINARY_READER_INTERNAL_HANDLER_NOT_IMPLEMENTED",
                            $"NodeBinaryReader.ReadBytesFromInternalPath: internal target handler is not implemented: {currentTarget.Handler}.");
                    }

                    bool isLastTarget = i == internalPath.Count - 1;

                    Result<byte[]> entryBytesResult = isLastTarget
                        ? ReadZipEntryBytes(currentContainerBytes, currentTarget.Value, offset, size)
                        : ReadFullZipEntryBytes(currentContainerBytes, currentTarget.Value);

                    if (!entryBytesResult.IsSuccess)
                    {
                        return entryBytesResult;
                    }

                    if (entryBytesResult.Value is null)
                    {
                        return Result<byte[]>.Failure(
                            "NODE_BINARY_READER_ENTRY_BYTES_MISSING",
                            "NodeBinaryReader.ReadBytesFromInternalPath: entry bytes are missing.");
                    }

                    if (isLastTarget)
                    {
                        return entryBytesResult;
                    }

                    currentContainerBytes = entryBytesResult.Value;
                }

                return Result<byte[]>.Failure(
                    "NODE_BINARY_READER_INTERNAL_PATH_EMPTY",
                    "NodeBinaryReader.ReadBytesFromInternalPath: internal path is empty.");
            }
            catch (Exception exception)
            {
                return Result<byte[]>.Failure(
                    "NODE_BINARY_READER_INTERNAL_READ_FAILED",
                    $"NodeBinaryReader.ReadBytesFromInternalPath: failed to read internal path. {exception.Message}");
            }
        }

        private Result<byte[]> ReadFullZipEntryBytes(
            byte[] zipBytes,
            string entryPath)
        {
            using MemoryStream zipStream = new(zipBytes);
            using ZipArchive zipArchive = new(zipStream, ZipArchiveMode.Read);

            ZipArchiveEntry? entry = zipArchive.GetEntry(entryPath);

            if (entry is null)
            {
                return Result<byte[]>.Failure(
                    "NODE_BINARY_READER_ZIP_ENTRY_NOT_FOUND",
                    $"NodeBinaryReader.ReadFullZipEntryBytes: zip entry was not found: {entryPath}.");
            }

            using Stream entryStream = entry.Open();
            using MemoryStream entryMemoryStream = new();

            entryStream.CopyTo(entryMemoryStream);

            return Result<byte[]>.Success(entryMemoryStream.ToArray());
        }

        private Result<byte[]> ReadZipEntryBytes(
            byte[] zipBytes,
            string entryPath,
            long offset,
            int size)
        {
            using MemoryStream zipStream = new(zipBytes);
            using ZipArchive zipArchive = new(zipStream, ZipArchiveMode.Read);

            ZipArchiveEntry? entry = zipArchive.GetEntry(entryPath);

            if (entry is null)
            {
                return Result<byte[]>.Failure(
                    "NODE_BINARY_READER_ZIP_ENTRY_NOT_FOUND",
                    $"NodeBinaryReader.ReadZipEntryBytes: zip entry was not found: {entryPath}.");
            }

            if (offset > entry.Length)
            {
                return Result<byte[]>.Failure(
                    "OFFSET_OUT_OF_RANGE",
                    "Offset is beyond the end of the internal file.");
            }

            int readableSize = (int)Math.Min(size, entry.Length - offset);
            byte[] bytes = new byte[readableSize];

            using Stream entryStream = entry.Open();

            if (offset > 0)
            {
                entryStream.Seek(offset, SeekOrigin.Begin);
            }

            int bytesRead = entryStream.Read(bytes, 0, readableSize);

            if (bytesRead != readableSize)
            {
                return Result<byte[]>.Failure(
                    "FILE_BLOCK_READ_INCOMPLETE",
                    "The requested internal block was not fully read.");
            }

            return Result<byte[]>.Success(bytes);
        }

        public Result<long> GetLength(IAnalysisNode node)
        {
            if (node is null)
            {
                return Result<long>.Failure(
                    "NODE_BINARY_READER_NODE_MISSING",
                    "NodeBinaryReader.GetLength: analysis node is missing.");
            }

            if (string.IsNullOrWhiteSpace(node.TargetRootFilePath))
            {
                return Result<long>.Failure(
                    "NODE_BINARY_READER_ROOT_FILEPATH_EMPTY",
                    "NodeBinaryReader.GetLength: root filepath is empty.");
            }

            try
            {
                if (node.TargetInternalPath is null || node.TargetInternalPath.Count == 0)
                {
                    FileInfo fileInfo = new(node.TargetRootFilePath);

                    if (!fileInfo.Exists)
                    {
                        return Result<long>.Failure(
                            "FILE_NOT_FOUND",
                            "File was not found.");
                    }

                    return Result<long>.Success(fileInfo.Length);
                }

                byte[] currentContainerBytes = System.IO.File.ReadAllBytes(node.TargetRootFilePath);

                for (int i = 0; i < node.TargetInternalPath.Count; i++)
                {
                    (string Handler, string Value) currentTarget = node.TargetInternalPath[i];

                    if (string.IsNullOrWhiteSpace(currentTarget.Handler))
                    {
                        return Result<long>.Failure(
                            "NODE_BINARY_READER_INTERNAL_HANDLER_EMPTY",
                            "NodeBinaryReader.GetLength: internal target handler is empty.");
                    }

                    if (string.IsNullOrWhiteSpace(currentTarget.Value))
                    {
                        return Result<long>.Failure(
                            "NODE_BINARY_READER_INTERNAL_VALUE_EMPTY",
                            "NodeBinaryReader.GetLength: internal target value is empty.");
                    }

                    if (!string.Equals(currentTarget.Handler, "zip", StringComparison.OrdinalIgnoreCase))
                    {
                        return Result<long>.Failure(
                            "NODE_BINARY_READER_INTERNAL_HANDLER_NOT_IMPLEMENTED",
                            $"NodeBinaryReader.GetLength: internal target handler is not implemented: {currentTarget.Handler}.");
                    }

                    using MemoryStream zipStream = new(currentContainerBytes);
                    using ZipArchive zipArchive = new(zipStream, ZipArchiveMode.Read);

                    ZipArchiveEntry? entry = zipArchive.GetEntry(currentTarget.Value);

                    if (entry is null)
                    {
                        return Result<long>.Failure(
                            "NODE_BINARY_READER_ZIP_ENTRY_NOT_FOUND",
                            $"NodeBinaryReader.GetLength: zip entry was not found: {currentTarget.Value}.");
                    }

                    bool isLastTarget = i == node.TargetInternalPath.Count - 1;

                    if (isLastTarget)
                    {
                        return Result<long>.Success(entry.Length);
                    }

                    using Stream entryStream = entry.Open();
                    using MemoryStream entryMemoryStream = new();

                    entryStream.CopyTo(entryMemoryStream);
                    currentContainerBytes = entryMemoryStream.ToArray();
                }

                return Result<long>.Failure(
                    "NODE_BINARY_READER_INTERNAL_PATH_EMPTY",
                    "NodeBinaryReader.GetLength: internal path is empty.");
            }
            catch (Exception exception)
            {
                return Result<long>.Failure(
                    "NODE_BINARY_READER_LENGTH_FAILED",
                    $"NodeBinaryReader.GetLength: failed to get node length. {exception.Message}");
            }
        }
    }
}