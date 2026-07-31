using Microsoft.Data.Sqlite;

namespace SqliteUtil
{
    public static class DbCommands
    {
        public static async Task<List<T>> QueryAsync<T>(SqliteConnection connection, string sql, Func<SqliteDataReader, T> map)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = sql;

            using SqliteDataReader reader = await command.ExecuteReaderAsync();

            List<T> values = [];

            while (await reader.ReadAsync())
            {
                values.Add(map(reader));
            }

            return values;
        }

        public static async Task<int> ExecuteAsync(SqliteTransaction transaction, string sql)
        {
            using SqliteCommand command = transaction.Connection!.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;

            return await command.ExecuteNonQueryAsync();
        }

        public static async Task<int> InsertAsync(SqliteTransaction transaction, string sql)
        {
            using SqliteCommand command = transaction.Connection!.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;

            object? result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }
}
