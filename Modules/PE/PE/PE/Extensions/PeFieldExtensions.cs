using PE.Entities;
using PE.Enums;
using PE.Repositories;
using System.Globalization;
using Util;

namespace PE.Extensions
{
    internal static class PeFieldExtensions
    {
        public static string ToDisplayString(this PeField field)
        {
            if (!string.IsNullOrWhiteSpace(field.MappingName))
            {
                var peRepository = new PeRepository();
                var mapTableResult = peRepository.GetMapTable(field.MappingName);
                if (mapTableResult.IsSuccess)
                {
                    switch (field.DisplayFormat)
                    {
                        case PeFieldDisplayFormat.Flags:
                            return field.ToFlagString(mapTableResult.Value!);

                        case PeFieldDisplayFormat.Decimal:
                        case PeFieldDisplayFormat.Hexadecimal:
                            return field.ToMapString(mapTableResult.Value!);
                    }
                }
            }

            return field.DisplayFormat switch
            {
                PeFieldDisplayFormat.Decimal => field.ToDecimalString(),
                PeFieldDisplayFormat.Hexadecimal => field.ToHexadecimalString(),
                PeFieldDisplayFormat.Text => field.ToTextString(),
                PeFieldDisplayFormat.DateTime => field.ToDateTimeString(),
                PeFieldDisplayFormat.HexBytes => field.ToHexBytesString(),
                PeFieldDisplayFormat.Guid => field.ToGuidString(),
                _ => string.Empty
            };
        }

        #region Simple Display Format Methods

        public static string ToDecimalString(this PeField field)
        {
            if (field.Value is null)
                return string.Empty;

            return Convert.ToString(field.Value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        public static string ToHexadecimalString(this PeField field)
        {
            if (field.Value is null)
                return string.Empty;

            return field.Type switch
            {
                PeFieldType.Byte => $"0x{(byte)field.Value:X2}",
                PeFieldType.UInt16 => $"0x{(ushort)field.Value:X4}",
                PeFieldType.UInt32 => $"0x{(uint)field.Value:X8}",
                PeFieldType.UInt64 => $"0x{(ulong)field.Value:X16}",
                PeFieldType.Int16 => $"0x{unchecked((ushort)(short)field.Value):X4}",
                PeFieldType.Int32 => $"0x{unchecked((uint)(int)field.Value):X8}",
                _ => string.Empty
            };
        }

        public static string ToTextString(this PeField field)
        {
            return field.Value?.ToString() ?? string.Empty;
        }

        public static string ToDateTimeString(this PeField field)
        {
            if (field.Value is null)
                return string.Empty;

            try
            {
                long seconds = Convert.ToInt64(field.Value, CultureInfo.InvariantCulture);
                return DateTimeOffset.FromUnixTimeSeconds(seconds).ToString("O", CultureInfo.InvariantCulture);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string ToHexBytesString(this PeField field)
        {
            if (field.RawValue is null)
                return string.Empty;

            var result = Bin.FromBytes(field.RawValue).ToHexString();

            return result.IsSuccess
                ? result.Value ?? string.Empty
                : string.Empty;
        }

        public static string ToGuidString(this PeField field)
        {
            if (field.RawValue is null || field.RawValue.Length != 16)
                return string.Empty;

            return new Guid(field.RawValue).ToString();
        }

        #endregion

        #region Mapped Display Format Methods

        public static string ToFlagString(this PeField field, List<KeyValuePair<long, string>> mapTable)
        {
            if (field.Value is null)
                return string.Empty;

            ulong value = Convert.ToUInt64(field.Value, CultureInfo.InvariantCulture);
            int bitCount = field.Size * 8;

            string binaryValue = Convert.ToString((long)value, 2).PadLeft(bitCount, '0');

            List<string> flags = [];

            foreach (KeyValuePair<long, string> item in mapTable.OrderByDescending(item => item.Key))
            {
                ulong flag = Convert.ToUInt64(item.Key);

                if ((value & flag) == flag && !string.IsNullOrWhiteSpace(item.Value))
                    flags.Add(item.Value);
            }

            return flags.Count == 0
                ? binaryValue
                : $"{binaryValue} ({string.Join(", ", flags)})";
        }

        public static string ToMapString(this PeField field, List<KeyValuePair<long, string>> mapTable)
        {
            if (field.Value is null)
                return string.Empty;

            long value = Convert.ToInt64(field.Value, CultureInfo.InvariantCulture);

            foreach (KeyValuePair<long, string> item in mapTable)
            {
                if (item.Key == value)
                    return item.Value;
            }

            return field.DisplayFormat switch
            {
                PeFieldDisplayFormat.Decimal => field.ToDecimalString(),
                PeFieldDisplayFormat.Hexadecimal => field.ToHexadecimalString(),
                _ => string.Empty
            };
        }

        #endregion
    }
}