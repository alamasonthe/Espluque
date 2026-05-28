using Espluque.Contracts.Ports;
using Microsoft.Extensions.Logging;
using Util;

namespace Espluque.Application.Services
{
    public class DyneService
    {
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly IDyneFileSource _dyneFileSource;
        private readonly IDyneExtensionRepository _dyneExtensionRepository;
        private readonly IDyneCategoryRepository _dyneCategoryRepository;
        private readonly IDyneCategoryExtensionRepository _dyneCategoryExtensionRepository;

        public DyneService(
            Espluque.Contracts.Ports.ILogger logger,
            IDyneFileSource dyneFileSource,
            IDyneExtensionRepository dyneExtensionRepository,
            IDyneCategoryRepository dyneCategoryRepository,
            IDyneCategoryExtensionRepository dyneCategoryExtensionRepository)
        {
            _logger = logger;
            _dyneFileSource = dyneFileSource;
            _dyneExtensionRepository = dyneExtensionRepository;
            _dyneCategoryRepository = dyneCategoryRepository;
            _dyneCategoryExtensionRepository = dyneCategoryExtensionRepository;
        }

        public async Task<bool> ImportExtensionFromCsv(string filePath)
        {
            Result<bool> canOpenReadResult = Util.File.CanOpenRead(filePath);

            if (!canOpenReadResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"File check failed: {canOpenReadResult.Error?.Code} - {canOpenReadResult.Error?.Message}");
                _logger.Log(LogLevel.Information, $"Analysis complete: {filePath}");
                return false;
            }
            else
            {
                _logger.Log(LogLevel.Information, $"File check succeeded: {filePath}");
            }


            Result<int> countBefore = await _dyneExtensionRepository.CountAsync();

            if (!countBefore.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"Dyne Extension DB failed: {countBefore.Error?.Code} - {countBefore.Error?.Message}");
                return false;
            }
            else
            {
                _logger.Log(LogLevel.Information, $"Dyne Extension count before import: {countBefore.Value}");
            }


            Result<bool> importExtensionResult = await _dyneFileSource.ImportExtensionFromCsvAsync(filePath);

            if (!importExtensionResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"Dyne Extension import failed: {importExtensionResult.Error?.Code} - {importExtensionResult.Error?.Message}");
                return false;
            }
            else
            {
                _logger.Log(LogLevel.Information, $"Dyne Extension import succeeded: {filePath}");
            }


            Result<int> countAfter = await _dyneExtensionRepository.CountAsync();

            if (!countAfter.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"Dyne Extension DB failed: {countAfter.Error?.Code} - {countAfter.Error?.Message}");
                return false;
            }
            else
            {
                _logger.Log(LogLevel.Information, $"Dyne Extension count after import: {countAfter.Value} (+{countAfter.Value - countBefore.Value})");
            }


            return true;
        }

        public async Task<bool> ImportExtensionCategoryFromJson(string filePath)
        {
            Result<bool> canOpenReadResult = Util.File.CanOpenRead(filePath);

            if (!canOpenReadResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"File check failed: {canOpenReadResult.Error?.Code} - {canOpenReadResult.Error?.Message}");
                _logger.Log(LogLevel.Information, $"Analysis complete: {filePath}");
                return false;
            }

            _logger.Log(LogLevel.Information, $"File check succeeded: {filePath}");

            Result<int> extensionCountBefore = await _dyneExtensionRepository.CountAsync();

            if (!extensionCountBefore.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"Dyne Extension DB failed: {extensionCountBefore.Error?.Code} - {extensionCountBefore.Error?.Message}");
                return false;
            }

            Result<int> categoryCountBefore = await _dyneCategoryRepository.CountAsync();

            if (!categoryCountBefore.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"Dyne Category DB failed: {categoryCountBefore.Error?.Code} - {categoryCountBefore.Error?.Message}");
                return false;
            }

            Result<int> categoryExtensionCountBefore = await _dyneCategoryExtensionRepository.CountAsync();

            if (!categoryExtensionCountBefore.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"Dyne Category-Extension DB failed: {categoryExtensionCountBefore.Error?.Code} - {categoryExtensionCountBefore.Error?.Message}");
                return false;
            }

            _logger.Log(
                LogLevel.Information,
                $"Dyne counts before JSON import: extensions={extensionCountBefore.Value}, categories={categoryCountBefore.Value}, category-extensions={categoryExtensionCountBefore.Value}");

            Result<bool> importResult = await _dyneFileSource.ImportExtensionFromJsonAsync(filePath);

            if (!importResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"Dyne Extension/Category JSON import failed: {importResult.Error?.Code} - {importResult.Error?.Message}");
                return false;
            }

            _logger.Log(LogLevel.Information, $"Dyne Extension/Category JSON import succeeded: {filePath}");

            Result<int> extensionCountAfter = await _dyneExtensionRepository.CountAsync();

            if (!extensionCountAfter.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"Dyne Extension DB failed: {extensionCountAfter.Error?.Code} - {extensionCountAfter.Error?.Message}");
                return false;
            }

            Result<int> categoryCountAfter = await _dyneCategoryRepository.CountAsync();

            if (!categoryCountAfter.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"Dyne Category DB failed: {categoryCountAfter.Error?.Code} - {categoryCountAfter.Error?.Message}");
                return false;
            }

            Result<int> categoryExtensionCountAfter = await _dyneCategoryExtensionRepository.CountAsync();

            if (!categoryExtensionCountAfter.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"Dyne Category-Extension DB failed: {categoryExtensionCountAfter.Error?.Code} - {categoryExtensionCountAfter.Error?.Message}");
                return false;
            }

            _logger.Log(
                LogLevel.Information,
                $"Dyne counts after JSON import: extensions={extensionCountAfter.Value} (+{extensionCountAfter.Value - extensionCountBefore.Value}), categories={categoryCountAfter.Value} (+{categoryCountAfter.Value - categoryCountBefore.Value}), category-extensions={categoryExtensionCountAfter.Value} (+{categoryExtensionCountAfter.Value - categoryExtensionCountBefore.Value})");

            return true;
        }

        public async Task<Result<List<KeyValuePair<string, string>>?>> GetInfosFromExtensionAsync(string extension)
        {
            Result<List<KeyValuePair<string, string>>?> infosResult =
                await _dyneExtensionRepository.GetInfosFromExtensionAsync(extension);

            if (!infosResult.IsSuccess)
            {
                _logger.Log(
                    LogLevel.Error,
                    $"Dyne Extension info lookup failed: {infosResult.Error?.Code} - {infosResult.Error?.Message}");

                return infosResult;
            }

            if (infosResult.Value is null)
            {
                _logger.Log(
                    LogLevel.Information,
                    $"Dyne Extension info not found: {extension}");

                return infosResult;
            }

            _logger.Log(
                LogLevel.Information,
                $"Dyne Extension info found: {extension} ({infosResult.Value.Count} values)");

            return infosResult;
        }
    }
}
