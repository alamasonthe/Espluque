using Espluque.Contracts.CrossCutting;
using PE.Services;
using System.Text.Json.Nodes;

namespace PE.Entities
{
    internal class PeFile
    {
        public PeDosMzHeader DosMzHeader { get; set; }
        public PeDosStub DosStub { get; set; }
        public PeHeader Header { get; set; }
        public List<PeSectionHead> SectionTable { get; set; }
        public List<PeSection> Sections { get; set; }

        public PeFile(string filePath, string tempFolderPath, ILogger logger)
        {
            string cacheFilePath = PeModulePaths.CacheFilePath(tempFolderPath);

            JsonObject? cache = null;

            if (File.Exists(cacheFilePath))
                cache = JsonNode.Parse(File.ReadAllText(cacheFilePath))?.AsObject();

            DosMzHeader = new PeDosMzHeader( filePath, logger, cache?["DosMzHeader"] as JsonObject);
        }
    }
}
