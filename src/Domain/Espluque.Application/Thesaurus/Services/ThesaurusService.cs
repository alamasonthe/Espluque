using Espluque.Contracts.Interfaces;
using Espluque.Contracts.Ports;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Util;

namespace Espluque.Application.Thesaurus.Services
{
    public class ThesaurusService : IThesaurusService
    {
        private static string _treeLevelSeparator = "/";

        private readonly Contracts.Ports.ILogger _logger;
        private readonly IThesaurusSource _thesaurusSource;

        public ThesaurusService(Contracts.Ports.ILogger logger, IThesaurusSource thesaurusSource)
        {
            _logger = logger;
            _thesaurusSource = thesaurusSource;
        }

        public async Task<List<IThesaurusConcept>> GetConcepts()
        {
            var thesaurusConceptsResult = await _thesaurusSource.GetConcepts();
            if (!thesaurusConceptsResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{thesaurusConceptsResult.Error!.Code} {thesaurusConceptsResult.Error.Message}");
                return [];
            }
            return thesaurusConceptsResult.Value!;
        }

        public async Task<TreeNode<IThesaurusConcept>?> GetConceptsTree()
        {
            var thesaurusConceptsResult = await _thesaurusSource.GetConcepts();
            if (!thesaurusConceptsResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{thesaurusConceptsResult.Error.Code} {thesaurusConceptsResult.Error.Message}");
                return null;
            }

            var flatConceptTreeResult = CreateFlatTreeItems(thesaurusConceptsResult.Value!);
            if (!flatConceptTreeResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{flatConceptTreeResult.Error.Code} {flatConceptTreeResult.Error.Message}");
                return null;
            }

            TreeNode<IThesaurusConcept> tree = TreeBuilder.Build(
                flatConceptTreeResult.Value!,
                [_treeLevelSeparator],
                "Thesaurus");

            return tree;
        }

        public async Task<int?> SaveConcept(IThesaurusConcept concept)
        {
            foreach (IThesaurusTerm term in concept.Terms)
            {
                term.NormalizedTerm = NormalizeTerm(term.Term ?? string.Empty);
            }

            var saveConceptResult = await _thesaurusSource.SaveConcept(concept);
            if (!saveConceptResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{saveConceptResult.Error.Code} {saveConceptResult.Error.Message}");
                return null;
            }

            IThesaurusTerm? preferredTerm = concept.Terms.FirstOrDefault(term => term.IsPreferred);
            _logger.Log( LogLevel.Information, $"SAVE_CONCEPT_SUCCESS: concept ({saveConceptResult.Value} {preferredTerm?.Term}) saved.");

            return saveConceptResult.Value;
        }

        public async Task<bool> SaveParentChildLink(int parentConceptId, int childConceptId)
        {
            if (parentConceptId == childConceptId)
            {
                _logger.Log(LogLevel.Error, $"Cannot create link because concept {parentConceptId} cannot be parent of itself.");
                return false;
            }

            var isPathExistsResult = await _thesaurusSource.GetConceptPathExists(childConceptId, parentConceptId);
            if (!isPathExistsResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{isPathExistsResult.Error.Code} {isPathExistsResult.Error.Message}");
                return false;
            }

            if (isPathExistsResult.Value)
            {
                _logger.Log(LogLevel.Error, $"Cannot create link between parent {parentConceptId} and child {childConceptId} because concept {childConceptId} is an ancestor of concept {parentConceptId}.");
                return false;
            }

            var isSavedResult = await _thesaurusSource.SaveParentChildLink(parentConceptId, childConceptId);
            if (!isSavedResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{isSavedResult.Error.Code} {isSavedResult.Error.Message}");
                return false;
            }

            _logger.Log(LogLevel.Information, $"INSERT_CONCEPT_LINK_SUCCESS: concept {parentConceptId} is now parent of {childConceptId}.");
            return true;
        }

        public async Task<bool> DeleteConcept(int conceptId)
        {
            var deleteConceptResult = await _thesaurusSource.DeleteConcept(conceptId);
            if (!deleteConceptResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{deleteConceptResult.Error.Code} {deleteConceptResult.Error.Message}");
                return false;
            }
            _logger.Log(LogLevel.Information, $"DELETE_CONCEPT_SUCCESS: concept {conceptId} deleted.");
            return true;
        }

        public async Task<List<string>?> GetAncestorPreferredTerms(IFileFormat fileFormat)
        {
            string? referenceName;
            string? term;

            if (!string.IsNullOrWhiteSpace(fileFormat.MIMEType))
            {
                referenceName = "MIMEType";
                term = fileFormat.MIMEType;
            }
            else
            {
                referenceName = fileFormat.Referentiel;
                term = fileFormat.Label;
            }

            if (string.IsNullOrWhiteSpace(referenceName) || string.IsNullOrWhiteSpace(term))
            {
                return null;
            }

            var ancestorPreferredTermsResult = await _thesaurusSource.GetAncestorPreferredTerms(
                referenceName,
                term);

            if (!ancestorPreferredTermsResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{ancestorPreferredTermsResult.Error.Code} {ancestorPreferredTermsResult.Error.Message}");
                return null;
            }

            return ancestorPreferredTermsResult.Value;
        }

        public async Task<List<string>> GetReferences()
        {
            var referencesResult = await _thesaurusSource.GetReferences();
            if (!referencesResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{referencesResult.Error!.Code} {referencesResult.Error.Message}");
                return [];
            }

            return referencesResult.Value!;
        }

        public async Task<(int ConceptId, string MainTerm)?> GetConceptMainTermByTerm(string referenceName, string term)
        {
            var mainTermResult = await _thesaurusSource.GetConceptMainTermByTerm(referenceName, term);
            if (!mainTermResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{mainTermResult.Error!.Code} {mainTermResult.Error.Message}");
                return null;
            }

            return mainTermResult.Value!;
        }

        #region Thesaurus graph

        public async Task<List<(int ConceptId, string MainTerm, string Relation)>?> GetNodes(int conceptId)
        {
            var ancestorsResult = await _thesaurusSource.GetAncestorRefs(conceptId);
            if (!(ancestorsResult.IsSuccess))
            {
                _logger.Log(LogLevel.Error, $"{ancestorsResult.Error.Code} {ancestorsResult.Error.Message}");
                return null;
            }
            var ancestors = ancestorsResult.Value;

            var selectedConceptResult = await _thesaurusSource.GetConceptById(conceptId);
            if (!(selectedConceptResult.IsSuccess))
            {
                _logger.Log(LogLevel.Error, $"{selectedConceptResult.Error.Code} {selectedConceptResult.Error.Message}");
                return null;
            }
            var selectedConcept = selectedConceptResult.Value;

            var descendantsResult = await _thesaurusSource.GetDescendantRefs(conceptId);
            if (!(descendantsResult.IsSuccess))
            {
                _logger.Log(LogLevel.Error, $"{descendantsResult.Error.Code} {descendantsResult.Error.Message}");
                return null;
            }
            var descendants = descendantsResult.Value;

            IThesaurusTerm? selectedConceptMainTerm = selectedConcept.Terms.FirstOrDefault(term => term.IsPreferred);

            List<(int ConceptId, string MainTerm, string Relation)> nodes = [];

            nodes.AddRange((ancestors ?? []).Select(ancestor => (
                ConceptId: ancestor.ConceptId,
                MainTerm: ancestor.MainTerm,
                Relation: "Ancestor")));

            if (selectedConcept.Id.HasValue && selectedConceptMainTerm is not null)
            {
                nodes.Add((
                    ConceptId: selectedConcept.Id.Value,
                    MainTerm: selectedConceptMainTerm.Term ?? string.Empty,
                    Relation: "Selected"));
            }

            nodes.AddRange((descendants ?? []).Select(descendant => (
                ConceptId: descendant.ConceptId,
                MainTerm: descendant.MainTerm,
                Relation: "Descendant")));

            return nodes
                .GroupBy(node => node.ConceptId)
                .Select(group => group
                    .OrderBy(node => node.Relation == "Selected" ? 0 :
                                     node.Relation == "Ancestor" ? 1 : 2)
                    .First())
                .ToList();
        }

        public async Task<List<(int ParentConceptId, int ChildConceptId, string Relation)>?> GetEdges(int conceptId)
        {
            var ancestorLinksResult = await _thesaurusSource.GetAncestorLinks(conceptId);
            if (!(ancestorLinksResult.IsSuccess))
            {
                _logger.Log(LogLevel.Error, $"{ancestorLinksResult.Error.Code} {ancestorLinksResult.Error.Message}");
                return null;
            }
            var ancestorLinks = ancestorLinksResult.Value;

            var descendantLinksResult = await _thesaurusSource.GetDescendantLinks(conceptId);
            if (!(descendantLinksResult.IsSuccess))
            {
                _logger.Log(LogLevel.Error, $"{descendantLinksResult.Error.Code} {descendantLinksResult.Error.Message}");
                return null;
            }
            var descendantLinks = descendantLinksResult.Value;

            List<(int ParentConceptId, int ChildConceptId, string Relation)> edges = [];

            edges.AddRange((ancestorLinks ?? []).Select(link => (
                ParentConceptId: link.ParentConceptId,
                ChildConceptId: link.ChildConceptId,
                Relation: "Ancestor")));

            edges.AddRange((descendantLinks ?? []).Select(link => (
                ParentConceptId: link.ParentConceptId,
                ChildConceptId: link.ChildConceptId,
                Relation: "Descendant")));

            return edges;
        }

        #endregion

        public async Task<List<(int ParentConceptId, int ChildConceptId)>> GetDescendantLinks(int conceptId)
        {
            var descendantLinksResult = await _thesaurusSource.GetDescendantLinks(conceptId);
            if (!(descendantLinksResult.IsSuccess))
            {
                _logger.Log(LogLevel.Error, $"{descendantLinksResult.Error.Code} {descendantLinksResult.Error.Message}");
                return [];
            }
            return descendantLinksResult.Value;
        }

        public async Task<List<(int ConceptId, string MainTerm)>?> GetDescendantRefs(int conceptId)
        {
            var descendantRefsResult = await _thesaurusSource.GetDescendantRefs(conceptId);

            if (!descendantRefsResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"{descendantRefsResult.Error.Code} {descendantRefsResult.Error.Message}");
                return null;
            }

            return descendantRefsResult.Value;
        }

        public async Task<bool?> GetConceptPathExists( int ancestorConceptId, int descendantConceptId)
        {
            var conceptPathExistsResult = await _thesaurusSource.GetConceptPathExists(
                ancestorConceptId,
                descendantConceptId);

            if (!conceptPathExistsResult.IsSuccess)
            {
                _logger.Log( LogLevel.Error, $"{conceptPathExistsResult.Error.Code} {conceptPathExistsResult.Error.Message}");
                return null;
            }

            return conceptPathExistsResult.Value;
        }

        public async Task<List<IReferenceTerm>> GetReferenceTerms(string referenceName)
        {
            var referenceTermsResult = await _thesaurusSource.GetReferenceTerms(
                referenceName,
                "Reference");

            if (!referenceTermsResult.IsSuccess)
            {
                _logger.Log(
                    LogLevel.Error,
                    $"{referenceTermsResult.Error.Code} {referenceTermsResult.Error.Message}");

                return [];
            }

            return referenceTermsResult.Value!;
        }

        public async Task<List<IReferenceTerm>> GetAlternateTerms(string referenceName)
        {
            var alternateTermsResult = await _thesaurusSource.GetReferenceTerms(
                referenceName,
                "Alternate");

            if (!alternateTermsResult.IsSuccess)
            {
                _logger.Log(
                    LogLevel.Error,
                    $"{alternateTermsResult.Error.Code} {alternateTermsResult.Error.Message}");

                return [];
            }

            return alternateTermsResult.Value!;
        }


        #region Reference

        public async Task<bool> SaveReference(string name)
        {
            var saveReferenceResult =
                await _thesaurusSource.SaveReference(name);

            if (!saveReferenceResult.IsSuccess)
            {
                _logger.Log(
                    LogLevel.Error,
                    $"{saveReferenceResult.Error.Code} {saveReferenceResult.Error.Message}");

                return false;
            }

            _logger.Log(
                LogLevel.Information,
                $"SAVE_REFERENCE_SUCCESS: reference {name} saved.");

            return true;
        }

        public async Task<bool> RenameReference(string oldName, string newName)
        {
            var renameReferenceResult =
                await _thesaurusSource.RenameReference(oldName, newName);

            if (!renameReferenceResult.IsSuccess)
            {
                _logger.Log(
                    LogLevel.Error,
                    $"{renameReferenceResult.Error.Code} {renameReferenceResult.Error.Message}");

                return false;
            }

            _logger.Log(
                LogLevel.Information,
                $"RENAME_REFERENCE_SUCCESS: reference {oldName} renamed to {newName}.");

            return true;
        }

        public async Task<bool> DeleteReference(string name)
        {
            var deleteReferenceResult =
                await _thesaurusSource.DeleteReference(name);

            if (!deleteReferenceResult.IsSuccess)
            {
                _logger.Log(
                    LogLevel.Error,
                    $"{deleteReferenceResult.Error.Code} {deleteReferenceResult.Error.Message}");

                return false;
            }

            _logger.Log(
                LogLevel.Information,
                $"DELETE_REFERENCE_SUCCESS: reference {name} deleted.");

            return true;
        }

        #endregion


        #region Helpers

        private static Result<List<(string Path, bool IsLeaf, IThesaurusConcept Data)>> CreateFlatTreeItems(List<IThesaurusConcept> concepts)
        {
            List<(string Path, bool IsLeaf, IThesaurusConcept Data)> treeItems = [];

            List<IThesaurusConcept> rootConcepts = concepts
                .Where(concept => concept.Parents.Count == 0)
                .ToList();

            foreach (IThesaurusConcept rootConcept in rootConcepts)
            {
                Result<bool> result = AddBranchItems(
                    rootConcept,
                    string.Empty,
                    treeItems,
                    new HashSet<int>());

                if (!result.IsSuccess)
                {
                    return Result<List<(string Path, bool IsLeaf, IThesaurusConcept Data)>>.Failure(result.Error!.Code, result.Error.Message);
                }
            }

            return Result<List<(string Path, bool IsLeaf, IThesaurusConcept Data)>>.Success(treeItems);
        }

        private static Result<bool> AddBranchItems(
            IThesaurusConcept concept,
            string parentPath,
            List<(string Path, bool IsLeaf, IThesaurusConcept Data)> treeItems,
            HashSet<int> currentBranchConceptIds)
        {
            if (concept.Id is null)
            {
                return Result<bool>.Failure("THESAURUS_CONCEPT_ID_MISSING", "Thesaurus concept id is missing.");
            }

            if (!currentBranchConceptIds.Add(concept.Id.Value))
            {
                return Result<bool>.Failure("THESAURUS_CONCEPT_LOOP_DETECTED", "A thesaurus concept loop was detected.");
            }

            string? normalizedTerm = null;

            foreach (IThesaurusTerm term in concept.Terms)
            {
                if (term.IsPreferred)
                {
                    normalizedTerm = term.NormalizedTerm;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(normalizedTerm))
            {
                return Result<bool>.Failure("THESAURUS_CONCEPT_PREFERRED_TERM_MISSING", "Thesaurus concept preferred normalized term is missing.");
            }

            string path = string.IsNullOrWhiteSpace(parentPath)
                ? normalizedTerm
                : parentPath + "/" + normalizedTerm;

            treeItems.Add((
                path,
                concept.Children.Count == 0,
                concept));

            foreach (IThesaurusConcept child in concept.Children)
            {
                Result<bool> childResult = AddBranchItems(
                    child,
                    path,
                    treeItems,
                    new HashSet<int>(currentBranchConceptIds));

                if (!childResult.IsSuccess)
                {
                    return childResult;
                }
            }

            return Result<bool>.Success(true);
        }

        private static string NormalizeTerm(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return string.Empty;
            }

            string normalizedTerm = term.Trim();

            normalizedTerm = Regex.Replace(normalizedTerm, @"\s+", " ");

            normalizedTerm = Regex.Replace(
                normalizedTerm,
                $@"\s*{Regex.Escape(_treeLevelSeparator)}\s*",
                " ");

            normalizedTerm = Regex.Replace(normalizedTerm, @"\s+", " ");

            normalizedTerm = normalizedTerm
                .Trim()
                .Normalize(NormalizationForm.FormD);

            StringBuilder stringBuilder = new();

            foreach (char character in normalizedTerm)
            {
                UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);

                if (unicodeCategory == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                stringBuilder.Append(character);
            }

            string normalizedWithoutDiacritics = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

            normalizedWithoutDiacritics = Regex.Replace(normalizedWithoutDiacritics, @"\s+", " ").Trim();

            return Util.String.ToPascalCase(normalizedWithoutDiacritics);
        }

        #endregion

    }
}