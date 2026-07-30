namespace Espluque.Contracts.Result
{
    public interface IResultModelDefinition
    {
        string Name { get; set; }
        List<string> Properties { get; set; }
        List<ResultPropertyLink> PropertyLinks { get; set; }
        string ThesaurusTag { get; set; }
    }
}