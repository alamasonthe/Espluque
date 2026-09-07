using Espluque.Contracts.CrossCutting;
using PE.Repositories;
using PE.Services;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PE.Entities
{
    internal class PeDosStub : PeStructure
    {
        internal bool _isLoaded = false;
        internal PeField[] _fields = [];
        internal List<PeDosRelocation> _relocations = [];

        public PeField? LoadModule
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(LoadModule));
            }
        }

        public PeField? LoadModuleOffset
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(LoadModuleOffset));
            }
        }

        public PeField? LoadModuleSize
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(LoadModuleSize));
            }
        }

        public PeField? RelocationCount
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(RelocationCount));
            }
        }

        public PeField? RelocationTableOffset
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(RelocationTableOffset));
            }
        }

        public PeField? DosMessage
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(DosMessage));
            }
        }

        public List<PeDosRelocation>? Relocations
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _relocations;
            }
        }

        public PeDosStub(PeFile root, string filePath, ILogger logger, JsonObject? cache = null)
            : base(root, filePath, logger)
        {
            if (cache is null)
                return;

            _isLoaded = cache["IsLoaded"]?.GetValue<bool>() ?? false;
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

                if (!LoadDynamicFields())
                    return false;

                if (!LoadStructureData())
                    return false;

                if (!LoadDerivedFields())
                    return false;

                _isLoaded = true;
            }

            return true;
        }

        private bool LoadStructureDefinition()
        {
            PeRepository repository = new();
            var fieldsResult = repository.GetFields("DosStub");

            if (!fieldsResult.IsSuccess)
            {
                _logger.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    $"Failed to retrieve DOS stub fields: {fieldsResult.Error?.Message}");

                return false;
            }

            _fields = fieldsResult.Value!;

            return true;
        }

        private bool LoadDynamicFields()
        {
            PeField? loadModule = _fields.FirstOrDefault(item => item.Name == nameof(LoadModule));
            PeField? loadModuleOffset = _fields.FirstOrDefault(item => item.Name == nameof(LoadModuleOffset));
            PeField? loadModuleSize = _fields.FirstOrDefault(item => item.Name == nameof(LoadModuleSize));
            PeField? relocationCount = _fields.FirstOrDefault(item => item.Name == nameof(RelocationCount));
            PeField? relocationTableOffset = _fields.FirstOrDefault(item => item.Name == nameof(RelocationTableOffset));

            if (loadModule is null ||
                loadModuleOffset is null ||
                loadModuleSize is null ||
                relocationCount is null ||
                relocationTableOffset is null)
                return false;

            object? ecparhdrValue = Root?.GetValue("DosMzHeader.ECparhdr");
            object? elfanewValue = Root?.GetValue("DosMzHeader.ELfanew");
            object? ecrlcValue = Root?.GetValue("DosMzHeader.ECrlc");
            object? elfarlcValue = Root?.GetValue("DosMzHeader.ELfarlc");

            if (ecparhdrValue is null ||
                elfanewValue is null ||
                ecrlcValue is null ||
                elfarlcValue is null)
                return false;

            int headerSize = Convert.ToUInt16(ecparhdrValue) * 16;
            int peHeaderOffset = Convert.ToInt32(elfanewValue);
            int size = peHeaderOffset - headerSize;

            if (size < 0)
                return false;

            loadModule.Offset = headerSize;
            loadModule.Size = size;

            loadModuleOffset.RawValue = BitConverter.GetBytes((uint)headerSize);
            loadModuleSize.RawValue = BitConverter.GetBytes((uint)size);
            relocationCount.RawValue = BitConverter.GetBytes((uint)Convert.ToUInt16(ecrlcValue));
            relocationTableOffset.RawValue = BitConverter.GetBytes((uint)Convert.ToUInt16(elfarlcValue));

            return true;
        }

        private bool LoadStructureData()
        {
            PeReader reader = new();

            for (int i = 0; i < _fields.Length; i++)
            {
                if (_fields[i].RawValue is not null)
                    continue;

                var result = reader.ReadField(_filePath, 0, _fields[i]);

                if (!result.IsSuccess)
                {
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Failed to read DOS stub field {_fields[i].Name}: {result.Error?.Message}");
                    return false;
                }

                _fields[i] = result.Value!;
            }

            foreach (PeDosRelocation relocation in _relocations)
            {
                if (relocation.Offset is null || relocation.Segment is null)
                    return false;
            }

            return true;
        }

        private bool LoadDerivedFields()
        {
            PeField? loadModule = _fields.FirstOrDefault(item => item.Name == nameof(LoadModule));
            PeField? dosMessage = _fields.FirstOrDefault(item => item.Name == nameof(DosMessage));

            if (loadModule?.RawValue is null || dosMessage is null)
                return false;

            byte[] data = loadModule.RawValue;

            int bestStart = -1;
            int bestLength = 0;
            int currentStart = -1;

            for (int i = 0; i <= data.Length; i++)
            {
                bool printable = i < data.Length &&
                    (data[i] is >= 0x20 and <= 0x7E ||
                     data[i] == 0x0D ||
                     data[i] == 0x0A ||
                     data[i] == 0x09);

                if (printable)
                {
                    if (currentStart < 0)
                        currentStart = i;
                }
                else if (currentStart >= 0)
                {
                    int length = i - currentStart;

                    if (length > bestLength)
                    {
                        bestStart = currentStart;
                        bestLength = length;
                    }

                    currentStart = -1;
                }
            }

            if (bestStart < 0)
                return true;

            byte[] message = data
                .Skip(bestStart)
                .Take(bestLength)
                .ToArray();

            while (message.Length > 0 &&
                   (message[^1] == (byte)'$' ||
                    message[^1] == 0x0D ||
                    message[^1] == 0x0A ||
                    message[^1] == 0x20))
            {
                message = message[..^1];
            }

            dosMessage.RawValue = message;

            return true;
        }
    }
}