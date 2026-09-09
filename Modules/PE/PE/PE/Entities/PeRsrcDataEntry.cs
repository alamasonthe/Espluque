using Espluque.Contracts.CrossCutting;
using PE.Repositories;
using PE.Services;
using System.Text;

namespace PE.Entities
{
    internal class PeRsrcDataEntry : PeStructure
    {
        internal bool _isLoaded = false;
        internal PeField[] _fields = [];

        public PeField? OffsetToData { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(OffsetToData)); } }
        public PeField? Size { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(Size)); } }
        public PeField? CodePage { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(CodePage)); } }
        public PeField? Reserved { get { if (!EnsureLoaded()) return null; return _fields.First(item => item.Name == nameof(Reserved)); } }

        public PeRsrcDataEntry(PeFile root, string filePath, long structureStartOffset, ILogger logger)
    : base(root, filePath, logger)
        {
            _structureStartOffset = structureStartOffset;
        }

        #region Load

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
            var fieldsResult = repository.GetFields("RsrcDataEntry");
            if (!fieldsResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Failed to retrieve resource data entry fields: {fieldsResult.Error?.Message}");
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
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Failed to read resource data entry field {_fields[i].Name}: {result.Error?.Message}");
                    return false;
                }
                _fields[i] = result.Value!;
            }
            return true;
        }

        #endregion

        public List<string> GetRsrcStrings()
        {
            List<string> result = [];

            if (OffsetToData?.Value is null || Size?.Value is null)
                return result;

            uint dataRva = Convert.ToUInt32(OffsetToData.Value);
            int dataSize = Convert.ToInt32(Size.Value);

            object? sectionCountValue = Root?.GetValue("Header.CoffFileHeader.NumberOfSections");
            if (sectionCountValue is null)
                return result;

            int sectionCount = Convert.ToInt32(sectionCountValue);
            long? dataFileOffset = null;

            for (int i = 0; i < sectionCount; i++)
            {
                object? virtualAddressValue = Root?.GetValue($"SectionTable[{i}].VirtualAddress");
                object? virtualSizeValue = Root?.GetValue($"SectionTable[{i}].VirtualSize");
                object? sizeOfRawDataValue = Root?.GetValue($"SectionTable[{i}].SizeOfRawData");
                object? pointerToRawDataValue = Root?.GetValue($"SectionTable[{i}].PointerToRawData");

                if (virtualAddressValue is null || virtualSizeValue is null || sizeOfRawDataValue is null || pointerToRawDataValue is null)
                    continue;

                uint virtualAddress = Convert.ToUInt32(virtualAddressValue);
                uint virtualSize = Convert.ToUInt32(virtualSizeValue);
                uint sizeOfRawData = Convert.ToUInt32(sizeOfRawDataValue);
                uint sectionSize = Math.Max(virtualSize, sizeOfRawData);

                if (dataRva < virtualAddress || dataRva >= virtualAddress + sectionSize)
                    continue;

                uint pointerToRawData = Convert.ToUInt32(pointerToRawDataValue);
                dataFileOffset = pointerToRawData + (dataRva - virtualAddress);
                break;
            }

            if (dataFileOffset is null)
                return result;

            try
            {
                using FileStream stream = File.OpenRead(_filePath);
                using BinaryReader reader = new(stream);

                stream.Position = dataFileOffset.Value;
                long dataEndOffset = dataFileOffset.Value + dataSize;

                for (int i = 0; i < 16; i++)
                {
                    if (stream.Position + 2 > dataEndOffset)
                        break;

                    ushort length = reader.ReadUInt16();
                    int byteLength = length * 2;

                    if (stream.Position + byteLength > dataEndOffset)
                        break;

                    byte[] bytes = reader.ReadBytes(byteLength);
                    result.Add(Encoding.Unicode.GetString(bytes));
                }
            }
            catch (Exception exception)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Failed to read RT_STRING resource data: {exception.Message}");
            }

            return result;
        }
    }
}