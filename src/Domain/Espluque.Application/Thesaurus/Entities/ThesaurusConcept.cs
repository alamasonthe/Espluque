using Espluque.Contracts.Interfaces;

namespace Espluque.Application.Thesaurus.Entities
{
    public class ThesaurusConcept : IThesaurusConcept
    {
        public int? Id { get; set; }

        public List<IThesaurusTerm> Terms { get; set; } = new();

        public List<IThesaurusConcept> Parents { get; set; } = new();

        public List<IThesaurusConcept> Children { get; set; } = new();
    }
}
