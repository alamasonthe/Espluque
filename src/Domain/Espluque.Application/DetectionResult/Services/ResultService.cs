using Espluque.Contracts.DetectionResult;
using Espluque.Contracts.Ports;
using Microsoft.Extensions.Logging;

namespace Espluque.Application.DetectionResult.Services
{
    public class ResultService : IResultService
    {
        private readonly Contracts.Ports.ILogger _logger;
        private readonly IResultSource _resultSource;

        public ResultService(Contracts.Ports.ILogger logger, IResultSource resultSource)
        {
            _logger = logger;
            _resultSource = resultSource;
        }

        public async Task<List<IResultModelDefinition>> GetResultModelDefinitions()
        {
            var resultModelDefinitionsResult = await _resultSource.GetResultModelDefinitions();
            if (!resultModelDefinitionsResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{resultModelDefinitionsResult.Error!.Code} {resultModelDefinitionsResult.Error.Message}");
                return [];
            }

            return resultModelDefinitionsResult.Value!;
        }

        public async Task<List<IResultModelDefinition>> GetResultModelDefinitions(string thesaurusTag)
        {
            var resultModelDefinitionsResult = await _resultSource.GetResultModelDefinitions(thesaurusTag);
            if (!resultModelDefinitionsResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{resultModelDefinitionsResult.Error!.Code} {resultModelDefinitionsResult.Error.Message}");
                return [];
            }

            return resultModelDefinitionsResult.Value!;
        }

        public async Task<IResultModelDefinition?> GetResultModelDefinition(int id)
        {
            var resultModelDefinitionResult = await _resultSource.GetResultModelDefinition(id);
            if (!resultModelDefinitionResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{resultModelDefinitionResult.Error!.Code} {resultModelDefinitionResult.Error.Message}");
                return null;
            }

            return resultModelDefinitionResult.Value;
        }

        public async Task<IResultModelDefinition?> SaveResultModelDefinition(IResultModelDefinition resultModelDefinition)
        {
            var saveResultModelDefinitionResult = await _resultSource.SaveResultModelDefinition(resultModelDefinition);
            if (!saveResultModelDefinitionResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{saveResultModelDefinitionResult.Error!.Code} {saveResultModelDefinitionResult.Error.Message}");
                return null;
            }

            _logger.Log(LogLevel.Information, $"SAVE_RESULT_MODEL_DEFINITION_SUCCESS: result model {saveResultModelDefinitionResult.Value!.Id} {saveResultModelDefinitionResult.Value.Name} saved.");
            return saveResultModelDefinitionResult.Value;
        }

        public async Task<bool> DeleteResultModelDefinition(int id)
        {
            var deleteResultModelDefinitionResult = await _resultSource.DeleteResultModelDefinition(id);
            if (!deleteResultModelDefinitionResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{deleteResultModelDefinitionResult.Error!.Code} {deleteResultModelDefinitionResult.Error.Message}");
                return false;
            }

            if (!deleteResultModelDefinitionResult.Value)
            {
                return false;
            }

            _logger.Log(LogLevel.Information, $"DELETE_RESULT_MODEL_DEFINITION_SUCCESS: result model {id} deleted.");
            return true;
        }
    }
}