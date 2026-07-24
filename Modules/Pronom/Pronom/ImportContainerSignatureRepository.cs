using Microsoft.Data.Sqlite;
using Util;

namespace Pronom
{
    public static class ImportContainerSignatureRepository
    {
        public static async Task<Result> InsertContainerSignatureAsync(
            SqliteTransaction? transaction,
            string importSource,
            int id,
            string containerType,
            string description,
            string puid)
        {
            var connection = transaction.Connection;
            if (connection == null || transaction == null)
            {
                return Result.Failure("PRONOM_IMPORT_TRANSACTION_NOT_STARTED", "The PRONOM import transaction has not been started.");
            }

            if (string.IsNullOrWhiteSpace(importSource))
            {
                return Result.Failure("PRONOM_IMPORT_SOURCE_NAME_EMPTY", "The source name is empty.");
            }

            if (id <= 0)
            {
                return Result.Failure("PRONOM_IMPORT_CONTAINER_SIGNATURE_ID_INVALID", $"The container signature ID is invalid: {id}");
            }

            if (string.IsNullOrWhiteSpace(containerType))
            {
                return Result.Failure("PRONOM_IMPORT_CONTAINER_TYPE_EMPTY", $"The container type is empty for container signature ID {id}.");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return Result.Failure("PRONOM_IMPORT_CONTAINER_SIGNATURE_DESCRIPTION_EMPTY", $"The container signature description is empty for container signature ID {id}.");
            }

            if (string.IsNullOrWhiteSpace(puid))
            {
                return Result.Failure("PRONOM_IMPORT_FILE_FORMAT_PUID_EMPTY", $"The file format PUID is empty for container signature ID {id}.");
            }

            try
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = """
INSERT INTO ContainerSignature
(
    Source,
    Id,
    ContainerType,
    Description,
    Puid
)
VALUES
(
    @Source,
    @Id,
    @ContainerType,
    @Description,
    @Puid
);
""";

                command.Parameters.AddWithValue("@Source", importSource);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@ContainerType", containerType);
                command.Parameters.AddWithValue("@Description", description);
                command.Parameters.AddWithValue("@Puid", puid);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure("PRONOM_IMPORT_CONTAINER_SIGNATURE_INSERT_FAILED", $"ImportContainerSignatureRepository.InsertContainerSignatureAsync: failed to insert container signature. {exception.Message}");
            }
        }

        public static async Task<Result> InsertContainerFileAsync(
            SqliteConnection? connection,
            SqliteTransaction? transaction,
            string importSource,
            int containerSignatureId,
            string path)
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

            if (containerSignatureId <= 0)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_CONTAINER_SIGNATURE_ID_INVALID",
                    $"The container signature ID is invalid: {containerSignatureId}");
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return Result.Failure(
                    "PRONOM_IMPORT_CONTAINER_FILE_PATH_EMPTY",
                    $"The container file path is empty for container signature ID {containerSignatureId}.");
            }

            try
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = """
    INSERT INTO ContainerFile
    (
        Source,
        ContainerSignatureId,
        Path
    )
    VALUES
    (
        @Source,
        @ContainerSignatureId,
        @Path
    );
    """;

                command.Parameters.AddWithValue("@Source", importSource);
                command.Parameters.AddWithValue("@ContainerSignatureId", containerSignatureId);
                command.Parameters.AddWithValue("@Path", path);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_CONTAINER_FILE_INSERT_FAILED",
                    $"ImportContainerSignatureRepository.InsertContainerFileAsync: failed to insert container file. {exception.Message}");
            }
        }

        public static async Task<Result> InsertContainerFileInternalSignatureAsync(
            SqliteTransaction? transaction,
            string importSource,
            int signatureIdForContainer,
            int containerSignatureId,
            string filePath)
        {
            var connection = transaction?.Connection;
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

            if (signatureIdForContainer <= 0)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_CONTAINER_INTERNAL_SIGNATURE_ID_INVALID",
                    $"The container internal signature ID is invalid: {signatureIdForContainer}");
            }

            if (containerSignatureId <= 0)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_CONTAINER_SIGNATURE_ID_INVALID",
                    $"The container signature ID is invalid: {containerSignatureId}");
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Result.Failure(
                    "PRONOM_IMPORT_CONTAINER_FILE_PATH_EMPTY",
                    $"The container file path is empty for container signature ID {containerSignatureId}.");
            }

            try
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = """
    INSERT INTO ContainerFile_InternalSignature
    (
        Source,
        SignatureIdForContainer,
        ContainerSignatureId,
        FilePath
    )
    VALUES
    (
        @Source,
        @SignatureIdForContainer,
        @ContainerSignatureId,
        @FilePath
    );
    """;

                command.Parameters.AddWithValue("@Source", importSource);
                command.Parameters.AddWithValue("@SignatureIdForContainer", signatureIdForContainer);
                command.Parameters.AddWithValue("@ContainerSignatureId", containerSignatureId);
                command.Parameters.AddWithValue("@FilePath", filePath);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure(
                    "PRONOM_IMPORT_CONTAINER_FILE_INTERNAL_SIGNATURE_INSERT_FAILED",
                    $"ImportContainerSignatureRepository.InsertContainerFileInternalSignatureAsync: failed to insert container file internal signature. {exception.Message}");
            }
        }

        public static async Task<Result> InsertContainerTriggerAsync(
            SqliteTransaction? transaction,
            string importSource,
            string containerType,
            string puid)
        {
            var connection = transaction?.Connection;
            if (connection == null || transaction == null)
            {
                return Result.Failure("PRONOM_IMPORT_TRANSACTION_NOT_STARTED", "The PRONOM import transaction has not been started.");
            }

            if (string.IsNullOrWhiteSpace(importSource))
            {
                return Result.Failure("PRONOM_IMPORT_SOURCE_NAME_EMPTY", "The source name is empty.");
            }

            try
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                command.CommandText = """
INSERT INTO ContainerTrigger
(
    Source,
    ContainerType,
    Puid
)
VALUES
(
    @Source,
    @ContainerType,
    @Puid
);
""";

                command.Parameters.AddWithValue("@Source", importSource);
                command.Parameters.AddWithValue("@ContainerType", containerType);
                command.Parameters.AddWithValue("@Puid", puid);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure("PRONOM_IMPORT_CONTAINER_TRIGGER_INSERT_FAILED", $"ImportContainerSignatureRepository.InsertContainerTriggerAsync: failed to insert container trigger. {exception.Message}");
            }
        }
    }
}
