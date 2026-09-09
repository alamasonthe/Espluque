using PE.Entities;
using System.Text.Json.Nodes;

namespace PE.Extensions
{
    internal static class PeFileExtensions
    {
        public static JsonObject ToJson(this PeFile peFile)
        {
            JsonArray sectionTable = [];
            foreach (PeSectionHeader sectionHeader in peFile.SectionTable)
                sectionTable.Add(sectionHeader.ToJson());

            return new JsonObject
            {
                ["DosMzHeader"] = peFile.DosMzHeader?.ToJson(),
                ["DosStub"] = peFile.DosStub?.ToJson(),
                ["Header"] = peFile.Header?.ToJson(),
                ["SectionTable"] = sectionTable
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
