using PE.Entities;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PE.Extensions
{
    internal static class PeDosStubExtensions
    {
        public static List<KeyValuePair<string, string>> ToGrabberList(this PeDosStub stub)
        {
            List<KeyValuePair<string, string>> result = [];

            if (stub.LoadModule is not null)
                result.Add(new KeyValuePair<string, string>(nameof(stub.LoadModule), stub.LoadModule.ToDisplayString()));

            if (stub.LoadModuleOffset is not null)
                result.Add(new KeyValuePair<string, string>(nameof(stub.LoadModuleOffset), stub.LoadModuleOffset.ToDisplayString()));

            if (stub.LoadModuleSize is not null)
                result.Add(new KeyValuePair<string, string>(nameof(stub.LoadModuleSize), stub.LoadModuleSize.ToDisplayString()));

            if (stub.RelocationCount is not null)
                result.Add(new KeyValuePair<string, string>(nameof(stub.RelocationCount), stub.RelocationCount.ToDisplayString()));

            if (stub.RelocationTableOffset is not null)
                result.Add(new KeyValuePair<string, string>(nameof(stub.RelocationTableOffset), stub.RelocationTableOffset.ToDisplayString()));

            if (stub.DosMessage is not null)
                result.Add(new KeyValuePair<string, string>(nameof(stub.DosMessage), stub.DosMessage.ToDisplayString()));

            if (stub.Relocations is null)
                return result;

            for (int i = 0; i < stub.Relocations.Count; i++)
            {
                PeDosRelocation relocation = stub.Relocations[i];

                if (relocation.Offset is not null)
                    result.Add(new KeyValuePair<string, string>($"Relocation[{i}].Offset", relocation.Offset.ToDisplayString()));

                if (relocation.Segment is not null)
                    result.Add(new KeyValuePair<string, string>($"Relocation[{i}].Segment", relocation.Segment.ToDisplayString()));
            }

            return result;
        }

        public static JsonObject ToJson(this PeDosStub stub)
        {
            return new JsonObject
            {
                ["IsLoaded"] = stub._isLoaded,
                ["Fields"] = JsonSerializer.SerializeToNode(stub._fields),
                ["Relocations"] = JsonSerializer.SerializeToNode(stub._relocations)
            };
        }
    }
}