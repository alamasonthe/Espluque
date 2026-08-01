namespace Espluque.Contracts.DetectionResult
{
    public interface IResultModelDefinition
    {
        int? Id { get; set; }
        string Name { get; set; }
        List<string> Properties { get; set; }
        List<ResultPropertyLink> PropertyLinks { get; set; }
        string? ThesaurusTag { get; set; }
    }
}