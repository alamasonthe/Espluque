using Util;

namespace Espluque.Contracts.DetectionResult
{
    public interface IResultSource
    {
        Task<Result<bool>> DeleteResultModelDefinition(int id);
        Task<Result<IResultModelDefinition?>> GetResultModelDefinition(int id);
        Task<Result<List<IResultModelDefinition>>> GetResultModelDefinitions();
        Task<Result<List<IResultModelDefinition>>> GetResultModelDefinitions(string thesaurusTag);
        Task<Result<IResultModelDefinition>> SaveResultModelDefinition(IResultModelDefinition resultModelDefinition);
    }
}