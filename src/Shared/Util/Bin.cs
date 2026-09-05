using System.Text;
using Util.Enums;

namespace Util
{
    public static class Bin
    {

        public static Result<byte[]> ReadBytesFromFile(string filepath, long offset, int size)
        {
            if (string.IsNullOrWhiteSpace(filepath))
            {
                return Result<byte[]>.Failure(
                    "FILEPATH_EMPTY",
                    "File path is empty.");
            }

            if (offset < 0)
            {
                return Result<byte[]>.Failure(
                    "OFFSET_INVALID",
                    "Offset cannot be negative.");
            }

            if (size < 0)
            {
                return Result<byte[]>.Failure(
                    "SIZE_INVALID",
                    "Size cannot be negative.");
            }

            try
            {
                FileInfo fileInfo = new FileInfo(filepath);

                if (!fileInfo.Exists)
                {
                    return Result<byte[]>.Failure(
                        "FILE_NOT_FOUND",
                        "File was not found.");
                }

                if (offset > fileInfo.Length)
                {
                    return Result<byte[]>.Failure(
                        "OFFSET_OUT_OF_RANGE",
                        "Offset is beyond the end of the file.");
                }

                int readableSize = (int)Math.Min(size, fileInfo.Length - offset);

                byte[] bytes = new byte[readableSize];

                using FileStream fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read);
                using BinaryReader reader = new BinaryReader(fileStream);

                reader.BaseStream.Seek(offset, SeekOrigin.Begin);

                int bytesRead = reader.Read(bytes, 0, readableSize);

                if (bytesRead != readableSize)
                {
                    return Result<byte[]>.Failure(
                        "FILE_BLOCK_READ_INCOMPLETE",
                        "The requested block was not fully read.");
                }

                return Result<byte[]>.Success(bytes);
            }
            catch (Exception exception)
            {
                return Result<byte[]>.Failure(
                    "FILE_READ_ERROR",
                    exception.Message);
            }
        }

        public static Result<TextBinaryEnum> DetectTextOrBinary(this Result<byte[]> bytesResult)
        {
            if (!bytesResult.IsSuccess)
            {
                if (bytesResult.Error is not null)
                {
                    return Result<TextBinaryEnum>.Failure(bytesResult.Error.Code, bytesResult.Error.Message);
                }

                return Result<TextBinaryEnum>.Failure("BINARY_RESULT_ERROR_MISSING", "Binary result failed without error details.");
            }

            if (bytesResult.Value is null)
            {
                return Result<TextBinaryEnum>.Failure("BINARY_RESULT_VALUE_MISSING", "Binary result succeeded without a byte array.");
            }

            byte[] bytes = bytesResult.Value;

            if (bytes.Length == 0)
            {
                return Result<TextBinaryEnum>.Success(TextBinaryEnum.Empty);
            }

            if (bytes.Length >= 3 &&
                bytes[0] == 0xEF &&
                bytes[1] == 0xBB &&
                bytes[2] == 0xBF)
            {
                return Result<TextBinaryEnum>.Success(TextBinaryEnum.Text);
            }

            if (bytes.Length >= 2 &&
                ((bytes[0] == 0xFF && bytes[1] == 0xFE) ||
                 (bytes[0] == 0xFE && bytes[1] == 0xFF)))
            {
                return Result<TextBinaryEnum>.Success(TextBinaryEnum.Text);
            }

            if (bytes.Length >= 4)
            {
                int pairCount = bytes.Length / 2;
                int littleEndianTextPairs = 0;
                int bigEndianTextPairs = 0;

                for (int i = 0; i + 1 < bytes.Length; i += 2)
                {
                    byte first = bytes[i];
                    byte second = bytes[i + 1];

                    bool firstIsText = first >= 32 || first == 9 || first == 10 || first == 13;
                    bool secondIsText = second >= 32 || second == 9 || second == 10 || second == 13;

                    if (second == 0 && firstIsText)
                    {
                        littleEndianTextPairs++;
                    }

                    if (first == 0 && secondIsText)
                    {
                        bigEndianTextPairs++;
                    }
                }

                double littleEndianTextPairRatio = (double)littleEndianTextPairs / pairCount;
                double bigEndianTextPairRatio = (double)bigEndianTextPairs / pairCount;

                if (littleEndianTextPairRatio > 0.90 || bigEndianTextPairRatio > 0.90)
                {
                    return Result<TextBinaryEnum>.Success(TextBinaryEnum.Text);
                }
            }

            int suspiciousControlBytes = 0;

            foreach (byte b in bytes)
            {
                if (b == 0)
                {
                    return Result<TextBinaryEnum>.Success(TextBinaryEnum.Binary);
                }

                bool isAllowedTextControl =
                    b == 9 ||
                    b == 10 ||
                    b == 13;

                if (b < 32 && !isAllowedTextControl)
                {
                    suspiciousControlBytes++;
                }
            }

            double suspiciousRatio = (double)suspiciousControlBytes / bytes.Length;

            if (suspiciousRatio > 0.05)
            {
                return Result<TextBinaryEnum>.Success(TextBinaryEnum.Binary);
            }

            return Result<TextBinaryEnum>.Success(TextBinaryEnum.Text);
        }

        public static Result<int> Size(this Result<byte[]> bytesResult)
        {
            if (!bytesResult.IsSuccess)
            {
                if (bytesResult.Error is not null)
                {
                    return Result<int>.Failure(
                        bytesResult.Error.Code,
                        bytesResult.Error.Message);
                }

                return Result<int>.Failure(
                    "BINARY_RESULT_ERROR_MISSING",
                    "Binary result failed without error details.");
            }

            if (bytesResult.Value is null)
            {
                return Result<int>.Failure(
                    "BINARY_RESULT_VALUE_MISSING",
                    "Binary result succeeded without a byte array.");
            }

            return Result<int>.Success(bytesResult.Value.Length);
        }

        public static Result<string> ToUtf8String(this Result<byte[]> bytesResult)
        {
            if (!bytesResult.IsSuccess)
            {
                if (bytesResult.Error is not null)
                {
                    return Result<string>.Failure(
                        bytesResult.Error.Code,
                        bytesResult.Error.Message);
                }

                return Result<string>.Failure(
                    "BINARY_RESULT_ERROR_MISSING",
                    "Binary result failed without error details.");
            }

            if (bytesResult.Value is null)
            {
                return Result<string>.Failure(
                    "BINARY_RESULT_VALUE_MISSING",
                    "Binary result succeeded without a byte array.");
            }

            return Result<string>.Success(Encoding.UTF8.GetString(bytesResult.Value));
        }

        public static Result<string> ToUnicodeString(this Result<byte[]> bytesResult)
        {
            if (!bytesResult.IsSuccess)
            {
                if (bytesResult.Error is not null)
                {
                    return Result<string>.Failure(
                        bytesResult.Error.Code,
                        bytesResult.Error.Message);
                }

                return Result<string>.Failure(
                    "BINARY_RESULT_ERROR_MISSING",
                    "Binary result failed without error details.");
            }

            if (bytesResult.Value is null)
            {
                return Result<string>.Failure(
                    "BINARY_RESULT_VALUE_MISSING",
                    "Binary result succeeded without a byte array.");
            }

            return Result<string>.Success(Encoding.Unicode.GetString(bytesResult.Value));
        }

        public static Result<string> ToAsciiString(this Result<byte[]> bytesResult)
        {
            if (!bytesResult.IsSuccess)
            {
                if (bytesResult.Error is not null)
                {
                    return Result<string>.Failure(
                        bytesResult.Error.Code,
                        bytesResult.Error.Message);
                }

                return Result<string>.Failure(
                    "BINARY_RESULT_ERROR_MISSING",
                    "Binary result failed without error details.");
            }

            if (bytesResult.Value is null)
            {
                return Result<string>.Failure(
                    "BINARY_RESULT_VALUE_MISSING",
                    "Binary result succeeded without a byte array.");
            }

            return Result<string>.Success(
                Encoding.ASCII.GetString(bytesResult.Value.Where(b => b != 0).ToArray()));
        }

        public static Result<string> ToDecimalByteString(this Result<byte[]> bytesResult)
        {
            if (!bytesResult.IsSuccess)
            {
                if (bytesResult.Error is not null)
                {
                    return Result<string>.Failure(
                        bytesResult.Error.Code,
                        bytesResult.Error.Message);
                }

                return Result<string>.Failure(
                    "BINARY_RESULT_ERROR_MISSING",
                    "Binary result failed without error details.");
            }

            if (bytesResult.Value is null)
            {
                return Result<string>.Failure(
                    "BINARY_RESULT_VALUE_MISSING",
                    "Binary result succeeded without a byte array.");
            }

            string value = string.Join(" ", bytesResult.Value.Select(b => b.ToString()));

            return Result<string>.Success(value);
        }

        public static Result<string> ToHexString(this Result<byte[]> bytesResult)
        {
            if (!bytesResult.IsSuccess)
            {
                if (bytesResult.Error is not null)
                {
                    return Result<string>.Failure(
                        bytesResult.Error.Code,
                        bytesResult.Error.Message);
                }

                return Result<string>.Failure(
                    "BINARY_RESULT_ERROR_MISSING",
                    "Binary result failed without error details.");
            }

            if (bytesResult.Value is null)
            {
                return Result<string>.Failure(
                    "BINARY_RESULT_VALUE_MISSING",
                    "Binary result succeeded without a byte array.");
            }

            if (bytesResult.Value.Length == 0)
            {
                return Result<string>.Success(string.Empty);
            }

            string result = string.Empty;

            foreach (var b in bytesResult.Value)
                result += b.ToString("X2");

            return Result<string>.Success("0x" + result);
        }

        #region numeric convert
        public static Result<byte> ToUInt8(this Result<byte[]> bytesResult)
        {
            if (!bytesResult.IsSuccess)
            {
                if (bytesResult.Error is not null)
                {
                    return Result<byte>.Failure(
                        bytesResult.Error.Code,
                        bytesResult.Error.Message);
                }

                return Result<byte>.Failure(
                    "BINARY_RESULT_ERROR_MISSING",
                    "Binary result failed without error details.");
            }

            if (bytesResult.Value is null)
            {
                return Result<byte>.Failure(
                    "BINARY_RESULT_VALUE_MISSING",
                    "Binary result succeeded without a byte array.");
            }

            if (bytesResult.Value.Length != sizeof(byte))
            {
                return Result<byte>.Failure(
                    "BINARY_SIZE_INVALID",
                    "Byte array size must be exactly 1 byte to convert to UInt8.");
            }

            return Result<byte>.Success(bytesResult.Value[0]);
        }

        public static Result<byte> ToByte(this Result<byte[]> bytesResult)
        {
            return bytesResult.ToUInt8();
        }

        public static Result<ushort> ToUInt16(this Result<byte[]> bytesResult)
        {
            if (!bytesResult.IsSuccess)
            {
                if (bytesResult.Error is not null)
                {
                    return Result<ushort>.Failure(
                        bytesResult.Error.Code,
                        bytesResult.Error.Message);
                }

                return Result<ushort>.Failure(
                    "BINARY_RESULT_ERROR_MISSING",
                    "Binary result failed without error details.");
            }

            if (bytesResult.Value is null)
            {
                return Result<ushort>.Failure(
                    "BINARY_RESULT_VALUE_MISSING",
                    "Binary result succeeded without a byte array.");
            }

            if (bytesResult.Value.Length != sizeof(ushort))
            {
                return Result<ushort>.Failure(
                    "BINARY_SIZE_INVALID",
                    "Byte array size must be exactly 2 bytes to convert to UInt16.");
            }

            return Result<ushort>.Success(BitConverter.ToUInt16(bytesResult.Value));
        }

        public static Result<ushort> ToUShort(this Result<byte[]> bytesResult)
        {
            return bytesResult.ToUInt16();
        }

        public static Result<uint> ToUInt32(this Result<byte[]> bytesResult)
        {
            if (!bytesResult.IsSuccess)
            {
                if (bytesResult.Error is not null)
                {
                    return Result<uint>.Failure(
                        bytesResult.Error.Code,
                        bytesResult.Error.Message);
                }

                return Result<uint>.Failure(
                    "BINARY_RESULT_ERROR_MISSING",
                    "Binary result failed without error details.");
            }

            if (bytesResult.Value is null)
            {
                return Result<uint>.Failure(
                    "BINARY_RESULT_VALUE_MISSING",
                    "Binary result succeeded without a byte array.");
            }

            if (bytesResult.Value.Length != sizeof(uint))
            {
                return Result<uint>.Failure(
                    "BINARY_SIZE_INVALID",
                    "Byte array size must be exactly 4 bytes to convert to UInt32.");
            }

            return Result<uint>.Success(BitConverter.ToUInt32(bytesResult.Value));
        }

        public static Result<uint> ToUInt(this Result<byte[]> bytesResult)
        {
            return bytesResult.ToUInt32();
        }

        public static Result<ulong> ToUInt64(this Result<byte[]> bytesResult)
        {
            if (!bytesResult.IsSuccess)
            {
                if (bytesResult.Error is not null)
                {
                    return Result<ulong>.Failure(
                        bytesResult.Error.Code,
                        bytesResult.Error.Message);
                }

                return Result<ulong>.Failure(
                    "BINARY_RESULT_ERROR_MISSING",
                    "Binary result failed without error details.");
            }

            if (bytesResult.Value is null)
            {
                return Result<ulong>.Failure(
                    "BINARY_RESULT_VALUE_MISSING",
                    "Binary result succeeded without a byte array.");
            }

            if (bytesResult.Value.Length != sizeof(ulong))
            {
                return Result<ulong>.Failure(
                    "BINARY_SIZE_INVALID",
                    "Byte array size must be exactly 8 bytes to convert to UInt64.");
            }

            return Result<ulong>.Success(BitConverter.ToUInt64(bytesResult.Value));
        }

        public static Result<ulong> ToULong(this Result<byte[]> bytesResult)
        {
            return bytesResult.ToUInt64();
        }

        public static Result<short> ToInt16(this Result<byte[]> bytesResult)
        {
            if (!bytesResult.IsSuccess)
            {
                if (bytesResult.Error is not null)
                    return Result<short>.Failure(bytesResult.Error.Code, bytesResult.Error.Message);

                return Result<short>.Failure("BINARY_RESULT_ERROR_MISSING", "Binary result failed without error details.");
            }

            if (bytesResult.Value is null)
                return Result<short>.Failure("BINARY_RESULT_VALUE_MISSING", "Binary result succeeded without a byte array.");

            if (bytesResult.Value.Length != sizeof(short))
                return Result<short>.Failure("BINARY_SIZE_INVALID", "Byte array size must be exactly 2 bytes to convert to Int16.");

            return Result<short>.Success(BitConverter.ToInt16(bytesResult.Value));
        }

        public static Result<int> ToInt32(this Result<byte[]> bytesResult)
        {
            if (!bytesResult.IsSuccess)
            {
                if (bytesResult.Error is not null)
                    return Result<int>.Failure(bytesResult.Error.Code, bytesResult.Error.Message);

                return Result<int>.Failure("BINARY_RESULT_ERROR_MISSING", "Binary result failed without error details.");
            }

            if (bytesResult.Value is null)
                return Result<int>.Failure("BINARY_RESULT_VALUE_MISSING", "Binary result succeeded without a byte array.");

            if (bytesResult.Value.Length != sizeof(int))
                return Result<int>.Failure("BINARY_SIZE_INVALID", "Byte array size must be exactly 4 bytes to convert to Int32.");

            return Result<int>.Success(BitConverter.ToInt32(bytesResult.Value));
        }

        #endregion numeric convert

        public static Result<byte[]> FromBytes(byte[] bytes)
        {
            if (bytes is null)
            {
                return Result<byte[]>.Failure( "BINARY_BYTES_MISSING", "Byte array is null.");
            }

            return Result<byte[]>.Success(bytes);
        }

        /*
        AttributeUsageAttribute:
        Bin.FromBytes(bytes).ToUInt32();
        */
    }
}
