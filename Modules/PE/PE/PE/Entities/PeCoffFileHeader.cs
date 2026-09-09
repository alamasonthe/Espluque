using Espluque.Contracts.CrossCutting;
using PE.Repositories;
using PE.Services;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PE.Entities
{
    internal class PeCoffFileHeader: PeStructure
    {
        internal bool _isLoaded = false;
        internal PeField[] _fields = [];

        #region Properties

        public PeField? Machine
        {
            get
            {
                if (!EnsureLoaded())
                    return null;
                return _fields.First(item => item.Name == nameof(Machine));
            }
        }

        public PeField? NumberOfSections
        {
            get
            {
                if (!EnsureLoaded())
                    return null;
                return _fields.First(item => item.Name == nameof(NumberOfSections));
            }
        }

        public PeField? TimeDateStamp
        {
            get
            {
                if (!EnsureLoaded())
                    return null;
                return _fields.First(item => item.Name == nameof(TimeDateStamp));
            }
        }

        public PeField? PointerToSymbolTable
        {
            get
            {
                if (!EnsureLoaded())
                    return null;
                return _fields.First(item => item.Name == nameof(PointerToSymbolTable));
            }
        }

        public PeField? NumberOfSymbols
        {
            get
            {
                if (!EnsureLoaded())
                    return null;
                return _fields.First(item => item.Name == nameof(NumberOfSymbols));
            }
        }

        public PeField? SizeOfOptionalHeader
        {
            get
            {
                if (!EnsureLoaded())
                    return null;
                return _fields.First(item => item.Name == nameof(SizeOfOptionalHeader));
            }
        }

        public PeField? Characteristics
        {
            get
            {
                if (!EnsureLoaded())
                    return null;
                return _fields.First(item => item.Name == nameof(Characteristics));
            }
        }

        #endregion

        public PeCoffFileHeader(PeFile root, string filePath, ILogger logger, JsonObject? cache = null)
            : base(root, filePath, logger)
        {
            if (cache is null)
                return;
            _isLoaded = cache["IsLoaded"]?.GetValue<bool>() ?? false;
            _structureStartOffset = cache["StructureStartOffset"]?.GetValue<long>() ?? _structureStartOffset;
            _fields = cache["Fields"] is JsonNode fieldsNode
                ? JsonSerializer.Deserialize<PeField[]>(fieldsNode.ToJsonString()) ?? []
                : [];
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

                _isLoaded = true;
            }

            return true;
        }

        private bool LoadStructureOffset()
        {
            object? value = Root?.GetValue("DosMzHeader.ELfanew");
            if (value is null)
                return false;
            _structureStartOffset = Convert.ToInt64(value) + 4;
            return true;
        }

        private bool LoadStructureDefinition()
        {
            PeRepository repository = new();
            var fieldsResult = repository.GetFields("CoffFileHeader");

            if (!fieldsResult.IsSuccess)
            {
                _logger.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    $"Failed to retrieve COFF file header fields: {fieldsResult.Error?.Message}");

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
                    _logger.Log(
                        Microsoft.Extensions.Logging.LogLevel.Error,
                        $"Failed to read COFF file header field {_fields[i].Name}: {result.Error?.Message}");

                    return false;
                }

                _fields[i] = result.Value!;
            }

            return true;
        }
    }
}
