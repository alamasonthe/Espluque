using Espluque.Application.ModuleManager.Entities;
using Espluque.Contracts.ModuleInterfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Espluque.Application.ModuleManager
{
    internal class ModuleContributionJsonConverter : JsonConverter<IModuleContributionInfo>
    {
        public override IModuleContributionInfo? Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<ModuleContributionInfo>( ref reader, options);
        }

        public override void Write( Utf8JsonWriter writer, IModuleContributionInfo value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize( writer, value, value.GetType(), options);
        }
    }
}
