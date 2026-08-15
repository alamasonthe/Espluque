using Espluque.Contracts.Thesaurus;
using Microsoft.Data.Sqlite;
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

        public static async Task<Result> SaveReference(string dbFilePath, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return Result.Failure("SAVE_REFERENCE_FAILED", "Thesaurus reference name is missing.");
            }

            SqliteTransaction? transaction = null;
            bool commit = false;

            try
            {
                transaction = SqliteUtil.DbTransactionFactory.OpenTransaction(dbFilePath);
                SqliteConnection connection = transaction.Connection!;

                Result<bool> referenceExistsResult = await ReferenceExists(connection, transaction, name);
                if (!referenceExistsResult.IsSuccess)
                {
                    return Result.Failure(referenceExistsResult.Error!.Code, referenceExistsResult.Error.Message);
                }
                if (referenceExistsResult.Value)
                {
                    return Result.Success();
                }

                var insReferenceResult = await InsReference(connection, transaction, name);
                if (!insReferenceResult.IsSuccess)
                {
                    return Result.Failure(insReferenceResult.Error!.Code, insReferenceResult.Error.Message);
                }

                commit = true;
                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure("THESAURUS_REFERENCE_SAVE_FAILED", exception.Message);
            }
            finally
            {
                if (transaction is not null)
                {
                    SqliteUtil.DbTransactionFactory.CloseTransaction(transaction, commit);
                }
            }
        }

        public static async Task<Result> RenameReference(string dbFilePath, string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName))
            {
                return Result.Failure("RENAME_REFERENCE_FAILED", "Thesaurus reference old name is missing.");
            }
            if (string.IsNullOrEmpty(newName))
            {
                return Result.Failure("RENAME_REFERENCE_FAILED", "Thesaurus reference new name is missing.");
            }
            if (oldName == newName)
            {
                return Result.Success();
            }

            SqliteTransaction? transaction = null;
            bool commit = false;

            try
            {
                transaction = SqliteUtil.DbTransactionFactory.OpenTransaction(dbFilePath);
                SqliteConnection connection = transaction.Connection!;

                Result<bool> oldReferenceExistsResult = await ReferenceExists(connection, transaction, oldName);
                if (!oldReferenceExistsResult.IsSuccess)
                {
                    return Result.Failure(oldReferenceExistsResult.Error!.Code, oldReferenceExistsResult.Error.Message);
                }
                if (!oldReferenceExistsResult.Value)
                {
                    return Result.Failure("RENAME_REFERENCE_FAILED", $"Thesaurus reference '{oldName}' does not exist.");
                }

                Result<bool> newReferenceExistsResult = await ReferenceExists(connection, transaction, newName);
                if (!newReferenceExistsResult.IsSuccess)
                {
                    return Result.Failure(newReferenceExistsResult.Error!.Code, newReferenceExistsResult.Error.Message);
                }
                if (newReferenceExistsResult.Value)
                {
                    return Result.Failure("RENAME_REFERENCE_FAILED", $"Thesaurus reference '{newName}' already exists.");
                }

                Result referenceUpdateResult = await UpdReference(connection, transaction, oldName, newName);
                if (!referenceUpdateResult.IsSuccess)
                {
                    return Result.Failure(referenceUpdateResult.Error!.Code, referenceUpdateResult.Error.Message);
                }

                Result termsUpdateResult = await UpdTermsReference(connection, transaction, oldName, newName);
                if (!termsUpdateResult.IsSuccess)
                {
                    return Result.Failure(termsUpdateResult.Error!.Code, termsUpdateResult.Error.Message);
                }

                commit = true;
                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure("THESAURUS_REFERENCE_RENAME_FAILED", exception.Message);
            }
            finally
            {
                if (transaction is not null)
                {
                    SqliteUtil.DbTransactionFactory.CloseTransaction(transaction, commit);
                }
            }
        }

        public static async Task<Result> DeleteReference(string dbFilePath, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return Result.Failure("DELETE_REFERENCE_FAILED", "Thesaurus reference name is missing.");
            }

            SqliteTransaction? transaction = null;
            bool commit = false;

            try
            {
                transaction = SqliteUtil.DbTransactionFactory.OpenTransaction(dbFilePath);
                SqliteConnection connection = transaction.Connection!;
                
                Result<List<int>> conceptIdsResult = await GetPreferredTermsOfReference(connection, transaction, name);
                if (!conceptIdsResult.IsSuccess)
                {
                    return Result.Failure(conceptIdsResult.Error!.Code, conceptIdsResult.Error.Message);
                }
                var conceptIds = conceptIdsResult.Value;

                foreach (int conceptId in conceptIds)
                {
                    Result<bool> otherTermExistsResult = await OtherTermExists(connection, transaction, conceptId, name);
                    if (!otherTermExistsResult.IsSuccess)
                    {
                        return Result.Failure(otherTermExistsResult.Error!.Code, otherTermExistsResult.Error.Message);
                    }

                    if (!otherTermExistsResult.Value)
                    {
                        continue;
                    }

                    Result unsetPreferredTermResult = await SetPreferredTerm(connection, transaction, conceptId, name, false);
                    if (!unsetPreferredTermResult.IsSuccess)
                    {
                        return Result.Failure(unsetPreferredTermResult.Error!.Code, unsetPreferredTermResult.Error.Message);
                    }

                    Result setPreferredTermResult = await SetPreferredTerm(connection, transaction, conceptId, name, true);
                    if (!setPreferredTermResult.IsSuccess)
                    {
                        return Result.Failure(setPreferredTermResult.Error!.Code, setPreferredTermResult.Error.Message);
                    }
                }

                Result<List<int>> conceptIdsToDeleteResult = await GetPreferredTermsOfReference(connection, transaction, name);
                if (!conceptIdsToDeleteResult.IsSuccess)
                {
                    return Result.Failure(conceptIdsToDeleteResult.Error!.Code, conceptIdsToDeleteResult.Error.Message);
                }

                foreach (int conceptId in conceptIdsToDeleteResult.Value!)
                {
                    Result<bool> deleteConceptLinksResult = await DelLinksOfConcept(connection, transaction, conceptId);
                    if (!deleteConceptLinksResult.IsSuccess)
                    {
                        return Result.Failure(deleteConceptLinksResult.Error!.Code, deleteConceptLinksResult.Error.Message);
                    }

                    Result<bool> deleteConceptResult = await DelConcept(connection, transaction, conceptId);
                    if (!deleteConceptResult.IsSuccess)
                    {
                        return Result.Failure(deleteConceptResult.Error!.Code, deleteConceptResult.Error.Message);
                    }
                }

                Result<bool> deleteTermsResult = await DelTermsOfReference(connection, transaction, name);
                if (!deleteTermsResult.IsSuccess)
                {
                    return Result.Failure(deleteTermsResult.Error!.Code, deleteTermsResult.Error.Message);
                }

                Result<bool> deleteReferenceResult = await DelReference(connection, transaction, name);
                if (!deleteReferenceResult.IsSuccess)
                {
                    return Result.Failure(deleteReferenceResult.Error!.Code, deleteReferenceResult.Error.Message);
                }

                commit = true;
                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure("THESAURUS_REFERENCE_DELETE_FAILED", exception.Message);
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
        "IsPreferred",
        "Term",
        "NormalizedTerm"
    )
    VALUES (
        @conceptId,
        @referenceName,
        @isPreferred,
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
                command.Parameters.AddWithValue("@isPreferred", term.IsPreferred);
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

        #region Update

        private static async Task<Result> SetPreferredTerm(SqliteConnection connection, SqliteTransaction transaction, int conceptId, string referenceName, bool isPreferred)
        {
            int preferredValue = isPreferred switch
            {
                true => 1,
                false => 0
            };

            string sql = """
    UPDATE "ThesaurusTerm"
    SET "IsPreferred" = @isPreferred
    WHERE rowid = (
        SELECT rowid
        FROM "ThesaurusTerm"
        WHERE "ConceptId" = @conceptId
          AND (
              (@isPreferred = 0 AND "ReferenceName" = @referenceName)
              OR
              (@isPreferred = 1 AND "ReferenceName" <> @referenceName)
          )
        LIMIT 1
    );
    """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@conceptId", conceptId);
                command.Parameters.AddWithValue("@referenceName", referenceName);
                command.Parameters.AddWithValue("@isPreferred", preferredValue);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure("THESAURUS_PREFERRED_TERM_UPDATE_FAILED", exception.Message);
            }
            finally
            {
                command?.Dispose();
            }
        }


        #endregion

        #region Savers Reference

        private static async Task<Result<bool>> ReferenceExists(SqliteConnection connection, SqliteTransaction transaction, string name)
        {
            string sql = """
        SELECT COUNT(1)
        FROM "ThesaurusReference"
        WHERE "Name" = @name;
        """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@name", name);

                object? result = await command.ExecuteScalarAsync();

                return Result<bool>.Success(Convert.ToInt32(result) > 0);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure("THESAURUS_REFERENCE_EXISTS_CHECK_FAILED", exception.Message);
            }
            finally
            {
                command?.Dispose();
            }
        }

        private static async Task<Result> InsReference(SqliteConnection connection, SqliteTransaction transaction, string name)
        {
            string sql = """
        INSERT INTO "ThesaurusReference" ("Name")
        VALUES (@name);
        """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@name", name);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure("THESAURUS_REFERENCE_INSERT_FAILED", exception.Message);
            }
            finally
            {
                command?.Dispose();
            }
        }

        private static async Task<Result> UpdReference(SqliteConnection connection, SqliteTransaction transaction, string oldName, string newName)
        {
            string sql = """
        UPDATE "ThesaurusReference"
        SET "Name" = @newName
        WHERE "Name" = @oldName;
        """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@oldName", oldName);
                command.Parameters.AddWithValue("@newName", newName);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure("THESAURUS_REFERENCE_UPDATE_FAILED", exception.Message);
            }
            finally
            {
                command?.Dispose();
            }
        }

        private static async Task<Result> UpdTermsReference(SqliteConnection connection, SqliteTransaction transaction, string oldName, string newName)
        {
            string sql = """
        UPDATE "ThesaurusTerm"
        SET "ReferenceName" = @newName
        WHERE "ReferenceName" = @oldName;
        """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@oldName", oldName);
                command.Parameters.AddWithValue("@newName", newName);

                await command.ExecuteNonQueryAsync();

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure("THESAURUS_TERMS_REFERENCE_UPDATE_FAILED", exception.Message);
            }
            finally
            {
                command?.Dispose();
            }
        }

        #endregion


        #region Select Reference

        private static async Task<Result<List<int>>> GetPreferredTermsOfReference(SqliteConnection connection, SqliteTransaction transaction, string referenceName)
        {
            string sql = """
    SELECT "ConceptId"
    FROM "ThesaurusTerm"
    WHERE "ReferenceName" = @referenceName
      AND "IsPreferred" = 1
      AND "ConceptId" IS NOT NULL;
    """;

            SqliteCommand? command = null;
            SqliteDataReader? reader = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@referenceName", referenceName);

                reader = await command.ExecuteReaderAsync();

                List<int> conceptIds = [];

                while (await reader.ReadAsync())
                {
                    conceptIds.Add(reader.GetInt32(0));
                }

                return Result<List<int>>.Success(conceptIds);
            }
            catch (Exception exception)
            {
                return Result<List<int>>.Failure("THESAURUS_REFERENCE_PREFERRED_TERMS_READ_FAILED", exception.Message);
            }
            finally
            {
                reader?.Dispose();
                command?.Dispose();
            }
        }

        private static async Task<Result<bool>> OtherTermExists(SqliteConnection connection, SqliteTransaction transaction, int conceptId, string referenceName)
        {
            string sql = """
    SELECT EXISTS (
        SELECT 1
        FROM "ThesaurusTerm"
        WHERE "ConceptId" = @conceptId
          AND "ReferenceName" <> @referenceName
    );
    """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@conceptId", conceptId);
                command.Parameters.AddWithValue("@referenceName", referenceName);

                object? result = await command.ExecuteScalarAsync();

                return Result<bool>.Success(Convert.ToInt32(result) == 1);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure("THESAURUS_OTHER_TERM_EXISTS_CHECK_FAILED", exception.Message);
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

        private static async Task<Result<bool>> DelTermsOfReference(SqliteConnection connection, SqliteTransaction transaction, string referenceName)
        {
            string sql = """
    DELETE FROM "ThesaurusTerm"
    WHERE "ReferenceName" = @referenceName;
    """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@referenceName", referenceName);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure("THESAURUS_REFERENCE_TERMS_DELETE_FAILED", exception.Message);
            }
            finally
            {
                command?.Dispose();
            }
        }

        private static async Task<Result<bool>> DelReference(SqliteConnection connection, SqliteTransaction transaction, string name)
        {
            string sql = """
    DELETE FROM "ThesaurusReference"
    WHERE "Name" = @name;
    """;

            SqliteCommand? command = null;

            try
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddWithValue("@name", name);

                await command.ExecuteNonQueryAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure("THESAURUS_REFERENCE_DELETE_FAILED", exception.Message);
            }
            finally
            {
                command?.Dispose();
            }
        }

        #endregion
    }
}
