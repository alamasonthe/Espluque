using Microsoft.Data.Sqlite;
using Util;

namespace Pronom
{
    public class ImportFileSignatureRepository
    {
        public static async Task<Result> InsertFileFormatAsync(
            SqliteTransaction? transaction,
            string importSource,
            int id,
            string? mimeType,
            string name,
            string puid,
            string? version)
        {
            var connection = transaction?.Connection;
            if (connection == null || transaction == null)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_TRANSACTION_NOT_STARTED",
                    "The PRONOM import transaction has not been started.");
            }

            if (id <= 0)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_ID_INVALID",
                    $"The file format ID is invalid: {id}");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return Result.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_NAME_EMPTY",
                    "The file format name is empty.");
            }

            if (string.IsNullOrWhiteSpace(puid))
            {
                return Result.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_PUID_EMPTY",
                    "The file format PUID is empty.");
            }

            try
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                command.CommandText =
                """
        INSERT INTO FileFormat
        (
            Source,
            Id,
            MIMEType,
            Name,
            Puid,
            Version
        )
        VALUES
        (
            @Source,
            @Id,
            @MIMEType,
            @Name,
            @Puid,
            @Version
        );
        """;

                command.Parameters.AddWithValue("@Source", importSource);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@MIMEType", string.IsNullOrWhiteSpace(mimeType) ? DBNull.Value : mimeType);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Puid", puid);
                command.Parameters.AddWithValue("@Version", string.IsNullOrWhiteSpace(version) ? DBNull.Value : version);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_INSERT_FAILED",
                    $"ImportRepository.InsertFileFormatAsync: failed to insert file format. {exception.Message}");
            }
        }

        public static async Task<Result> InsertFileFormatInternalSignatureAsync(
            SqliteConnection? connection,
            SqliteTransaction? transaction,
            string importSource,
            int fileFormatId,
            int internalSignatureId)
        {
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

            if (fileFormatId <= 0)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_ID_INVALID",
                    $"The file format ID is invalid: {fileFormatId}");
            }

            if (internalSignatureId <= 0)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_INTERNAL_SIGNATURE_ID_INVALID",
                    $"The internal signature ID is invalid: {internalSignatureId}");
            }

            try
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                command.CommandText =
                """
INSERT INTO FileFormat_InternalSignature
(
    Source,
    FileFormatId,
    FileFormatSignatureId
)
VALUES
(
    @Source,
    @FileFormatId,
    @FileFormatSignatureId
);
""";

                command.Parameters.AddWithValue("@Source", importSource);
                command.Parameters.AddWithValue("@FileFormatId", fileFormatId);
                command.Parameters.AddWithValue("@FileFormatSignatureId", internalSignatureId);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_INTERNAL_SIGNATURE_INSERT_FAILED",
                    $"ImportFileSignatureRepository.InsertFileFormatInternalSignatureAsync: failed to insert file format internal signature. {exception.Message}");
            }
        }

        public static async Task<Result> InsertExtensionAsync(
            SqliteConnection? connection,
            SqliteTransaction? transaction,
            string importSource,
            int fileFormatId,
            string extension)
        {
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

            if (fileFormatId <= 0)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_ID_INVALID",
                    $"The file format ID is invalid: {fileFormatId}");
            }

            if (string.IsNullOrWhiteSpace(extension))
            {
                return Result.Failure(
                    "PRONOM_IMPORT_EXTENSION_EMPTY",
                    "The extension is empty.");
            }

            try
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                command.CommandText =
                """
INSERT INTO Extension
(
    Source,
    FileFormatId,
    Extension
)
VALUES
(
    @Source,
    @FileFormatId,
    @Extension
);
""";

                command.Parameters.AddWithValue("@Source", importSource);
                command.Parameters.AddWithValue("@FileFormatId", fileFormatId);
                command.Parameters.AddWithValue("@Extension", extension);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_EXTENSION_INSERT_FAILED",
                    $"ImportFileSignatureRepository.InsertExtensionAsync: failed to insert extension. {exception.Message}");
            }
        }

        public static async Task<Result> InsertAnalyzePriorityAsync(
            SqliteConnection? connection,
            SqliteTransaction? transaction,
            string importSource,
            int fileFormatId,
            int hasPriorityOverFileFormatId)
        {
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

            if (fileFormatId <= 0)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_ID_INVALID",
                    $"The file format ID is invalid: {fileFormatId}");
            }

            if (hasPriorityOverFileFormatId <= 0)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_PRIORITY_FILE_FORMAT_ID_INVALID",
                    $"The priority file format ID is invalid: {hasPriorityOverFileFormatId}");
            }

            try
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                command.CommandText =
                """
INSERT INTO AnalyzePriority
(
    Source,
    FileFormatId,
    HasPriorityOverFileFormatID
)
VALUES
(
    @Source,
    @FileFormatId,
    @HasPriorityOverFileFormatID
);
""";

                command.Parameters.AddWithValue("@Source", importSource);
                command.Parameters.AddWithValue("@FileFormatId", fileFormatId);
                command.Parameters.AddWithValue("@HasPriorityOverFileFormatID", hasPriorityOverFileFormatId);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_ANALYZE_PRIORITY_INSERT_FAILED",
                    $"ImportFileSignatureRepository.InsertAnalyzePriorityAsync: failed to insert analyze priority. {exception.Message}");
            }
        }

    }
}
