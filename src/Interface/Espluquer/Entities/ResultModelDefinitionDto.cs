using Espluque.Contracts.DetectionResult;

namespace Espluquer.Entities
{
    internal class ResultModelDefinitionDto: IResultModelDefinition
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string? ThesaurusTag { get; set; }
        public List<string> Properties { get; set; }
        public List<ResultPropertyLink> PropertyLinks { get; set; }

        public string Label => $"{Name} ({ThesaurusTag})";
    }
}
