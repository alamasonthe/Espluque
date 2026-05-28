using Microsoft.Data.Sqlite;
using Util;
using Espluque.Contracts.Ports;

namespace PronomSqlite
{
    public class ImportFileSignatureRepository : IImportFileSignatureRepository
    {
        private readonly DbConnectionFactory _dbConnectionFactory;

        private SqliteConnection? _connection;
        private SqliteTransaction? _transaction;

        public ImportFileSignatureRepository(DbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<Result<bool>> CreateTransactionAsync()
        {
            _connection = _dbConnectionFactory.CreateConnection();
            await _connection.OpenAsync();

            _transaction = (SqliteTransaction)await _connection.BeginTransactionAsync();

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> CleanPronomTablesAsync()
        {
            if (_connection == null || _transaction == null)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_TRANSACTION_NOT_STARTED",
                    "The PRONOM import transaction has not been started.");
            }

            try
            {
                await using SqliteCommand command = _connection.CreateCommand();
                command.Transaction = _transaction;

                command.CommandText =
                """
        DELETE FROM PronomAnalyzePriority;
        DELETE FROM PronomFileFormat_InternalSignature;
        DELETE FROM PronomExtension;

        DELETE FROM PronomShift;
        DELETE FROM PronomFragment;
        DELETE FROM PronomSubSequence;
        DELETE FROM PronomByteSequence;

        DELETE FROM PronomFileFormat;
        DELETE FROM PronomInternalSignature;
        """;

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_CLEAN_FAILED",
                    $"ImportRepository.CleanPronomTablesAsync: failed to clean PRONOM tables. {exception.Message}");
            }
        }

        public async Task<Result<bool>> UpsertSourceVersionAsync(string sourceName, string? version, string? date)
        {
            if (_connection == null || _transaction == null)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_TRANSACTION_NOT_STARTED",
                    "The PRONOM import transaction has not been started.");
            }

            if (string.IsNullOrWhiteSpace(sourceName))
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_SOURCE_NAME_EMPTY",
                    "The source name is empty.");
            }

            if (string.IsNullOrWhiteSpace(date))
            {
                date = DateTime.Today.ToString("yyyy-MM-dd");
            }

            try
            {
                await using SqliteCommand command = _connection.CreateCommand();
                command.Transaction = _transaction;

                command.CommandText =
                """
        INSERT INTO SourceVersions
        (
            SourceName,
            Version,
            Date
        )
        VALUES
        (
            @SourceName,
            @Version,
            @Date
        )
        ON CONFLICT(SourceName) DO UPDATE SET
            Version = excluded.Version,
            Date = excluded.Date;
        """;

                command.Parameters.AddWithValue("@SourceName", sourceName);
                command.Parameters.AddWithValue("@Version", string.IsNullOrWhiteSpace(version) ? DBNull.Value : version);
                command.Parameters.AddWithValue("@Date", date);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_SOURCE_VERSION_UPSERT_FAILED",
                    $"ImportRepository.UpsertSourceVersionAsync: failed to upsert source version. {exception.Message}");
            }
        }

        public async Task<Result<bool>> InsertInternalSignatureAsync(string id)
        {
            if (_connection == null || _transaction == null)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_TRANSACTION_NOT_STARTED",
                    "The PRONOM import transaction has not been started.");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_INTERNAL_SIGNATURE_ID_EMPTY",
                    "The internal signature ID is empty.");
            }

            if (!int.TryParse(id, out int internalSignatureId))
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_INTERNAL_SIGNATURE_ID_INVALID",
                    $"The internal signature ID is invalid: {id}");
            }

            try
            {
                await using SqliteCommand command = _connection.CreateCommand();
                command.Transaction = _transaction;

                command.CommandText =
                """
        INSERT INTO PronomInternalSignature
        (
            ID
        )
        VALUES
        (
            @ID
        );
        """;

                command.Parameters.AddWithValue("@ID", internalSignatureId);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_INTERNAL_SIGNATURE_INSERT_FAILED",
                    $"ImportRepository.InsertInternalSignatureAsync: failed to insert internal signature. {exception.Message}");
            }
        }

        public async Task<Result<bool>> InsertByteSequenceAsync(
            int internalSignatureId,
            int espluqueSequenceId,
            string? reference,
            string? endianness)
        {
            if (_connection == null || _transaction == null)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_TRANSACTION_NOT_STARTED",
                    "The PRONOM import transaction has not been started.");
            }

            if (internalSignatureId <= 0)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_INTERNAL_SIGNATURE_ID_INVALID",
                    $"The internal signature ID is invalid: {internalSignatureId}");
            }

            if (espluqueSequenceId <= 0)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_ESPLUQUE_SEQUENCE_ID_INVALID",
                    $"The Espluque sequence ID is invalid: {espluqueSequenceId}");
            }

            try
            {
                await using SqliteCommand command = _connection.CreateCommand();
                command.Transaction = _transaction;

                command.CommandText =
                """
        INSERT INTO PronomByteSequence
        (
            InternalSignatureId,
            EspluqueSequenceId,
            Reference,
            Endianness
        )
        VALUES
        (
            @InternalSignatureId,
            @EspluqueSequenceId,
            @Reference,
            @Endianness
        );
        """;

                command.Parameters.AddWithValue("@InternalSignatureId", internalSignatureId);
                command.Parameters.AddWithValue("@EspluqueSequenceId", espluqueSequenceId);
                command.Parameters.AddWithValue("@Reference", string.IsNullOrWhiteSpace(reference) ? DBNull.Value : reference);
                command.Parameters.AddWithValue("@Endianness", string.IsNullOrWhiteSpace(endianness) ? DBNull.Value : endianness);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_BYTE_SEQUENCE_INSERT_FAILED",
                    $"ImportRepository.InsertByteSequenceAsync: failed to insert byte sequence. {exception.Message}");
            }
        }

        public async Task<Result<bool>> InsertSubSequenceAsync(
            int espluqueSequenceId,
            int minFragLength,
            int position,
            int? subSeqMaxOffset,
            int subSeqMinOffset,
            string sequence,
            int defaultShift)
        {
            if (_connection == null || _transaction == null)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_TRANSACTION_NOT_STARTED",
                    "The PRONOM import transaction has not been started.");
            }

            if (espluqueSequenceId <= 0)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_ESPLUQUE_SEQUENCE_ID_INVALID",
                    $"The Espluque sequence ID is invalid: {espluqueSequenceId}");
            }

            if (position <= 0)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_SUB_SEQUENCE_POSITION_INVALID",
                    $"The sub-sequence position is invalid: {position}");
            }

            if (string.IsNullOrWhiteSpace(sequence))
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_SUB_SEQUENCE_SEQUENCE_EMPTY",
                    "The sub-sequence sequence is empty.");
            }

            try
            {
                await using SqliteCommand command = _connection.CreateCommand();
                command.Transaction = _transaction;

                command.CommandText =
                """
        INSERT INTO PronomSubSequence
        (
            EspluqueSequenceId,
            MinFragLength,
            Position,
            SubSeqMaxOffset,
            SubSeqMinOffset,
            Sequence,
            DefaultShift
        )
        VALUES
        (
            @EspluqueSequenceId,
            @MinFragLength,
            @Position,
            @SubSeqMaxOffset,
            @SubSeqMinOffset,
            @Sequence,
            @DefaultShift
        );
        """;

                command.Parameters.AddWithValue("@EspluqueSequenceId", espluqueSequenceId);
                command.Parameters.AddWithValue("@MinFragLength", minFragLength);
                command.Parameters.AddWithValue("@Position", position);
                command.Parameters.AddWithValue("@SubSeqMaxOffset", subSeqMaxOffset.HasValue ? subSeqMaxOffset.Value : DBNull.Value);
                command.Parameters.AddWithValue("@SubSeqMinOffset", subSeqMinOffset);
                command.Parameters.AddWithValue("@Sequence", sequence);
                command.Parameters.AddWithValue("@DefaultShift", defaultShift);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_SUB_SEQUENCE_INSERT_FAILED",
                    $"ImportRepository.InsertSubSequenceAsync: failed to insert sub-sequence. {exception.Message}");
            }
        }

        public async Task<Result<bool>> InsertFragmentAsync(
            int espluqueSequenceId,
            int subSequencePosition,
            string leftRight,
            int maxOffset,
            int minOffset,
            int position,
            string value)
        {
            if (_connection == null || _transaction == null)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_TRANSACTION_NOT_STARTED",
                    "The PRONOM import transaction has not been started.");
            }

            if (espluqueSequenceId <= 0)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_ESPLUQUE_SEQUENCE_ID_INVALID",
                    $"The Espluque sequence ID is invalid: {espluqueSequenceId}");
            }

            if (subSequencePosition <= 0)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_SUB_SEQUENCE_POSITION_INVALID",
                    $"The sub-sequence position is invalid: {subSequencePosition}");
            }

            if (string.IsNullOrWhiteSpace(leftRight))
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_FRAGMENT_SIDE_EMPTY",
                    "The fragment side is empty.");
            }

            if (leftRight != "Left" && leftRight != "Right")
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_FRAGMENT_SIDE_INVALID",
                    $"The fragment side is invalid: {leftRight}");
            }

            if (position <= 0)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_FRAGMENT_POSITION_INVALID",
                    $"The fragment position is invalid: {position}");
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_FRAGMENT_VALUE_EMPTY",
                    "The fragment value is empty.");
            }

            try
            {
                await using SqliteCommand command = _connection.CreateCommand();
                command.Transaction = _transaction;

                command.CommandText =
                """
        INSERT INTO PronomFragment
        (
            EspluqueSequenceId,
            SubSequencePosition,
            LeftRight,
            MaxOffset,
            MinOffset,
            Position,
            Value
        )
        VALUES
        (
            @EspluqueSequenceId,
            @SubSequencePosition,
            @LeftRight,
            @MaxOffset,
            @MinOffset,
            @Position,
            @Value
        );
        """;

                command.Parameters.AddWithValue("@EspluqueSequenceId", espluqueSequenceId);
                command.Parameters.AddWithValue("@SubSequencePosition", subSequencePosition);
                command.Parameters.AddWithValue("@LeftRight", leftRight);
                command.Parameters.AddWithValue("@MaxOffset", maxOffset);
                command.Parameters.AddWithValue("@MinOffset", minOffset);
                command.Parameters.AddWithValue("@Position", position);
                command.Parameters.AddWithValue("@Value", value);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_FRAGMENT_INSERT_FAILED",
                    $"ImportRepository.InsertFragmentAsync: failed to insert fragment. {exception.Message}");
            }
        }

        public async Task<Result<bool>> InsertShiftAsync(
            int espluqueSequenceId,
            int subSequencePosition,
            string byteValue,
            int value)
        {
            if (_connection == null || _transaction == null)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_TRANSACTION_NOT_STARTED",
                    "The PRONOM import transaction has not been started.");
            }

            if (espluqueSequenceId <= 0)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_ESPLUQUE_SEQUENCE_ID_INVALID",
                    $"The Espluque sequence ID is invalid: {espluqueSequenceId}");
            }

            if (subSequencePosition <= 0)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_SUB_SEQUENCE_POSITION_INVALID",
                    $"The sub-sequence position is invalid: {subSequencePosition}");
            }

            if (string.IsNullOrWhiteSpace(byteValue))
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_SHIFT_BYTE_EMPTY",
                    "The shift byte is empty.");
            }

            try
            {
                await using SqliteCommand command = _connection.CreateCommand();
                command.Transaction = _transaction;

                command.CommandText =
                """
        INSERT INTO PronomShift
        (
            EspluqueSequenceId,
            SubSequencePosition,
            Byte,
            Value
        )
        VALUES
        (
            @EspluqueSequenceId,
            @SubSequencePosition,
            @Byte,
            @Value
        );
        """;

                command.Parameters.AddWithValue("@EspluqueSequenceId", espluqueSequenceId);
                command.Parameters.AddWithValue("@SubSequencePosition", subSequencePosition);
                command.Parameters.AddWithValue("@Byte", byteValue);
                command.Parameters.AddWithValue("@Value", value);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_SHIFT_INSERT_FAILED",
                    $"ImportRepository.InsertShiftAsync: failed to insert shift. {exception.Message}");
            }
        }

        public async Task<Result<bool>> InsertFileFormatAsync(
            int id,
            string? mimeType,
            string name,
            string puid,
            string? version)
        {
            if (_connection == null || _transaction == null)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_TRANSACTION_NOT_STARTED",
                    "The PRONOM import transaction has not been started.");
            }

            if (id <= 0)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_ID_INVALID",
                    $"The file format ID is invalid: {id}");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_NAME_EMPTY",
                    "The file format name is empty.");
            }

            if (string.IsNullOrWhiteSpace(puid))
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_PUID_EMPTY",
                    "The file format PUID is empty.");
            }

            try
            {
                await using SqliteCommand command = _connection.CreateCommand();
                command.Transaction = _transaction;

                command.CommandText =
                """
        INSERT INTO PronomFileFormat
        (
            Id,
            MIMEType,
            Name,
            Puid,
            Version
        )
        VALUES
        (
            @Id,
            @MIMEType,
            @Name,
            @Puid,
            @Version
        );
        """;

                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@MIMEType", string.IsNullOrWhiteSpace(mimeType) ? DBNull.Value : mimeType);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Puid", puid);
                command.Parameters.AddWithValue("@Version", string.IsNullOrWhiteSpace(version) ? DBNull.Value : version);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_INSERT_FAILED",
                    $"ImportRepository.InsertFileFormatAsync: failed to insert file format. {exception.Message}");
            }
        }

        public async Task<Result<bool>> InsertFileFormatInternalSignatureAsync(
            int fileFormatId,
            int internalSignatureId)
        {
            if (_connection == null || _transaction == null)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_TRANSACTION_NOT_STARTED",
                    "The PRONOM import transaction has not been started.");
            }

            if (fileFormatId <= 0)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_ID_INVALID",
                    $"The file format ID is invalid: {fileFormatId}");
            }

            if (internalSignatureId <= 0)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_INTERNAL_SIGNATURE_ID_INVALID",
                    $"The internal signature ID is invalid: {internalSignatureId}");
            }

            try
            {
                await using SqliteCommand command = _connection.CreateCommand();
                command.Transaction = _transaction;

                command.CommandText =
                """
        INSERT INTO PronomFileFormat_InternalSignature
        (
            FileFormatId,
            InternalSignatureId
        )
        VALUES
        (
            @FileFormatId,
            @InternalSignatureId
        );
        """;

                command.Parameters.AddWithValue("@FileFormatId", fileFormatId);
                command.Parameters.AddWithValue("@InternalSignatureId", internalSignatureId);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_INTERNAL_SIGNATURE_INSERT_FAILED",
                    $"ImportRepository.InsertFileFormatInternalSignatureAsync: failed to insert file format internal signature. {exception.Message}");
            }
        }

        public async Task<Result<bool>> InsertExtensionAsync(
            int fileFormatId,
            string extension)
        {
            if (_connection == null || _transaction == null)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_TRANSACTION_NOT_STARTED",
                    "The PRONOM import transaction has not been started.");
            }

            if (fileFormatId <= 0)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_ID_INVALID",
                    $"The file format ID is invalid: {fileFormatId}");
            }

            if (string.IsNullOrWhiteSpace(extension))
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_EXTENSION_EMPTY",
                    "The extension is empty.");
            }

            try
            {
                await using SqliteCommand command = _connection.CreateCommand();
                command.Transaction = _transaction;

                command.CommandText =
                """
        INSERT INTO PronomExtension
        (
            FileFormatId,
            Extension
        )
        VALUES
        (
            @FileFormatId,
            @Extension
        );
        """;

                command.Parameters.AddWithValue("@FileFormatId", fileFormatId);
                command.Parameters.AddWithValue("@Extension", extension);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_EXTENSION_INSERT_FAILED",
                    $"ImportRepository.InsertExtensionAsync: failed to insert extension. {exception.Message}");
            }
        }

        public async Task<Result<bool>> InsertAnalyzePriorityAsync(
            int fileFormatId,
            int hasPriorityOverFileFormatId)
        {
            if (_connection == null || _transaction == null)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_TRANSACTION_NOT_STARTED",
                    "The PRONOM import transaction has not been started.");
            }

            if (fileFormatId <= 0)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_ID_INVALID",
                    $"The file format ID is invalid: {fileFormatId}");
            }

            if (hasPriorityOverFileFormatId <= 0)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_PRIORITY_FILE_FORMAT_ID_INVALID",
                    $"The priority file format ID is invalid: {hasPriorityOverFileFormatId}");
            }

            try
            {
                await using SqliteCommand command = _connection.CreateCommand();
                command.Transaction = _transaction;

                command.CommandText =
                """
        INSERT INTO PronomAnalyzePriority
        (
            FileFormatId,
            HasPriorityOverFileFormatID
        )
        VALUES
        (
            @FileFormatId,
            @HasPriorityOverFileFormatID
        );
        """;

                command.Parameters.AddWithValue("@FileFormatId", fileFormatId);
                command.Parameters.AddWithValue("@HasPriorityOverFileFormatID", hasPriorityOverFileFormatId);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_ANALYZE_PRIORITY_INSERT_FAILED",
                    $"ImportRepository.InsertAnalyzePriorityAsync: failed to insert analyze priority. {exception.Message}");
            }
        }

        public async Task<Result<bool>> CloseTransactionAsync(bool commit)
        {
            if (_transaction == null || _connection == null)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_TRANSACTION_NOT_STARTED",
                    "The PRONOM import transaction has not been started.");
            }

            if (commit)
            {
                await _transaction.CommitAsync();
            }
            else
            {
                await _transaction.RollbackAsync();
            }

            await _transaction.DisposeAsync();
            await _connection.DisposeAsync();

            _transaction = null;
            _connection = null;

            return Result<bool>.Success(true);
        }

    }
}