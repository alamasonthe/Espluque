using Espluque.Contracts.Ports;
using Microsoft.Data.Sqlite;
using Util;

namespace DyneSqlite
{
    public class CategoryExtensionRepository : IDyneCategoryExtensionRepository
    {
        private readonly DbConnectionFactory _dbConnectionFactory;

        public CategoryExtensionRepository(DbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<Result<bool>> InsertAsync(string extension, string category)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return Result<bool>.Failure(
                    "DYNE_CATEGORY_EXTENSION_EMPTY_EXTENSION",
                    "CategoryExtensionRepository.InsertAsync: extension is empty.");
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                return Result<bool>.Failure(
                    "DYNE_CATEGORY_EXTENSION_EMPTY_CATEGORY",
                    "CategoryExtensionRepository.InsertAsync: category is empty.");
            }

            try
            {
                using SqliteConnection connection = _dbConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
                    INSERT INTO "dyne-category-extension" (
                        Extension,
                        Category
                    )
                    VALUES (
                        @Extension,
                        @Category
                    )
                    ON CONFLICT(Extension, Category) DO NOTHING;
                    """;

                command.Parameters.AddWithValue("@Extension", extension);
                command.Parameters.AddWithValue("@Category", category);

                int affectedRows = await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(affectedRows > 0);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(
                    "DYNE_CATEGORY_EXTENSION_INSERT_FAILED",
                    $"CategoryExtensionRepository.InsertAsync: failed to insert association '{extension}' - '{category}'. {ex.Message}");
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
            FROM "dyne-category-extension";
            """;

                object? result = await command.ExecuteScalarAsync();

                int count = Convert.ToInt32(result);

                return Result<int>.Success(count);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(
                    "DYNE_CATEGORY_EXTENSION_COUNT_FAILED",
                    $"CategoryExtensionRepository.CountAsync: failed to count category-extension associations. {ex.Message}");
            }
        }
    }
}