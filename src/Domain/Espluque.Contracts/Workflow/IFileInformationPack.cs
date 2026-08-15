namespace Espluque.Contracts.Workflow
{
    public interface IFileInformationPack
    {
        string Label { get; set; }

        List<KeyValuePair<string, string>>? Information { get; set; }
    }
}
