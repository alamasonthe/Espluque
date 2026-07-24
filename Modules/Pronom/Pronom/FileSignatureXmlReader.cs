using Microsoft.Data.Sqlite;
using System.Xml.Linq;
using Util;

namespace Pronom
{
    public static class FileSignatureXmlReader
    {
        public static async Task<Result> ImportSignaturesFromXmlAsync(XDocument xmlDocument, string dbFilePath)
        {
            string importSource = "FileSignatures";

            XElement? root = xmlDocument.Root;

            if (root == null || root.Name.LocalName != "FFSignatureFile")
            {
                return Result.Failure( "PRONOM_IMPORT_ROOT_INVALID", "The XML root element is not FFSignatureFile.");
            }

            string? version = root.Attribute("Version")?.Value;
            string? dateCreated = root.Attribute("DateCreated")?.Value;

            var transaction = DbTransactionFactory.OpenTransaction(dbFilePath);

            try
            { 
                
                var cleanResult = await ImportSignaturesCommonRepository.CleanTablesAsync(transaction, importSource);
                if (!cleanResult.IsSuccess)
                {
                    DbTransactionFactory.CloseTransaction(transaction, false);
                    return Result.Failure(cleanResult.Error!.Code, cleanResult.Error.Message);
                }

                var resultVersionUpdate = await ImportSignaturesCommonRepository.UpsertSourceVersionAsync(transaction, importSource, version, dateCreated);
                if (!resultVersionUpdate.IsSuccess)
                {
                    DbTransactionFactory.CloseTransaction(transaction, false);
                    return Result.Failure(resultVersionUpdate.Error!.Code, resultVersionUpdate.Error.Message);
                }

               var resultInternalSignatureInsert = await ImportInternalSignatureCollectionAsync(transaction, importSource, root);
                if (!resultInternalSignatureInsert.IsSuccess)
                {
                    DbTransactionFactory.CloseTransaction(transaction, false);
                    return Result.Failure(resultInternalSignatureInsert.Error!.Code, resultInternalSignatureInsert.Error.Message);
                }

                var resultFileFormatInsert = await ImportFileFormatCollectionAsync(transaction, importSource, root);
                if (!resultFileFormatInsert.IsSuccess)
                {
                    DbTransactionFactory.CloseTransaction(transaction, false);
                    return Result.Failure(resultFileFormatInsert.Error!.Code, resultFileFormatInsert.Error.Message);
                }

                DbTransactionFactory.CloseTransaction(transaction, true);
                return Result.Success();
            }
            catch (Exception exception)
            {
                DbTransactionFactory.CloseTransaction(transaction, false);

                return Result.Failure("PRONOM_IMPORT_FILE_SIGNATURE_IMPORT_FAILED", $"FileSignatureXmlReader.ImportSignaturesFromXmlAsync: failed to import file signatures. {exception.Message}");
            }
        }


        private static async Task<Result> ImportInternalSignatureCollectionAsync(
            SqliteTransaction transaction,
            string importSource,
            XElement root)
        {
            var connection = transaction.Connection;
            XElement? internalSignatureCollection = root
                .Elements()
                .FirstOrDefault(element => element.Name.LocalName == "InternalSignatureCollection");

            if (internalSignatureCollection == null)
            {
                return Result.Failure("PRONOM_IMPORT_INTERNAL_SIGNATURE_COLLECTION_MISSING", "The InternalSignatureCollection element is missing.");
            }

            int nextEspluqueSequenceId = 1;

            foreach (XElement internalSignatureElement in internalSignatureCollection
                .Elements()
                .Where(element => element.Name.LocalName == "InternalSignature"))
            {
                string? internalSignatureIdText = internalSignatureElement.Attribute("ID")?.Value;

                if (!int.TryParse(internalSignatureIdText, out int internalSignatureId))
                {
                    return Result.Failure("PRONOM_IMPORT_INTERNAL_SIGNATURE_ID_INVALID", $"The internal signature ID is invalid: {internalSignatureIdText}");
                }

                var internalSignatureResult = await ImportSignaturesCommonRepository.InsertInternalSignatureAsync(
                    transaction,
                    importSource,
                    internalSignatureIdText);

                if (!internalSignatureResult.IsSuccess) return Result.Failure(internalSignatureResult.Error!.Code, internalSignatureResult.Error.Message);

                foreach (XElement byteSequenceElement in internalSignatureElement
                    .Elements()
                    .Where(element => element.Name.LocalName == "ByteSequence"))
                {
                    var importByteSequenceResult = await CommonSignatureXmlReader.ImportByteSequenceAsync(
                        transaction,
                        importSource,
                        internalSignatureId,
                        byteSequenceElement,
                        nextEspluqueSequenceId);

                    if (!importByteSequenceResult.IsSuccess) return Result.Failure(importByteSequenceResult.Error!.Code, importByteSequenceResult.Error.Message);

                    nextEspluqueSequenceId = importByteSequenceResult.Value;
                }
            }

            return Result.Success();
        }

        private static async Task<Result> ImportFileFormatCollectionAsync(
            SqliteTransaction transaction,
            string importSource,
            XElement root)
        {
            var connection = transaction?.Connection;
            XElement? fileFormatCollection = root
                .Elements()
                .FirstOrDefault(element => element.Name.LocalName == "FileFormatCollection");

            if (fileFormatCollection == null)
            {
                return Result.Failure("PRONOM_IMPORT_FILE_FORMAT_COLLECTION_MISSING", "The FileFormatCollection element is missing.");
            }

            List<XElement> fileFormatElements = fileFormatCollection
                .Elements()
                .Where(element => element.Name.LocalName == "FileFormat")
                .ToList();

            var fileFormatsResult = await ImportFileFormatsAsync(transaction, importSource, fileFormatElements);
            if (!fileFormatsResult.IsSuccess) return Result.Failure(fileFormatsResult.Error!.Code, fileFormatsResult.Error.Message);

            var relatedDataResult = await ImportFileFormatRelatedDataAsync(transaction, importSource, fileFormatElements);
            if (!relatedDataResult.IsSuccess) return Result.Failure(relatedDataResult.Error!.Code, relatedDataResult.Error.Message);

            return Result.Success();
        }

        private static async Task<Result> ImportFileFormatsAsync(
            SqliteTransaction transaction,
            string importSource,
            List<XElement> fileFormatElements)
        {
            var connection = transaction?.Connection;
            foreach (XElement fileFormatElement in fileFormatElements)
            {
                string? fileFormatIdText = fileFormatElement.Attribute("ID")?.Value;

                if (!int.TryParse(fileFormatIdText, out int fileFormatId))
                {
                    return Result.Failure("PRONOM_IMPORT_REQUIRED_INTEGER_ATTRIBUTE_INVALID", $"The required integer attribute ID is invalid or missing on {fileFormatElement.Name.LocalName}: {fileFormatIdText}");
                }

                string? name = fileFormatElement.Attribute("Name")?.Value;
                string? puid = fileFormatElement.Attribute("PUID")?.Value;
                string? version = fileFormatElement.Attribute("Version")?.Value;
                string? mimeType = fileFormatElement.Attribute("MIMEType")?.Value;

                if (string.IsNullOrWhiteSpace(name))
                {
                    return Result.Failure("PRONOM_IMPORT_FILE_FORMAT_NAME_EMPTY", $"The file format name is empty for file format ID {fileFormatId}.");
                }

                if (string.IsNullOrWhiteSpace(puid))
                {
                    return Result.Failure("PRONOM_IMPORT_FILE_FORMAT_PUID_EMPTY", $"The file format PUID is empty for file format ID {fileFormatId}.");
                }

                var fileFormatResult = await ImportFileSignatureRepository.InsertFileFormatAsync(transaction, importSource, fileFormatId, mimeType, name, puid, version);

                if (!fileFormatResult.IsSuccess) return Result.Failure(fileFormatResult.Error!.Code, fileFormatResult.Error.Message);
            }

            return Result.Success();
        }

        private static async Task<Result> ImportFileFormatRelatedDataAsync(
            SqliteTransaction transaction,
            string importSource,
            List<XElement> fileFormatElements)
        {
            var connection = transaction?.Connection;
            foreach (XElement fileFormatElement in fileFormatElements)
            {
                string? fileFormatIdText = fileFormatElement.Attribute("ID")?.Value;

                if (!int.TryParse(fileFormatIdText, out int fileFormatId))
                {
                    return Result.Failure("PRONOM_IMPORT_REQUIRED_INTEGER_ATTRIBUTE_INVALID", $"The required integer attribute ID is invalid or missing on {fileFormatElement.Name.LocalName}: {fileFormatIdText}");
                }

                foreach (XElement childElement in fileFormatElement.Elements())
                {
                    if (childElement.Name.LocalName == "Extension")
                    {
                        string extension = childElement.Value.Trim();

                        var extensionResult = await ImportFileSignatureRepository.InsertExtensionAsync(connection, transaction, importSource, fileFormatId, extension);

                        if (!extensionResult.IsSuccess) return Result.Failure(extensionResult.Error!.Code, extensionResult.Error.Message);
                    }

                    if (childElement.Name.LocalName == "InternalSignatureID")
                    {
                        if (!int.TryParse(childElement.Value, out int internalSignatureId))
                        {
                            return Result.Failure("PRONOM_IMPORT_FILE_FORMAT_INTERNAL_SIGNATURE_ID_INVALID", $"The internal signature ID is invalid for file format ID {fileFormatId}: {childElement.Value}");
                        }

                        var internalSignatureResult = await ImportFileSignatureRepository.InsertFileFormatInternalSignatureAsync(connection, transaction, importSource, fileFormatId, internalSignatureId);

                        if (!internalSignatureResult.IsSuccess) return Result.Failure(internalSignatureResult.Error!.Code, internalSignatureResult.Error.Message);
                    }

                    if (childElement.Name.LocalName == "HasPriorityOverFileFormatID")
                    {
                        if (!int.TryParse(childElement.Value, out int hasPriorityOverFileFormatId))
                        {
                            return Result.Failure("PRONOM_IMPORT_PRIORITY_FILE_FORMAT_ID_INVALID", $"The priority file format ID is invalid for file format ID {fileFormatId}: {childElement.Value}");
                        }

                        var analyzePriorityResult = await ImportFileSignatureRepository.InsertAnalyzePriorityAsync(connection, transaction, importSource, fileFormatId, hasPriorityOverFileFormatId);

                        if (!analyzePriorityResult.IsSuccess) return Result.Failure(analyzePriorityResult.Error!.Code, analyzePriorityResult.Error.Message);
                    }
                }
            }

            return Result.Success();
        }
    }
}
