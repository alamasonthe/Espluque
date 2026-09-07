using PE.Entities;
using System.Text.Json.Nodes;

namespace PE.Extensions
{
    internal static class PeFileExtensions
    {
        public static JsonObject ToJson(this PeFile peFile)
        {
            return new JsonObject
            {
                ["DosMzHeader"] = peFile.DosMzHeader?.ToJson()
            };
        }

        public static void SaveCache(this PeFile peFile, string tempFolderPath)
        {
            string assemblyName = typeof(PeFile).Assembly.GetName().Name!;
            string cacheFilePath = Path.Combine(tempFolderPath, $"{assemblyName}_pe_cache.json");

            JsonObject json = peFile.ToJson();
            File.WriteAllText(
                cacheFilePath,
                json.ToJsonString(new System.Text.Json.JsonSerializerOptions
                { WriteIndented = true }));
        }
    }
}
