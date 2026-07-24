using Microsoft.Data.Sqlite;
using Pronom.Entities;
using Util;

namespace Pronom
{
    public static class PronomRepository
    {
        public static async Task<Result<List<PronomFileFormatInfo>>> GetInfosFromExtensionAsync(string extension, string dbFilePath)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return Result<List<PronomFileFormatInfo>>.Failure("PRONOM_EXTENSION_EMPTY_EXTENSION", "PronomRepository.GetInfosFromExtensionAsync: extension is empty.");
            }

            string searchedExtension = extension.Trim().TrimStart('.');

            try
            {
                using SqliteConnection connection = DbConnectionFactory.CreateConnection(dbFilePath);
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
SELECT
    FileFormat.MIMEType,
    FileFormat.Name,
    FileFormat.Version,
    FileFormat.Puid
FROM Extension
INNER JOIN FileFormat
    ON FileFormat.Id = Extension.FileFormatId
WHERE Extension.Extension = @Extension COLLATE NOCASE
ORDER BY FileFormat.Name, FileFormat.Version;
""";

                command.Parameters.AddWithValue("@Extension", searchedExtension);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                List<PronomFileFormatInfo> fileFormatInfos = [];

                while (await reader.ReadAsync())
                {
                    fileFormatInfos.Add(CreateFileFormatInfoFromReader(reader));
                }

                return Result<List<PronomFileFormatInfo>>.Success(fileFormatInfos);
            }
            catch (Exception ex)
            {
                return Result<List<PronomFileFormatInfo>>.Failure("PRONOM_EXTENSION_GET_INFOS_FAILED", $"PronomRepository.GetInfosFromExtensionAsync: failed to get infos for extension '{extension}'. {ex.Message}");
            }
        }

        public static async Task<Result<PronomFileFormatInfo?>> GetInfosFromPuidAsync(string puid, string dbFilePath)
        {
            if (string.IsNullOrWhiteSpace(puid))
            {
                return Result<PronomFileFormatInfo?>.Failure(
                    "PRONOM_FILE_FORMAT_PUID_EMPTY",
                    "PronomRepository.GetInfosFromPuidAsync: PUID is empty.");
            }

            try
            {
                using SqliteConnection connection = DbConnectionFactory.CreateConnection(dbFilePath);
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
SELECT
    FileFormat.MIMEType,
    FileFormat.Name,
    FileFormat.Version,
    FileFormat.Puid
FROM FileFormat
WHERE FileFormat.Puid = @Puid
LIMIT 1;
""";

                command.Parameters.AddWithValue("@Puid", puid);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return Result<PronomFileFormatInfo?>.Success(null);
                }

                PronomFileFormatInfo fileFormatInfo = CreateFileFormatInfoFromReader(reader);

                return Result<PronomFileFormatInfo?>.Success(fileFormatInfo);
            }
            catch (Exception ex)
            {
                return Result<PronomFileFormatInfo?>.Failure(
                    "PRONOM_FILE_FORMAT_INFOS_FROM_PUID_GET_FAILED",
                    $"PronomRepository.GetInfosFromPuidAsync: failed to get format infos for PUID '{puid}'. {ex.Message}");
            }
        }

        public static async Task<Result<List<int>>> ListInternalSignatureIdsAsync(string dbFilePath)
        {
            try
            {
                using SqliteConnection connection = DbConnectionFactory.CreateConnection(dbFilePath);
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
            SELECT
                InternalSignature.IdForFileFormat
            FROM InternalSignature
            LEFT JOIN FileFormat_InternalSignature
                ON FileFormat_InternalSignature.FileFormatSignatureId = InternalSignature.IdForFileFormat
            LEFT JOIN AnalyzePriority AS PrioritySource
                ON PrioritySource.FileFormatId = FileFormat_InternalSignature.FileFormatId
            LEFT JOIN AnalyzePriority AS PriorityTarget
                ON PriorityTarget.HasPriorityOverFileFormatID = FileFormat_InternalSignature.FileFormatId
            WHERE InternalSignature.IdForFileFormat IS NOT NULL
            GROUP BY
                InternalSignature.IdForFileFormat
            ORDER BY
                COUNT(DISTINCT PrioritySource.HasPriorityOverFileFormatID) DESC,
                COUNT(DISTINCT PriorityTarget.FileFormatId) ASC,
                InternalSignature.IdForFileFormat;
            """;

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                List<int> internalSignatureIds = [];

                while (await reader.ReadAsync())
                {
                    internalSignatureIds.Add(reader.GetInt32(0));
                }

                return Result<List<int>>.Success(internalSignatureIds);
            }
            catch (Exception ex)
            {
                return Result<List<int>>.Failure(
                    "PRONOM_INTERNAL_SIGNATURE_IDS_LIST_FAILED",
                    $"PronomRepository.ListInternalSignatureIdsAsync: failed to list internal signature IDs. {ex.Message}");
            }
        }

        public static async Task<Result<PronomInternalSignature?>> GetInternalSignatureAsync(int internalSignatureId, string dbFilePath)
        {
            if (internalSignatureId <= 0)
            {
                return Result<PronomInternalSignature?>.Failure(
                    "PRONOM_INTERNAL_SIGNATURE_ID_INVALID",
                    $"PronomRepository.GetInternalSignatureAsync: internal signature ID is invalid: {internalSignatureId}.");
            }

            try
            {
                using SqliteConnection connection = DbConnectionFactory.CreateConnection(dbFilePath);
                await connection.OpenAsync();

                List<PronomByteSequence> byteSequences = await ReadByteSequencesAsync(
                    connection,
                    internalSignatureId);

                if (byteSequences.Count == 0)
                {
                    return Result<PronomInternalSignature?>.Success(null);
                }

                PronomInternalSignature internalSignature = new()
                {
                    Id = internalSignatureId,
                    ByteSequences = byteSequences
                };

                return Result<PronomInternalSignature?>.Success(internalSignature);
            }
            catch (Exception ex)
            {
                return Result<PronomInternalSignature?>.Failure(
                    "PRONOM_INTERNAL_SIGNATURE_GET_FAILED",
                    $"PronomRepository.GetInternalSignatureAsync: failed to get internal signature '{internalSignatureId}'. {ex.Message}");
            }
        }

        private static async Task<List<PronomByteSequence>> ReadByteSequencesAsync(
            SqliteConnection connection,
            int internalSignatureId)
        {
            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
        SELECT
            EspluqueSequenceId,
            Reference,
            Endianness
        FROM ByteSequence
        WHERE IdForFileFormat = @InternalSignatureId
        ORDER BY EspluqueSequenceId;
        """;

            command.Parameters.AddWithValue("@InternalSignatureId", internalSignatureId);

            using SqliteDataReader reader = await command.ExecuteReaderAsync();

            List<(int EspluqueSequenceId, PronomByteSequence ByteSequence)> byteSequenceRows = [];

            while (await reader.ReadAsync())
            {
                byteSequenceRows.Add((
                    reader.GetInt32(0),
                    CreateByteSequenceFromReader(reader)));
            }

            List<PronomByteSequence> byteSequences = [];

            foreach ((int espluqueSequenceId, PronomByteSequence byteSequence) in byteSequenceRows)
            {
                byteSequence.SubSequences = await ReadSubSequencesAsync(
                    connection,
                    espluqueSequenceId);

                byteSequences.Add(byteSequence);
            }

            return byteSequences;
        }

        private static async Task<List<PronomSubSequence>> ReadSubSequencesAsync(
            SqliteConnection connection,
            int espluqueSequenceId)
        {
            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
        SELECT
            MinFragLength,
            Position,
            SubSeqMaxOffset,
            SubSeqMinOffset,
            "Sequence"
        FROM SubSequence
        WHERE EspluqueSequenceId = @EspluqueSequenceId
        ORDER BY Position;
        """;

            command.Parameters.AddWithValue("@EspluqueSequenceId", espluqueSequenceId);

            using SqliteDataReader reader = await command.ExecuteReaderAsync();

            List<(int Position, PronomSubSequence SubSequence)> subSequenceRows = [];

            while (await reader.ReadAsync())
            {
                PronomSubSequence subSequence = CreateSubSequenceFromReader(reader);

                subSequenceRows.Add((
                    subSequence.Position,
                    subSequence));
            }

            List<PronomSubSequence> subSequences = [];

            foreach ((int position, PronomSubSequence subSequence) in subSequenceRows)
            {
                subSequence.Fragments = await ReadFragmentsAsync(
                    connection,
                    espluqueSequenceId,
                    position);

                subSequences.Add(subSequence);
            }

            return subSequences;
        }

        private static async Task<List<PronomFragment>> ReadFragmentsAsync(
            SqliteConnection connection,
            int espluqueSequenceId,
            int subSequencePosition)
        {
            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
        SELECT
            LeftRight,
            MaxOffset,
            MinOffset,
            Position,
            Value
        FROM Fragment
        WHERE EspluqueSequenceId = @EspluqueSequenceId
          AND SubSequencePosition = @SubSequencePosition
        ORDER BY
            LeftRight,
            Position,
            Value;
        """;

            command.Parameters.AddWithValue("@EspluqueSequenceId", espluqueSequenceId);
            command.Parameters.AddWithValue("@SubSequencePosition", subSequencePosition);

            using SqliteDataReader reader = await command.ExecuteReaderAsync();

            List<PronomFragment> fragments = [];

            while (await reader.ReadAsync())
            {
                fragments.Add(CreateFragmentFromReader(reader));
            }

            return fragments;
        }

        public static async Task<Result<List<PronomFileFormatInfo>>> GetInfosFromInternalSignatureAsync(int internalSignatureId, string dbFilePath)
        {
            if (internalSignatureId <= 0)
            {
                return Result<List<PronomFileFormatInfo>>.Failure(
                    "PRONOM_INTERNAL_SIGNATURE_ID_INVALID",
                    $"PronomRepository.GetInfosFromInternalSignatureAsync: internal signature ID is invalid: {internalSignatureId}.");
            }

            try
            {
                using SqliteConnection connection = DbConnectionFactory.CreateConnection(dbFilePath);
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
SELECT
    FileFormat.MIMEType,
    FileFormat.Name,
    FileFormat.Version,
    FileFormat.Puid
FROM FileFormat_InternalSignature
INNER JOIN FileFormat
    ON FileFormat.Id = FileFormat_InternalSignature.FileFormatId
WHERE FileFormat_InternalSignature.FileFormatSignatureId = @InternalSignatureId
ORDER BY
    FileFormat.Name,
    FileFormat.Version;
""";

                command.Parameters.AddWithValue("@InternalSignatureId", internalSignatureId);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                List<PronomFileFormatInfo> fileFormatInfos = [];

                while (await reader.ReadAsync())
                {
                    fileFormatInfos.Add(CreateFileFormatInfoFromReader(reader));
                }

                return Result<List<PronomFileFormatInfo>>.Success(fileFormatInfos);
            }
            catch (Exception ex)
            {
                return Result<List<PronomFileFormatInfo>>.Failure(
                    "PRONOM_INTERNAL_SIGNATURE_FORMAT_INFOS_GET_FAILED",
                    $"PronomRepository.GetInfosFromInternalSignatureAsync: failed to get format infos for internal signature '{internalSignatureId}'. {ex.Message}");
            }
        }

        public static async Task<Result<PronomFileFormatInfo?>> GetHighestPriorityFileFormatInfosFromInternalSignatureIdsAsync(List<int> internalSignatureIds, string dbFilePath)
        {
            if (internalSignatureIds is null || internalSignatureIds.Count == 0)
            {
                return Result<PronomFileFormatInfo?>.Success(null);
            }

            List<int> validInternalSignatureIds = internalSignatureIds.Where(internalSignatureId => internalSignatureId > 0).Distinct().ToList();

            if (validInternalSignatureIds.Count == 0)
            {
                return Result<PronomFileFormatInfo?>.Success(null);
            }

            try
            {
                using SqliteConnection connection = DbConnectionFactory.CreateConnection(dbFilePath);
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                List<string> parameterNames = [];

                for (int i = 0; i < validInternalSignatureIds.Count; i++)
                {
                    string parameterName = $"@InternalSignatureId{i}";
                    parameterNames.Add(parameterName);
                    command.Parameters.AddWithValue(parameterName, validInternalSignatureIds[i]);
                }

                string internalSignatureIdParameters = string.Join(", ", parameterNames);

                command.CommandText = $"""
WITH MatchedFileFormat AS
(
    SELECT DISTINCT
        FileFormat.Id,
        FileFormat.MIMEType,
        FileFormat.Name,
        FileFormat.Version,
        FileFormat.Puid
    FROM FileFormat_InternalSignature
    INNER JOIN FileFormat
        ON FileFormat.Id = FileFormat_InternalSignature.FileFormatId
    WHERE FileFormat_InternalSignature.FileFormatSignatureId IN ({internalSignatureIdParameters})
),
LowerPriorityFileFormat AS
(
    SELECT DISTINCT
        AnalyzePriority.HasPriorityOverFileFormatID AS Id
    FROM AnalyzePriority
    INNER JOIN MatchedFileFormat AS PrioritySource
        ON PrioritySource.Id = AnalyzePriority.FileFormatId
    INNER JOIN MatchedFileFormat AS PriorityTarget
        ON PriorityTarget.Id = AnalyzePriority.HasPriorityOverFileFormatID
)
SELECT
    MatchedFileFormat.MIMEType,
    MatchedFileFormat.Name,
    MatchedFileFormat.Version,
    MatchedFileFormat.Puid
FROM MatchedFileFormat
WHERE MatchedFileFormat.Id NOT IN
(
    SELECT Id
    FROM LowerPriorityFileFormat
)
ORDER BY
    MatchedFileFormat.Name,
    MatchedFileFormat.Version
LIMIT 1;
""";

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    Result<List<PronomFileFormatInfo>> fallbackResult = await GetInfosFromInternalSignatureAsync(validInternalSignatureIds[0], dbFilePath); 
                    return Result<PronomFileFormatInfo?>.Success(fallbackResult.Value?.FirstOrDefault());
                }

                return Result<PronomFileFormatInfo?>.Success(CreateFileFormatInfoFromReader(reader));
            }
            catch
            {
                Result<List<PronomFileFormatInfo>> fallbackResult = await GetInfosFromInternalSignatureAsync(validInternalSignatureIds[0], dbFilePath);

                if (fallbackResult.IsSuccess)
                {
                    return Result<PronomFileFormatInfo?>.Success(fallbackResult.Value.FirstOrDefault());
                }

                return Result<PronomFileFormatInfo?>.Success(null);
            }
        }

        public static async Task<Result<List<PronomFileFormatInfo>>> GetInfosFromExtensionWithoutInternalSignatureAsync(string extension, string dbFilePath)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return Result<List<PronomFileFormatInfo>>.Failure(
                    "PRONOM_EXTENSION_EMPTY_EXTENSION",
                    "PronomRepository.GetInfosFromExtensionWithoutInternalSignatureAsync: extension is empty.");
            }

            string searchedExtension = extension.Trim().TrimStart('.');

            try
            {
                using SqliteConnection connection = DbConnectionFactory.CreateConnection(dbFilePath);
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
SELECT
    FileFormat.MIMEType,
    FileFormat.Name,
    FileFormat.Version,
    FileFormat.Puid
FROM Extension
INNER JOIN FileFormat
    ON FileFormat.Id = Extension.FileFormatId
WHERE Extension.Extension = @Extension COLLATE NOCASE
  AND NOT EXISTS
  (
      SELECT 1
      FROM FileFormat_InternalSignature
      WHERE FileFormat_InternalSignature.FileFormatId = FileFormat.Id
  )
ORDER BY FileFormat.Name, FileFormat.Version;
""";

                command.Parameters.AddWithValue("@Extension", searchedExtension);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                List<PronomFileFormatInfo> fileFormatInfos = [];

                while (await reader.ReadAsync())
                {
                    fileFormatInfos.Add(CreateFileFormatInfoFromReader(reader));
                }

                return Result<List<PronomFileFormatInfo>>.Success(fileFormatInfos);
            }
            catch (Exception ex)
            {
                return Result<List<PronomFileFormatInfo>>.Failure(
                    "PRONOM_EXTENSION_WITHOUT_INTERNAL_SIGNATURE_GET_INFOS_FAILED",
                    $"PronomRepository.GetInfosFromExtensionWithoutInternalSignatureAsync: failed to get infos for extension '{extension}'. {ex.Message}");
            }
        }

        public static async Task<Result<string?>> GetTriggerByPuidAsync(string puid, string dbFilePath)
        {
            if (string.IsNullOrWhiteSpace(puid))
            {
                return Result<string?>.Failure("PRONOM_CONTAINER_TRIGGER_PUID_EMPTY", "PronomRepository.GetTriggerByPuidAsync: PUID is empty.");
            }

            try
            {
                using SqliteConnection connection = DbConnectionFactory.CreateConnection(dbFilePath);
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
SELECT
    ContainerType
FROM ContainerTrigger
WHERE Puid = @Puid
LIMIT 1;
""";

                command.Parameters.AddWithValue("@Puid", puid);

                object? result = await command.ExecuteScalarAsync();

                if (result is null || result == DBNull.Value)
                {
                    return Result<string?>.Success(null);
                }

                return Result<string?>.Success(result.ToString());
            }
            catch (Exception ex)
            {
                return Result<string?>.Failure("PRONOM_CONTAINER_TRIGGER_GET_FAILED", $"PronomRepository.GetTriggerByPuidAsync: failed to get container trigger for PUID '{puid}'. {ex.Message}");
            }
        }

        public static async Task<Result<List<PronomContainerSignature>>> GetContainerSignaturesByTypeAsync(string containerType, string dbFilePath)
        {
            if (string.IsNullOrWhiteSpace(containerType))
            {
                return Result<List<PronomContainerSignature>>.Failure("PRONOM_CONTAINER_TYPE_EMPTY", "PronomRepository.GetContainerSignaturesByTypeAsync: container type is empty.");
            }

            try
            {
                using SqliteConnection connection = DbConnectionFactory.CreateConnection(dbFilePath);
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
SELECT
    ContainerSignature.Id,
    ContainerSignature.ContainerType,
    ContainerSignature.Description,
    ContainerSignature.Puid,
    ContainerFile.Path,
    ContainerFile_InternalSignature.SignatureIdForContainer
FROM ContainerSignature
INNER JOIN ContainerFile
    ON ContainerFile.ContainerSignatureId = ContainerSignature.Id
LEFT JOIN ContainerFile_InternalSignature
    ON ContainerFile_InternalSignature.ContainerSignatureId = ContainerFile.ContainerSignatureId
   AND ContainerFile_InternalSignature.FilePath = ContainerFile.Path
WHERE ContainerSignature.ContainerType = @ContainerType
ORDER BY
    ContainerSignature.Id,
    ContainerFile.Path,
    ContainerFile_InternalSignature.SignatureIdForContainer;
""";

                command.Parameters.AddWithValue("@ContainerType", containerType);

                using SqliteDataReader dbReader = await command.ExecuteReaderAsync();

                Dictionary<int, PronomContainerSignature> containerSignaturesById = [];
                Dictionary<(int ContainerSignatureId, string Path), PronomContainerFile> containerFilesByKey = [];

                while (await dbReader.ReadAsync())
                {
                    int containerSignatureId = dbReader.GetInt32(0);
                    string filePath = dbReader.GetString(4);

                    if (!containerSignaturesById.TryGetValue(containerSignatureId, out PronomContainerSignature? containerSignature))
                    {
                        containerSignature = CreateContainerSignatureFromReader(dbReader);
                        containerSignaturesById.Add(containerSignatureId, containerSignature);
                    }

                    (int ContainerSignatureId, string Path) containerFileKey = (containerSignatureId, filePath);

                    if (!containerFilesByKey.TryGetValue(containerFileKey, out PronomContainerFile? containerFile))
                    {
                        containerFile = CreateContainerFileFromReader(dbReader);
                        containerFilesByKey.Add(containerFileKey, containerFile);
                        containerSignature.Files.Add(containerFile);
                    }

                    if (!dbReader.IsDBNull(5))
                    {
                        containerFile.InternalSignatureIds.Add(dbReader.GetInt32(5));
                    }
                }

                return Result<List<PronomContainerSignature>>.Success(containerSignaturesById.Values.ToList());
            }
            catch (Exception ex)
            {
                return Result<List<PronomContainerSignature>>.Failure("PRONOM_CONTAINER_SIGNATURES_GET_FAILED", $"PronomRepository.GetContainerSignaturesByTypeAsync: failed to get container signatures for type '{containerType}'. {ex.Message}");
            }
        }

        public static async Task<Result<PronomInternalSignature?>> GetContainerInternalSignatureAsync(int internalSignatureId, string dbFilePath)
        {
            if (internalSignatureId <= 0)
            {
                return Result<PronomInternalSignature?>.Failure("PRONOM_CONTAINER_INTERNAL_SIGNATURE_ID_INVALID", $"PronomRepository.GetContainerInternalSignatureAsync: internal signature ID is invalid: {internalSignatureId}.");
            }

            try
            {
                using SqliteConnection connection = DbConnectionFactory.CreateConnection(dbFilePath);
                await connection.OpenAsync();

                List<PronomByteSequence> byteSequences = await ReadContainerByteSequencesAsync(
                    connection,
                    internalSignatureId);

                if (byteSequences.Count == 0)
                {
                    return Result<PronomInternalSignature?>.Success(null);
                }

                PronomInternalSignature internalSignature = new()
                {
                    Id = internalSignatureId,
                    ByteSequences = byteSequences
                };

                return Result<PronomInternalSignature?>.Success(internalSignature);
            }
            catch (Exception ex)
            {
                return Result<PronomInternalSignature?>.Failure("PRONOM_CONTAINER_INTERNAL_SIGNATURE_GET_FAILED", $"PronomRepository.GetContainerInternalSignatureAsync: failed to get container internal signature '{internalSignatureId}'. {ex.Message}");
            }
        }

        private static async Task<List<PronomByteSequence>> ReadContainerByteSequencesAsync(
    SqliteConnection connection,
    int internalSignatureId)
        {
            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
SELECT
    EspluqueSequenceId,
    Reference,
    Endianness
FROM ByteSequence
WHERE IdForContainer = @InternalSignatureId
ORDER BY EspluqueSequenceId;
""";

            command.Parameters.AddWithValue("@InternalSignatureId", internalSignatureId);

            using SqliteDataReader reader = await command.ExecuteReaderAsync();

            List<(int EspluqueSequenceId, PronomByteSequence ByteSequence)> byteSequenceRows = [];

            while (await reader.ReadAsync())
            {
                byteSequenceRows.Add((
                    reader.GetInt32(0),
                    CreateByteSequenceFromReader(reader)));
            }

            List<PronomByteSequence> byteSequences = [];

            foreach ((int espluqueSequenceId, PronomByteSequence byteSequence) in byteSequenceRows)
            {
                byteSequence.SubSequences = await ReadSubSequencesAsync(
                    connection,
                    espluqueSequenceId);

                byteSequences.Add(byteSequence);
            }

            return byteSequences;
        }
        #region DTO
        private static PronomByteSequence CreateByteSequenceFromReader(SqliteDataReader reader)
        {
            return new PronomByteSequence
            {
                Reference = reader.IsDBNull(1) ? null : reader.GetString(1),
                Endianness = reader.IsDBNull(2) ? null : reader.GetString(2)
            };
        }

        private static PronomSubSequence CreateSubSequenceFromReader(SqliteDataReader reader)
        {
            return new PronomSubSequence
            {
                MinFragLength = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                Position = reader.GetInt32(1),
                SubSeqMaxOffset = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                SubSeqMinOffset = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                Sequence = reader.GetString(4)
            };
        }

        private static PronomFragment CreateFragmentFromReader(SqliteDataReader reader)
        {
            return new PronomFragment
            {
                LeftRight = reader.GetString(0),
                MaxOffset = reader.GetInt32(1),
                MinOffset = reader.GetInt32(2),
                Position = reader.GetInt32(3),
                Value = reader.GetString(4)
            };
        }

        private static PronomFileFormatInfo CreateFileFormatInfoFromReader(SqliteDataReader reader)
        {
            return new PronomFileFormatInfo
            {
                MimeType = reader.IsDBNull(0) ? null : reader.GetString(0),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Version = reader.IsDBNull(2) ? null : reader.GetString(2),
                Puid = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
            };
        }

        private static PronomContainerSignature CreateContainerSignatureFromReader(SqliteDataReader reader)
        {
            return new PronomContainerSignature
            {
                Id = reader.GetInt32(0),
                ContainerType = reader.GetString(1),
                Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Puid = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Files = []
            };
        }

        private static PronomContainerFile CreateContainerFileFromReader(SqliteDataReader reader)
        {
            return new PronomContainerFile
            {
                ContainerSignatureId = reader.GetInt32(0),
                Path = reader.GetString(4),
                InternalSignatureIds = []
            };
        }
        #endregion
    }
}
