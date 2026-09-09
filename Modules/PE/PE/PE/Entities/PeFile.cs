using Espluque.Contracts.CrossCutting;
using PE.Services;
using System.Text.Json.Nodes;

namespace PE.Entities
{
    internal class PeFile : PeStructure
    {
        public PeDosMzHeader DosMzHeader { get; set; }
        public PeDosStub DosStub { get; set; }
        public PeHeader Header { get; set; }
        public List<PeSectionHeader> SectionTable { get; set; }
        public List<PeRsrcSection> Sections { get; set; }

        public PeFile(string filePath, string tempFolderPath, ILogger logger)
            : base(filePath, logger)
        {
            Root = this;
            string cacheFilePath = PeModulePaths.CacheFilePath(tempFolderPath);
            JsonObject? cache = null;
            if (File.Exists(cacheFilePath))
                cache = JsonNode.Parse(File.ReadAllText(cacheFilePath))?.AsObject();

            DosMzHeader = new PeDosMzHeader(this, filePath, logger, cache?["DosMzHeader"] as JsonObject);
            DosStub = new PeDosStub(this, filePath, logger, cache?["DosStub"] as JsonObject);
            Header = new PeHeader(this, filePath, logger, cache?["Header"] as JsonObject);

            SectionTable = [];
            object? sectionCountValue = GetValue("Header.CoffFileHeader.NumberOfSections");
            int sectionCount = sectionCountValue is null ? 0 : Convert.ToInt32(sectionCountValue);
            JsonArray? sectionTableCache = cache?["SectionTable"] as JsonArray;
            for (int i = 0; i < sectionCount; i++)
            {
                JsonObject? sectionHeaderCache = sectionTableCache?[i] as JsonObject;
                SectionTable.Add(new PeSectionHeader(this, filePath, i, logger, sectionHeaderCache));
            }

            Sections = [];
            for (int i = 0; i < sectionCount; i++)
            {
                object? sectionNameValue = GetValue($"SectionTable[{i}].Name");
                if (sectionNameValue is null)
                    continue;

                string sectionName = Convert.ToString(sectionNameValue)?.TrimEnd('\0') ?? string.Empty;
                if (sectionName != ".rsrc")
                    continue;

                Sections.Add(new PeRsrcSection(this, filePath, logger));
            }
        }
    }
}
