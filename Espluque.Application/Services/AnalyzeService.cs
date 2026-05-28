using Espluque.Application.Entities;
using Espluque.Contracts.Enums;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.Ports;
using Microsoft.Extensions.Logging;
using Util;

namespace Espluque.Application.Services
{
    public class AnalyzeService
    {
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly DyneService _dyneService;
        private readonly IFileFormatService _fileFormatService;

        public AnalyzeService(Espluque.Contracts.Ports.ILogger logger, DyneService dyneService, IFileFormatService fileFormatService)
        {
            _logger = logger;
            _dyneService = dyneService;
            _fileFormatService = fileFormatService;
        }

        public async Task AnalyzeNodeAsync(AnalysisNode node)
        {
            _logger.Log(LogLevel.Information, $"Node: {node.Name} - LocalStatus: {node.LocalStatus}");

            if (!TryReadByteSample(node, out byte[] byteSample))
            {
                node.LocalStatus = AnalysisStatusEnum.Failed;
                return;
            }

            _logger.Log(LogLevel.Information, $"Read byte sample: {byteSample.Length} bytes");

            var textOrBinaryResult = Bin.FromBytes(byteSample).DetectTextOrBinary();

            string fileExtension = Path.GetExtension(node.Name);

            Result<List<KeyValuePair<string, string>>?> dyneInfos = await _dyneService.GetInfosFromExtensionAsync(fileExtension);

            Result<List<IFileFormat?>> fileInfos = await _fileFormatService.GetInfosFromExtensionAsync(fileExtension);

            Result<IFileFormat> signatureMatchResult = await _fileFormatService.MatchInternalSignaturesAsync(node);

            Result<List<IFileFormat?>> fileInfosWithNoSignature = await _fileFormatService.GetInfosFromExtensionAsync(fileExtension, true);


            if (!signatureMatchResult.IsSuccess)
            {
                _logger.Log(LogLevel.Information, $"Pronom internal signature not matched: {node.Name}");
            }

            // choix test spécifique
            // exécution tests spécifiques
            // génération des états de propriétés
            // récupération des enfants
        }

        private bool TryReadByteSample(AnalysisNode node, out byte[] byteSample)
        {
            byteSample = [];

            Result<byte[]> byteSampleResult = Bin.ReadBytesFromFile(node.TargetRootFilePath, 0, 4096);

            if (!byteSampleResult.IsSuccess)
            {
                string errorCode = byteSampleResult.Error?.Code ?? "BINARY_READ_UNKNOWN_ERROR";
                string errorMessage = byteSampleResult.Error?.Message ?? "Binary read failed without error details.";

                _logger.Log( LogLevel.Error, $"{node.Name}: {errorCode} - {errorMessage}");

                return false;
            }

            if (byteSampleResult.Value is null)
            {
                _logger.Log( LogLevel.Error, $"{node.Name}: BINARY_RESULT_VALUE_MISSING - Binary read succeeded without a byte array.");

                return false;
            }

            byteSample = byteSampleResult.Value;

            return true;
        }
    }
}
