using PE.Entities;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PE.Extensions
{
    internal static class PeDosRelocationExtensions
    {
        public static JsonObject ToJson(this PeDosRelocation relocation)
        {
            return new JsonObject
            {
                ["IsLoaded"] = relocation._isLoaded,
                ["StructureStartOffset"] = relocation.StructureStartOffset,
                ["Fields"] = JsonSerializer.SerializeToNode(relocation._fields)
            };
        }
    }
}