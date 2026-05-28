using Espluque.Contracts.Ports;
using Microsoft.Data.Sqlite;
using Util;

namespace DyneSqlite
{
    public class CategoryRepository : IDyneCategoryRepository
    {
        private readonly DbConnectionFactory _dbConnectionFactory;

        public CategoryRepository(DbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<Result<bool>> InsertAsync(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return Result<bool>.Failure(
                    "DYNE_CATEGORY_EMPTY_CATEGORY",
                    "CategoryRepository.InsertAsync: category is empty.");
            }

            try
            {
                using SqliteConnection connection = _dbConnectionFactory.CreateConnection();
                await connection.OpenAsync();

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText = """
                    INSERT INTO "dyne-category" (
                        Category
                    )
                    VALUES (
                        @Category
                    )
                    ON CONFLICT(Category) DO NOTHING;
                    """;

                command.Parameters.AddWithValue("@Category", category);

                int affectedRows = await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(affectedRows > 0);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(
                    "DYNE_CATEGORY_INSERT_FAILED",
                    $"CategoryRepository.InsertAsync: failed to insert category '{category}'. {ex.Message}");
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
            FROM "dyne-category";
            """;

                object? result = await command.ExecuteScalarAsync();

                int count = Convert.ToInt32(result);

                return Result<int>.Success(count);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(
                    "DYNE_CATEGORY_COUNT_FAILED",
                    $"CategoryRepository.CountAsync: failed to count categories. {ex.Message}");
            }
        }
    }
}