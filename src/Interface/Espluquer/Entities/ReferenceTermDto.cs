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

        private string? _preferredTerm;

        public string? PreferredTerm
        {
            get => string.IsNullOrEmpty(Status)
                ? _preferredTerm
                : null;

            set => _preferredTerm = value;
        }

        public bool IsLinked => ConceptId.HasValue;

        public bool IsAlternative =>
            ConceptId.HasValue && !IsPreferred;

        public string Status =>
            !ConceptId.HasValue
                ? string.Empty
                : ConceptTermCount == 1
                    ? "Sole term"
                    : IsPreferred
                        ? "Preferred"
                        : string.Empty;

        public string ConceptDisplay =>
            ConceptId.HasValue
                ? $"#{ConceptId.Value}"
                : "—";

        public int ConceptTermCount { get; set; }
    }
}