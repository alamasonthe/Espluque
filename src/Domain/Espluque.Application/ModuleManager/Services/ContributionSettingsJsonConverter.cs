using Espluque.Application.ModuleManager.Entities;
using Espluque.Contracts.ModuleInterfaces;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Espluque.Application.ModuleManager.Services
{
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
