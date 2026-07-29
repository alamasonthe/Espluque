using Espluque.Contracts.Interfaces;
using EspluqueSqlite.Dtos;
using Microsoft.Data.Sqlite;
using Util;

namespace EspluqueSqlite.Thesaurus
{
    internal class DbReader
    {
        #region Getters

        public static async Task<Result<List<IThesaurusConcept>>> GetConcepts( string dbFilepath, IEntityFactory entityFactory)
        {
            using SqliteConnection connection = SqliteUtil.DbConnectionFactory.CreateConnection(dbFilepath);

            try
            {
                await connection.OpenAsync();

                Result<List<ThesaurusConceptDto>> conceptDtosResult = await ReadConceptDtos(connection);
                if (!conceptDtosResult.IsSuccess)
                {
                    return Result<List<IThesaurusConcept>>.Failure(conceptDtosResult.Error!.Code, conceptDtosResult.Error.Message);
                }

                Result<List<ThesaurusTermDto>> termDtosResult = await ReadTermDtos(connection);
                if (!termDtosResult.IsSuccess)
                {
                    return Result<List<IThesaurusConcept>>.Failure(termDtosResult.Error!.Code, termDtosResult.Error.Message);
                }

                Result<List<ThesaurusConceptLinkDto>> linkDtosResult = await ReadConceptLinkDtos(connection);
                if (!linkDtosResult.IsSuccess)
                {
                    return Result<List<IThesaurusConcept>>.Failure(linkDtosResult.Error!.Code, linkDtosResult.Error.Message);
                }

                Dictionary<int, IThesaurusConcept> conceptsById = CreateConceptsById(
                    conceptDtosResult.Value!,
                    termDtosResult.Value!,
                    entityFactory);

                List<IThesaurusConcept> concepts = CreateConceptListWithLinks( conceptsById, linkDtosResult.Value!);

                return Result<List<IThesaurusConcept>>.Success(concepts);

            }
            catch (Exception exception)
            {
                return Result<List<IThesaurusConcept>>.Failure("THESAURUS_CONCEPTS_READ_FAILED", exception.Message);
            }
        }

        public static async Task<Result<IThesaurusConcept>> GetConceptById(string dbFilepath, int conceptId, IEntityFactory entityFactory)
        {
            using SqliteConnection connection = SqliteUtil.DbConnectionFactory.CreateConnection(dbFilepath);

            try
            {
                await connection.OpenAsync();

                Result<ThesaurusConceptDto> conceptDtoResult = await ReadConceptDtoById(
                    connection,
                    conceptId);

                if (!conceptDtoResult.IsSuccess)
                {
                    return Result<IThesaurusConcept>.Failure(conceptDtoResult.Error!.Code, conceptDtoResult.Error.Message);
                }

                Result<List<ThesaurusTermDto>> termDtosResult = await ReadTermDtosByConceptId(
                    connection,
                    conceptId);

                if (!termDtosResult.IsSuccess)
                {
                    return Result<IThesaurusConcept>.Failure(termDtosResult.Error!.Code, termDtosResult.Error.Message);
                }

                List<IThesaurusTerm> terms = CreateTerms(
                    termDtosResult.Value!,
                    entityFactory);

                IThesaurusConcept concept = entityFactory.CreateThesaurusConcept(
                    conceptDtoResult.Value!.Id,
                    terms,
                    [],
                    []);

                return Result<IThesaurusConcept>.Success(concept);
            }
            catch (Exception exception)
            {
                return Result<IThesaurusConcept>.Failure("THESAURUS_CONCEPT_BY_ID_READ_FAILED", exception.Message);
            }
        }

        public static async Task<Result<bool>> GetConceptPathExists( string dbFilepath, int ancestorConceptId, int descendantConceptId)
        {
            using SqliteConnection connection = SqliteUtil.DbConnectionFactory.CreateConnection(dbFilepath);

            try
            {
                await connection.OpenAsync();

                Result<bool> conceptPathExistsResult = await ReadConceptPathExists(
                    connection,
                    ancestorConceptId,
                    descendantConceptId);

                if (!conceptPathExistsResult.IsSuccess)
                {
                    return Result<bool>.Failure(conceptPathExistsResult.Error!.Code, conceptPathExistsResult.Error.Message);
                }

                return conceptPathExistsResult;
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure("THESAURUS_CONCEPT_PATH_EXISTS_READ_FAILED", exception.Message);
            }
        }

        public static async Task<Result<List<(int ConceptId, string MainTerm)>>> GetAncestorConcepts( string dbFilepath, int conceptId)
        {
            using SqliteConnection connection = SqliteUtil.DbConnectionFactory.CreateConnection(dbFilepath);

            try
            {
                await connection.OpenAsync();

                Result<List<(int ConceptId, string MainTerm)>> ancestorsResult = await ReadAncestors(
                    connection,
                    conceptId);

                if (!ancestorsResult.IsSuccess)
                {
                    return Result<List<(int ConceptId, string MainTerm)>>.Failure(ancestorsResult.Error!.Code, ancestorsResult.Error.Message);
                }

                return ancestorsResult;
            }
            catch (Exception exception)
            {
                return Result<List<(int ConceptId, string MainTerm)>>.Failure("THESAURUS_ANCESTORS_READ_FAILED", exception.Message);
            }
        }

        public static async Task<Result<List<(int ConceptId, string MainTerm)>>> GetDescendantConcepts( string dbFilepath, int conceptId)
        {
            using SqliteConnection connection = SqliteUtil.DbConnectionFactory.CreateConnection(dbFilepath);

            try
            {
                await connection.OpenAsync();

                Result<List<(int ConceptId, string MainTerm)>> descendantsResult = await ReadDescendants(
                    connection,
                    conceptId);

                if (!descendantsResult.IsSuccess)
                {
                    return Result<List<(int ConceptId, string MainTerm)>>.Failure(descendantsResult.Error!.Code, descendantsResult.Error.Message);
                }

                return descendantsResult;
            }
            catch (Exception exception)
            {
                return Result<List<(int ConceptId, string MainTerm)>>.Failure("THESAURUS_DESCENDANTS_READ_FAILED", exception.Message);
            }
        }

        public static async Task<Result<List<(int ParentConceptId, int ChildConceptId)>>> GetAncestorLinks( string dbFilepath, int conceptId)
        {
            using SqliteConnection connection = SqliteUtil.DbConnectionFactory.CreateConnection(dbFilepath);

            try
            {
                await connection.OpenAsync();

                Result<List<(int ParentConceptId, int ChildConceptId)>> ancestorLinksResult = await ReadAncestorLinks(
                    connection,
                    conceptId);

                if (!ancestorLinksResult.IsSuccess)
                {
                    return Result<List<(int ParentConceptId, int ChildConceptId)>>.Failure(ancestorLinksResult.Error!.Code, ancestorLinksResult.Error.Message);
                }

                return ancestorLinksResult;
            }
            catch (Exception exception)
            {
                return Result<List<(int ParentConceptId, int ChildConceptId)>>.Failure("THESAURUS_ANCESTOR_LINKS_READ_FAILED", exception.Message);
            }
        }

        public static async Task<Result<List<(int ParentConceptId, int ChildConceptId)>>> GetDescendantLinks( string dbFilepath, int conceptId)
        {
            using SqliteConnection connection = SqliteUtil.DbConnectionFactory.CreateConnection(dbFilepath);

            try
            {
                await connection.OpenAsync();

                Result<List<(int ParentConceptId, int ChildConceptId)>> descendantLinksResult = await ReadDescendantLinks(
                    connection,
                    conceptId);

                if (!descendantLinksResult.IsSuccess)
                {
                    return Result<List<(int ParentConceptId, int ChildConceptId)>>.Failure(descendantLinksResult.Error!.Code, descendantLinksResult.Error.Message);
                }

                return descendantLinksResult;
            }
            catch (Exception exception)
            {
                return Result<List<(int ParentConceptId, int ChildConceptId)>>.Failure("THESAURUS_DESCENDANT_LINKS_READ_FAILED", exception.Message);
            }
        }

        public static async Task<Result<List<string>?>> GetAncestorPreferredTerms( string dbFilepath, string referenceName, string term)
        {
            using SqliteConnection connection = SqliteUtil.DbConnectionFactory.CreateConnection(dbFilepath);

            try
            {
                await connection.OpenAsync();

                Result<(int ConceptId, string MainTerm)?> conceptMainTermResult = await ReadConceptMainTermByTerm(
                    connection,
                    referenceName,
                    term);

                if (!conceptMainTermResult.IsSuccess)
                {
                    return Result<List<string>?>.Failure(conceptMainTermResult.Error!.Code, conceptMainTermResult.Error.Message);
                }

                if (conceptMainTermResult.Value is not (int conceptId, string mainTerm))
                {
                    return Result<List<string>?>.Success(null);
                }

                Result<List<(int ConceptId, string MainTerm)>> ancestorsResult = await ReadAncestors(
                    connection,
                    conceptId);

                if (!ancestorsResult.IsSuccess)
                {
                    return Result<List<string>?>.Failure(ancestorsResult.Error!.Code, ancestorsResult.Error.Message);
                }

                List<string> preferredTerms = [mainTerm];

                foreach ((int _, string ancestorMainTerm) in ancestorsResult.Value!)
                {
                    if (preferredTerms.Any(preferredTerm => string.Equals(preferredTerm, ancestorMainTerm, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    preferredTerms.Add(ancestorMainTerm);
                }

                return Result<List<string>?>.Success(preferredTerms);
            }
            catch (Exception exception)
            {
                return Result<List<string>?>.Failure("THESAURUS_ANCESTOR_PREFERRED_TERMS_READ_FAILED", exception.Message);
            }
        }

        public static async Task<Result<List<string>>> GetReferences(string dbFilepath)
        {
            using SqliteConnection connection = SqliteUtil.DbConnectionFactory.CreateConnection(dbFilepath);

            try
            {
                await connection.OpenAsync();

                Result<List<string>> referencesResult = await ReadReferences(connection);

                if (!referencesResult.IsSuccess)
                {
                    return Result<List<string>>.Failure(referencesResult.Error!.Code, referencesResult.Error.Message);
                }

                return referencesResult;
            }
            catch (Exception exception)
            {
                return Result<List<string>>.Failure("THESAURUS_REFERENCES_READ_FAILED", exception.Message);
            }
        }

        public static async Task<Result<(int ConceptId, string MainTerm)?>> GetConceptMainTermByTerm(
            string dbFilepath,
            string referenceName,
            string term)
        {
            using SqliteConnection connection = SqliteUtil.DbConnectionFactory.CreateConnection(dbFilepath);

            try
            {
                await connection.OpenAsync();

                Result<(int ConceptId, string MainTerm)?> conceptMainTermResult =
                    await ReadConceptMainTermByTerm(connection, referenceName, term);

                if (!conceptMainTermResult.IsSuccess)
                {
                    return Result<(int ConceptId, string MainTerm)?>.Failure(
                        conceptMainTermResult.Error!.Code,
                        conceptMainTermResult.Error.Message);
                }

                return conceptMainTermResult;
            }
            catch (Exception exception)
            {
                return Result<(int ConceptId, string MainTerm)?>.Failure(
                    "THESAURUS_CONCEPT_MAIN_TERM_BY_TERM_READ_FAILED",
                    exception.Message);
            }
        }

        public static async Task<Result<List<IReferenceTerm>>> GetReferenceTerms(
            string dbFilepath,
            string referenceName,
            string referenceTermScope)
        {
            using SqliteConnection connection =
                SqliteUtil.DbConnectionFactory.CreateConnection(dbFilepath);

            try
            {
                await connection.OpenAsync();

                Result<List<IReferenceTerm>> referenceTermsResult =
                    await ReadReferenceTerms(
                        connection,
                        referenceName,
                        referenceTermScope);

                if (!referenceTermsResult.IsSuccess)
                {
                    return Result<List<IReferenceTerm>>.Failure(
                        referenceTermsResult.Error!.Code,
                        referenceTermsResult.Error.Message);
                }

                return referenceTermsResult;
            }
            catch (Exception exception)
            {
                return Result<List<IReferenceTerm>>.Failure(
                    "THESAURUS_REFERENCE_TERMS_READ_FAILED",
                    exception.Message);
            }
        }

        #endregion

        #region Readers

        private static async Task<Result<List<ThesaurusConceptDto>>> ReadConceptDtos( SqliteConnection connection)
        {
            try
            {
                List<ThesaurusConceptDto> conceptDtos = [];

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = """
            SELECT
                Id
            FROM ThesaurusConcept
            ORDER BY Id;
            """;

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    conceptDtos.Add(new ThesaurusConceptDto
                    {
                        Id = reader.GetInt32(0)
                    });
                }

                return Result<List<ThesaurusConceptDto>>.Success(conceptDtos);
            }
            catch (Exception exception)
            {
                return Result<List<ThesaurusConceptDto>>.Failure("THESAURUS_CONCEPT_DTOS_READ_FAILED", exception.Message);
            }
        }

        private static async Task<Result<List<ThesaurusTermDto>>> ReadTermDtos( SqliteConnection connection)
        {
            try
            {
                List<ThesaurusTermDto> termDtos = [];

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = """
            SELECT
                ConceptId,
                ReferenceName,
                IsPreferred,
                Term,
                NormalizedTerm
            FROM ThesaurusTerm
            ORDER BY ReferenceName, NormalizedTerm;
            """;

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    termDtos.Add(new ThesaurusTermDto
                    {
                        ConceptId = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                        ReferenceName = reader.IsDBNull(1) ? null : reader.GetString(1),
                        IsPreferred = reader.GetBoolean(2),
                        Term = reader.GetString(3),
                        NormalizedTerm = reader.GetString(4)
                    });
                }

                return Result<List<ThesaurusTermDto>>.Success(termDtos);
            }
            catch (Exception exception)
            {
                return Result<List<ThesaurusTermDto>>.Failure("THESAURUS_TERM_DTOS_READ_FAILED", exception.Message);
            }
        }

        private static async Task<Result<List<ThesaurusConceptLinkDto>>> ReadConceptLinkDtos( SqliteConnection connection)
        {
            try
            {
                List<ThesaurusConceptLinkDto> linkDtos = [];

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = """
            SELECT
                ParentConceptId,
                ChildConceptId
            FROM ThesaurusConceptLink
            ORDER BY ParentConceptId, ChildConceptId;
            """;

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    linkDtos.Add(new ThesaurusConceptLinkDto
                    {
                        ParentConceptId = reader.GetInt32(0),
                        ChildConceptId = reader.GetInt32(1)
                    });
                }

                return Result<List<ThesaurusConceptLinkDto>>.Success(linkDtos);
            }
            catch (Exception exception)
            {
                return Result<List<ThesaurusConceptLinkDto>>.Failure("THESAURUS_CONCEPT_LINK_DTOS_READ_FAILED", exception.Message);
            }
        }

        private static async Task<Result<bool>> ReadConceptPathExists( SqliteConnection connection, int ancestorConceptId, int descendantConceptId)
        {
            try
            {
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = """
            WITH RECURSIVE DescendantConceptPath(ConceptId) AS
            (
                SELECT
                    ChildConceptId
                FROM ThesaurusConceptLink
                WHERE ParentConceptId = $ancestorConceptId

                UNION

                SELECT
                    link.ChildConceptId
                FROM ThesaurusConceptLink link
                INNER JOIN DescendantConceptPath path
                    ON path.ConceptId = link.ParentConceptId
            )
            SELECT EXISTS
            (
                SELECT 1
                FROM DescendantConceptPath
                WHERE ConceptId = $descendantConceptId
            );
            """;

                command.Parameters.AddWithValue("$ancestorConceptId", ancestorConceptId);
                command.Parameters.AddWithValue("$descendantConceptId", descendantConceptId);

                object? result = await command.ExecuteScalarAsync();

                bool conceptPathExists = Convert.ToInt32(result) == 1;

                return Result<bool>.Success(conceptPathExists);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure("THESAURUS_CONCEPT_PATH_EXISTS_QUERY_FAILED", exception.Message);
            }
        }

        private static async Task<Result<List<(int ConceptId, string MainTerm)>>> ReadAncestors( SqliteConnection connection, int conceptId)
        {
            try
            {
                List<(int ConceptId, string MainTerm)> ancestors = [];

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = """
            WITH RECURSIVE AncestorConcepts(ConceptId) AS
            (
                SELECT
                    ParentConceptId
                FROM ThesaurusConceptLink
                WHERE ChildConceptId = $conceptId

                UNION

                SELECT
                    link.ParentConceptId
                FROM ThesaurusConceptLink link
                INNER JOIN AncestorConcepts ancestors
                    ON ancestors.ConceptId = link.ChildConceptId
            )
            SELECT
                ancestors.ConceptId,
                term.Term AS MainTerm
            FROM AncestorConcepts ancestors
            INNER JOIN ThesaurusTerm term
                ON term.ConceptId = ancestors.ConceptId
                AND term.IsPreferred = 1
            ORDER BY ancestors.ConceptId;
            """;

                command.Parameters.AddWithValue("$conceptId", conceptId);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    ancestors.Add((
                        ConceptId: reader.GetInt32(0),
                        MainTerm: reader.GetString(1)));
                }

                return Result<List<(int ConceptId, string MainTerm)>>.Success(ancestors);
            }
            catch (Exception exception)
            {
                return Result<List<(int ConceptId, string MainTerm)>>.Failure("THESAURUS_ANCESTORS_QUERY_FAILED", exception.Message);
            }
        }

        private static async Task<Result<List<(int ConceptId, string MainTerm)>>> ReadDescendants( SqliteConnection connection, int conceptId)
        {
            try
            {
                List<(int ConceptId, string MainTerm)> descendants = [];

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = """
            WITH RECURSIVE DescendantConcepts(ConceptId) AS
            (
                SELECT
                    ChildConceptId
                FROM ThesaurusConceptLink
                WHERE ParentConceptId = $conceptId

                UNION

                SELECT
                    link.ChildConceptId
                FROM ThesaurusConceptLink link
                INNER JOIN DescendantConcepts descendants
                    ON descendants.ConceptId = link.ParentConceptId
            )
            SELECT
                descendants.ConceptId,
                term.Term AS MainTerm
            FROM DescendantConcepts descendants
            INNER JOIN ThesaurusTerm term
                ON term.ConceptId = descendants.ConceptId
                AND term.IsPreferred = 1
            ORDER BY descendants.ConceptId;
            """;

                command.Parameters.AddWithValue("$conceptId", conceptId);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    descendants.Add((
                        ConceptId: reader.GetInt32(0),
                        MainTerm: reader.GetString(1)));
                }

                return Result<List<(int ConceptId, string MainTerm)>>.Success(descendants);
            }
            catch (Exception exception)
            {
                return Result<List<(int ConceptId, string MainTerm)>>.Failure("THESAURUS_DESCENDANTS_QUERY_FAILED", exception.Message);
            }
        }

        private static async Task<Result<ThesaurusConceptDto>> ReadConceptDtoById( SqliteConnection connection, int id)
        {
            try
            {
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = """
            SELECT
                Id
            FROM ThesaurusConcept
            WHERE Id = $id
            LIMIT 1;
            """;

                command.Parameters.AddWithValue("$id", id);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return Result<ThesaurusConceptDto>.Failure("THESAURUS_CONCEPT_BY_ID_NOT_FOUND", "Thesaurus concept not found.");
                }

                ThesaurusConceptDto conceptDto = new()
                {
                    Id = reader.GetInt32(0)
                };

                return Result<ThesaurusConceptDto>.Success(conceptDto);
            }
            catch (Exception exception)
            {
                return Result<ThesaurusConceptDto>.Failure("THESAURUS_CONCEPT_BY_ID_QUERY_FAILED", exception.Message);
            }
        }

        private static async Task<Result<List<ThesaurusTermDto>>> ReadTermDtosByConceptId( SqliteConnection connection, int conceptId)
        {
            try
            {
                List<ThesaurusTermDto> termDtos = [];

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = """
            SELECT
                ConceptId,
                ReferenceName,
                IsPreferred,
                Term,
                NormalizedTerm
            FROM ThesaurusTerm
            WHERE ConceptId = $conceptId
            ORDER BY IsPreferred DESC, ReferenceName, NormalizedTerm;
            """;

                command.Parameters.AddWithValue("$conceptId", conceptId);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    termDtos.Add(new ThesaurusTermDto
                    {
                        ConceptId = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                        ReferenceName = reader.IsDBNull(1) ? null : reader.GetString(1),
                        IsPreferred = reader.GetBoolean(2),
                        Term = reader.GetString(3),
                        NormalizedTerm = reader.GetString(4)
                    });
                }

                return Result<List<ThesaurusTermDto>>.Success(termDtos);
            }
            catch (Exception exception)
            {
                return Result<List<ThesaurusTermDto>>.Failure("THESAURUS_TERM_DTOS_BY_CONCEPT_ID_READ_FAILED", exception.Message);
            }
        }

        private static async Task<Result<List<(int ParentConceptId, int ChildConceptId)>>> ReadAncestorLinks( SqliteConnection connection, int conceptId)
        {
            try
            {
                List<(int ParentConceptId, int ChildConceptId)> links = [];

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = """
            WITH RECURSIVE AncestorLinks(ParentConceptId, ChildConceptId) AS
            (
                SELECT
                    ParentConceptId,
                    ChildConceptId
                FROM ThesaurusConceptLink
                WHERE ChildConceptId = $conceptId

                UNION

                SELECT
                    link.ParentConceptId,
                    link.ChildConceptId
                FROM ThesaurusConceptLink link
                INNER JOIN AncestorLinks ancestorLinks
                    ON ancestorLinks.ParentConceptId = link.ChildConceptId
            )
            SELECT
                ParentConceptId,
                ChildConceptId
            FROM AncestorLinks
            ORDER BY ParentConceptId, ChildConceptId;
            """;

                command.Parameters.AddWithValue("$conceptId", conceptId);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    links.Add((
                        ParentConceptId: reader.GetInt32(0),
                        ChildConceptId: reader.GetInt32(1)));
                }

                return Result<List<(int ParentConceptId, int ChildConceptId)>>.Success(links);
            }
            catch (Exception exception)
            {
                return Result<List<(int ParentConceptId, int ChildConceptId)>>.Failure("THESAURUS_ANCESTOR_LINKS_QUERY_FAILED", exception.Message);
            }
        }

        private static async Task<Result<List<(int ParentConceptId, int ChildConceptId)>>> ReadDescendantLinks( SqliteConnection connection, int conceptId)
        {
            try
            {
                List<(int ParentConceptId, int ChildConceptId)> links = [];

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = """
            WITH RECURSIVE DescendantLinks(ParentConceptId, ChildConceptId) AS
            (
                SELECT
                    ParentConceptId,
                    ChildConceptId
                FROM ThesaurusConceptLink
                WHERE ParentConceptId = $conceptId

                UNION

                SELECT
                    link.ParentConceptId,
                    link.ChildConceptId
                FROM ThesaurusConceptLink link
                INNER JOIN DescendantLinks descendantLinks
                    ON descendantLinks.ChildConceptId = link.ParentConceptId
            )
            SELECT
                ParentConceptId,
                ChildConceptId
            FROM DescendantLinks
            ORDER BY ParentConceptId, ChildConceptId;
            """;

                command.Parameters.AddWithValue("$conceptId", conceptId);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    links.Add((
                        ParentConceptId: reader.GetInt32(0),
                        ChildConceptId: reader.GetInt32(1)));
                }

                return Result<List<(int ParentConceptId, int ChildConceptId)>>.Success(links);
            }
            catch (Exception exception)
            {
                return Result<List<(int ParentConceptId, int ChildConceptId)>>.Failure("THESAURUS_DESCENDANT_LINKS_QUERY_FAILED", exception.Message);
            }
        }

        private static async Task<Result<(int ConceptId, string MainTerm)?>> ReadConceptMainTermByTerm( SqliteConnection connection, string referenceName, string term)
        {
            try
            {
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = """
            SELECT
                searchedTerm.ConceptId,
                preferredTerm.Term AS MainTerm
            FROM ThesaurusTerm searchedTerm
            INNER JOIN ThesaurusTerm preferredTerm
                ON preferredTerm.ConceptId = searchedTerm.ConceptId
                AND preferredTerm.IsPreferred = 1
            WHERE searchedTerm.ReferenceName = $referenceName
                AND searchedTerm.ConceptId IS NOT NULL
                AND (
                    searchedTerm.Term = $term
                    OR searchedTerm.NormalizedTerm = $term
                )
            LIMIT 1;
            """;

                command.Parameters.AddWithValue("$referenceName", referenceName);
                command.Parameters.AddWithValue("$term", term);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return Result<(int ConceptId, string MainTerm)?>.Success(null);
                }

                return Result<(int ConceptId, string MainTerm)?>.Success((
                    ConceptId: reader.GetInt32(0),
                    MainTerm: reader.GetString(1)));
            }
            catch (Exception exception)
            {
                return Result<(int ConceptId, string MainTerm)?>.Failure("THESAURUS_CONCEPT_MAIN_TERM_BY_TERM_QUERY_FAILED", exception.Message);
            }
        }

        private static async Task<Result<List<string>>> ReadReferences(SqliteConnection connection)
        {
            try
            {
                List<string> references = [];

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = """
            SELECT
                Name
            FROM ThesaurusReference
            ORDER BY Name;
            """;

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    references.Add(reader.GetString(0));
                }

                return Result<List<string>>.Success(references);
            }
            catch (Exception exception)
            {
                return Result<List<string>>.Failure("THESAURUS_REFERENCES_QUERY_FAILED", exception.Message);
            }
        }

        private static async Task<Result<List<IReferenceTerm>>> ReadReferenceTerms(
            SqliteConnection connection,
            string referenceName,
            string referenceTermScope)
        {
            try
            {
                List<IReferenceTerm> referenceTerms = [];

                using SqliteCommand command = connection.CreateCommand();

                switch (referenceTermScope)
                {
                    case "Reference":
                        command.CommandText = """
            SELECT
                term.ConceptId,
                term.ReferenceName,
                term.IsPreferred,
                term.Term,
                term.NormalizedTerm,
                preferredTerm.Term
            FROM ThesaurusTerm term
            LEFT JOIN ThesaurusTerm preferredTerm
                ON preferredTerm.ConceptId = term.ConceptId
                AND preferredTerm.IsPreferred = 1
            WHERE term.ReferenceName = $referenceName
            ORDER BY term.NormalizedTerm;
            """;
                        break;

                    case "Alternate":
                        command.CommandText = """
            SELECT
                term.ConceptId,
                term.ReferenceName,
                term.IsPreferred,
                term.Term,
                term.NormalizedTerm,
                preferredTerm.Term
            FROM ThesaurusTerm term
            LEFT JOIN ThesaurusTerm preferredTerm
                ON preferredTerm.ConceptId = term.ConceptId
                AND preferredTerm.IsPreferred = 1
            WHERE term.ReferenceName <> $referenceName
              AND term.ConceptId IN
              (
                  SELECT DISTINCT
                      ConceptId
                  FROM ThesaurusTerm
                  WHERE ReferenceName = $referenceName
                    AND ConceptId IS NOT NULL
              )
            ORDER BY term.ConceptId, term.ReferenceName, term.NormalizedTerm;
            """;
                        break;

                    default:
                        return Result<List<IReferenceTerm>>.Failure(
                            "THESAURUS_REFERENCE_TERM_SCOPE_INVALID",
                            $"Unknown reference term scope: {referenceTermScope}.");
                }

                command.Parameters.AddWithValue("$referenceName", referenceName);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    referenceTerms.Add(new ReferenceTermDto
                    {
                        ConceptId = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                        ReferenceName = reader.GetString(1),
                        IsPreferred = reader.GetBoolean(2),
                        Term = reader.GetString(3),
                        NormalizedTerm = reader.GetString(4),
                        PreferredTerm = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                    });
                }

                return Result<List<IReferenceTerm>>.Success(referenceTerms);
            }
            catch (Exception exception)
            {
                return Result<List<IReferenceTerm>>.Failure(
                    "THESAURUS_REFERENCE_TERMS_QUERY_FAILED",
                    exception.Message);
            }
        }

        #endregion

        #region helpers

        private static Dictionary<int, IThesaurusConcept> CreateConceptsById(
            List<ThesaurusConceptDto> conceptDtos,
            List<ThesaurusTermDto> termDtos,
            IEntityFactory entityFactory)
        {
            Dictionary<int, List<ThesaurusTermDto>> termsByConceptId = termDtos
                .Where(term => term.ConceptId.HasValue)
                .GroupBy(term => term.ConceptId!.Value)
                .ToDictionary(group => group.Key, group => group.ToList());

            Dictionary<int, IThesaurusConcept> conceptsById = [];

            foreach (ThesaurusConceptDto conceptDto in conceptDtos)
            {
                termsByConceptId.TryGetValue(conceptDto.Id, out List<ThesaurusTermDto>? conceptTermDtos);

                List<IThesaurusTerm> terms = CreateTerms(
                    conceptTermDtos ?? [],
                    entityFactory);

                IThesaurusConcept concept = entityFactory.CreateThesaurusConcept(
                    conceptDto.Id,
                    terms,
                    [],
                    []);

                conceptsById.Add(conceptDto.Id, concept);
            }

            return conceptsById;
        }

        private static List<IThesaurusTerm> CreateTerms(
            List<ThesaurusTermDto> termDtos,
            IEntityFactory entityFactory)
        {
            List<IThesaurusTerm> terms = [];

            foreach (ThesaurusTermDto termDto in termDtos)
            {
                terms.Add(entityFactory.CreateThesaurusTerm(
                    termDto.Term,
                    termDto.NormalizedTerm,
                    termDto.IsPreferred,
                    termDto.ReferenceName));
            }

            return terms;
        }

        private static List<IThesaurusConcept> CreateConceptListWithLinks(
            Dictionary<int, IThesaurusConcept> conceptsById,
            List<ThesaurusConceptLinkDto> linkDtos)
        {
            foreach (ThesaurusConceptLinkDto linkDto in linkDtos)
            {
                bool hasParent = conceptsById.TryGetValue(
                    linkDto.ParentConceptId,
                    out IThesaurusConcept? parent);

                bool hasChild = conceptsById.TryGetValue(
                    linkDto.ChildConceptId,
                    out IThesaurusConcept? child);

                if (!hasParent || !hasChild)
                {
                    continue;
                }

                parent!.Children.Add(child!);
                child!.Parents.Add(parent);
            }

            return conceptsById.Values.ToList();
        }

        #endregion
    }
}
