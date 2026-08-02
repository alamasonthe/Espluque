using System.IO;
using System.Text.Json;

namespace SoftwarePackage.Mapper
{
    internal static class MapSource
    {
        internal static async Task<List<MapLine>> Load()
        {
            string moduleDirectory = Path.GetDirectoryName(typeof(Fusioner).Assembly.Location) ?? AppContext.BaseDirectory;
            string mappingFilePath = Path.Combine(moduleDirectory, "mappings.json");

            if (!File.Exists(mappingFilePath))
                return [];

            try
            {
                string json = await File.ReadAllTextAsync(mappingFilePath);
                return JsonSerializer.Deserialize<List<MapLine>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? [];
            }
            catch
            {
                return [];
            }
        }

    }
}
