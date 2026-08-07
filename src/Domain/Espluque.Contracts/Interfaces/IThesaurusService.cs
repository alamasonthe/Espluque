using Util;

namespace Espluque.Contracts.Interfaces
{
    public interface IThesaurusService
    {
        Task<List<IThesaurusConcept>> GetConcepts();
        Task<IThesaurusConcept?> GetConceptById(int conceptId);

        Task<TreeNode<IThesaurusConcept>?> GetConceptsTree();

        Task<int?> SaveConcept(IThesaurusConcept concept);

        Task<bool> SaveParentChildLink(int parentConceptId, int childConceptId);

        Task<bool> DeleteConcept(int conceptId);

        Task<List<(int ConceptId, string MainTerm, string Relation)>?> GetNodes(int conceptId);

        Task<List<(int ParentConceptId, int ChildConceptId, string Relation)>?> GetEdges(int conceptId);

        Task<List<string>?> GetAncestorPreferredTerms(IFileFormat fileFormat);

        Task<List<string>> GetReferences();

        Task<(int ConceptId, string MainTerm)?> GetConceptMainTermByTerm(string referenceName, string term);

        Task<List<(int ParentConceptId, int ChildConceptId)>> GetDescendantLinks(int conceptId);

        Task<List<(int ConceptId, string MainTerm)>?> GetDescendantRefs(int conceptId);

        Task<bool?> GetConceptPathExists(int ancestorConceptId, int descendantConceptId);

        Task<List<IReferenceTerm>> GetReferenceTerms(string referenceName);

        Task<List<IReferenceTerm>> GetAlternateTerms(string referenceName);

        Task<bool> SaveReference(string name);
        Task<bool> RenameReference(string oldName, string newName);
        Task<bool> DeleteReference(string name);
    }
}