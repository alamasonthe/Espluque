using Espluque.Contracts.Interfaces;

namespace Espluquer.Entities
{
    internal class ReferenceTermDto: IReferenceTerm
    {
        public int? ConceptId { get; set; }

        public string ReferenceName { get; set; } = string.Empty;

        public string Term { get; set; } = string.Empty;

        public string NormalizedTerm { get; set; } = string.Empty;

        public bool IsPreferred { get; set; }

        public string? PreferredTerm { get; set; }

        public bool IsLinked => ConceptId.HasValue;

        public bool IsAlternative =>
            ConceptId.HasValue && !IsPreferred;

        public string Status =>
            !ConceptId.HasValue
                ? "Unlinked"
                : ConceptTermCount == 1
                    ? "Sole term"
                    : IsPreferred
                        ? "Preferred"
                        : "Alternative";

        public string ConceptDisplay =>
            ConceptId.HasValue
                ? $"#{ConceptId.Value}"
                : "—";

        public int ConceptTermCount { get; set; }
    }
}