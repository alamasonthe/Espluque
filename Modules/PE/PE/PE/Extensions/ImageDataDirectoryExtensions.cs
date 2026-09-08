using PE.Entities;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PE.Extensions
{
    internal static class ImageDataDirectoryExtensions
    {
        public static JsonObject ToJson(this ImageDataDirectory directory)
        {
            return new JsonObject
            {
                ["IsLoaded"] = directory._isLoaded,
                ["StructureStartOffset"] = directory.StructureStartOffset,
                ["Fields"] = JsonSerializer.SerializeToNode(directory._fields)
            };
        }
    }
}