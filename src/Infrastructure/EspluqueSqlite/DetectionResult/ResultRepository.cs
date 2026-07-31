using Espluque.Contracts.Interfaces;
using Espluque.Contracts.Ports;
using Espluque.Contracts.DetectionResult;
using Util;

namespace EspluqueSqlite.DetectionResult
{
    public class ResultRepository : IResultSource
    {
        public string DbFilepath;

        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly IEntityFactory _entityFactory;

        public ResultRepository(ILogger logger, IEntityFactory entityFactory, ISettingsService settingsService)
        {
            _logger = logger;
            _entityFactory = entityFactory;

            DbFilepath = DbFile.GetDbFilePath(settingsService);

            _logger.Log(Microsoft.Extensions.Logging.LogLevel.Debug, $"Espluque DB filepath: {DbFilepath}");
        }

        public async Task<Result<List<IResultModelDefinition>>> GetResultModelDefinitions()
        {
            var resultModelDefinitionsResult = await DbCrud.GetResultModelDefinitions(DbFilepath, _entityFactory);
            return resultModelDefinitionsResult;
        }

        public async Task<Result<List<IResultModelDefinition>>> GetResultModelDefinitions(string thesaurusTag)
        {
            string escapedThesaurusTag = thesaurusTag.Replace("'", "''");
            var resultModelDefinitionsResult = await DbCrud.GetResultModelDefinitions(DbFilepath, _entityFactory, $"WHERE ResultModel.ThesaurusTag = '{escapedThesaurusTag}'");
            return resultModelDefinitionsResult;
        }

        public async Task<Result<IResultModelDefinition?>> GetResultModelDefinition(int id)
        {
            Result<List<IResultModelDefinition>> resultModelDefinitionsResult = await DbCrud.GetResultModelDefinitions(DbFilepath, _entityFactory, $"WHERE ResultModel.Id = {id}");
            if (!resultModelDefinitionsResult.IsSuccess)
            {
                return Result<IResultModelDefinition?>.Failure(resultModelDefinitionsResult.Error!.Code, resultModelDefinitionsResult.Error.Message);
            }
            return Result<IResultModelDefinition?>.Success(resultModelDefinitionsResult.Value!.FirstOrDefault());
        }

        public async Task<Result<IResultModelDefinition>> SaveResultModelDefinition(IResultModelDefinition resultModelDefinition)
        {
            Result<IResultModelDefinition> result = await DbCrud.SaveResultModelDefinition(DbFilepath, resultModelDefinition);
            return result;
        }

        public async Task<Result<bool>> DeleteResultModelDefinition(int id)
        {
            Result<bool> result = await DbCrud.DeleteResultModelDefinition(DbFilepath, id);
            return result;
        }
    }
}
