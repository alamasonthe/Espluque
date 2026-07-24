using System.Globalization;
using System.Security.Cryptography;

namespace Util
{
    public static class Hash
    {
        private static readonly uint[] Crc32Table = BuildCrc32Table(0xEDB88320);
        private static readonly uint[] Crc32CTable = BuildCrc32Table(0x82F63B78);
        private static readonly ulong[] Crc64EcmaTable = BuildCrc64EcmaTable();

        public static async Task<List<KeyValuePair<string, string>>?> GetFileHashesAsync(string filePath)
        {
            using IncrementalHash md5 = CreateMd5();
            using IncrementalHash sha1 = CreateSha1();
            using IncrementalHash sha256 = CreateSha256();
            using IncrementalHash sha384 = CreateSha384();
            using IncrementalHash sha512 = CreateSha512();

            uint adler32 = CreateAdler32();
            ushort crc16 = CreateCrc16CcittFalse();
            uint crc24 = CreateCrc24OpenPgp();
            uint crc32 = CreateCrc32();
            uint crc32C = CreateCrc32C();
            ulong crc64Ecma = CreateCrc64Ecma();

            uint fnv132 = CreateFnv132();
            uint fnv1a32 = CreateFnv1a32();
            ulong fnv164 = CreateFnv164();
            ulong fnv1a64 = CreateFnv1a64();

            byte[] buffer = new byte[1024 * 1024];

            using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            long size = stream.Length;

            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                AppendMd5(md5, buffer, bytesRead);
                AppendSha1(sha1, buffer, bytesRead);
                AppendSha256(sha256, buffer, bytesRead);
                AppendSha384(sha384, buffer, bytesRead);
                AppendSha512(sha512, buffer, bytesRead);

                adler32 = AppendAdler32(adler32, buffer, bytesRead);
                crc16 = AppendCrc16CcittFalse(crc16, buffer, bytesRead);
                crc24 = AppendCrc24OpenPgp(crc24, buffer, bytesRead);
                crc32 = AppendCrc32(crc32, buffer, bytesRead);
                crc32C = AppendCrc32C(crc32C, buffer, bytesRead);
                crc64Ecma = AppendCrc64Ecma(crc64Ecma, buffer, bytesRead);

                fnv132 = AppendFnv132(fnv132, buffer, bytesRead);
                fnv1a32 = AppendFnv1a32(fnv1a32, buffer, bytesRead);
                fnv164 = AppendFnv164(fnv164, buffer, bytesRead);
                fnv1a64 = AppendFnv1a64(fnv1a64, buffer, bytesRead);
            }

            List<KeyValuePair<string, string?>> hashes =
            [
                new("Adler32", GetAdler32(adler32)),
                new("CRC16-CCITT-FALSE", GetCrc16CcittFalse(crc16)),
                new("CRC24-OpenPGP", GetCrc24OpenPgp(crc24)),
                new("CRC32", GetCrc32(crc32)),
                new("CRC32C", GetCrc32C(crc32C)),
                new("CRC64-ECMA", GetCrc64Ecma(crc64Ecma)),
                new("FNV-1-32", GetFnv132(fnv132)),
                new("FNV-1-64", GetFnv164(fnv164)),
                new("FNV-1a-32", GetFnv1a32(fnv1a32)),
                new("FNV-1a-64", GetFnv1a64(fnv1a64)),
                new("MD5", GetMd5(md5)),
                new("SHA1", GetSha1(sha1)),
                new("SHA256", GetSha256(sha256)),
                new("SHA384", GetSha384(sha384)),
                new("SHA512", GetSha512(sha512))
            ];

            hashes.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Key, right.Key));
            hashes.Insert(0, new KeyValuePair<string, string?>("Size", size.ToString(CultureInfo.InvariantCulture)));

            return hashes;
        }

        private static IncrementalHash CreateMd5()
        {
            return IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        }

        private static void AppendMd5(IncrementalHash md5, byte[] buffer, int length)
        {
            md5.AppendData(buffer, 0, length);
        }

        private static string GetMd5(IncrementalHash md5)
        {
            return Convert.ToHexString(md5.GetHashAndReset());
        }

        private static IncrementalHash CreateSha1()
        {
            return IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        }

        private static void AppendSha1(IncrementalHash sha1, byte[] buffer, int length)
        {
            sha1.AppendData(buffer, 0, length);
        }

        private static string GetSha1(IncrementalHash sha1)
        {
            return Convert.ToHexString(sha1.GetHashAndReset());
        }

        private static IncrementalHash CreateSha256()
        {
            return IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        }

        private static void AppendSha256(IncrementalHash sha256, byte[] buffer, int length)
        {
            sha256.AppendData(buffer, 0, length);
        }

        private static string GetSha256(IncrementalHash sha256)
        {
            return Convert.ToHexString(sha256.GetHashAndReset());
        }

        private static IncrementalHash CreateSha384()
        {
            return IncrementalHash.CreateHash(HashAlgorithmName.SHA384);
        }

        private static void AppendSha384(IncrementalHash sha384, byte[] buffer, int length)
        {
            sha384.AppendData(buffer, 0, length);
        }

        private static string GetSha384(IncrementalHash sha384)
        {
            return Convert.ToHexString(sha384.GetHashAndReset());
        }

        private static IncrementalHash CreateSha512()
        {
            return IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
        }

        private static void AppendSha512(IncrementalHash sha512, byte[] buffer, int length)
        {
            sha512.AppendData(buffer, 0, length);
        }

        private static string GetSha512(IncrementalHash sha512)
        {
            return Convert.ToHexString(sha512.GetHashAndReset());
        }

        private static uint CreateAdler32()
        {
            return 1;
        }

        private static uint AppendAdler32(uint adler32, byte[] buffer, int length)
        {
            const uint modulo = 65521;

            uint low = adler32 & 0xFFFF;
            uint high = adler32 >> 16;

            for (int index = 0; index < length; index++)
            {
                low = (low + buffer[index]) % modulo;
                high = (high + low) % modulo;
            }

            return (high << 16) | low;
        }

        private static string GetAdler32(uint adler32)
        {
            return adler32.ToString("X8", CultureInfo.InvariantCulture);
        }

        private static ushort CreateCrc16CcittFalse()
        {
            return 0xFFFF;
        }

        private static ushort AppendCrc16CcittFalse(ushort crc16, byte[] buffer, int length)
        {
            const ushort polynomial = 0x1021;

            for (int index = 0; index < length; index++)
            {
                crc16 ^= (ushort)(buffer[index] << 8);

                for (int bit = 0; bit < 8; bit++)
                {
                    crc16 = (crc16 & 0x8000) != 0 ? (ushort)((crc16 << 1) ^ polynomial) : (ushort)(crc16 << 1);
                }
            }

            return crc16;
        }

        private static string GetCrc16CcittFalse(ushort crc16)
        {
            return crc16.ToString("X4", CultureInfo.InvariantCulture);
        }

        private static uint CreateCrc24OpenPgp()
        {
            return 0xB704CE;
        }

        private static uint AppendCrc24OpenPgp(uint crc24, byte[] buffer, int length)
        {
            const uint polynomial = 0x1864CFB;

            for (int index = 0; index < length; index++)
            {
                crc24 ^= (uint)buffer[index] << 16;

                for (int bit = 0; bit < 8; bit++)
                {
                    crc24 <<= 1;

                    if ((crc24 & 0x1000000) != 0)
                    {
                        crc24 ^= polynomial;
                    }
                }

                crc24 &= 0xFFFFFF;
            }

            return crc24;
        }

        private static string GetCrc24OpenPgp(uint crc24)
        {
            return crc24.ToString("X6", CultureInfo.InvariantCulture);
        }

        private static uint CreateCrc32()
        {
            return 0xFFFFFFFF;
        }

        private static uint AppendCrc32(uint crc32, byte[] buffer, int length)
        {
            return AppendCrc32WithTable(crc32, buffer, length, Crc32Table);
        }

        private static string GetCrc32(uint crc32)
        {
            return (~crc32).ToString("X8", CultureInfo.InvariantCulture);
        }

        private static uint CreateCrc32C()
        {
            return 0xFFFFFFFF;
        }

        private static uint AppendCrc32C(uint crc32C, byte[] buffer, int length)
        {
            return AppendCrc32WithTable(crc32C, buffer, length, Crc32CTable);
        }

        private static string GetCrc32C(uint crc32C)
        {
            return (~crc32C).ToString("X8", CultureInfo.InvariantCulture);
        }

        private static ulong CreateCrc64Ecma()
        {
            return 0;
        }

        private static ulong AppendCrc64Ecma(ulong crc64, byte[] buffer, int length)
        {
            for (int index = 0; index < length; index++)
            {
                crc64 = Crc64EcmaTable[((crc64 >> 56) ^ buffer[index]) & 0xFF] ^ (crc64 << 8);
            }

            return crc64;
        }

        private static string GetCrc64Ecma(ulong crc64)
        {
            return crc64.ToString("X16", CultureInfo.InvariantCulture);
        }

        private static uint CreateFnv132()
        {
            return 2166136261;
        }

        private static uint AppendFnv132(uint hash, byte[] buffer, int length)
        {
            unchecked
            {
                for (int index = 0; index < length; index++)
                {
                    hash *= 16777619;
                    hash ^= buffer[index];
                }
            }

            return hash;
        }

        private static string GetFnv132(uint hash)
        {
            return hash.ToString("X8", CultureInfo.InvariantCulture);
        }

        private static uint CreateFnv1a32()
        {
            return 2166136261;
        }

        private static uint AppendFnv1a32(uint hash, byte[] buffer, int length)
        {
            unchecked
            {
                for (int index = 0; index < length; index++)
                {
                    hash ^= buffer[index];
                    hash *= 16777619;
                }
            }

            return hash;
        }

        private static string GetFnv1a32(uint hash)
        {
            return hash.ToString("X8", CultureInfo.InvariantCulture);
        }

        private static ulong CreateFnv164()
        {
            return 14695981039346656037;
        }

        private static ulong AppendFnv164(ulong hash, byte[] buffer, int length)
        {
            unchecked
            {
                for (int index = 0; index < length; index++)
                {
                    hash *= 1099511628211;
                    hash ^= buffer[index];
                }
            }

            return hash;
        }

        private static string GetFnv164(ulong hash)
        {
            return hash.ToString("X16", CultureInfo.InvariantCulture);
        }

        private static ulong CreateFnv1a64()
        {
            return 14695981039346656037;
        }

        private static ulong AppendFnv1a64(ulong hash, byte[] buffer, int length)
        {
            unchecked
            {
                for (int index = 0; index < length; index++)
                {
                    hash ^= buffer[index];
                    hash *= 1099511628211;
                }
            }

            return hash;
        }

        private static string GetFnv1a64(ulong hash)
        {
            return hash.ToString("X16", CultureInfo.InvariantCulture);
        }

        private static uint AppendCrc32WithTable(uint crc32, byte[] buffer, int length, uint[] table)
        {
            for (int index = 0; index < length; index++)
            {
                crc32 = (crc32 >> 8) ^ table[(crc32 ^ buffer[index]) & 0xFF];
            }

            return crc32;
        }

        private static uint[] BuildCrc32Table(uint polynomial)
        {
            uint[] table = new uint[256];

            for (uint index = 0; index < table.Length; index++)
            {
                uint value = index;

                for (int bit = 0; bit < 8; bit++)
                {
                    value = (value & 1) != 0 ? polynomial ^ (value >> 1) : value >> 1;
                }

                table[index] = value;
            }

            return table;
        }

        private static ulong[] BuildCrc64EcmaTable()
        {
            const ulong polynomial = 0x42F0E1EBA9EA3693;

            ulong[] table = new ulong[256];

            for (ulong index = 0; index < 256; index++)
            {
                ulong value = index << 56;

                for (int bit = 0; bit < 8; bit++)
                {
                    value = (value & 0x8000000000000000) != 0 ? (value << 1) ^ polynomial : value << 1;
                }

                table[index] = value;
            }

            return table;
        }
    }
}