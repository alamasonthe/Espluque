using Espluque.Contracts.Ports;
using Microsoft.Data.Sqlite;
using PronomSqlite.Entities;
using Util;

namespace PronomSqlite
{
    public class PronomRepository : IPronomRepository
    {
        private readonly DbConnectionFactory _dbConnectionFactory;

        public PronomRepository(DbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<Result<List<KeyValuePair<string, string>>?>> GetInfosFromExtensionAsync(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return Result<List<KeyValuePair<string, string>>?>.Failure(
                    "PRONOM_EXTENSION_EMPTY_EXTENSION",
                    "PronomRepository.GetInfosFromExtensionAsync: extension is empty.");
            }

            string searchedExtension = extension.Trim().TrimStart('.');

            try
            {
                using SqliteConnection connection = _dbConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
            SELECT
                PronomExtension.Extension,
                PronomFileFormat.MIMEType,
                PronomFileFormat.Name,
                PronomFileFormat.Version
            FROM PronomExtension
            INNER JOIN PronomFileFormat
                ON PronomFileFormat.Id = PronomExtension.FileFormatId
            WHERE PronomExtension.Extension = @Extension COLLATE NOCASE
            ORDER BY PronomFileFormat.Name, PronomFileFormat.Version;
            """;

                command.Parameters.AddWithValue("@Extension", searchedExtension);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                List<KeyValuePair<string, string>>? infos = null;
                int formatIndex = 1;

                while (await reader.ReadAsync())
                {
                    if (infos is null)
                    {
                        infos = [];

                        infos.Add(new KeyValuePair<string, string>(
                            "PronomExtension.Extension",
                            reader.GetString(0)));
                    }

                    if (!reader.IsDBNull(1))
                    {
                        infos.Add(new KeyValuePair<string, string>(
                            $"PronomFileFormat.MIMEType[{formatIndex}]",
                            reader.GetString(1)));
                    }

                    infos.Add(new KeyValuePair<string, string>(
                        $"PronomFileFormat.Name[{formatIndex}]",
                        reader.GetString(2)));

                    if (!reader.IsDBNull(3))
                    {
                        infos.Add(new KeyValuePair<string, string>(
                            $"PronomFileFormat.Version[{formatIndex}]",
                            reader.GetString(3)));
                    }

                    formatIndex++;
                }

                return Result<List<KeyValuePair<string, string>>?>.Success(infos);
            }
            catch (Exception ex)
            {
                return Result<List<KeyValuePair<string, string>>?>.Failure(
                    "PRONOM_EXTENSION_GET_INFOS_FAILED",
                    $"PronomRepository.GetInfosFromExtensionAsync: failed to get infos for extension '{extension}'. {ex.Message}");
            }
        }

        public async Task<Result<List<int>>> ListInternalSignatureIdsAsync()
        {
            try
            {
                using SqliteConnection connection = _dbConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
            SELECT
                PronomInternalSignature.ID
            FROM PronomInternalSignature
            LEFT JOIN PronomFileFormat_InternalSignature
                ON PronomFileFormat_InternalSignature.InternalSignatureId = PronomInternalSignature.ID
            LEFT JOIN PronomAnalyzePriority AS PrioritySource
                ON PrioritySource.FileFormatId = PronomFileFormat_InternalSignature.FileFormatId
            LEFT JOIN PronomAnalyzePriority AS PriorityTarget
                ON PriorityTarget.HasPriorityOverFileFormatID = PronomFileFormat_InternalSignature.FileFormatId
            GROUP BY
                PronomInternalSignature.ID
            ORDER BY
                COUNT(DISTINCT PrioritySource.HasPriorityOverFileFormatID) DESC,
                COUNT(DISTINCT PriorityTarget.FileFormatId) ASC,
                PronomInternalSignature.ID;
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

        public async Task<Result<PronomInternalSignature?>> GetInternalSignatureAsync(int internalSignatureId)
        {
            if (internalSignatureId <= 0)
            {
                return Result<PronomInternalSignature?>.Failure(
                    "PRONOM_INTERNAL_SIGNATURE_ID_INVALID",
                    $"PronomRepository.GetInternalSignatureAsync: internal signature ID is invalid: {internalSignatureId}.");
            }

            try
            {
                using SqliteConnection connection = _dbConnectionFactory.CreateConnection();
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

        private async Task<List<PronomByteSequence>> ReadByteSequencesAsync(
            SqliteConnection connection,
            int internalSignatureId)
        {
            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
        SELECT
            EspluqueSequenceId,
            Reference,
            Endianness
        FROM PronomByteSequence
        WHERE InternalSignatureId = @InternalSignatureId
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

        private async Task<List<PronomSubSequence>> ReadSubSequencesAsync(
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
            "Sequence",
            DefaultShift
        FROM PronomSubSequence
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

                subSequence.Shifts = await ReadShiftsAsync(
                    connection,
                    espluqueSequenceId,
                    position);

                subSequences.Add(subSequence);
            }

            return subSequences;
        }

        private async Task<List<PronomFragment>> ReadFragmentsAsync(
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
        FROM PronomFragment
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

        private async Task<List<PronomShift>> ReadShiftsAsync(
            SqliteConnection connection,
            int espluqueSequenceId,
            int subSequencePosition)
        {
            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
        SELECT
            Byte,
            Value
        FROM PronomShift
        WHERE EspluqueSequenceId = @EspluqueSequenceId
          AND SubSequencePosition = @SubSequencePosition
        ORDER BY Byte;
        """;

            command.Parameters.AddWithValue("@EspluqueSequenceId", espluqueSequenceId);
            command.Parameters.AddWithValue("@SubSequencePosition", subSequencePosition);

            using SqliteDataReader reader = await command.ExecuteReaderAsync();

            List<PronomShift> shifts = [];

            while (await reader.ReadAsync())
            {
                shifts.Add(CreateShiftFromReader(reader));
            }

            return shifts;
        }

        public async Task<Result<List<KeyValuePair<string, string>>?>> GetInfosFromInternalSignatureAsync(int internalSignatureId)
        {
            if (internalSignatureId <= 0)
            {
                return Result<List<KeyValuePair<string, string>>?>.Failure(
                    "PRONOM_INTERNAL_SIGNATURE_ID_INVALID",
                    $"PronomRepository.GetInfosFromInternalSignatureAsync: internal signature ID is invalid: {internalSignatureId}.");
            }

            try
            {
                using SqliteConnection connection = _dbConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
            SELECT
                PronomFileFormat.MIMEType,
                PronomFileFormat.Name,
                PronomFileFormat.Version
            FROM PronomFileFormat_InternalSignature
            INNER JOIN PronomFileFormat
                ON PronomFileFormat.Id = PronomFileFormat_InternalSignature.FileFormatId
            WHERE PronomFileFormat_InternalSignature.InternalSignatureId = @InternalSignatureId
            ORDER BY
                PronomFileFormat.Name,
                PronomFileFormat.Version;
            """;

                command.Parameters.AddWithValue("@InternalSignatureId", internalSignatureId);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                List<KeyValuePair<string, string>>? infos = null;
                int formatIndex = 1;

                while (await reader.ReadAsync())
                {
                    infos ??= [];

                    if (!reader.IsDBNull(0))
                    {
                        infos.Add(new KeyValuePair<string, string>(
                            $"PronomFileFormat.MIMEType[{formatIndex}]",
                            reader.GetString(0)));
                    }

                    infos.Add(new KeyValuePair<string, string>(
                        $"PronomFileFormat.Name[{formatIndex}]",
                        reader.GetString(1)));

                    if (!reader.IsDBNull(2))
                    {
                        infos.Add(new KeyValuePair<string, string>(
                            $"PronomFileFormat.Version[{formatIndex}]",
                            reader.GetString(2)));
                    }

                    formatIndex++;
                }

                return Result<List<KeyValuePair<string, string>>?>.Success(infos);
            }
            catch (Exception ex)
            {
                return Result<List<KeyValuePair<string, string>>?>.Failure(
                    "PRONOM_INTERNAL_SIGNATURE_FORMAT_INFOS_GET_FAILED",
                    $"PronomRepository.GetInfosFromInternalSignatureAsync: failed to get format infos for internal signature '{internalSignatureId}'. {ex.Message}");
            }
        }

        public async Task<Result<List<KeyValuePair<string, string>>?>> GetHighestPriorityFileFormatInfosFromInternalSignatureIdsAsync(
            List<int> internalSignatureIds)
        {
            if (internalSignatureIds is null || internalSignatureIds.Count == 0)
            {
                return Result<List<KeyValuePair<string, string>>?>.Success(null);
            }

            List<int> validInternalSignatureIds = internalSignatureIds
                .Where(internalSignatureId => internalSignatureId > 0)
                .Distinct()
                .ToList();

            if (validInternalSignatureIds.Count == 0)
            {
                return Result<List<KeyValuePair<string, string>>?>.Success(null);
            }

            try
            {
                using SqliteConnection connection = _dbConnectionFactory.CreateConnection();
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
                    PronomFileFormat.Id,
                    PronomFileFormat.MIMEType,
                    PronomFileFormat.Name,
                    PronomFileFormat.Version
                FROM PronomFileFormat_InternalSignature
                INNER JOIN PronomFileFormat
                    ON PronomFileFormat.Id = PronomFileFormat_InternalSignature.FileFormatId
                WHERE PronomFileFormat_InternalSignature.InternalSignatureId IN ({internalSignatureIdParameters})
            ),
            LowerPriorityFileFormat AS
            (
                SELECT DISTINCT
                    PronomAnalyzePriority.HasPriorityOverFileFormatID AS Id
                FROM PronomAnalyzePriority
                INNER JOIN MatchedFileFormat AS PrioritySource
                    ON PrioritySource.Id = PronomAnalyzePriority.FileFormatId
                INNER JOIN MatchedFileFormat AS PriorityTarget
                    ON PriorityTarget.Id = PronomAnalyzePriority.HasPriorityOverFileFormatID
            )
            SELECT
                MatchedFileFormat.MIMEType,
                MatchedFileFormat.Name,
                MatchedFileFormat.Version
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
                    return await GetInfosFromInternalSignatureAsync(validInternalSignatureIds[0]);
                }

                List<KeyValuePair<string, string>> infos = [];

                if (!reader.IsDBNull(0))
                {
                    infos.Add(new KeyValuePair<string, string>(
                        "PronomFileFormat.MIMEType[1]",
                        reader.GetString(0)));
                }

                infos.Add(new KeyValuePair<string, string>(
                    "PronomFileFormat.Name[1]",
                    reader.GetString(1)));

                if (!reader.IsDBNull(2))
                {
                    infos.Add(new KeyValuePair<string, string>(
                        "PronomFileFormat.Version[1]",
                        reader.GetString(2)));
                }

                return Result<List<KeyValuePair<string, string>>?>.Success(infos);
            }
            catch
            {
                Result<List<KeyValuePair<string, string>>?> fallbackResult =
                    await GetInfosFromInternalSignatureAsync(validInternalSignatureIds[0]);

                if (fallbackResult.IsSuccess)
                {
                    return fallbackResult;
                }

                return Result<List<KeyValuePair<string, string>>?>.Success(null);
            }
        }


        public async Task<Result<List<KeyValuePair<string, string>>?>> GetInfosFromExtensionWithoutInternalSignatureAsync(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return Result<List<KeyValuePair<string, string>>?>.Failure(
                    "PRONOM_EXTENSION_EMPTY_EXTENSION",
                    "PronomRepository.GetInfosFromExtensionWithoutInternalSignatureAsync: extension is empty.");
            }

            string searchedExtension = extension.Trim().TrimStart('.');

            try
            {
                using SqliteConnection connection = _dbConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
    SELECT
        PronomExtension.Extension,
        PronomFileFormat.MIMEType,
        PronomFileFormat.Name,
        PronomFileFormat.Version
    FROM PronomExtension
    INNER JOIN PronomFileFormat
        ON PronomFileFormat.Id = PronomExtension.FileFormatId
    WHERE PronomExtension.Extension = @Extension COLLATE NOCASE
      AND NOT EXISTS
      (
          SELECT 1
          FROM PronomFileFormat_InternalSignature
          WHERE PronomFileFormat_InternalSignature.FileFormatId = PronomFileFormat.Id
      )
    ORDER BY PronomFileFormat.Name, PronomFileFormat.Version;
    """;

                command.Parameters.AddWithValue("@Extension", searchedExtension);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                List<KeyValuePair<string, string>>? infos = null;
                int formatIndex = 1;

                while (await reader.ReadAsync())
                {
                    if (infos is null)
                    {
                        infos = [];

                        infos.Add(new KeyValuePair<string, string>(
                            "PronomExtension.Extension",
                            reader.GetString(0)));
                    }

                    if (!reader.IsDBNull(1))
                    {
                        infos.Add(new KeyValuePair<string, string>(
                            $"PronomFileFormat.MIMEType[{formatIndex}]",
                            reader.GetString(1)));
                    }

                    infos.Add(new KeyValuePair<string, string>(
                        $"PronomFileFormat.Name[{formatIndex}]",
                        reader.GetString(2)));

                    if (!reader.IsDBNull(3))
                    {
                        infos.Add(new KeyValuePair<string, string>(
                            $"PronomFileFormat.Version[{formatIndex}]",
                            reader.GetString(3)));
                    }

                    formatIndex++;
                }

                return Result<List<KeyValuePair<string, string>>?>.Success(infos);
            }
            catch (Exception ex)
            {
                return Result<List<KeyValuePair<string, string>>?>.Failure(
                    "PRONOM_EXTENSION_WITHOUT_INTERNAL_SIGNATURE_GET_INFOS_FAILED",
                    $"PronomRepository.GetInfosFromExtensionWithoutInternalSignatureAsync: failed to get infos for extension '{extension}'. {ex.Message}");
            }
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
                MinFragLength = reader.GetInt32(0),
                Position = reader.GetInt32(1),
                SubSeqMaxOffset = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                SubSeqMinOffset = reader.GetInt32(3),
                Sequence = reader.GetString(4),
                DefaultShift = reader.GetInt32(5)
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

        private static PronomShift CreateShiftFromReader(SqliteDataReader reader)
        {
            return new PronomShift
            {
                Byte = reader.GetString(0),
                Value = reader.GetInt32(1)
            };
        }

        #endregion
    }
}