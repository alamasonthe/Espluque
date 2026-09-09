using Espluque.Contracts.CrossCutting;
using PE.Entities;
using System.Xml.Linq;

namespace PE.Services
{
    internal class PeRsrcManifestService
    {
        private readonly PeFile _peFile;
        private readonly string _filePath;
        private readonly ILogger _logger;

        public PeRsrcManifestService(
            PeFile peFile,
            string filePath,
            ILogger logger)
        {
            _peFile = peFile;
            _filePath = filePath;
            _logger = logger;
        }

        public string? GetManifest(PeRsrcDataEntry dataEntry)
        {
            if (dataEntry.OffsetToData?.Value is null ||
                dataEntry.Size?.Value is null)
                return null;

            uint dataRva = Convert.ToUInt32(dataEntry.OffsetToData.Value);
            int dataSize = Convert.ToInt32(dataEntry.Size.Value);

            if (dataSize <= 0)
                return null;

            long? dataFileOffset = GetFileOffset(dataRva);
            if (dataFileOffset is null)
                return null;

            try
            {
                using FileStream stream = File.OpenRead(_filePath);

                if (dataFileOffset.Value + dataSize > stream.Length)
                    return null;

                stream.Position = dataFileOffset.Value;

                byte[] data = new byte[dataSize];
                stream.ReadExactly(data);

                using MemoryStream memoryStream = new(data);

                XDocument document = XDocument.Load(
                    memoryStream,
                    LoadOptions.None);

                return document.ToString(SaveOptions.None);
            }
            catch (Exception exception)
            {
                _logger.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    $"Failed to read PE manifest: {exception.Message}");

                return null;
            }
        }

        private long? GetFileOffset(uint rva)
        {
            object? sectionCountValue =
                _peFile.GetValue("Header.CoffFileHeader.NumberOfSections");

            if (sectionCountValue is null)
                return null;

            int sectionCount = Convert.ToInt32(sectionCountValue);

            for (int i = 0; i < sectionCount; i++)
            {
                object? virtualAddressValue =
                    _peFile.GetValue($"SectionTable[{i}].VirtualAddress");

                object? sizeOfRawDataValue =
                    _peFile.GetValue($"SectionTable[{i}].SizeOfRawData");

                object? pointerToRawDataValue =
                    _peFile.GetValue($"SectionTable[{i}].PointerToRawData");

                if (virtualAddressValue is null ||
                    sizeOfRawDataValue is null ||
                    pointerToRawDataValue is null)
                    continue;

                uint virtualAddress =
                    Convert.ToUInt32(virtualAddressValue);

                uint sizeOfRawData =
                    Convert.ToUInt32(sizeOfRawDataValue);

                if (rva < virtualAddress ||
                    rva >= (ulong)virtualAddress + sizeOfRawData)
                    continue;

                uint pointerToRawData =
                    Convert.ToUInt32(pointerToRawDataValue);

                return pointerToRawData + (rva - virtualAddress);
            }

            return null;
        }
    }
}