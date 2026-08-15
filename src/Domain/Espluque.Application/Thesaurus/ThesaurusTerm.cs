using Espluque.Contracts.Thesaurus;

namespace Espluque.Application.Thesaurus
{
    /// <summary>
    /// Represents a term used to identify a thesaurus concept.
    /// </summary>
    /// <remarks>
    /// Each concept has a preferred term that acts as its canonical name throughout Espluque.
    /// Preferred terms identify concepts in thesaurus paths and are used as tags to associate and trigger contributions during analysis.
    ///
    /// Alternate terms allow the same concept to be recognized through other vocabularies or representations.
    /// ReferenceName identifies the reference vocabulary of the term; NormalizedTerm provides its normalized form for thesaurus navigation.
    /// </remarks>

    public class ThesaurusTerm : IThesaurusTerm
    {
        public string? Term { get; set; }

        public string? NormalizedTerm { get; set; }

        public bool IsPreferred { get; set; } = false;

        public string? ReferenceName { get; set; }
    }
}
