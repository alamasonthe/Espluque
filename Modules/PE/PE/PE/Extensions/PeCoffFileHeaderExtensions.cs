using PE.Entities;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PE.Extensions
{
    internal static class PeCoffFileHeaderExtensions
    {
        public static List<KeyValuePair<string, string>> ToGrabberList(this PeCoffFileHeader header)
        {
            List<KeyValuePair<string, string>> result = [];

            if (header.Machine is not null)
                result.Add(new KeyValuePair<string, string>(nameof(header.Machine), header.Machine.ToDisplayString()));
            if (header.NumberOfSections is not null)
                result.Add(new KeyValuePair<string, string>(nameof(header.NumberOfSections), header.NumberOfSections.ToDisplayString()));
            if (header.TimeDateStamp is not null)
                result.Add(new KeyValuePair<string, string>(nameof(header.TimeDateStamp), header.TimeDateStamp.ToDisplayString()));
            if (header.PointerToSymbolTable is not null)
                result.Add(new KeyValuePair<string, string>(nameof(header.PointerToSymbolTable), header.PointerToSymbolTable.ToDisplayString()));
            if (header.NumberOfSymbols is not null)
                result.Add(new KeyValuePair<string, string>(nameof(header.NumberOfSymbols), header.NumberOfSymbols.ToDisplayString()));
            if (header.SizeOfOptionalHeader is not null)
                result.Add(new KeyValuePair<string, string>(nameof(header.SizeOfOptionalHeader), header.SizeOfOptionalHeader.ToDisplayString()));
            if (header.Characteristics is not null)
                result.Add(new KeyValuePair<string, string>(nameof(header.Characteristics), header.Characteristics.ToDisplayString()));

            return result;
        }

        public static JsonObject ToJson(this PeCoffFileHeader header)
        {
            return new JsonObject
            {
                ["IsLoaded"] = header._isLoaded,
                ["StructureStartOffset"] = header.StructureStartOffset,
                ["Fields"] = JsonSerializer.SerializeToNode(header._fields)
            };
        }
    }
}