using Microsoft.Data.Sqlite;
using Util;

namespace Pronom
{
    public static class ImportSignaturesCommonRepository
    {
        public static async Task<Result> CleanTablesAsync(SqliteTransaction transaction, string importSource)
        {
            SqliteConnection connection = transaction.Connection;

            await using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
        DELETE FROM ContainerFile_InternalSignature
        WHERE Source = @Source;

        DELETE FROM FileFormat_InternalSignature
        WHERE Source = @Source;

        DELETE FROM Fragment
        WHERE Source = @Source;

        DELETE FROM SubSequence
        WHERE Source = @Source;

        DELETE FROM ByteSequence
        WHERE Source = @Source;

        DELETE FROM ContainerFile
        WHERE Source = @Source;

        DELETE FROM ContainerSignature
        WHERE Source = @Source;

        DELETE FROM AnalyzePriority
        WHERE Source = @Source;

        DELETE FROM Extension
        WHERE Source = @Source;

        DELETE FROM ContainerTrigger
        WHERE Source = @Source;

        DELETE FROM InternalSignature
        WHERE Source = @Source;

        DELETE FROM FileFormat
        WHERE Source = @Source;
        """;

            try
            {
                command.Parameters.AddWithValue("@Source", importSource);
                await command.ExecuteNonQueryAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(
                    "PRONOM_TABLES_CLEAN_ERROR",
                    "Problem removing data from previous import.");
            }
        }

        public static async Task<Result> UpsertSourceVersionAsync(
            SqliteTransaction transaction,
            string importSource,
            string? version,
            string? date)
        {
            var connection = transaction.Connection;

            if (connection == null || transaction == null)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_TRANSACTION_NOT_STARTED",
                    "The PRONOM import transaction has not been started.");
            }

            if (string.IsNullOrWhiteSpace(importSource))
            {
                return Result.Failure(
                    "PRONOM_IMPORT_SOURCE_NAME_EMPTY",
                    "The source name is empty.");
            }

            if (string.IsNullOrWhiteSpace(date))
            {
                date = DateTime.Today.ToString("yyyy-MM-dd");
            }

            try
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;

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

                command.Parameters.AddWithValue("@SourceName", importSource);
                command.Parameters.AddWithValue("@Version", string.IsNullOrWhiteSpace(version) ? DBNull.Value : version);
                command.Parameters.AddWithValue("@Date", date);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_SOURCE_VERSION_UPSERT_FAILED",
                    $"ImportRepository.UpsertSourceVersionAsync: failed to upsert source version. {exception.Message}");
            }
        }

        public static async Task<Result> InsertInternalSignatureAsync(SqliteTransaction transaction, string importSource, string id)
        {
            var connection = transaction?.Connection;
            if (string.IsNullOrWhiteSpace(id))
            {
                return Result.Failure(
                    "PRONOM_IMPORT_INTERNAL_SIGNATURE_ID_EMPTY",
                    "The internal signature ID is empty.");
            }

            if (!int.TryParse(id, out int signatureId))
            {
                return Result.Failure(
                    "PRONOM_IMPORT_INTERNAL_SIGNATURE_ID_INVALID",
                    $"The internal signature ID is invalid: {id}");
            }


            int? idForFileFormat = null;
            int? idForContainer = null;
            switch (importSource)
            {
                case "FileSignatures":
                    idForFileFormat = signatureId;
                    break;
                case "ContainerSignatures":
                    idForContainer = signatureId;
                    break;
                default:
                    return Result.Failure( "PRONOM_IMPORT_SOURCE_INVALID", "The import source is invalid.");
            }

            try
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                command.CommandText =
                """
        INSERT INTO InternalSignature
        (
            Source,
            IdForFileFormat,
            IdForContainer
        )
        VALUES
        (
            @Source,
            @IdForFileFormat,
            @IdForContainer
        );
        """;

                command.Parameters.AddWithValue("@Source", importSource);
                command.Parameters.AddWithValue( "@IdForFileFormat", idForFileFormat.HasValue ? idForFileFormat.Value : DBNull.Value);
                command.Parameters.AddWithValue( "@IdForContainer", idForContainer.HasValue ? idForContainer.Value : DBNull.Value);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_INTERNAL_SIGNATURE_INSERT_FAILED",
                    $"ImportRepository.InsertInternalSignatureAsync: failed to insert internal signature. {exception.Message}");
            }
        }

        public static async Task<Result> InsertByteSequenceAsync(
            SqliteTransaction transaction, 
            string importSource,
            int signatureId,
            int espluqueSequenceId,
            string? reference,
            string? endianness)
        {
            var connection = transaction?.Connection;
            if (signatureId <= 0)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_INTERNAL_SIGNATURE_ID_INVALID",
                    $"The internal signature ID is invalid: {signatureId}");
            }

            if (espluqueSequenceId <= 0)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_ESPLUQUE_SEQUENCE_ID_INVALID",
                    $"The Espluque sequence ID is invalid: {espluqueSequenceId}");
            }

            int? idForFileFormat = null;
            int? idForContainer = null;
            switch (importSource)
            {
                case "FileSignatures":
                    idForFileFormat = signatureId;
                    break;
                case "ContainerSignatures":
                    idForContainer = signatureId;
                    break;
                default:
                    return Result.Failure("PRONOM_IMPORT_SOURCE_INVALID", "The import source is invalid.");
            }

            try
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                command.CommandText =
                """
        INSERT INTO ByteSequence
        (
            Source,
            IdForFileFormat,
            IdForContainer,
            EspluqueSequenceId,
            Reference,
            Endianness
        )
        VALUES
        (
            @Source,
            @IdForFileFormat,
            @IdForContainer,
            @EspluqueSequenceId,
            @Reference,
            @Endianness
        );
        """;

                command.Parameters.AddWithValue("@Source", importSource);
                command.Parameters.AddWithValue("@IdForFileFormat", idForFileFormat.HasValue ? idForFileFormat.Value : DBNull.Value);
                command.Parameters.AddWithValue("@IdForContainer", idForContainer.HasValue ? idForContainer.Value : DBNull.Value);
                command.Parameters.AddWithValue("@EspluqueSequenceId", espluqueSequenceId);
                command.Parameters.AddWithValue("@Reference", string.IsNullOrWhiteSpace(reference) ? DBNull.Value : reference);
                command.Parameters.AddWithValue("@Endianness", string.IsNullOrWhiteSpace(endianness) ? DBNull.Value : endianness);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_BYTE_SEQUENCE_INSERT_FAILED",
                    $"ImportRepository.InsertByteSequenceAsync: failed to insert byte sequence. {exception.Message}");
            }
        }

        public static async Task<Result> InsertSubSequenceAsync(SqliteConnection connection, SqliteTransaction transaction, string importSource,
            int espluqueSequenceId,
            int? minFragLength,
            int position,
            int? subSeqMaxOffset,
            int? subSeqMinOffset,
            string sequence)
        {
            if (connection == null || transaction == null)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_TRANSACTION_NOT_STARTED",
                    "The PRONOM import transaction has not been started.");
            }

            if (espluqueSequenceId <= 0)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_ESPLUQUE_SEQUENCE_ID_INVALID",
                    $"The Espluque sequence ID is invalid: {espluqueSequenceId}");
            }

            if (string.IsNullOrWhiteSpace(sequence))
            {
                return Result.Failure(
                    "PRONOM_IMPORT_SUB_SEQUENCE_SEQUENCE_EMPTY",
                    "The sub-sequence sequence is empty.");
            }

            try
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                command.CommandText =
                """
        INSERT INTO SubSequence
        (
            Source,
            EspluqueSequenceId,
            MinFragLength,
            Position,
            SubSeqMaxOffset,
            SubSeqMinOffset,
            Sequence
        )
        VALUES
        (
            @Source,
            @EspluqueSequenceId,
            @MinFragLength,
            @Position,
            @SubSeqMaxOffset,
            @SubSeqMinOffset,
            @Sequence
        );
        """;

                command.Parameters.AddWithValue("@Source", importSource);
                command.Parameters.AddWithValue("@EspluqueSequenceId", espluqueSequenceId);
                command.Parameters.AddWithValue("@MinFragLength", minFragLength.HasValue ? minFragLength.Value : DBNull.Value);
                command.Parameters.AddWithValue("@Position", position);
                command.Parameters.AddWithValue("@SubSeqMaxOffset", subSeqMaxOffset.HasValue ? subSeqMaxOffset.Value : DBNull.Value);
                command.Parameters.AddWithValue("@SubSeqMinOffset", subSeqMinOffset.HasValue ? subSeqMinOffset.Value : DBNull.Value);
                command.Parameters.AddWithValue("@Sequence", sequence);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_SUB_SEQUENCE_INSERT_FAILED",
                    $"ImportRepository.InsertSubSequenceAsync: failed to insert sub-sequence. {exception.Message}");
            }
        }

        public static async Task<Result> InsertFragmentAsync(SqliteConnection connection, SqliteTransaction transaction, string importSource,
            int espluqueSequenceId,
            int subSequencePosition,
            string leftRight,
            int maxOffset,
            int minOffset,
            int position,
            string value)
        {
            if (connection == null || transaction == null)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_TRANSACTION_NOT_STARTED",
                    "The PRONOM import transaction has not been started.");
            }

            if (espluqueSequenceId <= 0)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_ESPLUQUE_SEQUENCE_ID_INVALID",
                    $"The Espluque sequence ID is invalid: {espluqueSequenceId}");
            }

            if (subSequencePosition <= 0)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_SUB_SEQUENCE_POSITION_INVALID",
                    $"The sub-sequence position is invalid: {subSequencePosition}");
            }

            if (string.IsNullOrWhiteSpace(leftRight))
            {
                return Result.Failure(
                    "PRONOM_IMPORT_FRAGMENT_SIDE_EMPTY",
                    "The fragment side is empty.");
            }

            if (leftRight != "Left" && leftRight != "Right")
            {
                return Result.Failure(
                    "PRONOM_IMPORT_FRAGMENT_SIDE_INVALID",
                    $"The fragment side is invalid: {leftRight}");
            }

            if (position <= 0)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_FRAGMENT_POSITION_INVALID",
                    $"The fragment position is invalid: {position}");
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return Result.Failure(
                    "PRONOM_IMPORT_FRAGMENT_VALUE_EMPTY",
                    "The fragment value is empty.");
            }

            try
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                command.CommandText =
                """
        INSERT INTO Fragment
        (
            Source,
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
            @Source,
            @EspluqueSequenceId,
            @SubSequencePosition,
            @LeftRight,
            @MaxOffset,
            @MinOffset,
            @Position,
            @Value
        );
        """;

                command.Parameters.AddWithValue("@Source", importSource);
                command.Parameters.AddWithValue("@EspluqueSequenceId", espluqueSequenceId);
                command.Parameters.AddWithValue("@SubSequencePosition", subSequencePosition);
                command.Parameters.AddWithValue("@LeftRight", leftRight);
                command.Parameters.AddWithValue("@MaxOffset", maxOffset);
                command.Parameters.AddWithValue("@MinOffset", minOffset);
                command.Parameters.AddWithValue("@Position", position);
                command.Parameters.AddWithValue("@Value", value);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_FRAGMENT_INSERT_FAILED",
                    $"ImportRepository.InsertFragmentAsync: failed to insert fragment. {exception.Message}");
            }
        }
    }
}
