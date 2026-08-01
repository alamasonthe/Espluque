using Espluque.Contracts.DetectionResult;
using Espluque.Contracts.Interfaces;
using Microsoft.Data.Sqlite;
using SqliteUtil;
using Util;

namespace EspluqueSqlite.DetectionResult
{
    internal class DbCrud
    {
        private record ResultPropertyRow(int ResultModelId, string PropertyName);
        private record ResultPropertyLinkRow(int ResultModelId, ResultPropertyLink PropertyLink);

        public static async Task<Result<List<IResultModelDefinition>>> GetResultModelDefinitions(string dbFilepath, IEntityFactory entityFactory, string whereClause = "")
        {
            using SqliteConnection connection = DbSession.CreateConnection(dbFilepath);
            try
            {
                await connection.OpenAsync();

                List<IResultModelDefinition> definitions = await SqliteUtil.DbCommands.QueryAsync(connection,
                    $"""
                    SELECT ResultModel.Id, ResultModel.Name, ResultModel.ThesaurusTag
                    FROM ResultModel
                    {whereClause}
                    ORDER BY ResultModel.Name
                    """,
                    reader => entityFactory.CreateResultModelDefinition(
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.IsDBNull(2) ? null : reader.GetString(2),
                        [],
                        []));

                List<int> resultModelIdList = [];
                foreach (IResultModelDefinition definition in definitions)
                {
                    resultModelIdList.Add(definition.Id!.Value);
                }
                string resultModelIds = string.Join(", ", resultModelIdList);

                List<ResultPropertyRow> properties = await SqliteUtil.DbCommands.QueryAsync(connection,
                    $"""
                    SELECT ResultProperty.ResultModelId, ResultProperty.Name
                    FROM ResultProperty
                    WHERE ResultProperty.ResultModelId IN ({resultModelIds})
                    ORDER BY ResultProperty.ResultModelId, ResultProperty.Position
                    """,
                    reader => new ResultPropertyRow(
                        reader.GetInt32(0),
                        reader.GetString(1)));

                List<ResultPropertyLinkRow> propertyLinks = await SqliteUtil.DbCommands.QueryAsync(connection,
                    $"""
                    SELECT ResultPropertyLink.ResultModelId,
                           ResultPropertyLink.GrabberModuleName,
                           ResultPropertyLink.GrabberContributionLabel,
                           ResultPropertyLink.GrabberKey,
                           ResultPropertyLink.PropertyName
                    FROM ResultPropertyLink
                    WHERE ResultPropertyLink.ResultModelId IN ({resultModelIds})
                    ORDER BY ResultPropertyLink.ResultModelId, ResultPropertyLink.PropertyName
                    """,
                    reader => new ResultPropertyLinkRow(
                        reader.GetInt32(0),
                        new ResultPropertyLink(
                            reader.GetString(1),
                            reader.GetString(2),
                            reader.GetString(3),
                            reader.GetString(4))));

                Dictionary<int, IResultModelDefinition> definitionsById = [];

                foreach (IResultModelDefinition definition in definitions)
                {
                    definitionsById.Add(definition.Id!.Value, definition);
                }

                foreach (ResultPropertyRow property in properties)
                {
                    definitionsById[property.ResultModelId].Properties.Add(property.PropertyName);
                }

                foreach (ResultPropertyLinkRow propertyLink in propertyLinks)
                {
                    definitionsById[propertyLink.ResultModelId].PropertyLinks.Add(propertyLink.PropertyLink);
                }

                return Result<List<IResultModelDefinition>>.Success(definitions);

            }
            catch (Exception exception)
            {
                return Result<List<IResultModelDefinition>>.Failure("RESULT_MODEL_DEFINITIONS_READ_FAILED", exception.Message);
            }
        }

        public static async Task<Result<IResultModelDefinition>> SaveResultModelDefinition(string dbFilepath, IResultModelDefinition resultModelDefinition)
        {
            SqliteTransaction transaction = SqliteUtil.DbSession.OpenTransaction(dbFilepath);

            try
            {
                string name = resultModelDefinition.Name.Replace("'", "''");
                string thesaurusTag = resultModelDefinition.ThesaurusTag.Replace("'", "''");
                int resultModelId;

                if (resultModelDefinition.Id == null)
                {
                    resultModelId = await SqliteUtil.DbCommands.InsertAsync(transaction,
                        $"""
                        INSERT INTO ResultModel (Name, ThesaurusTag)
                        VALUES ('{name}', '{thesaurusTag}')
                        RETURNING Id
                        """);
                }
                else
                {
                    resultModelId = resultModelDefinition.Id.Value;

                    await SqliteUtil.DbCommands.ExecuteAsync(transaction,
                        $"""
                        UPDATE ResultModel
                        SET Name = '{name}', ThesaurusTag = '{thesaurusTag}'
                        WHERE Id = {resultModelId}
                        """);
                }

                await SqliteUtil.DbCommands.ExecuteAsync(transaction,
                    $"""
                    DELETE FROM ResultPropertyLink
                    WHERE ResultModelId = {resultModelId}
                    """);

                await SqliteUtil.DbCommands.ExecuteAsync(transaction,
                    $"""
                    DELETE FROM ResultProperty
                    WHERE ResultModelId = {resultModelId}
                    """);

                for (int position = 0; position < resultModelDefinition.Properties.Count; position++)
                {
                    string propertyName = resultModelDefinition.Properties[position].Replace("'", "''");

                    await SqliteUtil.DbCommands.ExecuteAsync(transaction,
                        $"""
                        INSERT INTO ResultProperty (ResultModelId, Name, Position)
                        VALUES ({resultModelId}, '{propertyName}', {position})
                        """);
                }

                foreach (ResultPropertyLink propertyLink in resultModelDefinition.PropertyLinks)
                {
                    string moduleName = propertyLink.GrabberModuleName.Replace("'", "''");
                    string contributionLabel = propertyLink.GrabberContributionLabel.Replace("'", "''");
                    string grabberKey = propertyLink.GrabberKey.Replace("'", "''");
                    string propertyName = propertyLink.ResultModelPropertyName.Replace("'", "''");

                    await SqliteUtil.DbCommands.ExecuteAsync(transaction,
                        $"""
                        INSERT INTO ResultPropertyLink
                            (ResultModelId, PropertyName, GrabberModuleName, GrabberContributionLabel, GrabberKey)
                        VALUES
                            ({resultModelId}, '{propertyName}', '{moduleName}', '{contributionLabel}', '{grabberKey}')
                """);
                }

                SqliteUtil.DbSession.CloseTransaction(transaction, true);

                resultModelDefinition.Id = resultModelId;
                return Result<IResultModelDefinition>.Success(resultModelDefinition);
            }
            catch (Exception exception)
            {
                if (transaction.Connection != null)
                {
                    SqliteUtil.DbSession.CloseTransaction(transaction, false);
                }

                return Result<IResultModelDefinition>.Failure("RESULT_MODEL_DEFINITION_SAVE_FAILED", exception.Message);
            }
        }

        public static async Task<Result<bool>> DeleteResultModelDefinition(string dbFilepath, int id)
        {
            SqliteTransaction transaction = DbSession.OpenTransaction(dbFilepath);

            try
            {
                await DbCommands.ExecuteAsync(transaction,
                    $"""
                    DELETE FROM ResultPropertyLink
                    WHERE ResultModelId = {id}
                    """);

                await DbCommands.ExecuteAsync(transaction,
                    $"""
                    DELETE FROM ResultProperty
                    WHERE ResultModelId = {id}
                    """);

                int affectedRows = await DbCommands.ExecuteAsync(transaction,
                    $"""
                    DELETE FROM ResultModel
                    WHERE Id = {id}
                    """);

                DbSession.CloseTransaction(transaction, true);
                return Result<bool>.Success(affectedRows > 0);
            }
            catch (Exception exception)
            {
                if (transaction.Connection != null)
                {
                    DbSession.CloseTransaction(transaction, false);
                }

                return Result<bool>.Failure("RESULT_MODEL_DEFINITION_DELETE_FAILED", exception.Message);
            }
        }

    }
}
