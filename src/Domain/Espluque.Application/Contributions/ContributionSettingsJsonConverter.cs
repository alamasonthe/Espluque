using Espluque.Contracts.Contributions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Espluque.Application.Contributions
{
    /// <summary>
    /// Converts IContributionSettings values to and from JSON.
    /// </summary>
    /// <remarks>
    /// Deserialization creates ContributionSettings instances.
    /// Serialization preserves the concrete runtime type of the settings object.
    /// </remarks>

    internal class ContributionSettingsJsonConverter : JsonConverter<IContributionSettings>
    {
        public override IContributionSettings? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<ContributionSettings>(ref reader, options);
        }

        public override void Write(
            Utf8JsonWriter writer,
            IContributionSettings value,
            JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
