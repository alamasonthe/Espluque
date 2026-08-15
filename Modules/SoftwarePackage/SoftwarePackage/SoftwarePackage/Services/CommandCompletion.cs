using Espluque.Contracts.Workflow;
using Microsoft.Extensions.Logging;
using SoftwarePackage.Entities;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SoftwarePackage.Services
{
    
    internal class CommandCompletion
    {
        private readonly Espluque.Contracts.CrossCutting.ILogger _logger;

        public CommandCompletion(Espluque.Contracts.CrossCutting.ILogger logger)
        {
            _logger = logger;
        }

        public async Task<CommandLineTemplate?> GetTemplate(string filename, string formatTag)
        {
            string formattedFilename = filename.PadRight(35);

            List<CommandLineTemplate> commandLineTemplates = [];
            string moduleDirectory = Path.GetDirectoryName(typeof(CommandCompletion).Assembly.Location) ?? string.Empty;
            string commandLinesPath = Path.Combine(moduleDirectory, "commandlines.json");

            try
            {
                if (File.Exists(commandLinesPath))
                {
                    string json = await File.ReadAllTextAsync(commandLinesPath);
                    commandLineTemplates = JsonSerializer.Deserialize<List<CommandLineTemplate>>(json) ?? [];
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"{formattedFilename}\tError reading command lines file {ex.Message}");
            }

            var commandLineTemplate = commandLineTemplates.FirstOrDefault(template => string.Equals(template.FormatTag, formatTag, StringComparison.OrdinalIgnoreCase));

            return commandLineTemplate;
        }

        public string ReplaceVariables(string commandLineTemplate, IAnalysisContext analysisContext, string favoriteObservedDataList)
        {
            return Regex.Replace(commandLineTemplate, @"\{([^{}]+)\}", match =>
            {
                string variableName = match.Groups[1].Value;
                string value = GetVariableValue(variableName, analysisContext, favoriteObservedDataList);

                return string.IsNullOrWhiteSpace(value) ? match.Value : value;
            });
        }

        private string GetVariableValue(string variableName, IAnalysisContext analysisContext, string favoriteObservedDataList)
        {
            if (variableName == "InstallerFilePath")
            {
                return $"%installerPath%\\{Path.GetFileName(analysisContext.FilePath)}";
            }

            List<KeyValuePair<string, string>>? favoriteList = analysisContext.ObservedData.FirstOrDefault(result => result.ContributionLabel == favoriteObservedDataList)?.GrabbedInformation;
            string? value = favoriteList?.FirstOrDefault(item => item.Key == variableName).Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            foreach (var observedData in analysisContext.ObservedData)
            {
                value = observedData.GrabbedInformation.FirstOrDefault(item => item.Key == variableName).Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }
    }
}
