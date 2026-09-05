using Microsoft.Data.Sqlite;
using PE.Entities;
using PE.Enums;
using Util;

namespace PE.Repositories
{
    internal class PeRepository
    {
        private readonly string _dbFilePath;

        public PeRepository(string dbFilePath)
        {
            _dbFilePath = dbFilePath;
        }

        public Result<PeField[]> GetFields(string structureName)
        {
            try
            {
                List<PeField> fields = [];

                using SqliteConnection connection = SqliteUtil.DbConnectionFactory.CreateConnection(_dbFilePath);
                connection.Open();

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = """
                    SELECT
                        Name,
                        Offset,
                        Size,
                        Type,
                        MappingTable,
                        DisplayFormat
                    FROM PeField
                    WHERE StructureName = $structureName
                    ORDER BY Ordinal;
                    """;

                command.Parameters.AddWithValue("$structureName", structureName);

                using SqliteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    fields.Add(new PeField
                    {
                        Name = reader.GetString(0),
                        Offset = reader.GetInt32(1),
                        Size = reader.GetInt32(2),
                        Type = Enum.Parse<PeFieldType>(reader.GetString(3)),
                        MappingName = reader.IsDBNull(4) ? null : reader.GetString(4),
                        DisplayFormat = reader.IsDBNull(5)
                            ? null
                            : Enum.Parse<PeFieldDisplayFormat>(reader.GetString(5))
                    });
                }

                return Result<PeField[]>.Success(fields.ToArray());
            }
            catch (Exception exception)
            {
                return Result<PeField[]>.Failure("PE_FIELDS_READ_FAILED", exception.Message);
            }
        }
    }
}