using Espluque.Contracts.Thesaurus;

namespace Espluque.Application.Thesaurus
{
    /// <summary>
    /// Represents a semantic concept at the core of the Espluque thesaurus.
    /// </summary>
    /// <remarks>
    /// A concept groups equivalent terms under a preferred term that acts as its canonical identifier.
    /// This preferred term is also used as the tag linking thesaurus concepts to contributions executed by the analysis engines.
    ///
    /// Parent/child relations form the multi-parent semantic graph used to navigate specialization and inheritance,
    /// allowing contributions associated with a concept or its ancestors to participate in the analysis.
    /// </remarks>

    public class ThesaurusConcept : IThesaurusConcept
    {
        public int? Id { get; set; }

        public List<IThesaurusTerm> Terms { get; set; } = new();

        public List<IThesaurusConcept> Parents { get; set; } = new();

        public List<IThesaurusConcept> Children { get; set; } = new();
    }
}
