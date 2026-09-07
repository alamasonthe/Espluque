using PE.Enums;
using System.Text;
using System.Text.Json.Serialization;

namespace PE.Entities
{
    internal class PeField
    {
        public string Name { get; set; }
        public int Offset { get; set; }
        public int Size { get; set; }
        public PeFieldType Type { get; set; }
        public string? MappingName { get; set; }
        public byte[]? RawValue { get; set; }
        [JsonIgnore]
        public object? Value
        {
            get
            {
                if (RawValue is null)
                    return null;

                return Type switch
                {
                    PeFieldType.Byte => RawValue[0],
                    PeFieldType.UInt16 => BitConverter.ToUInt16(RawValue),
                    PeFieldType.UInt32 => BitConverter.ToUInt32(RawValue),
                    PeFieldType.UInt64 => BitConverter.ToUInt64(RawValue),
                    PeFieldType.Int16 => BitConverter.ToInt16(RawValue),
                    PeFieldType.Int32 => BitConverter.ToInt32(RawValue),
                    PeFieldType.Bytes => RawValue,
                    PeFieldType.AsciiString => Encoding.ASCII.GetString(RawValue),
                    PeFieldType.Utf8String => Encoding.UTF8.GetString(RawValue),
                    PeFieldType.Utf16String => Encoding.Unicode.GetString(RawValue),
                    _ => null
                };
            }
        }

        private PeFieldDisplayFormat? _displayFormat;

        public PeFieldDisplayFormat? DisplayFormat
        {
            get
            {
                if (_displayFormat is not null)
                    return _displayFormat.Value;

                return Type switch
                {
                    PeFieldType.Byte => PeFieldDisplayFormat.Hexadecimal,
                    PeFieldType.UInt16 => PeFieldDisplayFormat.Hexadecimal,
                    PeFieldType.UInt32 => PeFieldDisplayFormat.Hexadecimal,
                    PeFieldType.UInt64 => PeFieldDisplayFormat.Hexadecimal,
                    PeFieldType.Int16 => PeFieldDisplayFormat.Hexadecimal,
                    PeFieldType.Int32 => PeFieldDisplayFormat.Hexadecimal,
                    PeFieldType.Bytes => PeFieldDisplayFormat.HexBytes,
                    PeFieldType.AsciiString => PeFieldDisplayFormat.Text,
                    PeFieldType.Utf8String => PeFieldDisplayFormat.Text,
                    PeFieldType.Utf16String => PeFieldDisplayFormat.Text,
                    _ => PeFieldDisplayFormat.HexBytes
                };
            }
            set
            {
                _displayFormat = value;
            }
        }
    }
}
