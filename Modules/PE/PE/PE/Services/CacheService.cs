using PE.Entities;
using SqliteUtil;

namespace PE.Services
{
    /// <summary>
    /// Provides a per-analysis SQLite cache for PE structural offsets.
    /// The cache stores offsets that are expensive or redundant to recalculate
    /// while processing the same Portable Executable file.
    /// </summary>
    /// <remarks>
    /// The cache database is created in the analysis temporary folder and is
    /// specific to the PE module. Only one PeOffsets record is stored at a time.
    /// </remarks>
    public class CacheService
    {
        private readonly string _sqliteCacheFilePath;

        public CacheService(string tempFolderPath)
        {
            string assemblyName = typeof(CacheService).Assembly.GetName().Name!;
            _sqliteCacheFilePath = Path.Combine(tempFolderPath, $"{assemblyName}_pe_offsets_cache.db");
        }

        private void CreateDb()
        {
            string sql =
            """
            CREATE TABLE IF NOT EXISTS PeOffsets
            (
                NtHeader        INTEGER NOT NULL,
                FileHeader      INTEGER NOT NULL,
                OptionalHeader  INTEGER NOT NULL,
                DataDirectory   INTEGER NOT NULL,
                SectionHeaders  INTEGER NOT NULL,
                ResourceSection INTEGER NOT NULL,
                ResourceSectionRva INTEGER NOT NULL
            );
            """;

            using var connection = DbConnectionFactory.CreateConnection(_sqliteCacheFilePath);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();

        }

        public PeOffsets Load()
        {
            if (!File.Exists(_sqliteCacheFilePath))
            {
                return new PeOffsets();
            }

            using var connection = DbConnectionFactory.CreateConnection(_sqliteCacheFilePath);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText =
            """
            SELECT
                NtHeader,
                FileHeader,
                OptionalHeader,
                DataDirectory,
                SectionHeaders,
                ResourceSection,
                ResourceSectionRva
            FROM PeOffsets
            LIMIT 1;
            """;

            using var reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return new PeOffsets();
            }

            return new PeOffsets
            {
                NtHeader = reader.GetInt64(0),
                FileHeader = reader.GetInt64(1),
                OptionalHeader = reader.GetInt64(2),
                DataDirectory = reader.GetInt64(3),
                SectionHeaders = reader.GetInt64(4),
                ResourceSection = reader.GetInt64(5),
                ResourceSectionRva = checked((uint)reader.GetInt64(6))
            };
        }

        public void Save(PeOffsets peOffsets)
        {
            if (!File.Exists(_sqliteCacheFilePath))
            {
                CreateDb();
            }

            using var connection = DbConnectionFactory.CreateConnection(_sqliteCacheFilePath);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            using var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM PeOffsets;";
            deleteCommand.ExecuteNonQuery();

            using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText =
            """
            INSERT INTO PeOffsets
            (
                NtHeader,
                FileHeader,
                OptionalHeader,
                DataDirectory,
                SectionHeaders,
                ResourceSection,
                ResourceSectionRva
            )
            VALUES
            (
                $ntHeader,
                $fileHeader,
                $optionalHeader,
                $dataDirectory,
                $sectionHeaders,
                $resourceSection,
                $resourceSectionRva
            );
            """;

            insertCommand.Parameters.AddWithValue("$ntHeader", peOffsets.NtHeader);
            insertCommand.Parameters.AddWithValue("$fileHeader", peOffsets.FileHeader);
            insertCommand.Parameters.AddWithValue("$optionalHeader", peOffsets.OptionalHeader);
            insertCommand.Parameters.AddWithValue("$dataDirectory", peOffsets.DataDirectory);
            insertCommand.Parameters.AddWithValue("$sectionHeaders", peOffsets.SectionHeaders);
            insertCommand.Parameters.AddWithValue("$resourceSection", peOffsets.ResourceSection);
            insertCommand.Parameters.AddWithValue("$resourceSectionRva", (long)peOffsets.ResourceSectionRva);

            insertCommand.ExecuteNonQuery();

            transaction.Commit();
        }
    }
}
