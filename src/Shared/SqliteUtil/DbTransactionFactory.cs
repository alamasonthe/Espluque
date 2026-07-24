using Microsoft.Data.Sqlite;

namespace SqliteUtil
{
    public class DbTransactionFactory
    {
        public static SqliteTransaction OpenTransaction(string dbFilePath)
        {
            var connection = DbConnectionFactory.CreateConnection(dbFilePath);
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
