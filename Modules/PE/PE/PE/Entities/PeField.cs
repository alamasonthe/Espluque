using PE.Enums;

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
        public object? Value { get; set; }

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
