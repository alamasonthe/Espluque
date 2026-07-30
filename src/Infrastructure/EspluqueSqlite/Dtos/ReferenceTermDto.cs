using Espluque.Contracts.Interfaces;

namespace EspluqueSqlite.Dtos
{
    public class ReferenceTermDto : IReferenceTerm
    {
        public int? ConceptId { get; set; }

        public string ReferenceName { get; set; } = string.Empty;

        public string Term { get; set; } = string.Empty;

        public string NormalizedTerm { get; set; } = string.Empty;

        public bool IsPreferred { get; set; }

        public string? PreferredTerm { get; set; } = string.Empty;

    }
}