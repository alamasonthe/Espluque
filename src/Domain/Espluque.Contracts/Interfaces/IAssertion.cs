namespace Espluque.Contracts.Entities
{
    public interface IAssertion
    {
        string AssertionType { get; set; }
        string ClaimJson { get; set; }
        string SourceContribution { get; set; }
        string SourceModule { get; set; }

        List<KeyValuePair<string, string>>? Summary { get; set; }
    }
}