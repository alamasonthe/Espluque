using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;
using System.Text;
using Util;

namespace RegViewer
{
    public class RegService
    {
        private readonly Espluque.Contracts.CrossCutting.ILogger _logger;

        public RegService(Espluque.Contracts.CrossCutting.ILogger logger)
        {
            _logger = logger;
        }

        public static async Task<Result<IConfiguration>> Load(string filePath)
        {
            try
            {
                string regText = await System.IO.File.ReadAllTextAsync(filePath);

                string[] lines = regText
                    .Replace("\r\n", "\n")
                    .Replace('\r', '\n')
                    .Split('\n');

                string header = lines.FirstOrDefault() ?? string.Empty;

                bool isReg =
                    header.Equals("Windows Registry Editor Version 5.00", StringComparison.OrdinalIgnoreCase) ||
                    header.Equals("REGEDIT4", StringComparison.OrdinalIgnoreCase);

                if (!isReg)
                {

                    return Result<IConfiguration>.Failure("REG_LOAD_ERROR", "This is not a reg file.");
                }

                // multi-line values in reg files are indicated by a backslash at the end of the line. We need to combine those lines into a single logical line before parsing.
                List<string> logicalLines = new();
                StringBuilder currentLine = new();

                foreach (string line in lines.Skip(1))
                {
                    string trimmedLine = line.TrimStart();

                    if (currentLine.Length > 0)
                    {
                        currentLine.Append(trimmedLine);
                    }
                    else
                    {
                        currentLine.Append(line);
                    }

                    if (currentLine.ToString().TrimEnd().EndsWith('\\'))
                    {
                        currentLine.Length--;

                        while (currentLine.Length > 0 &&
                               char.IsWhiteSpace(currentLine[^1]))
                        {
                            currentLine.Length--;
                        }

                        continue;
                    }

                    logicalLines.Add(currentLine.ToString());
                    currentLine.Clear();
                }

                if (currentLine.Length > 0)
                {
                    logicalLines.Add(currentLine.ToString());
                }

                string iniText = string.Join(Environment.NewLine, logicalLines);

                using MemoryStream stream = new(Encoding.UTF8.GetBytes(iniText));

                IConfiguration configuration = new ConfigurationBuilder()
                    .AddIniStream(stream)
                    .Build();

                return Result<IConfiguration>.Success(configuration);
            }
            catch (Exception ex)
            {
                return Result<IConfiguration>.Failure("REG_LOAD_ERROR", $"{ex.Message}");
            }
        }

        private Result<List<RegistryValue>> GetRegistryValues(IConfiguration configuration)
        {
            try
            {
                List<RegistryValue> registryValues = new();

                foreach (IConfigurationSection section in configuration.GetChildren())
                {
                    foreach (IConfigurationSection valueSection in section.GetChildren())
                    {
                        if (valueSection.Value is null)
                        {
                            continue;
                        }

                        string rawValue = valueSection.Value;
                        RegistryValueKind type = GetRegistryValueKind(rawValue);

                        registryValues.Add(new RegistryValue
                        {
                            KeyPath = section.Key,
                            Name = GetRegistryValueName(valueSection.Key),
                            Type = type,
                            RawData = GetRegistryValueData(rawValue, type)
                        });
                    }
                }

                return Result<List<RegistryValue>>.Success(registryValues);
            }
            catch (Exception e)
            {
                return Result<List<RegistryValue>>.Failure("REG_ERROR", $"{e.Message}");
            }
        }

        private Result<TreeNode<List<KeyValuePair<string, string>>>> GetKeyValueTree(List<RegistryValue> registryValues)
        {
            try
            {
                IEnumerable<(string Path, bool IsLeaf, List<KeyValuePair<string, string>> Data)> items = registryValues.Select(registryValue =>
                {
                    string valueName = string.IsNullOrWhiteSpace(registryValue.Name) ? "(Default)" : registryValue.Name;

                    return
                    (
                        Path: registryValue.KeyPath + "\\" + valueName,
                        IsLeaf: true,
                        Data: new List<KeyValuePair<string, string>>
                        {
                    new("Path", registryValue.KeyPath + "\\" + valueName),
                    new("Type", registryValue.DisplayType),
                    new("Data", registryValue.RawData)
                        }
                    );
                });

                TreeNode<List<KeyValuePair<string, string>>> tree = TreeBuilder.Build(items, ["\\"], "Registry");

                return Result<TreeNode<List<KeyValuePair<string, string>>>>.Success(tree);
            }
            catch (Exception ex)
            {
                return Result<TreeNode<List<KeyValuePair<string, string>>>>.Failure("REG_TREE_ERROR", $"{ex.Message}");
            }
        }

        public async Task<Result<TreeNode<List<KeyValuePair<string, string>>>>> GetRegistryKeyValueTree(string filePath)
        {
            var configurationResult = await Load(filePath);
            if (!configurationResult.IsSuccess)
            {
                return Result<TreeNode<List<KeyValuePair<string, string>>>>.Failure(configurationResult.Error.Code, configurationResult.Error.Message);
            }

            var regKeyListResult = GetRegistryValues(configurationResult.Value);
            if (!regKeyListResult.IsSuccess)
            {
                return Result<TreeNode<List<KeyValuePair<string, string>>>>.Failure(regKeyListResult.Error.Code, regKeyListResult.Error.Message);
            }

            var regTreeKeyValueResult = GetKeyValueTree(regKeyListResult.Value);
            if (!regTreeKeyValueResult.IsSuccess)
            {
                return Result<TreeNode<List<KeyValuePair<string, string>>>>.Failure(regTreeKeyValueResult.Error.Code, regTreeKeyValueResult.Error.Message);
            }

            return Result<TreeNode<List<KeyValuePair<string, string>>>>.Success(regTreeKeyValueResult.Value);
        }


        #region Helpers

        private string GetRegistryValueName(string rawName)
        {
            if (rawName == "@")
            {
                return string.Empty;
            }

            if (rawName.Length >= 2 && rawName.StartsWith('"') && rawName.EndsWith('"'))
            {
                return rawName[1..^1];
            }

            return rawName;
        }

        private RegistryValueKind GetRegistryValueKind(string rawValue)
        {
            if (rawValue.StartsWith("dword:", StringComparison.OrdinalIgnoreCase))
            {
                return RegistryValueKind.DWord;
            }

            if (rawValue.StartsWith("hex(b):", StringComparison.OrdinalIgnoreCase))
            {
                return RegistryValueKind.QWord;
            }

            if (rawValue.StartsWith("hex(7):", StringComparison.OrdinalIgnoreCase))
            {
                return RegistryValueKind.MultiString;
            }

            if (rawValue.StartsWith("hex(2):", StringComparison.OrdinalIgnoreCase))
            {
                return RegistryValueKind.ExpandString;
            }

            if (rawValue.StartsWith("hex(0):", StringComparison.OrdinalIgnoreCase))
            {
                return RegistryValueKind.None;
            }

            if (rawValue.StartsWith("hex:", StringComparison.OrdinalIgnoreCase))
            {
                return RegistryValueKind.Binary;
            }

            return RegistryValueKind.String;

        }

        private string GetRegistryValueData(string rawValue, RegistryValueKind type)
        {
            return type switch
            {
                RegistryValueKind.DWord => FormatDWord(rawValue),
                RegistryValueKind.QWord => FormatQWord(rawValue),
                RegistryValueKind.Binary => FormatBinary(rawValue, "hex:"),
                RegistryValueKind.None => FormatBinary(rawValue, "hex(0):"),
                RegistryValueKind.ExpandString => FormatUnicodeString(rawValue, "hex(2):", "REG_EXPAND_SZ"),
                RegistryValueKind.MultiString => FormatMultiString(rawValue),
                RegistryValueKind.String => FormatString(rawValue),
                _ => rawValue
            };
        }

        private bool TryParseHexBytes(
    string rawValue,
    string prefix,
    string registryType,
    out byte[] bytes)
        {
            bytes = [];

            if (!rawValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Log(
                    LogLevel.Error,
                    $"REG_FORMAT_ERROR\tInvalid {registryType} value: {rawValue}");

                return false;
            }

            string[] values = rawValue[prefix.Length..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            byte[] parsedBytes = new byte[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                if (!byte.TryParse(
                    values[i],
                    System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out parsedBytes[i]))
                {
                    _logger.Log(
                        LogLevel.Error,
                        $"REG_FORMAT_ERROR\tInvalid {registryType} value: {rawValue}");

                    return false;
                }
            }

            bytes = parsedBytes;
            return true;
        }

        #endregion


        #region format values

        private string FormatDWord(string rawValue)
        {
            string hexValue = rawValue["dword:".Length..];

            return uint.TryParse(
                hexValue,
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture,
                out uint value)
                ? $"0x{value:x8} ({value})"
                : $"0x{hexValue}";
        }

        private string FormatQWord(string rawValue)
        {
            if (!TryParseHexBytes(
                rawValue,
                "hex(b):",
                "REG_QWORD",
                out byte[] bytes))
            {
                return rawValue;
            }

            if (bytes.Length != 8)
            {
                _logger.Log(
                    LogLevel.Error,
                    $"REG_FORMAT_ERROR\tInvalid REG_QWORD length: {rawValue}");

                return rawValue;
            }

            ulong value = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes);

            return $"0x{value:x16} ({value})";
        }

        private string FormatBinary(string rawValue, string prefix)
        {
            return rawValue[prefix.Length..].Replace(",", " ");
        }

        private string FormatUnicodeString( string rawValue, string prefix, string registryType)
        {
            if (!TryParseHexBytes(
                rawValue,
                prefix,
                registryType,
                out byte[] bytes))
            {
                return rawValue;
            }

            if (bytes.Length % 2 != 0)
            {
                _logger.Log(
                    LogLevel.Error,
                    $"REG_FORMAT_ERROR\tInvalid {registryType} byte count: {rawValue}");

                return rawValue;
            }

            return Encoding.Unicode
                .GetString(bytes)
                .TrimEnd('\0');
        }

        private string FormatMultiString(string rawValue)
        {
            if (!TryParseHexBytes(
                rawValue,
                "hex(7):",
                "REG_MULTI_SZ",
                out byte[] bytes))
            {
                return rawValue;
            }

            if (bytes.Length % 2 != 0)
            {
                _logger.Log(
                    LogLevel.Error,
                    $"REG_FORMAT_ERROR\tInvalid REG_MULTI_SZ byte count: {rawValue}");

                return rawValue;
            }

            string value = Encoding.Unicode
                .GetString(bytes)
                .TrimEnd('\0');

            return string.Join(
                " ",
                value.Split('\0', StringSplitOptions.RemoveEmptyEntries));
        }

        private string FormatString(string rawValue)
        {
            string value = rawValue;

            if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
            {
                value = value[1..^1];
            }

            return value
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }

        #endregion


    }
}
