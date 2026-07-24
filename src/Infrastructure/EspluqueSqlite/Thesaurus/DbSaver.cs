using Espluque.Contracts.Interfaces;
using Microsoft.Data.Sqlite;
using System.Globalization;
using Util;

namespace EspluqueSqlite.Thesaurus
{
    internal class DbSaver
    {
        public static async Task<Result<int>> SaveConcept(string dbFilePath, IThesaurusConcept thesaurusConcept)
        {
            SqliteTransaction? transaction = null;
            bool commit = false;

            try
            {
                transaction = SqliteUtil.DbTransactionFactory.OpenTransaction(dbFilePath);
                SqliteConnection connection = transaction.Connection!;

                Result<int> conceptIdResult = await SaveConceptRow(connection, transaction, thesaurusConcept.Id);
                if (!conceptIdResult.IsSuccess)
                {
                    return Result<int>.Failure(conceptIdResult.Error.Code, conceptIdResult.Error.Message);
                }

                int conceptId = conceptIdResult.Value;

                Result<bool> deleteTermsResult = await DelTermsOfConcept(connection, transaction, conceptId);
                if (!deleteTermsResult.IsSuccess)
                {
                    return Result<int>.Failure(deleteTermsResult.Error.Code, deleteTermsResult.Error.Message);
                }

                Result<bool> deleteLinksResult = await DelLinksOfConcept(connection, transaction, conceptId);
                if (!deleteLinksResult.IsSuccess)
                {
                    return Result<int>.Failure(deleteLinksResult.Error.Code, deleteLinksResult.Error.Message);
                }

                foreach (IThesaurusTerm term in thesaurusConcept.Terms)
                {
                    Result<bool> insertTermResult = await InsTermOfConcept(connection, transaction, conceptId, term);
                    if (!insertTermResult.IsSuccess)
                    {
                        return Result<int>.Failure(insertTermResult.Error.Code, insertTermResult.Error.Message);
                    }
                }

                foreach (IThesaurusConcept parentConcept in thesaurusConcept.Parents)
                {
                    if (parentConcept.Id is not int parentConceptId)
                    {
                        continue;
                    }

                    Result<bool> insertLinkResult = await InsConceptLink(connection, transaction, parentConceptId, conceptId);
                    if (!insertLinkResult.IsSuccess)
                    {
                        return Result<int>.Failure(insertLinkResult.Error.Code, insertLinkResult.Error.Message);
                    }
                }

                foreach (IThesaurusConcept childConcept in thesaurusConcept.Children)
                {
                    if (childConcept.Id is not int childConceptId)
                    {
                        continue;
                    }

                    Result<bool> insertLinkResult = await InsConceptLink(connection, transaction, conceptId, childConceptId);
                    if (!insertLinkResult.IsSuccess)
                    {
                        return Result<int>.Failure(insertLinkResult.Error.Code, insertLinkResult.Error.Message);
                    }
                }

                commit = true;
                return Result<int>.Success(conceptId);
            }
            catch (Exception exception)
            {
                return Result<int>.Failure("THESAURUS_CONCEPT_SAVE_FAILED", exception.Message);
            }
            finally
            {
                if (transaction is not null)
                {
                    SqliteUtil.DbTransactionFactory.CloseTransaction(transaction, commit);
                }
            }
        }

        public static async Task<Result> DeleteConcept(string dbFilePath, int conceptId)
        {
            SqliteTransaction? transaction = null;
            bool commit = false;

            try
            {
                transaction = SqliteUtil.DbTransactionFactory.OpenTransaction(dbFilePath);
                SqliteConnection connection = transaction.Connection!;

                Result<bool> deleteTermsResult = await DelTermsOfConcept(connection, transaction, conceptId);
                if (!deleteTermsResult.IsSuccess)
                {
                    return Result.Failure(deleteTermsResult.Error!.Code, deleteTermsResult.Error.Message);
                }

                Result<bool> deleteLinksResult = await DelLinksOfConcept(connection, transaction, conceptId);
                if (!deleteLinksResult.IsSuccess)
                {
                    return Result.Failure(deleteLinksResult.Error!.Code, deleteLinksResult.Error.Message);
                }

                Result<bool> deleteConceptResult = await DelConcept(connection, transaction, conceptId);
                if (!deleteConceptResult.IsSuccess)
                {
                    return Result.Failure(deleteConceptResult.Error!.Code, deleteConceptResult.Error.Message);
                }

                commit = true;
                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure("THESAURUS_CONCEPT_DELETE_FAILED", exception.Message);
            }
            finally
            {
                if (transaction is not null)
                {
                    SqliteUtil.DbTransactionFactory.CloseTransaction(transaction, commit);
                }
            }
        }

        public static async Task<Result> SaveParentChildLink(string dbFilePath, int parentConceptId, int childConceptId)
        {
            SqliteTransaction? transaction = null;
            bool commit = false;

            try
            {
                transaction = SqliteUtil.DbTransactionFactory.OpenTransaction(dbFilePath);
                SqliteConnection connection = transaction.Connection!;

                Result<bool> insertLinkResult = await InsConceptLink(connection, transaction, parentConceptId, childConceptId);
                if (!insertLinkResult.IsSuccess)
                {
                    return Result.Failure(insertLinkResult.Error!.Code, insertLinkResult.Error.Message);
                }

                commit = true;
                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure("THESAURUS_CONCEPT_LINK_SAVE_FAILED", exception.Message);
            }
            finally
            {
                if (transaction is not null)
                {
                    SqliteUtil.DbTransactionFactory.CloseTransaction(transaction, commit);
                }
            }
        }

        #region Savers

        private static async Task<Result<int>> SaveConceptRow(SqliteConnection connection, SqliteTransaction transaction, int? conceptId)
        {
            if (conceptId is null)
            {
                return await InsConcept(connection, transaction);
            }

            Result<bool> conceptExistsResult = await ConceptExists(connection, transaction, conceptId.Value);
            if (!conceptExistsResult.IsSuccess)
            {
                return Result<int>.Failure(conceptExistsResult.Error.Code, conceptExistsResult.Error.Message);
            }

            if (conceptExistsResult.Value)
            {
                return Result<int>.Success(conceptId.Value);
            }

            return await InsConceptWithId(connection, transaction, conceptId.Value);
        }

        private static async Task<Result<int>> InsConcept(SqliteConnection connection, SqliteTransaction transaction)
        {
            string sql = """
            INSERT INTO "ThesaurusConcept" DEFAULT VALUES;
            SELECT last_insert_rowid();
            """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;

                object? result = await command.ExecuteScalarAsync();

                return Result<int>.Success(Convert.ToInt32(result));
            }
            catch (Exception exception)
            {
                return Result<int>.Failure("THESAURUS_CONCEPT_INSERT_FAILED", exception.Message);
            }
            finally
            {
                command?.Dispose();
            }
        }

        private static async Task<Result<int>> InsConceptWithId(SqliteConnection connection, SqliteTransaction transaction, int conceptId)
        {
            string sql = """
                INSERT INTO "ThesaurusConcept" ("Id")
                VALUES (@conceptId);
                """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@conceptId", conceptId);

                await command.ExecuteNonQueryAsync();

                return Result<int>.Success(conceptId);
            }
            catch (Exception exception)
            {
                return Result<int>.Failure("THESAURUS_CONCEPT_INSERT_WITH_ID_FAILED", exception.Message);
            }
            finally
            {
                command?.Dispose();
            }
        }

        private static async Task<Result<bool>> ConceptExists(SqliteConnection connection, SqliteTransaction transaction, int conceptId)
        {
            string sql = """
                SELECT COUNT(1)
                FROM "ThesaurusConcept"
                WHERE "Id" = @conceptId;
                """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@conceptId", conceptId);

                object? result = await command.ExecuteScalarAsync();

                return Result<bool>.Success(Convert.ToInt32(result) > 0);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure("THESAURUS_CONCEPT_EXISTS_CHECK_FAILED", exception.Message);
            }
            finally
            {
                command?.Dispose();
            }
        }

        private static async Task<Result<bool>> InsTermOfConcept(SqliteConnection connection, SqliteTransaction transaction, int conceptId, IThesaurusTerm term)
        {
            if (string.IsNullOrWhiteSpace(term.Term))
            {
                return Result<bool>.Failure("THESAURUS_TERM_VALUE_MISSING", "Thesaurus term value is missing.");
            }

            if (string.IsNullOrWhiteSpace(term.NormalizedTerm))
            {
                return Result<bool>.Failure("THESAURUS_TERM_NORMALIZED_VALUE_MISSING", "Thesaurus normalized term value is missing.");
            }

            string? referenceName = string.IsNullOrWhiteSpace(term.ReferenceName)
                ? null
                : term.ReferenceName.Trim();

            if (referenceName is not null)
            {
                Result<bool> insertReferenceResult = await InsReferenceIfMissing(connection, transaction, referenceName);
                if (!insertReferenceResult.IsSuccess)
                {
                    return Result<bool>.Failure(insertReferenceResult.Error.Code, insertReferenceResult.Error.Message);
                }
            }

            string sql = """
    INSERT INTO "ThesaurusTerm" (
        "ConceptId",
        "ReferenceName",
        "IsPrefered",
        "Term",
        "NormalizedTerm"
    )
    VALUES (
        @conceptId,
        @referenceName,
        @isPrefered,
        @term,
        @normalizedTerm
    );
    """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@conceptId", conceptId);
                command.Parameters.AddWithValue("@referenceName", (object?)referenceName ?? DBNull.Value);
                command.Parameters.AddWithValue("@isPrefered", term.IsPrefered);
                command.Parameters.AddWithValue("@term", term.Term);
                command.Parameters.AddWithValue("@normalizedTerm", term.NormalizedTerm);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure("THESAURUS_TERM_INSERT_FAILED", exception.Message);
            }
            finally
            {
                command?.Dispose();
            }
        }

        private static async Task<Result<bool>> InsConceptLink(SqliteConnection connection, SqliteTransaction transaction, int parentConceptId, int childConceptId)
        {
            string sql = """
    INSERT OR IGNORE INTO "ThesaurusConceptLink" (
        "ParentConceptId",
        "ChildConceptId"
    )
    VALUES (
        @parentConceptId,
        @childConceptId
    );
    """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@parentConceptId", parentConceptId);
                command.Parameters.AddWithValue("@childConceptId", childConceptId);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure("THESAURUS_CONCEPT_LINK_INSERT_FAILED", exception.Message);
            }
            finally
            {
                command?.Dispose();
            }
        }

        private static async Task<Result<bool>> InsReferenceIfMissing(SqliteConnection connection, SqliteTransaction transaction, string referenceName)
        {
            string sql = """
    INSERT OR IGNORE INTO "ThesaurusReference" (
        "Name"
    )
    VALUES (
        @name
    );
    """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@name", referenceName);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure("THESAURUS_REFERENCE_INSERT_FAILED", exception.Message);
            }
            finally
            {
                command?.Dispose();
            }
        }

        #endregion

        #region Deleters

        private static async Task<Result<bool>> DelConcept(SqliteConnection connection, SqliteTransaction transaction, int conceptId)
        {
            string sql = """
            DELETE FROM "ThesaurusConcept"
            WHERE "Id" = @conceptId;
            """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@conceptId", conceptId);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure("THESAURUS_CONCEPT_DELETE_FAILED", exception.Message);
            }
            finally
            {
                command?.Dispose();
            }
        }

        private static async Task<Result<bool>> DelTermsOfConcept(SqliteConnection connection, SqliteTransaction transaction, int conceptId)
        {
            string sql = """
            DELETE FROM "ThesaurusTerm"
            WHERE "ConceptId" = @conceptId;
            """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@conceptId", conceptId);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure("THESAURUS_TERMS_DELETE_FAILED", exception.Message);
            }
            finally
            {
                command?.Dispose();
            }
        }

        private static async Task<Result<bool>> DelLinksOfConcept(SqliteConnection connection, SqliteTransaction transaction, int conceptId)
        {
            string sql = """
    DELETE FROM "ThesaurusConceptLink"
    WHERE "ParentConceptId" = @conceptId
       OR "ChildConceptId" = @conceptId;
    """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@conceptId", conceptId);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure("THESAURUS_CONCEPT_LINKS_DELETE_FAILED", exception.Message);
            }
            finally
            {
                command?.Dispose();
            }
        }

        #endregion
    }
}
