using PE.Entities;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PE.Extensions
{
    internal static class PeDosMzHeaderExtensions
    {
        public static List<KeyValuePair<string, string>> ToGrabberList(this PeDosMzHeader header)
        {
            List<KeyValuePair<string, string>> result = [];

            if (header.Fields is null)
                return result;

            foreach (PeField item in header.Fields)
            {
                string value = item.ToDisplayString();

                result.Add(new KeyValuePair<string, string>(item.Name, value));
            }

            return result;
        }

        public static JsonObject ToJson(this PeDosMzHeader header)
        {
            return new JsonObject
            {
                ["IsLoaded"] = header._isLoaded,
                ["Fields"] = JsonSerializer.SerializeToNode(header._fields)
            };
        }
    }
}