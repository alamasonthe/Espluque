using Espluque.Contracts.Interfaces;
using Espluquer.Entities;

namespace Espluquer.Adapters
{
    internal class ConceptAdapter
    {
        public static ConceptDto FromDomain(IThesaurusConcept concept, IEntityFactory entityFactory)
        {
            ConceptDto conceptDto = new()
            {
                Id = concept.Id,
                Terms = CloneTerms(concept.Terms, entityFactory)
            };

            foreach (IThesaurusConcept parent in concept.Parents)
            {
                conceptDto.Parents.Add(FromDomainReference(parent, entityFactory));
            }

            foreach (IThesaurusConcept child in concept.Children)
            {
                conceptDto.Children.Add(FromDomainReference(child, entityFactory));
            }

            return conceptDto;
        }

        public static IThesaurusConcept ToDomain(ConceptDto conceptDto, IEntityFactory entityFactory)
        {
            List<IThesaurusConcept> parents = [];

            foreach (ConceptDto parentDto in conceptDto.Parents)
            {
                parents.Add(ToDomainReference(parentDto, entityFactory));
            }

            List<IThesaurusConcept> children = [];

            foreach (ConceptDto childDto in conceptDto.Children)
            {
                children.Add(ToDomainReference(childDto, entityFactory));
            }

            return entityFactory.CreateThesaurusConcept(
                conceptDto.Id,
                CloneTerms(conceptDto.Terms, entityFactory),
                parents,
                children);
        }

        private static ConceptDto FromDomainReference(IThesaurusConcept concept, IEntityFactory entityFactory)
        {
            return new ConceptDto
            {
                Id = concept.Id,
                Terms = CloneTerms(concept.Terms, entityFactory)
            };
        }

        private static IThesaurusConcept ToDomainReference(ConceptDto conceptDto, IEntityFactory entityFactory)
        {
            return entityFactory.CreateThesaurusConcept(
                conceptDto.Id,
                CloneTerms(conceptDto.Terms, entityFactory),
                [],
                []);
        }

        private static List<IThesaurusTerm> CloneTerms(List<IThesaurusTerm> terms, IEntityFactory entityFactory)
        {
            List<IThesaurusTerm> clonedTerms = [];

            foreach (IThesaurusTerm term in terms)
            {
                clonedTerms.Add(entityFactory.CreateThesaurusTerm(
                    term.Term,
                    term.NormalizedTerm,
                    term.IsPreferred,
                    term.ReferenceName));
            }

            return clonedTerms;
        }
    }
}
