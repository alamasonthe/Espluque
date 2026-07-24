using Espluque.Contracts.Interfaces;
using Util;

namespace Espluque.Contracts.Ports
{
    public interface IThesaurusSource
    {
        Task<Result<List<IThesaurusConcept>>> GetConcepts();

        Task<Result<IThesaurusConcept>> GetConceptById(int conceptId);

        Task<Result<(int ConceptId, string MainTerm)?>> GetConceptMainTermByTerm(string referenceName, string term);

        Task<Result<int>> SaveConcept(IThesaurusConcept concept);

        Task<Result> DeleteConcept(int conceptId);

        Task<Result<List<(int ConceptId, string MainTerm)>>> GetAncestorRefs(int conceptId);

        Task<Result<List<(int ConceptId, string MainTerm)>>> GetDescendantRefs(int conceptId);

        Task<Result<List<string>?>> GetAncestorPreferredTerms(string referenceName, string term);

        Task<Result<bool>> GetConceptPathExists(int ancestorConceptId, int descendantConceptId);

        Task<Result> SaveParentChildLink(int parentConceptId, int childConceptId);

        Task<Result<List<(int ParentConceptId, int ChildConceptId)>>> GetAncestorLinks(int conceptId);

        Task<Result<List<(int ParentConceptId, int ChildConceptId)>>> GetDescendantLinks(int conceptId);

        Task<Result<List<string>>> GetReferences();
    }
}