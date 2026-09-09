using Espluque.Contracts.CrossCutting;
using PE.Entities;
using System.Text;

namespace PE.Services
{
    internal class PeRsrcVersionInfoService
    {
        private readonly PeFile _peFile;
        private readonly string _filePath;
        private readonly ILogger _logger;

        public PeRsrcVersionInfoService(PeFile peFile, string filePath, ILogger logger)
        {
            _peFile = peFile;
            _filePath = filePath;
            _logger = logger;
        }

        public List<KeyValuePair<string, string>> GetVersionInfo(PeRsrcDataEntry dataEntry)
        {
            List<KeyValuePair<string, string>> result = [];

            if (dataEntry.OffsetToData?.Value is null ||
                dataEntry.Size?.Value is null)
                return result;

            uint dataRva = Convert.ToUInt32(dataEntry.OffsetToData.Value);
            int dataSize = Convert.ToInt32(dataEntry.Size.Value);

            long? dataFileOffset = GetFileOffset(dataRva);
            if (dataFileOffset is null)
                return result;

            try
            {
                using FileStream stream = File.OpenRead(_filePath);
                using BinaryReader reader = new(stream);

                long rootStart = dataFileOffset.Value;
                long rootEnd = rootStart + dataSize;

                if (!TryReadBlockHeader(
                    reader,
                    rootStart,
                    rootEnd,
                    out ushort rootLength,
                    out ushort rootValueLength,
                    out _,
                    out string rootKey,
                    out long rootValueStart,
                    out long rootBlockEnd))
                    return result;

                if (rootKey != "VS_VERSION_INFO")
                    return result;

                if (rootValueLength > 0)
                    ReadFixedFileInfo(
                        reader,
                        rootValueStart,
                        rootValueLength,
                        rootBlockEnd,
                        result);

                long childOffset = Align4(rootValueStart + rootValueLength);

                while (childOffset + 6 <= rootBlockEnd)
                {
                    if (!TryReadBlockHeader(
                        reader,
                        childOffset,
                        rootBlockEnd,
                        out ushort childLength,
                        out _,
                        out _,
                        out string childKey,
                        out _,
                        out _))
                        break;

                    if (childKey == "StringFileInfo")
                        ReadStringFileInfo(reader, childOffset, rootBlockEnd, result);
                    else if (childKey == "VarFileInfo")
                        ReadVarFileInfo(reader, childOffset, rootBlockEnd, result);

                    long nextOffset = Align4(childOffset + childLength);

                    if (nextOffset <= childOffset)
                        break;

                    childOffset = nextOffset;
                }
            }
            catch (Exception exception)
            {
                _logger.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    $"Failed to read VERSIONINFO resource: {exception.Message}");
            }

            return result;
        }

        private void ReadFixedFileInfo(
            BinaryReader reader,
            long valueStart,
            ushort valueLength,
            long blockEnd,
            List<KeyValuePair<string, string>> result)
        {
            const int fixedFileInfoSize = 52;

            if (valueLength < fixedFileInfoSize ||
                valueStart + fixedFileInfoSize > blockEnd)
                return;

            reader.BaseStream.Position = valueStart;

            uint signature = reader.ReadUInt32();
            uint structureVersion = reader.ReadUInt32();
            uint fileVersionMs = reader.ReadUInt32();
            uint fileVersionLs = reader.ReadUInt32();
            uint productVersionMs = reader.ReadUInt32();
            uint productVersionLs = reader.ReadUInt32();
            uint fileFlagsMask = reader.ReadUInt32();
            uint fileFlags = reader.ReadUInt32();
            uint fileOs = reader.ReadUInt32();
            uint fileType = reader.ReadUInt32();
            uint fileSubtype = reader.ReadUInt32();
            uint fileDateMs = reader.ReadUInt32();
            uint fileDateLs = reader.ReadUInt32();

            result.Add(new("Fixed.Signature", $"0x{signature:X8}"));
            result.Add(new("Fixed.StructureVersion", $"0x{structureVersion:X8}"));

            result.Add(new(
                "Fixed.FileVersion",
                $"{fileVersionMs >> 16}.{fileVersionMs & 0xFFFF}.{fileVersionLs >> 16}.{fileVersionLs & 0xFFFF}"));

            result.Add(new(
                "Fixed.ProductVersion",
                $"{productVersionMs >> 16}.{productVersionMs & 0xFFFF}.{productVersionLs >> 16}.{productVersionLs & 0xFFFF}"));

            result.Add(new("Fixed.FileFlagsMask", $"0x{fileFlagsMask:X8}"));
            result.Add(new("Fixed.FileFlags", $"0x{fileFlags:X8}"));
            result.Add(new("Fixed.FileOS", $"0x{fileOs:X8}"));
            result.Add(new("Fixed.FileType", $"0x{fileType:X8}"));
            result.Add(new("Fixed.FileSubtype", $"0x{fileSubtype:X8}"));
            result.Add(new("Fixed.FileDate", $"0x{fileDateMs:X8}{fileDateLs:X8}"));
        }

        private void ReadStringFileInfo(
            BinaryReader reader,
            long blockStart,
            long parentEnd,
            List<KeyValuePair<string, string>> result)
        {
            if (!TryReadBlockHeader(
                reader,
                blockStart,
                parentEnd,
                out ushort blockLength,
                out _,
                out _,
                out _,
                out long childOffset,
                out long blockEnd))
                return;

            childOffset = Align4(childOffset);

            while (childOffset + 6 <= blockEnd)
            {
                if (!TryReadBlockHeader(
                    reader,
                    childOffset,
                    blockEnd,
                    out ushort tableLength,
                    out _,
                    out _,
                    out string tableKey,
                    out long stringOffset,
                    out long tableEnd))
                    break;

                stringOffset = Align4(stringOffset);

                while (stringOffset + 6 <= tableEnd)
                {
                    if (!TryReadBlockHeader(
                        reader,
                        stringOffset,
                        tableEnd,
                        out ushort stringLength,
                        out ushort valueLength,
                        out ushort type,
                        out string key,
                        out long valueStart,
                        out long stringEnd))
                        break;

                    if (type == 1 && valueLength > 0)
                    {
                        int byteLength = valueLength * 2;

                        if (valueStart + byteLength <= stringEnd)
                        {
                            reader.BaseStream.Position = valueStart;

                            string value = Encoding.Unicode
                                .GetString(reader.ReadBytes(byteLength))
                                .TrimEnd('\0');

                            result.Add(new(
                                $"{tableKey} | {key}",
                                value));
                        }
                    }

                    long nextStringOffset = Align4(stringOffset + stringLength);

                    if (nextStringOffset <= stringOffset)
                        break;

                    stringOffset = nextStringOffset;
                }

                long nextTableOffset = Align4(childOffset + tableLength);

                if (nextTableOffset <= childOffset)
                    break;

                childOffset = nextTableOffset;
            }
        }

        private void ReadVarFileInfo(
            BinaryReader reader,
            long blockStart,
            long parentEnd,
            List<KeyValuePair<string, string>> result)
        {
            if (!TryReadBlockHeader(
                reader,
                blockStart,
                parentEnd,
                out _,
                out _,
                out _,
                out _,
                out long varOffset,
                out long blockEnd))
                return;

            varOffset = Align4(varOffset);
            int index = 0;

            while (varOffset + 6 <= blockEnd)
            {
                if (!TryReadBlockHeader(
                    reader,
                    varOffset,
                    blockEnd,
                    out ushort varLength,
                    out ushort valueLength,
                    out _,
                    out string key,
                    out long valueStart,
                    out long varEnd))
                    break;

                if (key == "Translation" &&
                    valueLength >= 4 &&
                    valueStart + valueLength <= varEnd)
                {
                    reader.BaseStream.Position = valueStart;
                    int translationCount = valueLength / 4;

                    for (int i = 0; i < translationCount; i++)
                    {
                        ushort languageId = reader.ReadUInt16();
                        ushort codePage = reader.ReadUInt16();

                        result.Add(new(
                            $"Translation[{index}]",
                            $"{languageId:X4} | {codePage:X4}"));

                        index++;
                    }
                }

                long nextOffset = Align4(varOffset + varLength);

                if (nextOffset <= varOffset)
                    break;

                varOffset = nextOffset;
            }
        }

        private static bool TryReadBlockHeader(
            BinaryReader reader,
            long blockStart,
            long parentEnd,
            out ushort blockLength,
            out ushort valueLength,
            out ushort type,
            out string key,
            out long valueStart,
            out long blockEnd)
        {
            blockLength = 0;
            valueLength = 0;
            type = 0;
            key = string.Empty;
            valueStart = 0;
            blockEnd = 0;

            if (blockStart + 6 > parentEnd)
                return false;

            reader.BaseStream.Position = blockStart;

            blockLength = reader.ReadUInt16();
            valueLength = reader.ReadUInt16();
            type = reader.ReadUInt16();

            if (blockLength < 6)
                return false;

            blockEnd = blockStart + blockLength;

            if (blockEnd > parentEnd)
                return false;

            key = ReadUnicodeNullTerminated(reader, blockEnd);
            valueStart = Align4(reader.BaseStream.Position);

            return valueStart <= blockEnd;
        }

        private static string ReadUnicodeNullTerminated(
            BinaryReader reader,
            long endOffset)
        {
            StringBuilder result = new();

            while (reader.BaseStream.Position + 2 <= endOffset)
            {
                ushort character = reader.ReadUInt16();

                if (character == 0)
                    break;

                result.Append((char)character);
            }

            return result.ToString();
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

                uint virtualAddress = Convert.ToUInt32(virtualAddressValue);
                uint sizeOfRawData = Convert.ToUInt32(sizeOfRawDataValue);

                if (rva < virtualAddress ||
                    rva >= (ulong)virtualAddress + sizeOfRawData)
                    continue;

                uint pointerToRawData =
                    Convert.ToUInt32(pointerToRawDataValue);

                return pointerToRawData + (rva - virtualAddress);
            }

            return null;
        }

        private static long Align4(long offset)
        {
            return (offset + 3) & ~3L;
        }
    }
}