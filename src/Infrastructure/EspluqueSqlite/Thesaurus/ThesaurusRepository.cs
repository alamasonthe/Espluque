using Espluque.Contracts.Interfaces;
using Espluque.Contracts.Ports;
using System.Diagnostics;
using System.Reflection;
using Util;

namespace EspluqueSqlite.Thesaurus
{
    public class ThesaurusRepository : IThesaurusSource
    {
        public string DbFilepath;

        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly IEntityFactory _entityFactory;

        public ThesaurusRepository(ILogger logger, IEntityFactory entityFactory, ISettingsService settingsService)
        {
            _logger = logger;
            _entityFactory = entityFactory;

            DbFilepath = DbFile.GetDbFilePath(settingsService);

            _logger.Log(Microsoft.Extensions.Logging.LogLevel.Debug, $"Espluque DB filepath: {DbFilepath}");
        }


        #region Concepts

        public async Task<Result<List<IThesaurusConcept>>> GetConcepts()
        {
            var thesaurusConceptsResult = await DbReader.GetConcepts(DbFilepath, _entityFactory);
            return thesaurusConceptsResult;
        }

        public async Task<Result<IThesaurusConcept>> GetConceptById(int conceptId)
        {
            var thesaurusConceptResult = await DbReader.GetConceptById(DbFilepath, conceptId, _entityFactory);
            return thesaurusConceptResult;
        }

        public async Task<Result<(int ConceptId, string MainTerm)?>> GetConceptMainTermByTerm(string referenceName, string term)
        {
            var thesaurusConceptResult = await DbReader.GetConceptMainTermByTerm(DbFilepath, referenceName, term);
            return thesaurusConceptResult;
        }

        public async Task<Result<int>> SaveConcept(IThesaurusConcept thesaurusConcept)
        {
            var thesaurusConceptSaveResult = await DbSaver.SaveConcept(DbFilepath, thesaurusConcept);
            return thesaurusConceptSaveResult;
        }

        public async Task<Result> DeleteConcept(int conceptId)
        {
            var thesaurusConceptDeleteResult = await DbSaver.DeleteConcept(DbFilepath, conceptId);
            return thesaurusConceptDeleteResult;
        }

        public async Task<Result<List<(int ConceptId, string MainTerm)>>> GetAncestorRefs(int conceptId)
        {
            var ancestorsResult = await DbReader.GetAncestorConcepts(DbFilepath, conceptId);
            return ancestorsResult;
        }

        public async Task<Result<List<(int ConceptId, string MainTerm)>>> GetDescendantRefs(int conceptId)
        {
            var descendantsResult = await DbReader.GetDescendantConcepts(DbFilepath, conceptId);
            return descendantsResult;
        }

        #endregion


        #region Terms

        public async Task<Result<List<string>?>> GetAncestorPreferredTerms(string referenceName, string term)
        {
            var ancestorPreferredTermsResult = await DbReader.GetAncestorPreferredTerms(DbFilepath, referenceName, term);
            return ancestorPreferredTermsResult;
        }

        #endregion


        #region Concept links

        public async Task<Result<bool>> GetConceptPathExists(int ancestorConceptId, int descendantConceptId)
        {
            var isPathExistsResult = await DbReader.GetConceptPathExists(DbFilepath, ancestorConceptId, descendantConceptId);
            return isPathExistsResult;
        }

        public async Task<Result> SaveParentChildLink(int parentConceptId, int childConceptId)
        {
            var saveLinkResult = await DbSaver.SaveParentChildLink(DbFilepath, parentConceptId, childConceptId);
            return saveLinkResult;
        }

        public async Task<Result<List<(int ParentConceptId, int ChildConceptId)>>> GetAncestorLinks(int conceptId)
        {
            var ancestorLinksResult = await DbReader.GetAncestorLinks(DbFilepath, conceptId);
            return ancestorLinksResult;
        }

        public async Task<Result<List<(int ParentConceptId, int ChildConceptId)>>> GetDescendantLinks(int conceptId)
        {
            var descendantLinksResult = await DbReader.GetDescendantLinks(DbFilepath, conceptId);
            return descendantLinksResult;
        }

        
        #endregion


        #region References

        public async Task<Result<List<string>>> GetReferences()
        {
            var referencesResult = await DbReader.GetReferences(DbFilepath);
            return referencesResult;
        }

        #endregion

        #region reference

        public async Task<Result<List<IReferenceTerm>>> GetReferenceTerms( string referenceName, string referenceTermScope)
        {
            var referenceTermsResult = await DbReader.GetReferenceTerms(
                DbFilepath,
                referenceName,
                referenceTermScope);

            return referenceTermsResult;
        }

        public async Task<Result> SaveReference(string name)
        {
            var saveReferenceResult = await DbSaver.SaveReference(
                DbFilepath,
                name);

            return saveReferenceResult;
        }

        public async Task<Result> RenameReference(string oldName, string newName)
        {
            var renameReferenceResult = await DbSaver.RenameReference(
                DbFilepath,
                oldName,
                newName);

            return renameReferenceResult;
        }

        public async Task<Result> DeleteReference(string name)
        {
            var deleteReferenceResult = await DbSaver.DeleteReference(
                DbFilepath,
                name);

            return deleteReferenceResult;
        }

        #endregion
    }
}
