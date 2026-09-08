using Espluque.Contracts.CrossCutting;
using PE.Repositories;
using PE.Services;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PE.Entities
{
    internal class ImageDataDirectory : PeStructure
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

        public PeField? VirtualAddress
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(VirtualAddress));
            }
        }

        public PeField? Size
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(Size));
            }
        }

        public ImageDataDirectory( PeFile root, string filePath, long structureStartOffset, ILogger logger, JsonObject? cache = null)
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
                /*
                if (!LoadStructureOffset())
                    return false;
                */

                if (!LoadStructureDefinition())
                    return false;

                if (!LoadStructureData())
                    return false;

                _isLoaded = true;
            }

            return true;
        }

        /*
        private bool LoadStructureOffset()
        {
            object? peHeaderOffsetValue = Root?.GetValue("DosMzHeader.ELfanew");
            object? magicValue = Root?.GetValue("Header.OptionalHeader.Magic");
            if (peHeaderOffsetValue is null || magicValue is null)
                return false;

            long optionalHeaderOffset = Convert.ToInt64(peHeaderOffsetValue) + 24;
            int dataDirectoryStartOffset = Convert.ToUInt16(magicValue) switch
            {
                0x10B => 96,
                0x20B => 112,
                _ => -1
            };
            if (dataDirectoryStartOffset < 0)
                return false;

            _structureStartOffset =
                optionalHeaderOffset +
                dataDirectoryStartOffset +
                (_dataDirectoryTableEntryIndex * 8);

            return true;
        }
        */

        private bool LoadStructureDefinition()
        {
            PeRepository repository = new();
            var fieldsResult = repository.GetFields("ImageDataDirectory");

            if (!fieldsResult.IsSuccess)
            {
                _logger.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    $"Failed to retrieve IMAGE_DATA_DIRECTORY fields: {fieldsResult.Error?.Message}");

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
                var result = reader.ReadField(
                    _filePath,
                    _structureStartOffset.Value,
                    _fields[i]);

                if (!result.IsSuccess)
                {
                    _logger.Log(
                        Microsoft.Extensions.Logging.LogLevel.Error,
                        $"Failed to read IMAGE_DATA_DIRECTORY field {_fields[i].Name}: {result.Error?.Message}");

                    return false;
                }

                _fields[i] = result.Value!;
            }

            return true;
        }
    }
}