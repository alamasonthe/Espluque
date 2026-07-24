using Espluque.Contracts.Interfaces;

namespace Espluque.Application.Thesaurus.Entities
{
    public class ThesaurusTerm : IThesaurusTerm
    {
        public string? Term { get; set; }

        public string? NormalizedTerm { get; set; }

        public bool IsPrefered { get; set; } = false;

        public string? ReferenceName { get; set; }
    }
}
