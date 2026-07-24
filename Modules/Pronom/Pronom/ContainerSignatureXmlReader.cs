using Microsoft.Data.Sqlite;
using System.Xml.Linq;
using Util;

namespace Pronom
{
    public class ContainerSignatureXmlReader
    {
        public static async Task<Result> ImportSignaturesFromXmlAsync(XDocument xmlDocument, string dbFilePath)
        {
            string importSource = "ContainerSignatures";

            XElement? root = xmlDocument.Root;

            if (root == null || root.Name.LocalName != "ContainerSignatureMapping")
            {
                return Result.Failure("PRONOM_IMPORT_ROOT_INVALID", "The XML root element is not ContainerSignatureMapping.");
            }

            string? version = root.Attribute("signatureVersion")?.Value;
            string? date = DateTime.Today.ToString("yyyy-MM-dd");

            var transaction = DbTransactionFactory.OpenTransaction(dbFilePath);

            try
            {
                var cleanResult = await ImportSignaturesCommonRepository.CleanTablesAsync(
                    transaction,
                    importSource);

                if (!cleanResult.IsSuccess)
                {
                    DbTransactionFactory.CloseTransaction(transaction, false);
                    return Result.Failure(cleanResult.Error!.Code, cleanResult.Error.Message);
                }

                var resultVersionUpdate = await ImportSignaturesCommonRepository.UpsertSourceVersionAsync(
                    transaction,
                    importSource,
                    version,
                    date);

                if (!resultVersionUpdate.IsSuccess)
                {
                    DbTransactionFactory.CloseTransaction(transaction, false);
                    return Result.Failure(resultVersionUpdate.Error!.Code, resultVersionUpdate.Error.Message);
                }

                var resultContainerSignaturesImport = await ImportContainerSignaturesAsync(
                    transaction,
                    importSource,
                    root);

                if (!resultContainerSignaturesImport.IsSuccess)
                {
                    DbTransactionFactory.CloseTransaction(transaction, false);
                    return Result.Failure(resultContainerSignaturesImport.Error!.Code, resultContainerSignaturesImport.Error.Message);
                }

                var resultTriggerPuidsImport = await ImportTriggerPuidsAsync(
                    transaction,
                    importSource,
                    root);

                if (!resultTriggerPuidsImport.IsSuccess)
                {
                    DbTransactionFactory.CloseTransaction(transaction, false);
                    return Result.Failure(resultTriggerPuidsImport.Error!.Code, resultTriggerPuidsImport.Error.Message);
                }

                DbTransactionFactory.CloseTransaction(transaction, true);

                return Result.Success();
            }
            catch (Exception exception)
            {
                DbTransactionFactory.CloseTransaction(transaction, false);

                return Result.Failure("PRONOM_IMPORT_CONTAINER_SIGNATURE_IMPORT_FAILED", $"ContainerSignatureXmlReader.ImportSignaturesFromXmlAsync: failed to import container signatures. {exception.Message}");
            }
        }

        private static async Task<Result> ImportContainerSignaturesAsync(
            SqliteTransaction transaction,
            string importSource,
            XElement root)
        {
            var connexion = transaction.Connection;
            var resultFileFormatMappingsDictionnary = LoadFileFormatMappings(root);
            if (!resultFileFormatMappingsDictionnary.IsSuccess)
            {
                return Result.Failure(resultFileFormatMappingsDictionnary.Error!.Code, resultFileFormatMappingsDictionnary.Error.Message);
            }

            Dictionary<int, string> puidsByContainerSignatureId = resultFileFormatMappingsDictionnary.Value!;

            XElement? containerSignaturesElement = root
                .Elements()
                .FirstOrDefault(element => element.Name.LocalName == "ContainerSignatures");

            if (containerSignaturesElement == null)
            {
                return Result.Failure("PRONOM_IMPORT_CONTAINER_SIGNATURES_MISSING", "The ContainerSignatures element is missing.");
            }

            // What a nice magic number! I'll be delighted if one day it becomes a problem.
            int nextEspluqueSequenceId = 100001;
            int nextContainerInternalSignatureId = 100001;

            foreach (XElement containerSignatureElement in containerSignaturesElement
                .Elements()
                .Where(element => element.Name.LocalName == "ContainerSignature"))
            {
                string? containerSignatureIdText = containerSignatureElement.Attribute("Id")?.Value;

                if (!int.TryParse(containerSignatureIdText, out int containerSignatureId))
                {
                    // return Result.Failure("PRONOM_IMPORT_CONTAINER_SIGNATURE_ID_INVALID", $"The container signature ID is invalid or missing: {containerSignatureIdText}");
                    continue;
                }

                if (!puidsByContainerSignatureId.TryGetValue(containerSignatureId, out string? puid))
                {
                    // return Result.Failure("PRONOM_IMPORT_CONTAINER_FILE_FORMAT_MAPPING_MISSING", $"The FileFormatMapping is missing for container signature ID {containerSignatureId}.");
                    continue;
                }

                string? containerType = containerSignatureElement.Attribute("ContainerType")?.Value;

                if (string.IsNullOrWhiteSpace(containerType))
                {
                    return Result.Failure("PRONOM_IMPORT_CONTAINER_TYPE_EMPTY", $"The container type is empty for container signature ID {containerSignatureId}.");
                }

                string description = containerSignatureElement
                    .Elements()
                    .First(element => element.Name.LocalName == "Description")
                    .Value
                    .Trim();

                var containerSignatureResult = await ImportContainerSignatureRepository.InsertContainerSignatureAsync(
                    transaction,
                    importSource,
                    containerSignatureId,
                    containerType.Trim(),
                    description,
                    puid);

                if (!containerSignatureResult.IsSuccess) return Result.Failure(containerSignatureResult.Error!.Code, containerSignatureResult.Error.Message);

                var importFilesResult = await ImportFilesAsync(
                    transaction,
                    importSource,
                    containerSignatureId,
                    containerSignatureElement,
                    nextEspluqueSequenceId,
                    nextContainerInternalSignatureId);

                if (!importFilesResult.IsSuccess) return Result.Failure(importFilesResult.Error!.Code, importFilesResult.Error.Message);

                nextEspluqueSequenceId = importFilesResult.Value.NextEspluqueSequenceId;
                nextContainerInternalSignatureId = importFilesResult.Value.NextContainerInternalSignatureId;

            }
            return Result.Success();
        }

        private static Result<Dictionary<int, string>> LoadFileFormatMappings(XElement root)
        {
            XElement? fileFormatMappingsElement = root
                .Elements()
                .FirstOrDefault(element => element.Name.LocalName == "FileFormatMappings");

            if (fileFormatMappingsElement == null)
            {
                return Result<Dictionary<int, string>>.Failure("PRONOM_IMPORT_FILE_FORMAT_MAPPINGS_MISSING", "The FileFormatMappings element is missing.");
            }

            Dictionary<int, string> puidsByContainerSignatureId = [];

            foreach (XElement fileFormatMappingElement in fileFormatMappingsElement
                .Elements()
                .Where(element => element.Name.LocalName == "FileFormatMapping"))
            {
                string? signatureIdText = fileFormatMappingElement.Attribute("signatureId")?.Value;

                if (!int.TryParse(signatureIdText, out int signatureId))
                {
                    return Result<Dictionary<int, string>>.Failure("PRONOM_IMPORT_FILE_FORMAT_MAPPING_SIGNATURE_ID_INVALID", $"The FileFormatMapping signatureId is invalid or missing: {signatureIdText}");
                }

                string? puid = fileFormatMappingElement.Attribute("Puid")?.Value;

                if (string.IsNullOrWhiteSpace(puid))
                {
                    return Result<Dictionary<int, string>>.Failure("PRONOM_IMPORT_FILE_FORMAT_MAPPING_PUID_EMPTY", $"The FileFormatMapping PUID is empty for signature ID {signatureId}.");
                }

                puidsByContainerSignatureId[signatureId] = puid.Trim();
            }

            return Result<Dictionary<int, string>>.Success(puidsByContainerSignatureId);
        }

        private static async Task<Result<(int NextEspluqueSequenceId, int NextContainerInternalSignatureId)>> ImportFilesAsync(
            SqliteTransaction transaction,
            string importSource,
            int containerSignatureId,
            XElement containerSignatureElement,
            int nextEspluqueSequenceId,
            int nextContainerInternalSignatureId)
        {
            var connection = transaction.Connection;

            XElement? filesElement = containerSignatureElement
                    .Elements()
                    .FirstOrDefault(element => element.Name.LocalName == "Files");

            if (filesElement == null)
            {
                return Result<(int NextEspluqueSequenceId, int NextContainerInternalSignatureId)>.Failure("PRONOM_IMPORT_CONTAINER_FILES_MISSING", $"The Files element is missing for container signature ID {containerSignatureId}.");
            }

            List<(string Path, XElement FileElement)> fileImports = [];

            foreach (XElement fileElement in filesElement
                    .Elements()
                    .Where(element => element.Name.LocalName == "File"))
            {
                string? path = fileElement
                        .Elements()
                        .FirstOrDefault(element => element.Name.LocalName == "Path")
                        ?.Value
                        ?.Trim();
                fileImports.Add((path, fileElement));
            }

            var fileGroups = fileImports.GroupBy( fileImport => fileImport.Path, fileImport => fileImport.FileElement);

            foreach (var fileGroup in fileGroups)
            {
                string filePath = fileGroup.Key;

                var containerFileResult = await ImportContainerSignatureRepository.InsertContainerFileAsync(
                    connection,
                    transaction,
                    importSource,
                    containerSignatureId,
                    filePath);

                if (!containerFileResult.IsSuccess) return Result<(int NextEspluqueSequenceId, int NextContainerInternalSignatureId)>.Failure(containerFileResult.Error!.Code, containerFileResult.Error.Message);

                foreach (XElement fileElement in fileGroup)
                {
                    XElement? binarySignaturesElement = fileElement
                        .Elements()
                        .FirstOrDefault(element => element.Name.LocalName == "BinarySignatures");

                    if (binarySignaturesElement == null)
                    {
                        continue;
                    }

                    var importBinarySignaturesResult = await ImportBinarySignaturesAsync(
                        connection,
                        transaction,
                        importSource,
                        containerSignatureId,
                        filePath,
                        binarySignaturesElement,
                        nextEspluqueSequenceId,
                        nextContainerInternalSignatureId);

                    if (!importBinarySignaturesResult.IsSuccess) return Result<(int NextEspluqueSequenceId, int NextContainerInternalSignatureId)>.Failure(importBinarySignaturesResult.Error!.Code, importBinarySignaturesResult.Error.Message);

                    nextEspluqueSequenceId = importBinarySignaturesResult.Value.NextEspluqueSequenceId;
                    nextContainerInternalSignatureId = importBinarySignaturesResult.Value.NextContainerInternalSignatureId;
                }
            }

            return Result<(int NextEspluqueSequenceId, int NextContainerInternalSignatureId)>.Success((nextEspluqueSequenceId, nextContainerInternalSignatureId));
        }

        private static async Task<Result<(int NextEspluqueSequenceId, int NextContainerInternalSignatureId)>> ImportBinarySignaturesAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string importSource,
            int containerSignatureId,
            string filePath,
            XElement binarySignaturesElement,
            int nextEspluqueSequenceId,
            int nextContainerInternalSignatureId)
        {
            XElement internalSignatureCollectionElement = binarySignaturesElement
                .Elements()
                .First(element => element.Name.LocalName == "InternalSignatureCollection");

            XElement internalSignatureElement = internalSignatureCollectionElement
                .Elements()
                .First(element => element.Name.LocalName == "InternalSignature");

            int signatureIdForContainer = nextContainerInternalSignatureId;
            nextContainerInternalSignatureId++;

            var internalSignatureResult = await ImportSignaturesCommonRepository.InsertInternalSignatureAsync(
                transaction,
                importSource,
                signatureIdForContainer.ToString());

            if (!internalSignatureResult.IsSuccess) return Result<(int NextEspluqueSequenceId, int NextContainerInternalSignatureId)>.Failure(internalSignatureResult.Error!.Code, internalSignatureResult.Error.Message);

            var containerFileInternalSignatureResult = await ImportContainerSignatureRepository.InsertContainerFileInternalSignatureAsync(
                transaction,
                importSource,
                signatureIdForContainer,
                containerSignatureId,
                filePath);

            if (!containerFileInternalSignatureResult.IsSuccess) return Result<(int NextEspluqueSequenceId, int NextContainerInternalSignatureId)>.Failure(containerFileInternalSignatureResult.Error!.Code, containerFileInternalSignatureResult.Error.Message);

            foreach (XElement byteSequenceElement in internalSignatureElement
                .Elements()
                .Where(element => element.Name.LocalName == "ByteSequence"))
            {
                var importByteSequenceResult = await CommonSignatureXmlReader.ImportByteSequenceAsync(
                    transaction,
                    importSource,
                    signatureIdForContainer,
                    byteSequenceElement,
                    nextEspluqueSequenceId);

                if (!importByteSequenceResult.IsSuccess) return Result<(int NextEspluqueSequenceId, int NextContainerInternalSignatureId)>.Failure(importByteSequenceResult.Error!.Code, importByteSequenceResult.Error.Message);

                nextEspluqueSequenceId = importByteSequenceResult.Value;
            }

            return Result<(int NextEspluqueSequenceId, int NextContainerInternalSignatureId)>.Success((nextEspluqueSequenceId, nextContainerInternalSignatureId));
        }

        private static async Task<Result> ImportTriggerPuidsAsync(
            SqliteTransaction transaction,
            string importSource,
            XElement root)
        {
            XElement triggerPuidsElement = root
                .Elements()
                .First(element => element.Name.LocalName == "TriggerPuids");

            foreach (XElement triggerPuidElement in triggerPuidsElement
                .Elements()
                .Where(element => element.Name.LocalName == "TriggerPuid"))
            {
                string containerType = triggerPuidElement.Attribute("ContainerType")!.Value.Trim();
                string puid = triggerPuidElement.Attribute("Puid")!.Value.Trim();

                var triggerResult = await ImportContainerSignatureRepository.InsertContainerTriggerAsync(
                    transaction,
                    importSource,
                    containerType,
                    puid);

                if (!triggerResult.IsSuccess) return Result.Failure(triggerResult.Error!.Code, triggerResult.Error.Message);
            }

            return Result.Success();
        }

    }
}
