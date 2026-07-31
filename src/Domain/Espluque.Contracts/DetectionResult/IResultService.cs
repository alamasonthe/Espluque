namespace Espluque.Contracts.DetectionResult
{
    public interface IResultService
    {
        Task<bool> DeleteResultModelDefinition(int id);
        Task<IResultModelDefinition?> GetResultModelDefinition(int id);
        Task<List<IResultModelDefinition>> GetResultModelDefinitions();
        Task<List<IResultModelDefinition>> GetResultModelDefinitions(string thesaurusTag);
        Task<IResultModelDefinition?> SaveResultModelDefinition(IResultModelDefinition resultModelDefinition);
    }
}