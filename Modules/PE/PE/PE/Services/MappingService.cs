using Tomlyn;
using Tomlyn.Model;

namespace PE.Services
{
    /// <summary>
    /// Loads and exposes the static PE value mappings defined in PeMappings.toml.
    /// </summary>
    /// <remarks>
    /// Mappings translate raw PE values into readable symbolic labels.
    /// The mapping file is loaded once when the service is created.
    /// </remarks>
    public class MappingsService
    {
        private readonly TomlTable _mappings;

        public MappingsService()
        {
            string moduleDirectory = Path.GetDirectoryName(typeof(MappingsService).Assembly.Location)!;
            string mappingFilePath = Path.Combine(moduleDirectory, "PeMappings.toml");

            string toml = File.ReadAllText(mappingFilePath);
            _mappings = TomlSerializer.Deserialize<TomlTable>(toml)!;
        }

        public string GetValue(string tableName, string code)
        {
            if (!_mappings.TryGetValue(tableName, out object? tableObject))
                return string.Empty;

            if (tableObject is not TomlTable table)
                return string.Empty;

            if (!table.TryGetValue(code, out object? value))
                return string.Empty;

            return value?.ToString() ?? string.Empty;
        }

        public string GetValue(string tableName, ulong code)
        {
            return GetValue(tableName, $"0x{code:X}");
        }
    }
}