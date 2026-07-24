using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Util;

namespace Shortcut
{
    internal class Grabber
    {

        #region Shortcut infos

        public static Result<List<KeyValuePair<string, string>>> GetShortcutInfos(string filePath)
        {
            try
            {
                string? directoryPath = Path.GetDirectoryName(filePath);
                string fileName = Path.GetFileName(filePath);

                if (string.IsNullOrWhiteSpace(directoryPath) ||
                    string.IsNullOrWhiteSpace(fileName))
                {
                    return Result<List<KeyValuePair<string, string>>>.Failure(
                        "GET_SHORTCUT_INFOS_ERROR",
                        "Invalid shortcut path.");
                }

                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");

                if (shellType is null)
                {
                    return Result<List<KeyValuePair<string, string>>>.Failure(
                        "GET_SHORTCUT_INFOS_ERROR",
                        "WScript.Shell is unavailable.");
                }

                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(filePath);

                List<KeyValuePair<string, string>> infos =
                [
                    new("Shortcut Name", fileName),
                    new("Target Path", Convert.ToString(shortcut.TargetPath) ?? string.Empty),
                    new("Arguments", Convert.ToString(shortcut.Arguments) ?? string.Empty),
                    new("Working Directory", Convert.ToString(shortcut.WorkingDirectory) ?? string.Empty),
                    new("Description", Convert.ToString(shortcut.Description) ?? string.Empty),
                    new("Hotkey", Convert.ToString(shortcut.Hotkey) ?? string.Empty),
                    new("Show Command", Convert.ToString(shortcut.WindowStyle) ?? string.Empty)
                ];

                return Result<List<KeyValuePair<string, string>>>.Success(infos);
            }
            catch (Exception ex)
            {
                return Result<List<KeyValuePair<string, string>>>.Failure(
                    "GET_SHORTCUT_INFOS_ERROR",
                    ex.Message);
            }
        }

        #endregion


        #region Link flags

        public static async Task<Result<List<KeyValuePair<string, string>>>> GetLinkFlags(string filePath)
        {
            Result<uint> linkFlagsResult = await ReadLinkFlags(filePath);

            if (!linkFlagsResult.IsSuccess)
            {
                return Result<List<KeyValuePair<string, string>>>.Failure(linkFlagsResult.Error.Code, linkFlagsResult.Error.Message);
            }

            uint linkFlags = linkFlagsResult.Value;

            List<KeyValuePair<string, string>> linkFlagInfos = new()
            {
                new KeyValuePair<string, string>("HasLinkTargetIDList", HasFlag(linkFlags, 0x00000001)),
                new KeyValuePair<string, string>("HasLinkInfo", HasFlag(linkFlags, 0x00000002)),
                new KeyValuePair<string, string>("HasName", HasFlag(linkFlags, 0x00000004)),
                new KeyValuePair<string, string>("HasRelativePath", HasFlag(linkFlags, 0x00000008)),
                new KeyValuePair<string, string>("HasWorkingDir", HasFlag(linkFlags, 0x00000010)),
                new KeyValuePair<string, string>("HasArguments", HasFlag(linkFlags, 0x00000020)),
                new KeyValuePair<string, string>("HasIconLocation", HasFlag(linkFlags, 0x00000040)),
                new KeyValuePair<string, string>("IsUnicode", HasFlag(linkFlags, 0x00000080)),
                new KeyValuePair<string, string>("ForceNoLinkInfo", HasFlag(linkFlags, 0x00000100)),
                new KeyValuePair<string, string>("HasExpString", HasFlag(linkFlags, 0x00000200)),
                new KeyValuePair<string, string>("RunInSeparateProcess", HasFlag(linkFlags, 0x00000400)),
                new KeyValuePair<string, string>("HasDarwinID", HasFlag(linkFlags, 0x00001000)),
                new KeyValuePair<string, string>("RunAsUser", HasFlag(linkFlags, 0x00002000)),
                new KeyValuePair<string, string>("HasExpIcon", HasFlag(linkFlags, 0x00004000)),
                new KeyValuePair<string, string>("NoPidlAlias", HasFlag(linkFlags, 0x00008000)),
                new KeyValuePair<string, string>("RunWithShimLayer", HasFlag(linkFlags, 0x00020000)),
                new KeyValuePair<string, string>("ForceNoLinkTrack", HasFlag(linkFlags, 0x00040000)),
                new KeyValuePair<string, string>("EnableTargetMetadata", HasFlag(linkFlags, 0x00080000)),
                new KeyValuePair<string, string>("DisableLinkPathTracking", HasFlag(linkFlags, 0x00100000)),
                new KeyValuePair<string, string>("DisableKnownFolderTracking", HasFlag(linkFlags, 0x00200000)),
                new KeyValuePair<string, string>("DisableKnownFolderAlias", HasFlag(linkFlags, 0x00400000)),
                new KeyValuePair<string, string>("AllowLinkToLink", HasFlag(linkFlags, 0x00800000)),
                new KeyValuePair<string, string>("UnaliasOnSave", HasFlag(linkFlags, 0x01000000)),
                new KeyValuePair<string, string>("PreferEnvironmentPath", HasFlag(linkFlags, 0x02000000)),
                new KeyValuePair<string, string>("KeepLocalIDListForUNCTarget", HasFlag(linkFlags, 0x04000000))
            };

            return Result<List<KeyValuePair<string, string>>>.Success(linkFlagInfos);
        }
        
        private static async Task<Result<uint>> ReadLinkFlags(string filePath)
        {
            try
            {
                using FileStream fileStream = System.IO.File.OpenRead(filePath);
                using BinaryReader binaryReader = new BinaryReader(fileStream);

                uint headerSize = binaryReader.ReadUInt32();

                if (headerSize != 0x0000004C)
                {
                    return Result<uint>.Failure("SHORTCUT-FLAG_READER_ERROR", "Invalid .lnk header size.");
                }

                fileStream.Position = 0x14;

                return Result<uint>.Success(binaryReader.ReadUInt32());
            }
            catch (IOException ex)
            {
                return Result<uint>.Failure("SHORTCUT-FLAG_READER_ERROR", $"{ex.Message}");
            }
        }

        #endregion


        #region Link Resolution data

        public static async Task<Result<List<KeyValuePair<string, string>>>> GetLinkResolutionData(string filePath)
        {
            try
            {
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                if (fileBytes.Length < 0x4C)
                {
                    return Result<List<KeyValuePair<string, string>>>.Failure("SHORTCUT_LINK_RESOLUTION_DATA_ERROR", "Invalid .lnk file size.");
                }

                uint headerSize = ReadUInt32(fileBytes, 0x00);

                if (headerSize != 0x0000004C)
                {
                    return Result<List<KeyValuePair<string, string>>>.Failure("SHORTCUT_LINK_RESOLUTION_DATA_ERROR", "Invalid .lnk header size.");
                }

                uint linkFlags = ReadUInt32(fileBytes, 0x14);

                bool hasLinkTargetIDList = (linkFlags & 0x00000001) == 0x00000001;
                bool hasLinkInfo = (linkFlags & 0x00000002) == 0x00000002;

                List<KeyValuePair<string, string>> infos = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("Has LinkInfo", YesNo(hasLinkInfo))
        };

                if (!hasLinkInfo)
                {
                    return Result<List<KeyValuePair<string, string>>>.Success(infos);
                }

                int linkInfoOffset = 0x4C;

                if (hasLinkTargetIDList)
                {
                    ushort idListSize = ReadUInt16(fileBytes, linkInfoOffset);
                    linkInfoOffset += 2 + idListSize;
                }

                Result rangeResult = EnsureRange(fileBytes, linkInfoOffset, 0x1C);

                if (!rangeResult.IsSuccess)
                {
                    return Result<List<KeyValuePair<string, string>>>.Failure(rangeResult.Error.Code, rangeResult.Error.Message);
                }

                uint linkInfoSize = ReadUInt32(fileBytes, linkInfoOffset + 0x00);
                uint linkInfoHeaderSize = ReadUInt32(fileBytes, linkInfoOffset + 0x04);
                uint linkInfoFlags = ReadUInt32(fileBytes, linkInfoOffset + 0x08);

                if (linkInfoSize < 0x1C)
                {
                    return Result<List<KeyValuePair<string, string>>>.Failure("SHORTCUT_LINK_RESOLUTION_DATA_ERROR", "Invalid LinkInfo size.");
                }

                if (linkInfoHeaderSize < 0x1C)
                {
                    return Result<List<KeyValuePair<string, string>>>.Failure("SHORTCUT_LINK_RESOLUTION_DATA_ERROR", "Invalid LinkInfo header size.");
                }

                rangeResult = EnsureRange(fileBytes, linkInfoOffset, checked((int)linkInfoSize));

                if (!rangeResult.IsSuccess)
                {
                    return Result<List<KeyValuePair<string, string>>>.Failure(rangeResult.Error.Code, rangeResult.Error.Message);
                }

                bool hasLocalBasePath = (linkInfoFlags & 0x00000001) == 0x00000001;
                bool hasNetworkRelativeLink = (linkInfoFlags & 0x00000002) == 0x00000002;

                uint localBasePathOffset = ReadUInt32(fileBytes, linkInfoOffset + 0x10);
                uint commonNetworkRelativeLinkOffset = ReadUInt32(fileBytes, linkInfoOffset + 0x14);
                uint commonPathSuffixOffset = ReadUInt32(fileBytes, linkInfoOffset + 0x18);

                uint localBasePathOffsetUnicode = 0;
                uint commonPathSuffixOffsetUnicode = 0;

                if (linkInfoHeaderSize >= 0x24)
                {
                    localBasePathOffsetUnicode = ReadUInt32(fileBytes, linkInfoOffset + 0x1C);
                    commonPathSuffixOffsetUnicode = ReadUInt32(fileBytes, linkInfoOffset + 0x20);
                }

                string localBasePath = ReadAnsiStringFromBlock(fileBytes, linkInfoOffset, linkInfoSize, localBasePathOffset);
                string commonPathSuffix = ReadAnsiStringFromBlock(fileBytes, linkInfoOffset, linkInfoSize, commonPathSuffixOffset);
                string reconstructedLocalPath = CombineLinkPath(localBasePath, commonPathSuffix);

                string localBasePathUnicode = ReadUnicodeStringFromBlock(fileBytes, linkInfoOffset, linkInfoSize, localBasePathOffsetUnicode);
                string commonPathSuffixUnicode = ReadUnicodeStringFromBlock(fileBytes, linkInfoOffset, linkInfoSize, commonPathSuffixOffsetUnicode);
                string reconstructedLocalPathUnicode = CombineLinkPath(localBasePathUnicode, commonPathSuffixUnicode);

                string networkShareName = string.Empty;
                string networkDeviceName = string.Empty;
                string networkProviderType = string.Empty;

                if (hasNetworkRelativeLink && commonNetworkRelativeLinkOffset != 0)
                {
                    Result networkRelativeLinkResult = ReadCommonNetworkRelativeLink(
                        fileBytes,
                        linkInfoOffset,
                        linkInfoSize,
                        commonNetworkRelativeLinkOffset,
                        out networkShareName,
                        out networkDeviceName,
                        out networkProviderType);

                    if (!networkRelativeLinkResult.IsSuccess)
                    {
                        return Result<List<KeyValuePair<string, string>>>.Failure(networkRelativeLinkResult.Error.Code, networkRelativeLinkResult.Error.Message);
                    }
                }

                string reconstructedNetworkPath = CombineLinkPath(
                    networkShareName,
                    string.IsNullOrWhiteSpace(commonPathSuffixUnicode) ? commonPathSuffix : commonPathSuffixUnicode);

                infos.Add(new KeyValuePair<string, string>("Resolution Mode", GetResolutionMode(hasLocalBasePath, hasNetworkRelativeLink)));
                infos.Add(new KeyValuePair<string, string>("Has Local Base Path", YesNo(hasLocalBasePath)));
                infos.Add(new KeyValuePair<string, string>("Has Network Relative Link", YesNo(hasNetworkRelativeLink)));
                infos.Add(new KeyValuePair<string, string>("Local Base Path", localBasePath));
                infos.Add(new KeyValuePair<string, string>("Common Path Suffix", commonPathSuffix));
                infos.Add(new KeyValuePair<string, string>("Reconstructed Local Path", reconstructedLocalPath));
                infos.Add(new KeyValuePair<string, string>("Local Base Path Unicode", localBasePathUnicode));
                infos.Add(new KeyValuePair<string, string>("Common Path Suffix Unicode", commonPathSuffixUnicode));
                infos.Add(new KeyValuePair<string, string>("Reconstructed Local Path Unicode", reconstructedLocalPathUnicode));
                infos.Add(new KeyValuePair<string, string>("Network Share Name", networkShareName));
                infos.Add(new KeyValuePair<string, string>("Network Device Name", networkDeviceName));
                infos.Add(new KeyValuePair<string, string>("Network Provider Type", networkProviderType));
                infos.Add(new KeyValuePair<string, string>("Reconstructed Network Path", reconstructedNetworkPath));

                return Result<List<KeyValuePair<string, string>>>.Success(infos);
            }
            catch (Exception ex)
            {
                return Result<List<KeyValuePair<string, string>>>.Failure("SHORTCUT_LINK_RESOLUTION_DATA_ERROR", $"{ex.Message}");
            }
        }

        private static Result ReadCommonNetworkRelativeLink(
            byte[] fileBytes,
            int linkInfoOffset,
            uint linkInfoSize,
            uint commonNetworkRelativeLinkOffset,
            out string networkShareName,
            out string networkDeviceName,
            out string networkProviderType)
        {
            networkShareName = string.Empty;
            networkDeviceName = string.Empty;
            networkProviderType = string.Empty;

            int blockOffset = linkInfoOffset + checked((int)commonNetworkRelativeLinkOffset);

            Result rangeResult = EnsureRange(fileBytes, blockOffset, 0x14);

            if (!rangeResult.IsSuccess)
            {
                return Result.Failure(rangeResult.Error.Code, rangeResult.Error.Message);
            }

            uint blockSize = ReadUInt32(fileBytes, blockOffset + 0x00);
            uint blockFlags = ReadUInt32(fileBytes, blockOffset + 0x04);
            uint netNameOffset = ReadUInt32(fileBytes, blockOffset + 0x08);
            uint deviceNameOffset = ReadUInt32(fileBytes, blockOffset + 0x0C);
            uint networkProviderTypeValue = ReadUInt32(fileBytes, blockOffset + 0x10);

            if (blockSize < 0x14)
            {
                return Result.Failure("SHORTCUT_COMMON_NETWORK_RELATIVE_LINK_ERROR", "Invalid CommonNetworkRelativeLink size.");
            }

            rangeResult = EnsureRange(fileBytes, blockOffset, checked((int)blockSize));

            if (!rangeResult.IsSuccess)
            {
                return Result.Failure(rangeResult.Error.Code, rangeResult.Error.Message);
            }

            bool hasDeviceName = (blockFlags & 0x00000001) == 0x00000001;
            bool hasNetworkProviderType = (blockFlags & 0x00000002) == 0x00000002;

            uint netNameOffsetUnicode = 0;
            uint deviceNameOffsetUnicode = 0;

            if (netNameOffset > 0x14)
            {
                rangeResult = EnsureRange(fileBytes, blockOffset + 0x14, 0x08);

                if (!rangeResult.IsSuccess)
                {
                    return Result.Failure(rangeResult.Error.Code, rangeResult.Error.Message);
                }

                netNameOffsetUnicode = ReadUInt32(fileBytes, blockOffset + 0x14);
                deviceNameOffsetUnicode = ReadUInt32(fileBytes, blockOffset + 0x18);
            }

            networkShareName = ReadAnsiStringFromBlock(fileBytes, blockOffset, blockSize, netNameOffset);

            string networkShareNameUnicode = ReadUnicodeStringFromBlock(fileBytes, blockOffset, blockSize, netNameOffsetUnicode);

            if (!string.IsNullOrWhiteSpace(networkShareNameUnicode))
            {
                networkShareName = networkShareNameUnicode;
            }

            if (hasDeviceName)
            {
                networkDeviceName = ReadAnsiStringFromBlock(fileBytes, blockOffset, blockSize, deviceNameOffset);

                string networkDeviceNameUnicode = ReadUnicodeStringFromBlock(fileBytes, blockOffset, blockSize, deviceNameOffsetUnicode);

                if (!string.IsNullOrWhiteSpace(networkDeviceNameUnicode))
                {
                    networkDeviceName = networkDeviceNameUnicode;
                }
            }

            if (hasNetworkProviderType)
            {
                networkProviderType = $"0x{networkProviderTypeValue:X8}";
            }

            return Result.Success();
        }

        private static string ReadAnsiStringFromBlock(byte[] fileBytes, int blockOffset, uint blockSize, uint stringOffset)
        {
            if (stringOffset == 0)
            {
                return string.Empty;
            }

            int absoluteOffset = blockOffset + checked((int)stringOffset);
            int blockEnd = blockOffset + checked((int)blockSize);

            if (absoluteOffset < blockOffset || absoluteOffset >= blockEnd)
            {
                return string.Empty;
            }

            int endOffset = absoluteOffset;

            while (endOffset < blockEnd && fileBytes[endOffset] != 0)
            {
                endOffset++;
            }

            return Encoding.Default.GetString(fileBytes, absoluteOffset, endOffset - absoluteOffset);
        }

        private static string ReadUnicodeStringFromBlock(byte[] fileBytes, int blockOffset, uint blockSize, uint stringOffset)
        {
            if (stringOffset == 0)
            {
                return string.Empty;
            }

            int absoluteOffset = blockOffset + checked((int)stringOffset);
            int blockEnd = blockOffset + checked((int)blockSize);

            if (absoluteOffset < blockOffset || absoluteOffset + 1 >= blockEnd)
            {
                return string.Empty;
            }

            int endOffset = absoluteOffset;

            while (endOffset + 1 < blockEnd &&
                   (fileBytes[endOffset] != 0 || fileBytes[endOffset + 1] != 0))
            {
                endOffset += 2;
            }

            int length = endOffset - absoluteOffset;

            if (length <= 0)
            {
                return string.Empty;
            }

            return Encoding.Unicode.GetString(fileBytes, absoluteOffset, length);
        }

        private static uint ReadUInt32(byte[] fileBytes, int offset)
        {
            EnsureRange(fileBytes, offset, 4);

            return BitConverter.ToUInt32(fileBytes, offset);
        }

        private static ushort ReadUInt16(byte[] fileBytes, int offset)
        {
            EnsureRange(fileBytes, offset, 2);

            return BitConverter.ToUInt16(fileBytes, offset);
        }

        private static Result EnsureRange(byte[] fileBytes, int offset, int length)
        {
            if (offset < 0 ||
                length < 0 ||
                (long)offset + length > fileBytes.Length)
            {
                return Result.Failure("SHORTCUT_RANGE_ERROR", "Unexpected end of .lnk file.");
            }

            return Result.Success();
        }

        private static string YesNo(bool value)
        {
            return value ? "Yes" : "No";
        }

        private static string GetResolutionMode(bool hasLocalBasePath, bool hasNetworkRelativeLink)
        {
            if (hasLocalBasePath && hasNetworkRelativeLink)
            {
                return "Local path + network path";
            }

            if (hasLocalBasePath)
            {
                return "Local path";
            }

            if (hasNetworkRelativeLink)
            {
                return "Network path";
            }

            return "No path data";
        }

        private static string CombineLinkPath(string basePath, string suffix)
        {
            if (string.IsNullOrWhiteSpace(basePath))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(suffix))
            {
                return basePath;
            }

            return basePath.TrimEnd('\\') + "\\" + suffix.TrimStart('\\');
        }

        #endregion


        #region Tracker data

        public static async Task<Result<List<KeyValuePair<string, string>>>> GetTrackerData(string filePath)
        {
            try
            {
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                if (fileBytes.Length < 0x4C)
                {
                    return Result<List<KeyValuePair<string, string>>>.Failure("SHORTCUT_TRACKER_DATA_ERROR", "Invalid .lnk file size.");
                }

                uint headerSize = ReadUInt32(fileBytes, 0x00);

                if (headerSize != 0x0000004C)
                {
                    return Result<List<KeyValuePair<string, string>>>.Failure("SHORTCUT_TRACKER_DATA_ERROR", "Invalid .lnk header size.");
                }

                int extraDataOffset = GetExtraDataOffset(fileBytes);
                int trackerDataOffset = FindExtraDataBlock(fileBytes, extraDataOffset, 0xA0000003);

                List<KeyValuePair<string, string>> infos = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Has Tracker Data", YesNo(trackerDataOffset >= 0))
                    };

                if (trackerDataOffset < 0)
                {
                    return Result<List<KeyValuePair<string, string>>>.Success(infos);
                }

                Result rangeResult = EnsureRange(fileBytes, trackerDataOffset, 0x60);

                if (!rangeResult.IsSuccess)
                {
                    return Result<List<KeyValuePair<string, string>>>.Failure(rangeResult.Error.Code, rangeResult.Error.Message);
                }

                uint blockSize = ReadUInt32(fileBytes, trackerDataOffset + 0x00);
                uint blockSignature = ReadUInt32(fileBytes, trackerDataOffset + 0x04);
                uint trackerLength = ReadUInt32(fileBytes, trackerDataOffset + 0x08);
                uint trackerVersion = ReadUInt32(fileBytes, trackerDataOffset + 0x0C);

                if (blockSize < 0x60)
                {
                    return Result<List<KeyValuePair<string, string>>>.Failure("SHORTCUT_TRACKER_DATA_ERROR", "Invalid TrackerDataBlock size.");
                }

                if (blockSignature != 0xA0000003)
                {
                    return Result<List<KeyValuePair<string, string>>>.Failure("SHORTCUT_TRACKER_DATA_ERROR", "Invalid TrackerDataBlock signature.");
                }

                string machineId = ReadFixedAnsiString(fileBytes, trackerDataOffset + 0x10, 16);

                Guid droidVolumeId = ReadGuid(fileBytes, trackerDataOffset + 0x20);
                Guid droidFileObjectId = ReadGuid(fileBytes, trackerDataOffset + 0x30);
                Guid birthDroidVolumeId = ReadGuid(fileBytes, trackerDataOffset + 0x40);
                Guid birthDroidFileObjectId = ReadGuid(fileBytes, trackerDataOffset + 0x50);

                infos.Add(new KeyValuePair<string, string>("Tracker Block Size", $"0x{blockSize:X8}"));
                infos.Add(new KeyValuePair<string, string>("Tracker Length", $"0x{trackerLength:X8}"));
                infos.Add(new KeyValuePair<string, string>("Tracker Version", $"0x{trackerVersion:X8}"));
                infos.Add(new KeyValuePair<string, string>("Machine ID", machineId));

                AddGuidTrackerInfos(infos, "Droid Volume ID", droidVolumeId);
                AddGuidTrackerInfos(infos, "Droid File Object ID", droidFileObjectId);
                AddGuidTrackerInfos(infos, "Birth Droid Volume ID", birthDroidVolumeId);
                AddGuidTrackerInfos(infos, "Birth Droid File Object ID", birthDroidFileObjectId);

                return Result<List<KeyValuePair<string, string>>>.Success(infos);
            }
            catch (Exception ex)
            {
                return Result<List<KeyValuePair<string, string>>>.Failure("SHORTCUT_TRACKER_DATA_ERROR", $"{ex.Message}");
            }
        }
        
        private static int GetExtraDataOffset(byte[] fileBytes)
        {
            uint linkFlags = ReadUInt32(fileBytes, 0x14);

            bool hasLinkTargetIDList = (linkFlags & 0x00000001) == 0x00000001;
            bool hasLinkInfo = (linkFlags & 0x00000002) == 0x00000002;
            bool hasName = (linkFlags & 0x00000004) == 0x00000004;
            bool hasRelativePath = (linkFlags & 0x00000008) == 0x00000008;
            bool hasWorkingDir = (linkFlags & 0x00000010) == 0x00000010;
            bool hasArguments = (linkFlags & 0x00000020) == 0x00000020;
            bool hasIconLocation = (linkFlags & 0x00000040) == 0x00000040;
            bool isUnicode = (linkFlags & 0x00000080) == 0x00000080;

            int offset = 0x4C;

            if (hasLinkTargetIDList)
            {
                ushort idListSize = ReadUInt16(fileBytes, offset);
                offset += 2 + idListSize;
            }

            if (hasLinkInfo)
            {
                uint linkInfoSize = ReadUInt32(fileBytes, offset);
                offset += checked((int)linkInfoSize);
            }

            if (hasName)
            {
                offset = SkipStringData(fileBytes, offset, isUnicode);
            }

            if (hasRelativePath)
            {
                offset = SkipStringData(fileBytes, offset, isUnicode);
            }

            if (hasWorkingDir)
            {
                offset = SkipStringData(fileBytes, offset, isUnicode);
            }

            if (hasArguments)
            {
                offset = SkipStringData(fileBytes, offset, isUnicode);
            }

            if (hasIconLocation)
            {
                offset = SkipStringData(fileBytes, offset, isUnicode);
            }

            return offset;
        }

        private static int SkipStringData(byte[] fileBytes, int offset, bool isUnicode)
        {
            ushort characterCount = ReadUInt16(fileBytes, offset);

            int byteCount = isUnicode
                ? checked(characterCount * 2)
                : characterCount;

            EnsureRange(fileBytes, offset, 2 + byteCount);

            return offset + 2 + byteCount;
        }

        private static int FindExtraDataBlock(byte[] fileBytes, int extraDataOffset, uint blockSignatureToFind)
        {
            int offset = extraDataOffset;

            while (offset + 4 <= fileBytes.Length)
            {
                uint blockSize = ReadUInt32(fileBytes, offset);

                if (blockSize < 0x00000004)
                {
                    return -1;
                }

                EnsureRange(fileBytes, offset, checked((int)blockSize));

                if (blockSize >= 0x00000008)
                {
                    uint blockSignature = ReadUInt32(fileBytes, offset + 0x04);

                    if (blockSignature == blockSignatureToFind)
                    {
                        return offset;
                    }
                }

                offset += checked((int)blockSize);
            }

            return -1;
        }

        private static void AddGuidTrackerInfos(List<KeyValuePair<string, string>> infos, string label, Guid guid)
        {
            UuidInfo uuidInfo = GetUuidInfo(guid);

            infos.Add(new KeyValuePair<string, string>(label, guid.ToString("B").ToUpperInvariant()));
            infos.Add(new KeyValuePair<string, string>($"{label} UUID Version", uuidInfo.Version));
            infos.Add(new KeyValuePair<string, string>($"{label} Creation Time UTC", uuidInfo.CreationTimeUtc));
            infos.Add(new KeyValuePair<string, string>($"{label} Sequence Number", uuidInfo.SequenceNumber));
            infos.Add(new KeyValuePair<string, string>($"{label} Node ID", uuidInfo.NodeId));
        }

        private static UuidInfo GetUuidInfo(Guid guid)
        {
            byte[] bytes = guid.ToByteArray();

            int version = (bytes[7] >> 4) & 0x0F;

            if (version != 1)
            {
                return new UuidInfo(
                    version.ToString(),
                    string.Empty,
                    string.Empty,
                    string.Empty);
            }

            uint timeLow = BitConverter.ToUInt32(bytes, 0);
            ushort timeMid = BitConverter.ToUInt16(bytes, 4);
            ushort timeHighAndVersion = BitConverter.ToUInt16(bytes, 6);

            ulong timestamp =
                ((ulong)(timeHighAndVersion & 0x0FFF) << 48) |
                ((ulong)timeMid << 32) |
                timeLow;

            int sequenceNumber = ((bytes[8] & 0x3F) << 8) | bytes[9];

            string nodeId = string.Join(
                ":",
                bytes
                    .Skip(10)
                    .Take(6)
                    .Select(value => value.ToString("X2")));

            string creationTimeUtc = string.Empty;

            try
            {
                DateTimeOffset uuidEpoch = new DateTimeOffset(
                    1582,
                    10,
                    15,
                    0,
                    0,
                    0,
                    TimeSpan.Zero);

                creationTimeUtc = uuidEpoch
                    .AddTicks(checked((long)timestamp))
                    .UtcDateTime
                    .ToString("yyyy-MM-dd HH:mm:ss.fffffff 'UTC'");
            }
            catch
            {
                creationTimeUtc = string.Empty;
            }

            return new UuidInfo(
                version.ToString(),
                creationTimeUtc,
                sequenceNumber.ToString(),
                nodeId);
        }

        private static Guid ReadGuid(byte[] fileBytes, int offset)
        {
            EnsureRange(fileBytes, offset, 16);

            byte[] guidBytes = new byte[16];
            Buffer.BlockCopy(fileBytes, offset, guidBytes, 0, 16);

            return new Guid(guidBytes);
        }

        private static string ReadFixedAnsiString(byte[] fileBytes, int offset, int length)
        {
            EnsureRange(fileBytes, offset, length);

            int endOffset = offset;

            while (endOffset < offset + length && fileBytes[endOffset] != 0)
            {
                endOffset++;
            }

            return Encoding.Default.GetString(fileBytes, offset, endOffset - offset);
        }

        private readonly record struct UuidInfo(
            string Version,
            string CreationTimeUtc,
            string SequenceNumber,
            string NodeId);

        #endregion


        #region Helpers

        private static void ReleaseComObject(object? value)
        {
            if (value is not null && Marshal.IsComObject(value))
            {
                Marshal.ReleaseComObject(value);
            }
        }

        private static string HasFlag(uint linkFlags, uint flag)
        {
            return (linkFlags & flag) == flag ? "Yes" : "No";
        }

        #endregion

    }
}
