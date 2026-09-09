using Espluque.Contracts.CrossCutting;
using PE.Repositories;
using PE.Services;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PE.Entities
{
    internal class PeHeader : PeStructure
    {
        internal bool _isLoaded = false;
        internal PeField[] _fields = [];

        internal PeCoffFileHeader? _coffFileHeader;
        internal PeOptionalHeader? _optionalHeader;

        #region Properties

        public PeField? PeSignature
        {
            get
            {
                if (!EnsureLoaded())
                    return null;
                return _fields.First(item => item.Name == nameof(PeSignature));
            }
        }

        public PeCoffFileHeader? CoffFileHeader
        {
            get
            {
                if (!EnsureLoaded())
                    return null;
                return _coffFileHeader;
            }
        }

        public PeOptionalHeader? OptionalHeader
        {
            get
            {
                if (!EnsureLoaded())
                    return null;
                return _optionalHeader;
            }
        }

        #endregion

        public PeHeader(PeFile root, string filePath, ILogger logger, JsonObject? cache = null)
            : base(root, filePath, logger)
        {
            if (cache is null)
                return;
            _isLoaded = cache["IsLoaded"]?.GetValue<bool>() ?? false;
            _structureStartOffset = cache["StructureStartOffset"]?.GetValue<long>() ?? _structureStartOffset;
            _fields = cache["Fields"] is JsonNode fieldsNode
                ? JsonSerializer.Deserialize<PeField[]>(fieldsNode.ToJsonString()) ?? []
                : [];
            _coffFileHeader = new PeCoffFileHeader(root, filePath, logger, cache["CoffFileHeader"] as JsonObject);
            _optionalHeader = new PeOptionalHeader(root, filePath, logger, cache["OptionalHeader"] as JsonObject);
        }

        private bool EnsureLoaded()
        {
            if (!_isLoaded)
            {
                if (!LoadStructureOffset())
                    return false;
                if (!LoadStructureDefinition())
                    return false;
                if (!LoadStructureData())
                    return false;
                if (!LoadSubStructures())
                    return false;
                _isLoaded = true;
            }
            return true;
        }

        private bool LoadStructureOffset()
        {
            object? value = Root?.GetValue("DosMzHeader.ELfanew");
            if (value is null)
                return false;
            _structureStartOffset = Convert.ToInt64(value);
            return true;
        }

        private bool LoadStructureDefinition()
        {
            PeRepository repository = new();
            var fieldsResult = repository.GetFields("PeHeader");
            if (!fieldsResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Failed to retrieve PE header fields: {fieldsResult.Error?.Message}");
                return false;
            }
            _fields = fieldsResult.Value!;
            return true;
        }

        private bool LoadStructureData()
        {
            if (_structureStartOffset is null)
                return false;
            PeReader reader = new();
            for (int i = 0; i < _fields.Length; i++)
            {
                var result = reader.ReadField(_filePath, _structureStartOffset.Value, _fields[i]);
                if (!result.IsSuccess)
                {
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Failed to read PE header field {_fields[i].Name}: {result.Error?.Message}");
                    return false;
                }
                _fields[i] = result.Value!;
            }
            return true;
        }

        private bool LoadSubStructures()
        {
            if (_structureStartOffset is null)
                return false;
            _coffFileHeader = new PeCoffFileHeader(Root!, _filePath, _logger);
            _optionalHeader = new PeOptionalHeader(Root!, _filePath, _logger);
            return true;
        }
    }
}