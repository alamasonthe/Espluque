using Espluque.Application.Entities;
using Espluque.Application.Services;
using Espluque.Contracts.Enums;
using Espluque.Contracts.Orchestrators;
using Espluque.Contracts.Ports;
using Microsoft.Extensions.Logging;
using Util;

namespace Espluque.Application.Orchestrators
{
    public class Analyzer : IAnalyzer
    {
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly AnalyzeService _analyzeService;

        public Analyzer(Espluque.Contracts.Ports.ILogger logger, DyneService dyneService, IFileFormatService fileFormatService)
        {
            _logger = logger;
            _analyzeService = new AnalyzeService(logger, dyneService, fileFormatService);
        }

        public void AnalyzeFile(string filePath)
        {
            _logger.Log(LogLevel.Information, $"Start analysis: {filePath}");

            Result<bool> canOpenReadResult = Util.File.CanOpenRead(filePath);

            if (!canOpenReadResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"File check failed: {canOpenReadResult.Error?.Code} - {canOpenReadResult.Error?.Message}");
                _logger.Log(LogLevel.Information, $"Analysis complete: {filePath}");
                return;
            }
            else
            {
                _logger.Log(LogLevel.Information, $"File check succeeded: {filePath}");
            }

            AnalysisNode node = new()
            {
                Name = Path.GetFileName(filePath),
                TargetRootFilePath = filePath,
                TargetInternalPath = [],
                LocalStatus = AnalysisStatusEnum.Pending
            };

            _analyzeService.AnalyzeNodeAsync(node);

            _logger.Log(LogLevel.Information, $"Analysis complete: {filePath}");
        }

    }
}
