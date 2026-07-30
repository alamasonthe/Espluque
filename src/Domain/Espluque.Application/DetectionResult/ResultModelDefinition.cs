using Espluque.Contracts.DetectionResult;

namespace Espluque.Application.DetectionResult
{
    public class ResultModelDefinition : IResultModelDefinition
    {
        public string Name { get; set; }
        public string ThesaurusTag { get; set; }
        public List<string> Properties { get; set; } = [];
        public List<ResultPropertyLink> PropertyLinks { get; set; } = [];
    }
}
