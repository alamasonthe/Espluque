using Espluque.Contracts.Ports;
using System.Xml.Linq;
using Util;

namespace PronomSqlite
{
    public class FileSignatureSource : IFileSignatureSource
    {
        private readonly IImportFileSignatureRepository _importRepository;

        public FileSignatureSource(IImportFileSignatureRepository importRepository)
        {
            _importRepository = importRepository;
        }

        public async Task<Result<bool>> ImportFileExtensionFromXmlAsync(string filePath)
        {
            string xmlContent;

            try
            {
                xmlContent = await System.IO.File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_XML_READ_FAILED",
                    $"PronomFileSource.ImportFileExtensionFromXmlAsync: failed to read file '{filePath}'. {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(xmlContent))
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_XML_EMPTY",
                    "The PRONOM XML content is empty.");
            }

            XDocument xmlDocument;

            try
            {
                xmlDocument = XDocument.Parse(xmlContent);
            }
            catch (Exception exception)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_XML_INVALID",
                    $"ImportService.ImportFileExtensionFromXmlAsync: invalid XML content. {exception.Message}");
            }

            XElement? root = xmlDocument.Root;

            if (root == null || root.Name.LocalName != "FFSignatureFile")
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_ROOT_INVALID",
                    "The XML root element is not FFSignatureFile.");
            }

            string? version = root.Attribute("Version")?.Value;
            string? dateCreated = root.Attribute("DateCreated")?.Value;

            Result<bool> createTransactionResult = await _importRepository.CreateTransactionAsync();

            if (!createTransactionResult.IsSuccess)
            {
                return createTransactionResult;
            }

            Result<bool> cleanResult = await _importRepository.CleanPronomTablesAsync();

            if (!cleanResult.IsSuccess)
            {
                await _importRepository.CloseTransactionAsync(false);
                return cleanResult;
            }

            Result<bool> sourceVersionResult = await _importRepository.UpsertSourceVersionAsync(
                "PronomSignatures",
                version,
                dateCreated);

            if (!sourceVersionResult.IsSuccess)
            {
                await _importRepository.CloseTransactionAsync(false);
                return sourceVersionResult;
            }

            Result<bool> internalSignatureCollectionResult = await ImportInternalSignatureCollectionAsync(root);

            if (!internalSignatureCollectionResult.IsSuccess)
            {
                await _importRepository.CloseTransactionAsync(false);
                return internalSignatureCollectionResult;
            }

            Result<bool> fileFormatCollectionResult = await ImportFileFormatCollectionAsync(root);

            if (!fileFormatCollectionResult.IsSuccess)
            {
                await _importRepository.CloseTransactionAsync(false);
                return fileFormatCollectionResult;
            }

            Result<bool> closeTransactionResult = await _importRepository.CloseTransactionAsync(true);

            if (!closeTransactionResult.IsSuccess)
            {
                return closeTransactionResult;
            }

            return Result<bool>.Success(true);
        }

        private async Task<Result<bool>> ImportInternalSignatureCollectionAsync(XElement root)
        {
            XElement? internalSignatureCollection = root
                .Elements()
                .FirstOrDefault(element => element.Name.LocalName == "InternalSignatureCollection");

            if (internalSignatureCollection == null)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_INTERNAL_SIGNATURE_COLLECTION_MISSING",
                    "The InternalSignatureCollection element is missing.");
            }

            int espluqueSequenceId = 0;

            foreach (XElement internalSignatureElement in internalSignatureCollection
                .Elements()
                .Where(element => element.Name.LocalName == "InternalSignature"))
            {
                string? internalSignatureIdText = internalSignatureElement.Attribute("ID")?.Value;

                if (!int.TryParse(internalSignatureIdText, out int internalSignatureId))
                {
                    return Result<bool>.Failure(
                        "PRONOM_IMPORT_INTERNAL_SIGNATURE_ID_INVALID",
                        $"The internal signature ID is invalid: {internalSignatureIdText}");
                }

                Result<bool> internalSignatureResult =
                    await _importRepository.InsertInternalSignatureAsync(internalSignatureIdText);

                if (!internalSignatureResult.IsSuccess)
                {
                    return internalSignatureResult;
                }

                foreach (XElement byteSequenceElement in internalSignatureElement
                    .Elements()
                    .Where(element => element.Name.LocalName == "ByteSequence"))
                {
                    espluqueSequenceId++;

                    string? reference = byteSequenceElement.Attribute("Reference")?.Value;
                    string? endianness = byteSequenceElement.Attribute("Endianness")?.Value;

                    Result<bool> byteSequenceResult =
                        await _importRepository.InsertByteSequenceAsync(
                            internalSignatureId,
                            espluqueSequenceId,
                            reference,
                            endianness);

                    if (!byteSequenceResult.IsSuccess)
                    {
                        return byteSequenceResult;
                    }

                    foreach (XElement subSequenceElement in byteSequenceElement
                        .Elements()
                        .Where(element => element.Name.LocalName == "SubSequence"))
                    {
                        Result<int> minFragLengthResult = ReadRequiredIntAttribute(subSequenceElement, "MinFragLength");
                        if (!minFragLengthResult.IsSuccess) return Result<bool>.Failure(minFragLengthResult.Error!.Code, minFragLengthResult.Error.Message);

                        Result<int> positionResult = ReadRequiredIntAttribute(subSequenceElement, "Position");
                        if (!positionResult.IsSuccess) return Result<bool>.Failure(positionResult.Error!.Code, positionResult.Error.Message);

                        Result<int> subSeqMinOffsetResult = ReadRequiredIntAttribute(subSequenceElement, "SubSeqMinOffset");
                        if (!subSeqMinOffsetResult.IsSuccess) return Result<bool>.Failure(subSeqMinOffsetResult.Error!.Code, subSeqMinOffsetResult.Error.Message);

                        Result<int?> subSeqMaxOffsetResult = ReadOptionalIntAttribute(subSequenceElement, "SubSeqMaxOffset");
                        if (!subSeqMaxOffsetResult.IsSuccess) return Result<bool>.Failure(subSeqMaxOffsetResult.Error!.Code, subSeqMaxOffsetResult.Error.Message);

                        string? sequence = subSequenceElement
                            .Elements()
                            .FirstOrDefault(element => element.Name.LocalName == "Sequence")
                            ?.Value;

                        if (string.IsNullOrWhiteSpace(sequence))
                        {
                            return Result<bool>.Failure(
                                "PRONOM_IMPORT_SEQUENCE_EMPTY",
                                $"The sequence is empty for internal signature {internalSignatureId}, EspluqueSequenceId {espluqueSequenceId}.");
                        }

                        string? defaultShiftText = subSequenceElement
                            .Elements()
                            .FirstOrDefault(element => element.Name.LocalName == "DefaultShift")
                            ?.Value;

                        if (!int.TryParse(defaultShiftText, out int defaultShift))
                        {
                            return Result<bool>.Failure(
                                "PRONOM_IMPORT_DEFAULT_SHIFT_INVALID",
                                $"The default shift is invalid for internal signature {internalSignatureId}, EspluqueSequenceId {espluqueSequenceId}: {defaultShiftText}");
                        }

                        Result<bool> subSequenceResult =
                            await _importRepository.InsertSubSequenceAsync(
                                espluqueSequenceId,
                                minFragLengthResult.Value,
                                positionResult.Value,
                                subSeqMaxOffsetResult.Value,
                                subSeqMinOffsetResult.Value,
                                sequence.Trim(),
                                defaultShift);

                        if (!subSequenceResult.IsSuccess)
                        {
                            return subSequenceResult;
                        }

                        foreach (XElement childElement in subSequenceElement.Elements())
                        {
                            if (childElement.Name.LocalName == "Shift")
                            {
                                string? byteValue = childElement.Attribute("Byte")?.Value;

                                if (!int.TryParse(childElement.Value, out int shiftValue))
                                {
                                    return Result<bool>.Failure(
                                        "PRONOM_IMPORT_SHIFT_VALUE_INVALID",
                                        $"The shift value is invalid for internal signature {internalSignatureId}, EspluqueSequenceId {espluqueSequenceId}, SubSequence position {positionResult.Value}: {childElement.Value}");
                                }

                                Result<bool> shiftResult =
                                    await _importRepository.InsertShiftAsync(
                                        espluqueSequenceId,
                                        positionResult.Value,
                                        byteValue ?? string.Empty,
                                        shiftValue);

                                if (!shiftResult.IsSuccess)
                                {
                                    return shiftResult;
                                }
                            }

                            if (childElement.Name.LocalName == "LeftFragment" ||
                                childElement.Name.LocalName == "RightFragment")
                            {
                                string leftRight = childElement.Name.LocalName == "LeftFragment"
                                    ? "Left"
                                    : "Right";

                                Result<int> maxOffsetResult = ReadRequiredIntAttribute(childElement, "MaxOffset");
                                if (!maxOffsetResult.IsSuccess) return Result<bool>.Failure(maxOffsetResult.Error!.Code, maxOffsetResult.Error.Message);

                                Result<int> minOffsetResult = ReadRequiredIntAttribute(childElement, "MinOffset");
                                if (!minOffsetResult.IsSuccess) return Result<bool>.Failure(minOffsetResult.Error!.Code, minOffsetResult.Error.Message);

                                Result<int> fragmentPositionResult = ReadRequiredIntAttribute(childElement, "Position");
                                if (!fragmentPositionResult.IsSuccess) return Result<bool>.Failure(fragmentPositionResult.Error!.Code, fragmentPositionResult.Error.Message);

                                Result<bool> fragmentResult =
                                    await _importRepository.InsertFragmentAsync(
                                        espluqueSequenceId,
                                        positionResult.Value,
                                        leftRight,
                                        maxOffsetResult.Value,
                                        minOffsetResult.Value,
                                        fragmentPositionResult.Value,
                                        childElement.Value.Trim());

                                if (!fragmentResult.IsSuccess)
                                {
                                    return fragmentResult;
                                }
                            }
                        }
                    }
                }
            }

            return Result<bool>.Success(true);
        }

        private async Task<Result<bool>> ImportFileFormatCollectionAsync(XElement root)
        {
            XElement? fileFormatCollection = root
                .Elements()
                .FirstOrDefault(element => element.Name.LocalName == "FileFormatCollection");

            if (fileFormatCollection == null)
            {
                return Result<bool>.Failure(
                    "PRONOM_IMPORT_FILE_FORMAT_COLLECTION_MISSING",
                    "The FileFormatCollection element is missing.");
            }

            List<XElement> fileFormatElements = fileFormatCollection
                .Elements()
                .Where(element => element.Name.LocalName == "FileFormat")
                .ToList();

            foreach (XElement fileFormatElement in fileFormatElements)
            {
                Result<int> fileFormatIdResult = ReadRequiredIntAttribute(fileFormatElement, "ID");

                if (!fileFormatIdResult.IsSuccess)
                {
                    return Result<bool>.Failure(
                        fileFormatIdResult.Error!.Code,
                        fileFormatIdResult.Error.Message);
                }

                string? name = fileFormatElement.Attribute("Name")?.Value;
                string? puid = fileFormatElement.Attribute("PUID")?.Value;
                string? version = fileFormatElement.Attribute("Version")?.Value;
                string? mimeType = fileFormatElement.Attribute("MIMEType")?.Value;

                if (string.IsNullOrWhiteSpace(name))
                {
                    return Result<bool>.Failure(
                        "PRONOM_IMPORT_FILE_FORMAT_NAME_EMPTY",
                        $"The file format name is empty for file format ID {fileFormatIdResult.Value}.");
                }

                if (string.IsNullOrWhiteSpace(puid))
                {
                    return Result<bool>.Failure(
                        "PRONOM_IMPORT_FILE_FORMAT_PUID_EMPTY",
                        $"The file format PUID is empty for file format ID {fileFormatIdResult.Value}.");
                }

                Result<bool> fileFormatResult = await _importRepository.InsertFileFormatAsync(
                    fileFormatIdResult.Value,
                    mimeType,
                    name,
                    puid,
                    version);

                if (!fileFormatResult.IsSuccess)
                {
                    return fileFormatResult;
                }
            }

            foreach (XElement fileFormatElement in fileFormatElements)
            {
                Result<int> fileFormatIdResult = ReadRequiredIntAttribute(fileFormatElement, "ID");

                if (!fileFormatIdResult.IsSuccess)
                {
                    return Result<bool>.Failure(
                        fileFormatIdResult.Error!.Code,
                        fileFormatIdResult.Error.Message);
                }

                int fileFormatId = fileFormatIdResult.Value;

                foreach (XElement childElement in fileFormatElement.Elements())
                {
                    if (childElement.Name.LocalName == "Extension")
                    {
                        string extension = childElement.Value.Trim();

                        Result<bool> extensionResult = await _importRepository.InsertExtensionAsync(
                            fileFormatId,
                            extension);

                        if (!extensionResult.IsSuccess)
                        {
                            return extensionResult;
                        }
                    }

                    if (childElement.Name.LocalName == "InternalSignatureID")
                    {
                        if (!int.TryParse(childElement.Value, out int internalSignatureId))
                        {
                            return Result<bool>.Failure(
                                "PRONOM_IMPORT_FILE_FORMAT_INTERNAL_SIGNATURE_ID_INVALID",
                                $"The internal signature ID is invalid for file format ID {fileFormatId}: {childElement.Value}");
                        }

                        Result<bool> internalSignatureResult = await _importRepository.InsertFileFormatInternalSignatureAsync(
                            fileFormatId,
                            internalSignatureId);

                        if (!internalSignatureResult.IsSuccess)
                        {
                            return internalSignatureResult;
                        }
                    }

                    if (childElement.Name.LocalName == "HasPriorityOverFileFormatID")
                    {
                        if (!int.TryParse(childElement.Value, out int hasPriorityOverFileFormatId))
                        {
                            return Result<bool>.Failure(
                                "PRONOM_IMPORT_PRIORITY_FILE_FORMAT_ID_INVALID",
                                $"The priority file format ID is invalid for file format ID {fileFormatId}: {childElement.Value}");
                        }

                        Result<bool> analyzePriorityResult = await _importRepository.InsertAnalyzePriorityAsync(
                            fileFormatId,
                            hasPriorityOverFileFormatId);

                        if (!analyzePriorityResult.IsSuccess)
                        {
                            return analyzePriorityResult;
                        }
                    }
                }
            }

            return Result<bool>.Success(true);
        }

        private static Result<int> ReadRequiredIntAttribute(XElement element, string attributeName)
        {
            string? value = element.Attribute(attributeName)?.Value;

            if (!int.TryParse(value, out int intValue))
            {
                return Result<int>.Failure(
                    "PRONOM_IMPORT_REQUIRED_INTEGER_ATTRIBUTE_INVALID",
                    $"The required integer attribute {attributeName} is invalid or missing on {element.Name.LocalName}: {value}");
            }

            return Result<int>.Success(intValue);
        }

        private static Result<int?> ReadOptionalIntAttribute(XElement element, string attributeName)
        {
            string? value = element.Attribute(attributeName)?.Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                return Result<int?>.Success(null);
            }

            if (!int.TryParse(value, out int intValue))
            {
                return Result<int?>.Failure(
                    "PRONOM_IMPORT_OPTIONAL_INTEGER_ATTRIBUTE_INVALID",
                    $"The optional integer attribute {attributeName} is invalid on {element.Name.LocalName}: {value}");
            }

            return Result<int?>.Success(intValue);
        }
    }
}
