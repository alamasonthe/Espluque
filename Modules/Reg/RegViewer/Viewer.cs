using Espluque.Contracts.Entities;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System.IO;
using System.Text;
using Util;

namespace RegViewer
{
    internal class Viewer: IWpfViewer
    {
        private readonly IMessageCenter _messageCenter;
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IEntityFactory _entityFactory;

        public Viewer(IMessageCenter messageCenter,
            ILogger logger,
            ISettingsService settingsService,
            IEntityFactory entityFactory)
        {
            _messageCenter = messageCenter;
            _logger = logger;
            _settingsService = settingsService;
            _entityFactory = entityFactory;
        }

        public async Task<object?> GetViewer(AnalysisContext analysisContext)
        {
            object viewer = new RegEdit(analysisContext.FilePath, _logger);
            return viewer;
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

                string iniText = string.Join(Environment.NewLine, lines.Skip(1));

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

        private static Result<List<RegistryValue>> GetRegistryValues(IConfiguration configuration)
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

                        registryValues.Add(new RegistryValue
                        {
                            KeyPath = section.Key,
                            Name = GetRegistryValueName(valueSection.Key),
                            Type = GetRegistryValueKind(valueSection.Value),
                            RawData = GetRegistryValueData(valueSection.Value)
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

        private static Result<TreeNode<List<KeyValuePair<string, string>>>> GetKeyValueTree(List<RegistryValue> registryValues)
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
                    new("Type", registryValue.Type.ToString()),
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

        private static Result<TreeNode<RegistryValue>> GetRegistryTree(List<RegistryValue> registryValues)
        {
            try
            {
                IEnumerable<(string Path, bool IsLeaf, RegistryValue Data)> items = registryValues.Select(registryValue =>
                (
                    Path: registryValue.KeyPath + "\\" + (string.IsNullOrWhiteSpace(registryValue.Name) ? "(Default)" : registryValue.Name),
                    IsLeaf: true,
                    Data: registryValue
                ));

                var tree = TreeBuilder.Build(items, ["\\"], "Registry");

                return Result<TreeNode<RegistryValue>>.Success(tree);
            }
            catch (Exception ex)
            {
                return Result<TreeNode<RegistryValue>>.Failure("REG_TREE_ERROR", $"{ex.Message}");
            }
        }

        public static async Task<Result<TreeNode<List<KeyValuePair<string, string>>>>> GetRegistryKeyValueTree(string filePath)
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

        private static string GetRegistryValueName(string rawName)
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

        private static RegistryValueKind GetRegistryValueKind(string rawValue)
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

            if (rawValue.Length >= 2 && rawValue.StartsWith('"') && rawValue.EndsWith('"'))
            {
                return RegistryValueKind.String;
            }

            return RegistryValueKind.Unknown;
        }

        private static string GetRegistryValueData(string rawValue)
        {
            string[] prefixes =
            [
                "dword:",
        "hex(b):",
        "hex(7):",
        "hex(2):",
        "hex(0):",
        "hex:"
            ];

            foreach (string prefix in prefixes)
            {
                if (rawValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return rawValue[prefix.Length..];
                }
            }

            if (rawValue.Length >= 2 && rawValue.StartsWith('"') && rawValue.EndsWith('"'))
            {
                return rawValue[1..^1];
            }

            return rawValue;
        }

        #endregion
    }
}
