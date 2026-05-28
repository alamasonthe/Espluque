using Microsoft.Data.Sqlite;
using Util;
using Espluque.Contracts.Ports;

namespace DyneSqlite
{
    public class ExtensionRepository : IDyneExtensionRepository
    {
        private readonly DbConnectionFactory _dbConnectionFactory;

        public ExtensionRepository(DbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<Result<bool>> UpsertAsync(string extension, string? openClose, string? textBinary)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return Result<bool>.Failure(
                    "DYNE_EXTENSION_EMPTY_EXTENSION",
                    "ExtensionRepository.UpsertAsync: extension is empty.");
            }

            try
            {
                using SqliteConnection connection = _dbConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
            INSERT INTO "dyne-extension" (
                Extension,
                OpenClose,
                TextBinary
            )
            VALUES (
                @Extension,
                @OpenClose,
                @TextBinary
            )
            ON CONFLICT(Extension) DO UPDATE SET
                OpenClose = excluded.OpenClose,
                TextBinary = excluded.TextBinary;
            """;

                command.Parameters.AddWithValue("@Extension", extension);
                command.Parameters.AddWithValue("@OpenClose", (object?)openClose ?? DBNull.Value);
                command.Parameters.AddWithValue("@TextBinary", (object?)textBinary ?? DBNull.Value);

                int affectedRows = await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(affectedRows > 0);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(
                    "DYNE_EXTENSION_UPSERT_FAILED",
                    $"ExtensionRepository.UpsertAsync: failed to upsert extension '{extension}'. {ex.Message}");
            }
        }

        public async Task<Result<int>> CountAsync()
        {
            try
            {
                using SqliteConnection connection = _dbConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
            SELECT COUNT(*)
            FROM "dyne-extension";
            """;

                object? result = await command.ExecuteScalarAsync();

                int count = Convert.ToInt32(result);

                return Result<int>.Success(count);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(
                    "DYNE_EXTENSION_COUNT_FAILED",
                    $"ExtensionRepository.CountAsync: failed to count extensions. {ex.Message}");
            }
        }

        public async Task<Result<bool>> InsertAsync(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return Result<bool>.Failure(
                    "DYNE_EXTENSION_EMPTY_EXTENSION",
                    "ExtensionRepository.InsertAsync: extension is empty.");
            }

            try
            {
                using SqliteConnection connection = _dbConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
            INSERT INTO "dyne-extension" (
                Extension
            )
            VALUES (
                @Extension
            )
            ON CONFLICT(Extension) DO NOTHING;
            """;

                command.Parameters.AddWithValue("@Extension", extension);

                int affectedRows = await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(affectedRows > 0);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(
                    "DYNE_EXTENSION_INSERT_FAILED",
                    $"ExtensionRepository.InsertAsync: failed to insert extension '{extension}'. {ex.Message}");
            }
        }

        public async Task<Result<List<KeyValuePair<string, string>>?>> GetInfosFromExtensionAsync(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return Result<List<KeyValuePair<string, string>>?>.Failure(
                    "DYNE_EXTENSION_EMPTY_EXTENSION",
                    "ExtensionRepository.GetInfosFromExtensionAsync: extension is empty.");
            }

            string searchedExtension = extension.Trim().TrimStart('.');

            try
            {
                using SqliteConnection connection = _dbConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
            SELECT
                "dyne-extension".Extension,
                "dyne-extension".OpenClose,
                "dyne-extension".TextBinary,
                "dyne-category-extension".Category
            FROM "dyne-extension"
            LEFT JOIN "dyne-category-extension"
                ON "dyne-category-extension".Extension = "dyne-extension".Extension
            WHERE "dyne-extension".Extension = @Extension COLLATE NOCASE
            ORDER BY "dyne-category-extension".Category;
            """;

                command.Parameters.AddWithValue("@Extension", searchedExtension);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                List<KeyValuePair<string, string>>? infos = null;
                int categoryIndex = 1;

                while (await reader.ReadAsync())
                {
                    if (infos is null)
                    {
                        infos = [];

                        infos.Add(new KeyValuePair<string, string>(
                            "dyne-extension.Extension",
                            reader.GetString(0)));

                        if (!reader.IsDBNull(1))
                        {
                            string openClose = reader.GetString(1);

                            if (openClose != "-")
                            {
                                infos.Add(new KeyValuePair<string, string>(
                                    "dyne-extension.OpenClose",
                                    openClose));
                            }
                        }

                        if (!reader.IsDBNull(2))
                        {
                            string textBinary = reader.GetString(2);

                            if (textBinary != "-")
                            {
                                infos.Add(new KeyValuePair<string, string>(
                                    "dyne-extension.TextBinary",
                                    textBinary));
                            }
                        }
                    }

                    if (!reader.IsDBNull(3))
                    {
                        infos.Add(new KeyValuePair<string, string>(
                            $"dyne-category-extension.Category[{categoryIndex}]",
                            reader.GetString(3)));

                        categoryIndex++;
                    }
                }

                return Result<List<KeyValuePair<string, string>>?>.Success(infos);
            }
            catch (Exception ex)
            {
                return Result<List<KeyValuePair<string, string>>?>.Failure(
                    "DYNE_EXTENSION_GET_INFOS_FAILED",
                    $"ExtensionRepository.GetInfosFromExtensionAsync: failed to get infos for extension '{extension}'. {ex.Message}");
            }
        }
    }
}