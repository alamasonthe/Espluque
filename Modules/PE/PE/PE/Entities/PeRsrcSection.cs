using Espluque.Contracts.CrossCutting;
using PE.Enums;
using PE.Repositories;

namespace PE.Entities
{
    internal class PeRsrcSection : PeStructure
    {
        internal bool _isLoaded = false;
        internal PeRsrcDirectoryTable? _typeDirectoryTable;

        #region Properties

        public PeRsrcDirectoryTable? TypeDirectoryTable { get { if (!EnsureLoaded()) return null; return _typeDirectoryTable; } }

        #endregion

        public PeRsrcSection(PeFile root, string filePath, ILogger logger)
            : base(root, filePath, logger)
        {
        }

        #region Load
        private bool EnsureLoaded()
        {
            if (!_isLoaded)
            {
                if (!LoadStructureOffset())
                    return false;
                if (!LoadStructureData())
                    return false;

                _isLoaded = true;
            }

            return true;
        }

        private bool LoadStructureOffset()
        {
            object? resourceRvaValue = Root?.GetValue("Header.OptionalHeader.ResourceTable.VirtualAddress");
            if (resourceRvaValue is null)
                return false;

            uint resourceRva = Convert.ToUInt32(resourceRvaValue);
            if (resourceRva == 0)
                return false;

            object? sectionCountValue = Root?.GetValue("Header.CoffFileHeader.NumberOfSections");
            if (sectionCountValue is null)
                return false;

            int sectionCount = Convert.ToInt32(sectionCountValue);

            for (int i = 0; i < sectionCount; i++)
            {
                object? virtualAddressValue = Root?.GetValue($"SectionTable[{i}].VirtualAddress");
                object? virtualSizeValue = Root?.GetValue($"SectionTable[{i}].VirtualSize");
                object? sizeOfRawDataValue = Root?.GetValue($"SectionTable[{i}].SizeOfRawData");
                object? pointerToRawDataValue = Root?.GetValue($"SectionTable[{i}].PointerToRawData");

                if (virtualAddressValue is null ||
                    virtualSizeValue is null ||
                    sizeOfRawDataValue is null ||
                    pointerToRawDataValue is null)
                    continue;

                uint virtualAddress = Convert.ToUInt32(virtualAddressValue);
                uint virtualSize = Convert.ToUInt32(virtualSizeValue);
                uint sizeOfRawData = Convert.ToUInt32(sizeOfRawDataValue);
                uint sectionSize = Math.Max(virtualSize, sizeOfRawData);

                if (resourceRva < virtualAddress ||
                    resourceRva >= (ulong)virtualAddress + sectionSize)
                    continue;

                uint pointerToRawData = Convert.ToUInt32(pointerToRawDataValue);

                _structureStartOffset =
                    pointerToRawData + (resourceRva - virtualAddress);

                return true;
            }

            return false;
        }

        private bool LoadStructureData()
        {
            if (_structureStartOffset is null)
                return false;
            _typeDirectoryTable = new PeRsrcDirectoryTable(Root!, _filePath, _structureStartOffset.Value, PeRsrcDirectoryLevel.Type, _logger);
            return true;
        }

        #endregion

        public List<KeyValuePair<string, string>> GetRsrcStringGrabberList()
        {
            List<KeyValuePair<string, string>> result = [];

            const uint RT_STRING = 6;

            if (TypeDirectoryTable?.Entries is null)
                return result;

            PeRsrcDirectoryEntry? stringTypeEntry = TypeDirectoryTable.Entries
                .FirstOrDefault(entry => entry.Name?.Value is not null && Convert.ToUInt32(entry.Name.Value) == RT_STRING);

            if (stringTypeEntry?.ChildDirectoryTable?.Entries is null)
                return result;

            foreach (PeRsrcDirectoryEntry blockEntry in stringTypeEntry.ChildDirectoryTable.Entries)
            {
                if (blockEntry.Name?.Value is null || blockEntry.ChildDirectoryTable?.Entries is null)
                    continue;

                int blockId = Convert.ToInt32(blockEntry.Name.Value);
                if (blockId <= 0)
                    continue;

                foreach (PeRsrcDirectoryEntry languageEntry in blockEntry.ChildDirectoryTable.Entries)
                {
                    if (languageEntry.Name?.Value is null || languageEntry.DataEntry is null)
                        continue;

                    int languageId = Convert.ToInt32(languageEntry.Name.Value);
                    List<string> strings = languageEntry.DataEntry.GetRsrcStrings();

                    for (int i = 0; i < strings.Count; i++)
                    {
                        if (string.IsNullOrEmpty(strings[i]))
                            continue;

                        int stringId = ((blockId - 1) * 16) + i;
                        string key = $"{languageId} | {stringId}";

                        result.Add(new KeyValuePair<string, string>(key, strings[i]));
                    }
                }
            }

            return result;
        }

        public List<KeyValuePair<string, string>> GetRsrcDirectoryGrabberList()
        {
            List<KeyValuePair<string, string>> result = [];

            if (TypeDirectoryTable?.Entries is null)
                return result;

            PeRepository repository = new();
            var mappingResult = repository.GetMapTable("ResourceType");
            List<KeyValuePair<long, string>> mappings = mappingResult.IsSuccess
                ? mappingResult.Value!
                : [];

            int index = 0;

            foreach (PeRsrcDirectoryEntry entry in TypeDirectoryTable.Entries)
            {
                if (entry.Name?.Value is null)
                    continue;

                uint value = Convert.ToUInt32(entry.Name.Value);

                // Bit 31 = name in string form, therefore not a standard numeric Resource Type.
                if ((value & 0x80000000) != 0)
                    continue;

                long resourceType = value;
                string? label = mappings
                    .FirstOrDefault(item => item.Key == resourceType)
                    .Value;

                string displayValue = string.IsNullOrWhiteSpace(label)
                    ? resourceType.ToString()
                    : $"{resourceType} ({label})";

                result.Add(new KeyValuePair<string, string>(
                    $"ResourceType[{index}]",
                    displayValue));

                index++;
            }

            return result;
        }
    }
}