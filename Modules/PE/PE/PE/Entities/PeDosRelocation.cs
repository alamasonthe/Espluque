using Espluque.Contracts.CrossCutting;
using PE.Repositories;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PE.Entities
{
    internal class PeDosRelocation : PeStructure
    {
        internal bool _isLoaded = false;
        internal PeField[] _fields = [];

        internal PeField[]? Fields
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields;
            }
        }

        public PeField? Offset
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(Offset));
            }
        }

        public PeField? Segment
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(Segment));
            }
        }

        public PeDosRelocation(
            PeFile root,
            string filePath,
            long structureStartOffset,
            ILogger logger,
            JsonObject? cache = null)
            : base(root, filePath, logger)
        {
            _structureStartOffset = structureStartOffset;

            if (cache is null)
                return;

            _isLoaded = cache["IsLoaded"]?.GetValue<bool>() ?? false;
            _structureStartOffset = cache["StructureStartOffset"]?.GetValue<long>() ?? structureStartOffset;
            _fields = cache["Fields"] is JsonNode fieldsNode
                ? JsonSerializer.Deserialize<PeField[]>(fieldsNode.ToJsonString()) ?? []
                : [];
        }

        private bool EnsureLoaded()
        {
            if (!_isLoaded)
            {
                if (!LoadStructureDefinition())
                    return false;

                if (!LoadStructureData())
                    return false;

                _isLoaded = true;
            }

            return true;
        }

        private bool LoadStructureDefinition()
        {
            PeRepository repository = new();
            var fieldsResult = repository.GetFields("DosRelocation");

            if (!fieldsResult.IsSuccess)
            {
                _logger.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    $"Failed to retrieve DOS relocation fields: {fieldsResult.Error?.Message}");

                return false;
            }

            _fields = fieldsResult.Value!;

            return true;
        }

        private bool LoadStructureData()
        {
            if (_structureStartOffset is null)
                return false;

            PE.Services.PeReader reader = new();

            for (int i = 0; i < _fields.Length; i++)
            {
                var result = reader.ReadField(_filePath, _structureStartOffset.Value, _fields[i]);

                if (!result.IsSuccess)
                {
                    _logger.Log(
                        Microsoft.Extensions.Logging.LogLevel.Error,
                        $"Failed to read DOS relocation field {_fields[i].Name}: {result.Error?.Message}");

                    return false;
                }

                _fields[i] = result.Value!;
            }

            return true;
        }
    }
}