using Microsoft.Data.Sqlite;

namespace SqliteUtil
{
    public static class DbConnectionFactory
    {
        public static SqliteConnection CreateConnection(string dbFilePath)
        {
            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = dbFilePath
            }.ToString();

            return new SqliteConnection(connectionString);
        }
    }
}
