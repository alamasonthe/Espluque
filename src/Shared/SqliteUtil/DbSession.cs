using Microsoft.Data.Sqlite;

namespace SqliteUtil
{
    public static class DbSession
    {
        public static SqliteConnection CreateConnection(string dbFilePath)
        {
            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = dbFilePath
            }.ToString();

            return new SqliteConnection(connectionString);
        }

        public static SqliteTransaction OpenTransaction(string dbFilePath)
        {
            var connection = CreateConnection(dbFilePath);
            connection.Open();
            return connection.BeginTransaction();
        }

        public static void CloseTransaction(SqliteTransaction transaction, bool commit)
        {
            SqliteConnection? connection = transaction.Connection;

            try
            {
                if (commit)
                {
                    transaction.Commit();
                }
                else
                {
                    transaction.Rollback();
                }
            }
            finally
            {
                transaction.Dispose();
                connection?.Dispose();
            }
        }

    }
}
