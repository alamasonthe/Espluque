using PE.Entities;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PE.Extensions
{
    internal static class PeSectionHeaderExtensions
    {
        public static List<KeyValuePair<string, string>> ToGrabberList(this PeSectionHeader header)
        {
            List<KeyValuePair<string, string>> result = [];
            void AddField(string name, PeField? field)
            {
                if (field is not null)
                    result.Add(new KeyValuePair<string, string>(name, field.ToDisplayString()));
            }
            AddField(nameof(header.Name), header.Name);
            AddField(nameof(header.VirtualSize), header.VirtualSize);
            AddField(nameof(header.VirtualAddress), header.VirtualAddress);
            AddField(nameof(header.SizeOfRawData), header.SizeOfRawData);
            AddField(nameof(header.PointerToRawData), header.PointerToRawData);
            AddField(nameof(header.PointerToRelocations), header.PointerToRelocations);
            AddField(nameof(header.PointerToLinenumbers), header.PointerToLinenumbers);
            AddField(nameof(header.NumberOfRelocations), header.NumberOfRelocations);
            AddField(nameof(header.NumberOfLinenumbers), header.NumberOfLinenumbers);
            AddField(nameof(header.Characteristics), header.Characteristics);
            return result;
        }

        public static JsonObject ToJson(this PeSectionHeader header)
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