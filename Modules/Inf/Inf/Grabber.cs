using System.Runtime.InteropServices;
using System.Text;
using Util;

namespace Inf
{
    internal class Grabber
    {
        public static async Task<Result<List<KeyValuePair<string, string>>>> GetVersionSectionInfos(string infFilePath)
        {
            try
            {
                string section = "Version";

                List<string> keys =
                [
                    "Signature",
                    "Class",
                    "ClassGUID",
                    "Provider",
                    "ExtensionId",
                    "ClassVer",
                    "LayoutFile",
                    "CatalogFile",
                    "CatalogFile.nt",
                    "CatalogFile.ntx86",
                    "CatalogFile.ntia64",
                    "CatalogFile.ntamd64",
                    "CatalogFile.ntarm",
                    "CatalogFile.ntarm64",
                    "DriverVer",
                    "PnpLockDown",
                    "DriverPackageDisplayName",
                    "DriverPackageType"
                ];

                IntPtr infHandle = InfCommon.SetupOpenInfFile(infFilePath, null, InfCommon.INF_STYLE_SUPPORTED, out uint errorLine);

                if (infHandle == InfCommon.InvalidHandleValue)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    return Result<List<KeyValuePair<string, string>>>.Failure("INF_OPEN_FAILED", $"SetupOpenInfFile failed at line {errorLine}. Win32Error={errorCode}");
                }

                try
                {
                    List<KeyValuePair<string, string>> versionInfos = [];

                    foreach (string key in keys)
                    {
                        string? value = GetValue(infHandle, section, key);

                        if (value is not null)
                        {
                            versionInfos.Add(new KeyValuePair<string, string>(key, value));
                        }
                    }

                    if (versionInfos.Count == 0)
                    {
                        return Result<List<KeyValuePair<string, string>>>.Failure("INF_GET_ERROR", "Cannot read VERSION section");
                    }

                    return Result<List<KeyValuePair<string, string>>>.Success(versionInfos);
                }
                finally
                {
                    InfCommon.SetupCloseInfFile(infHandle);
                }
            }
            catch (Exception ex)
            {
                return Result<List<KeyValuePair<string, string>>>.Failure("INF_VERSION_READ_FAILED", $"INF [Version] read failed: {ex.Message}");
            }
        }

        private static string? GetValue(IntPtr infHandle, string section, string key)
        {
            try
            {
                if (!InfCommon.SetupFindFirstLine(infHandle, section, key, out InfCommon.InfContext context))
                {
                    return null;
                }

                uint fieldCount = InfCommon.SetupGetFieldCount(ref context);

                if (fieldCount == 0)
                {
                    return string.Empty;
                }

                List<string> values = [];

                for (uint fieldIndex = 1; fieldIndex <= fieldCount; fieldIndex++)
                {
                    Result<string> valueResult = GetInfStringField(context, fieldIndex);

                    if (!valueResult.IsSuccess)
                    {
                        return null;
                    }

                    values.Add(valueResult.Value);
                }

                return string.Join(", ", values);
            }
            catch
            {
                return null;
            }
        }

        private static Result<string> GetInfStringField(InfCommon.InfContext context, uint fieldIndex)
        {
            if (!InfCommon.SetupGetStringField(ref context, fieldIndex, null, 0, out uint requiredSize))
            {
                int sizeErrorCode = Marshal.GetLastWin32Error();

                if (requiredSize == 0)
                {
                    return Result<string>.Failure("INF_STRING_FIELD_SIZE_READ_FAILED", $"SetupGetStringField size read failed for field {fieldIndex}. Win32Error={sizeErrorCode}");
                }
            }

            StringBuilder buffer = new((int)requiredSize);

            if (!InfCommon.SetupGetStringField(ref context, fieldIndex, buffer, requiredSize, out _))
            {
                int valueErrorCode = Marshal.GetLastWin32Error();
                return Result<string>.Failure("INF_STRING_FIELD_READ_FAILED", $"SetupGetStringField value read failed for field {fieldIndex}. Win32Error={valueErrorCode}");
            }

            return Result<string>.Success(buffer.ToString());
        }
    }
}