using Microsoft.Data.Sqlite;

namespace PronomSqlite
{
    public class DbConnectionFactory
    {

        private readonly string _connectionString;

        public DbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SqliteConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }
    }
}

