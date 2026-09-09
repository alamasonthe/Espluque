using PE.Repositories;
using PE.Services;
using System.Text.Json;
using System.Text.Json.Nodes;
using Espluque.Contracts.CrossCutting;

namespace PE.Entities
{
    internal class PeSectionHeader: PeStructure
    {
        internal bool _isLoaded = false;
        internal PeField[] _fields = [];
        private readonly int _sectionHeaderIndex;

        #region Properties

        public PeField? Name { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(Name)); } }
        public PeField? VirtualSize { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(VirtualSize)); } }
        public PeField? VirtualAddress { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(VirtualAddress)); } }
        public PeField? SizeOfRawData { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(SizeOfRawData)); } }
        public PeField? PointerToRawData { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(PointerToRawData)); } }
        public PeField? PointerToRelocations { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(PointerToRelocations)); } }
        public PeField? PointerToLinenumbers { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(PointerToLinenumbers)); } }
        public PeField? NumberOfRelocations { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(NumberOfRelocations)); } }
        public PeField? NumberOfLinenumbers { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(NumberOfLinenumbers)); } }
        public PeField? Characteristics { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(Characteristics)); } }

        #endregion

        public PeSectionHeader(PeFile root, string filePath, int sectionHeaderIndex, ILogger logger, JsonObject? cache = null)
            : base(root, filePath, logger)
        {
            _sectionHeaderIndex = sectionHeaderIndex;
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
            object? peHeaderOffsetValue = Root?.GetValue("DosMzHeader.ELfanew");
            object? optionalHeaderSizeValue = Root?.GetValue("Header.CoffFileHeader.SizeOfOptionalHeader");
            if (peHeaderOffsetValue is null || optionalHeaderSizeValue is null)
                return false;
            _structureStartOffset = Convert.ToInt64(peHeaderOffsetValue) + 24 + Convert.ToInt64(optionalHeaderSizeValue) + (_sectionHeaderIndex * 40);
            return true;
        }

        private bool LoadStructureDefinition()
        {
            PeRepository repository = new();
            var fieldsResult = repository.GetFields("SectionHeader");
            if (!fieldsResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Failed to retrieve section header fields: {fieldsResult.Error?.Message}");
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
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Failed to read section header field {_fields[i].Name}: {result.Error?.Message}");
                    return false;
                }
                _fields[i] = result.Value!;
            }
            return true;
        }
    }
}
