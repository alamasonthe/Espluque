namespace Espluque.Contracts.Thesaurus
{
    public interface IReferenceTerm
    {
        int? ConceptId { get; set; }

        string ReferenceName { get; set; }

        string Term { get; set; }

        string NormalizedTerm { get; set; }

        bool IsPreferred { get; set; }

        string? PreferredTerm { get; set; }
    }
}
