using Espluque.Contracts.Entities;

namespace Espluque.Application.Entities
{
    public class Assertion : IAssertion
    {
        public string SourceModule { get; set; }
        public string SourceContribution { get; set; }
        public string AssertionType { get; set; }
        public string ClaimJson { get; set; }

        public List<KeyValuePair<string, string>>? Summary { get; set; } = [];
    }
}
