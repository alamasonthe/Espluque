using PE.Entities;
using PE.Enums;
using Util;

namespace PE.Services
{
    internal class PeReader
    {
        /// <summary>
        /// Reads a single PE field from a file using its relative offset, size, and declared data type.
        /// </summary>
        /// <remarks>
        /// Returns a new PeField containing the original field definition,
        /// the raw bytes read from the file, and the converted value.
        /// </remarks>
        public Result<PeField> ReadField(string filePath, long structureOffset, PeField field)
        {
            long offset = structureOffset + field.Offset;
            var bytesResult = Bin.ReadBytesFromFile(filePath, offset, field.Size);

            if (!bytesResult.IsSuccess)
                return Result<PeField>.Failure(bytesResult.Error.Code, bytesResult.Error.Message);

            object? value;

            switch (field.Type)
            {
                case PeFieldType.Byte:
                    var byteResult = bytesResult.ToUInt8();
                    if (!byteResult.IsSuccess)
                        return Result<PeField>.Failure(byteResult.Error.Code, byteResult.Error.Message);
                    value = byteResult.Value;
                    break;

                case PeFieldType.UInt16:
                    var uint16Result = bytesResult.ToUInt16();
                    if (!uint16Result.IsSuccess)
                        return Result<PeField>.Failure(uint16Result.Error.Code, uint16Result.Error.Message);
                    value = uint16Result.Value;
                    break;

                case PeFieldType.UInt32:
                    var uint32Result = bytesResult.ToUInt32();
                    if (!uint32Result.IsSuccess)
                        return Result<PeField>.Failure(uint32Result.Error.Code, uint32Result.Error.Message);
                    value = uint32Result.Value;
                    break;

                case PeFieldType.UInt64:
                    var uint64Result = bytesResult.ToUInt64();
                    if (!uint64Result.IsSuccess)
                        return Result<PeField>.Failure(uint64Result.Error.Code, uint64Result.Error.Message);
                    value = uint64Result.Value;
                    break;

                case PeFieldType.Int16:
                    var int16Result = bytesResult.ToInt16();
                    if (!int16Result.IsSuccess)
                        return Result<PeField>.Failure(int16Result.Error.Code, int16Result.Error.Message);
                    value = int16Result.Value;
                    break;

                case PeFieldType.Int32:
                    var int32Result = bytesResult.ToInt32();
                    if (!int32Result.IsSuccess)
                        return Result<PeField>.Failure(int32Result.Error.Code, int32Result.Error.Message);
                    value = int32Result.Value;
                    break;

                case PeFieldType.Bytes:
                    value = bytesResult.Value;
                    break;

                case PeFieldType.AsciiString:
                    var asciiResult = bytesResult.ToAsciiString();
                    if (!asciiResult.IsSuccess)
                        return Result<PeField>.Failure(asciiResult.Error.Code, asciiResult.Error.Message);
                    value = asciiResult.Value;
                    break;

                case PeFieldType.Utf8String:
                    var utf8Result = bytesResult.ToUtf8String();
                    if (!utf8Result.IsSuccess)
                        return Result<PeField>.Failure(utf8Result.Error.Code, utf8Result.Error.Message);
                    value = utf8Result.Value;
                    break;

                case PeFieldType.Utf16String:
                    var utf16Result = bytesResult.ToUnicodeString();
                    if (!utf16Result.IsSuccess)
                        return Result<PeField>.Failure(utf16Result.Error.Code, utf16Result.Error.Message);
                    value = utf16Result.Value;
                    break;

                default:
                    return Result<PeField>.Failure("PE_FIELD_TYPE_UNSUPPORTED", $"Unsupported PE field type: {field.Type}");
            }

            PeField result = new()
            {
                Name = field.Name,
                Offset = field.Offset,
                Size = field.Size,
                Type = field.Type,
                MappingName = field.MappingName,
                DisplayFormat = field.DisplayFormat,
                RawValue = bytesResult.Value,
                Value = value
            };

            return Result<PeField>.Success(result);
        }

    }
}