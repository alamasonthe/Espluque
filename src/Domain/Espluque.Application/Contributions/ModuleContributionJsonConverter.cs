using Espluque.Contracts.Contributions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Espluque.Application.Contributions
{
    /// <summary>
    /// Converts IModuleContributionInfo values to and from JSON.
    /// </summary>
    /// <remarks>
    /// Used when reading contribution definitions from module JSON descriptors where IModuleInfo exposes contributions through IModuleContributionInfo.
    /// Deserialization creates ModuleContributionInfo instances; serialization preserves the concrete runtime type.
    /// </remarks>

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
