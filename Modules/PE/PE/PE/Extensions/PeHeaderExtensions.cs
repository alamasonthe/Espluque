using PE.Entities;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PE.Extensions
{
    internal static class PeHeaderExtensions
    {
        public static List<KeyValuePair<string, string>> ToGrabberList(this PeHeader header)
        {
            List<KeyValuePair<string, string>> result = [];
            if (header.PeSignature is not null)
                result.Add(new KeyValuePair<string, string>(nameof(header.PeSignature), header.PeSignature.ToDisplayString()));
            if (header.CoffFileHeader is not null)
            {
                foreach (KeyValuePair<string, string> item in header.CoffFileHeader.ToGrabberList())
                    result.Add(new KeyValuePair<string, string>($"CoffFileHeader.{item.Key}", item.Value));
            }
            if (header.OptionalHeader is not null)
            {
                foreach (KeyValuePair<string, string> item in header.OptionalHeader.ToGrabberList())
                    result.Add(new KeyValuePair<string, string>($"OptionalHeader.{item.Key}", item.Value));
            }
            return result;
        }

        public static JsonObject ToJson(this PeHeader header)
        {
            return new JsonObject
            {
                ["IsLoaded"] = header._isLoaded,
                ["StructureStartOffset"] = header.StructureStartOffset,
                ["Fields"] = JsonSerializer.SerializeToNode(header._fields),
                ["CoffFileHeader"] = header._coffFileHeader?.ToJson(),
                ["OptionalHeader"] = header._optionalHeader?.ToJson()
            };
        }
    }
}