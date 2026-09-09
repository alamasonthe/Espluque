using Espluque.Contracts.CrossCutting;
using PE.Enums;
using PE.Repositories;
using PE.Services;

namespace PE.Entities
{
    internal class PeRsrcDirectoryEntry : PeStructure
    {
        internal bool _isLoaded = false;
        internal PeField[] _fields = [];
        private PeRsrcDirectoryLevel _level { get; set; }
        private PeRsrcDirectoryTable? _childDirectoryTable;
        public PeRsrcDirectoryTable? ChildDirectoryTable { get { if (!EnsureLoaded()) return null; return _childDirectoryTable; } }
        private PeRsrcDirectoryString? _directoryString;
        private PeRsrcDataEntry? _dataEntry;
        public PeRsrcDataEntry? DataEntry { get { if (!EnsureLoaded()) return null; return _dataEntry; } }

        public PeField? Name { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(Name)); } }
        public PeField? OffsetToData { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(OffsetToData)); } }

        public PeRsrcDirectoryEntry(PeFile root, string filePath, long structureStartOffset, PeRsrcDirectoryLevel level, ILogger logger)
            : base(root, filePath, logger)
        {
            _structureStartOffset = structureStartOffset;
            _level = level;
        }

        private bool EnsureLoaded()
        {
            if (!_isLoaded)
            {
                if (!LoadStructureDefinition())
                    return false;
                if (!LoadStructureData())
                    return false;

                PeField? nameField = _fields.FirstOrDefault(item => item.Name == nameof(Name));
                if (nameField?.Value is null)
                    return false;
                if ((Convert.ToUInt32(nameField.Value) & 0x80000000) != 0)
                {
                    if (!CreateDirectoryString())
                        return false;
                }

                PeField? offsetToDataField = _fields.FirstOrDefault(item => item.Name == nameof(OffsetToData));
                if (offsetToDataField?.Value is null)
                    return false;
                if ((Convert.ToUInt32(offsetToDataField.Value) & 0x80000000) != 0)
                {
                    if (!CreateChildDirectoryTable())
                        return false;
                }
                else
                {
                    if (!CreateDataEntry())
                        return false;
                }

                _isLoaded = true;
            }
            return true;
        }

        private bool LoadStructureDefinition()
        {
            PeRepository repository = new();
            var fieldsResult = repository.GetFields("RsrcDirectoryEntry");
            if (!fieldsResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Failed to retrieve resource directory entry fields: {fieldsResult.Error?.Message}");
                return false;
            }
            _fields = fieldsResult.Value!;
            if (_level == PeRsrcDirectoryLevel.Type)
            {
                PeField? nameField = _fields.FirstOrDefault(item => item.Name == nameof(Name));
                if (nameField is not null)
                    nameField.MappingName = "ResourceType";
            }
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
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Failed to read resource directory entry field {_fields[i].Name}: {result.Error?.Message}");
                    return false;
                }
                _fields[i] = result.Value!;
            }
            return true;
        }

        private bool CreateDirectoryString()
        {
            PeField? nameField = _fields.FirstOrDefault(item => item.Name == nameof(Name));
            if (nameField?.Value is null)
                return false;

            uint nameValue = Convert.ToUInt32(nameField.Value);
            if ((nameValue & 0x80000000) == 0)
                return false;

            uint directoryStringOffset = nameValue & 0x7FFFFFFF;

            object? sectionCountValue = Root?.GetValue("Header.CoffFileHeader.NumberOfSections");
            if (sectionCountValue is null)
                return false;

            int sectionCount = Convert.ToInt32(sectionCountValue);
            for (int i = 0; i < sectionCount; i++)
            {
                object? sectionNameValue = Root?.GetValue($"SectionTable[{i}].Name");
                if (sectionNameValue is null)
                    continue;

                string sectionName = Convert.ToString(sectionNameValue)?.TrimEnd('\0') ?? string.Empty;
                if (sectionName != ".rsrc")
                    continue;

                object? pointerToRawDataValue = Root?.GetValue($"SectionTable[{i}].PointerToRawData");
                if (pointerToRawDataValue is null)
                    return false;

                long rsrcStartOffset = Convert.ToInt64(pointerToRawDataValue);
                _directoryString = new PeRsrcDirectoryString(Root!, _filePath, rsrcStartOffset + directoryStringOffset, _logger);
                return true;
            }

            return false;
        }

        private bool CreateDataEntry()
        {
            PeField? offsetToDataField = _fields.FirstOrDefault(item => item.Name == nameof(OffsetToData));
            if (offsetToDataField?.Value is null)
                return false;

            uint offsetToDataValue = Convert.ToUInt32(offsetToDataField.Value);
            if ((offsetToDataValue & 0x80000000) != 0)
                return false;

            uint dataEntryOffset = offsetToDataValue & 0x7FFFFFFF;

            object? sectionCountValue = Root?.GetValue("Header.CoffFileHeader.NumberOfSections");
            if (sectionCountValue is null)
                return false;

            int sectionCount = Convert.ToInt32(sectionCountValue);
            for (int i = 0; i < sectionCount; i++)
            {
                object? sectionNameValue = Root?.GetValue($"SectionTable[{i}].Name");
                if (sectionNameValue is null)
                    continue;

                string sectionName = Convert.ToString(sectionNameValue)?.TrimEnd('\0') ?? string.Empty;
                if (sectionName != ".rsrc")
                    continue;

                object? pointerToRawDataValue = Root?.GetValue($"SectionTable[{i}].PointerToRawData");
                if (pointerToRawDataValue is null)
                    return false;

                long rsrcStartOffset = Convert.ToInt64(pointerToRawDataValue);
                _dataEntry = new PeRsrcDataEntry(Root!, _filePath, rsrcStartOffset + dataEntryOffset, _logger);
                return true;
            }

            return false;
        }

        private bool CreateChildDirectoryTable()
        {
            if (_level >= PeRsrcDirectoryLevel.Language)
                return false;

            PeField? offsetToDataField = _fields.FirstOrDefault(item => item.Name == nameof(OffsetToData));
            if (offsetToDataField?.Value is null)
                return false;

            uint offsetToData = Convert.ToUInt32(offsetToDataField.Value);
            if ((offsetToData & 0x80000000) == 0)
                return false;

            uint childDirectoryOffset = offsetToData & 0x7FFFFFFF;

            object? sectionCountValue = Root?.GetValue("Header.CoffFileHeader.NumberOfSections");
            if (sectionCountValue is null)
                return false;

            int sectionCount = Convert.ToInt32(sectionCountValue);
            long? rsrcStartOffset = null;

            for (int i = 0; i < sectionCount; i++)
            {
                object? nameValue = Root?.GetValue($"SectionTable[{i}].Name");
                if (nameValue is null)
                    continue;

                string sectionName = Convert.ToString(nameValue)?.TrimEnd('\0') ?? string.Empty;
                if (sectionName != ".rsrc")
                    continue;

                object? pointerToRawDataValue = Root?.GetValue($"SectionTable[{i}].PointerToRawData");
                if (pointerToRawDataValue is null)
                    return false;

                rsrcStartOffset = Convert.ToInt64(pointerToRawDataValue);
                break;
            }

            if (rsrcStartOffset is null)
                return false;

            PeRsrcDirectoryLevel childLevel = (PeRsrcDirectoryLevel)((int)_level + 1);
            _childDirectoryTable = new PeRsrcDirectoryTable(Root!, _filePath, rsrcStartOffset.Value + childDirectoryOffset, childLevel, _logger);
            return true;
        }
    }
}