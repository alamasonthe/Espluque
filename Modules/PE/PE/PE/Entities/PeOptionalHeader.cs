using Espluque.Contracts.CrossCutting;
using PE.Repositories;
using PE.Services;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PE.Entities
{
    internal class PeOptionalHeader : PeStructure
    {
        internal bool _isLoaded = false;
        internal PeField[] _fields = [];
        internal Dictionary<string, ImageDataDirectory> _dataDirectories = [];

        #region Properties

        public PeField? Magic
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(Magic));
            }
        }

        public PeField? MajorLinkerVersion
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(MajorLinkerVersion));
            }
        }

        public PeField? MinorLinkerVersion
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(MinorLinkerVersion));
            }
        }

        public PeField? SizeOfCode
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(SizeOfCode));
            }
        }

        public PeField? SizeOfInitializedData
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(SizeOfInitializedData));
            }
        }

        public PeField? SizeOfUninitializedData
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(SizeOfUninitializedData));
            }
        }

        public PeField? AddressOfEntryPoint
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(AddressOfEntryPoint));
            }
        }

        public PeField? BaseOfCode
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(BaseOfCode));
            }
        }

        public PeField? BaseOfData
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.FirstOrDefault(item => item.Name == nameof(BaseOfData));
            }
        }

        public PeField? ImageBase
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(ImageBase));
            }
        }

        public PeField? SectionAlignment
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(SectionAlignment));
            }
        }

        public PeField? FileAlignment
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(FileAlignment));
            }
        }

        public PeField? MajorOperatingSystemVersion
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(MajorOperatingSystemVersion));
            }
        }

        public PeField? MinorOperatingSystemVersion
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(MinorOperatingSystemVersion));
            }
        }

        public PeField? MajorImageVersion
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(MajorImageVersion));
            }
        }

        public PeField? MinorImageVersion
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(MinorImageVersion));
            }
        }

        public PeField? MajorSubsystemVersion
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(MajorSubsystemVersion));
            }
        }

        public PeField? MinorSubsystemVersion
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(MinorSubsystemVersion));
            }
        }

        public PeField? Win32VersionValue
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(Win32VersionValue));
            }
        }

        public PeField? SizeOfImage
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(SizeOfImage));
            }
        }

        public PeField? SizeOfHeaders
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(SizeOfHeaders));
            }
        }

        public PeField? CheckSum
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(CheckSum));
            }
        }

        public PeField? Subsystem
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(Subsystem));
            }
        }

        public PeField? DllCharacteristics
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(DllCharacteristics));
            }
        }

        public PeField? SizeOfStackReserve
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(SizeOfStackReserve));
            }
        }

        public PeField? SizeOfStackCommit
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(SizeOfStackCommit));
            }
        }

        public PeField? SizeOfHeapReserve
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(SizeOfHeapReserve));
            }
        }

        public PeField? SizeOfHeapCommit
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(SizeOfHeapCommit));
            }
        }

        public PeField? LoaderFlags
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(LoaderFlags));
            }
        }

        public PeField? NumberOfRvaAndSizes
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _fields.First(item => item.Name == nameof(NumberOfRvaAndSizes));
            }
        }

        public ImageDataDirectory? ExportTable
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _dataDirectories.GetValueOrDefault(nameof(ExportTable));
            }
        }

        public ImageDataDirectory? ImportTable
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _dataDirectories.GetValueOrDefault(nameof(ImportTable));
            }
        }

        public ImageDataDirectory? ResourceTable
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _dataDirectories.GetValueOrDefault(nameof(ResourceTable));
            }
        }

        public ImageDataDirectory? ExceptionTable
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _dataDirectories.GetValueOrDefault(nameof(ExceptionTable));
            }
        }

        public ImageDataDirectory? CertificateTable
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _dataDirectories.GetValueOrDefault(nameof(CertificateTable));
            }
        }

        public ImageDataDirectory? BaseRelocationTable
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _dataDirectories.GetValueOrDefault(nameof(BaseRelocationTable));
            }
        }

        public ImageDataDirectory? Debug
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _dataDirectories.GetValueOrDefault(nameof(Debug));
            }
        }

        public ImageDataDirectory? Architecture
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _dataDirectories.GetValueOrDefault(nameof(Architecture));
            }
        }

        public ImageDataDirectory? GlobalPtr
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _dataDirectories.GetValueOrDefault(nameof(GlobalPtr));
            }
        }

        public ImageDataDirectory? TLSTable
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _dataDirectories.GetValueOrDefault(nameof(TLSTable));
            }
        }

        public ImageDataDirectory? LoadConfigTable
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _dataDirectories.GetValueOrDefault(nameof(LoadConfigTable));
            }
        }

        public ImageDataDirectory? BoundImport
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _dataDirectories.GetValueOrDefault(nameof(BoundImport));
            }
        }

        public ImageDataDirectory? IAT
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _dataDirectories.GetValueOrDefault(nameof(IAT));
            }
        }

        public ImageDataDirectory? DelayImportDescriptor
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _dataDirectories.GetValueOrDefault(nameof(DelayImportDescriptor));
            }
        }

        public ImageDataDirectory? CLRRuntimeHeader
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _dataDirectories.GetValueOrDefault(nameof(CLRRuntimeHeader));
            }
        }

        public ImageDataDirectory? Reserved
        {
            get
            {
                if (!EnsureLoaded())
                    return null;

                return _dataDirectories.GetValueOrDefault(nameof(Reserved));
            }
        }

        #endregion

        public PeOptionalHeader(PeFile root, string filePath, ILogger logger, JsonObject? cache = null)
            : base(root, filePath, logger)
        {
            if (cache is null)
                return;

            _isLoaded = cache["IsLoaded"]?.GetValue<bool>() ?? false;
            _structureStartOffset = cache["StructureStartOffset"]?.GetValue<long>() ?? _structureStartOffset;
            _fields = cache["Fields"] is JsonNode fieldsNode
                ? JsonSerializer.Deserialize<PeField[]>(fieldsNode.ToJsonString()) ?? []
                : [];

            if (cache["DataDirectories"] is JsonObject dataDirectories)
                BuildDataDirectories(dataDirectories);
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

                BuildDataDirectories();

                _isLoaded = true;
            }

            return true;
        }

        private bool LoadStructureOffset()
        {
            object? value = Root?.GetValue("DosMzHeader.ELfanew");
            if (value is null)
                return false;
            _structureStartOffset = Convert.ToInt64(value) + 24;
            return true;
        }

        private bool LoadStructureDefinition()
        {
            if (_structureStartOffset is null)
                return false;

            PeRepository repository = new();

            var baseFieldsResult = repository.GetFields("OptionalHeader");

            if (!baseFieldsResult.IsSuccess)
            {
                _logger.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    $"Failed to retrieve optional header base fields: {baseFieldsResult.Error?.Message}");

                return false;
            }

            PeField? magicField = baseFieldsResult.Value!
                .FirstOrDefault(item => item.Name == nameof(Magic));

            if (magicField is null)
                return false;

            PeReader reader = new();
            var magicResult = reader.ReadField(
                _filePath,
                _structureStartOffset.Value,
                magicField);

            if (!magicResult.IsSuccess)
            {
                _logger.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    $"Failed to read optional header Magic: {magicResult.Error?.Message}");

                return false;
            }

            ushort magic = Convert.ToUInt16(magicResult.Value!.Value);

            string structureName = magic switch
            {
                0x10B => "OptionalHeader32",
                0x20B => "OptionalHeader64",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(structureName))
            {
                _logger.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    $"Unsupported optional header Magic: 0x{magic:X4}");

                return false;
            }

            var fieldsResult = repository.GetFields(structureName);

            if (!fieldsResult.IsSuccess)
            {
                _logger.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    $"Failed to retrieve {structureName} fields: {fieldsResult.Error?.Message}");

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
                        $"Failed to read optional header field {_fields[i].Name}: {result.Error?.Message}");

                    return false;
                }

                _fields[i] = result.Value!;
            }

            return true;
        }

        private void BuildDataDirectories(JsonObject? dataDirectoriesCache = null)
        {
            if (_structureStartOffset is null)
                return;

            PeField? magicField = _fields.FirstOrDefault(item => item.Name == nameof(Magic));
            if (magicField?.Value is null)
                return;

            int dataDirectoryStartOffset = Convert.ToUInt16(magicField.Value) switch
            {
                0x10B => 96,
                0x20B => 112,
                _ => 0
            };
            if (dataDirectoryStartOffset == 0)
                return;

            string[] directoryNames =
            [
                nameof(ExportTable),
                nameof(ImportTable),
                nameof(ResourceTable),
                nameof(ExceptionTable),
                nameof(CertificateTable),
                nameof(BaseRelocationTable),
                nameof(Debug),
                nameof(Architecture),
                nameof(GlobalPtr),
                nameof(TLSTable),
                nameof(LoadConfigTable),
                nameof(BoundImport),
                nameof(IAT),
                nameof(DelayImportDescriptor),
                nameof(CLRRuntimeHeader),
                nameof(Reserved)
            ];

            PeField? numberOfRvaAndSizesField =
                _fields.FirstOrDefault(item => item.Name == nameof(NumberOfRvaAndSizes));

            if (numberOfRvaAndSizesField?.Value is null)
                return;

            int directoryCount = Math.Min(
                Convert.ToInt32(numberOfRvaAndSizesField.Value),
                directoryNames.Length);

            for (int i = 0; i < directoryCount; i++)
            {
                long directoryOffset = _structureStartOffset.Value + dataDirectoryStartOffset + (i * 8);
                JsonObject? directoryCache = dataDirectoriesCache?[directoryNames[i]] as JsonObject;

                _dataDirectories[directoryNames[i]] =
                    new ImageDataDirectory(Root!, _filePath, directoryOffset, _logger, directoryCache);
            }
        }
    }
}