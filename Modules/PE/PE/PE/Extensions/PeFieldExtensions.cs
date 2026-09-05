using PE.Entities;
using PE.Enums;
using System.Globalization;
using Util;

namespace PE.Extensions
{
    internal static class PeFieldExtensions
    {
        public static string ToDisplayString(this PeField field)
        {
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
    }
}