using Microsoft.Data.Sqlite;
using System.Xml.Linq;
using Util;

namespace Pronom
{
    public class CommonSignatureXmlReader
    {
        public static async Task<Result<int>> ImportByteSequenceAsync(
            SqliteTransaction transaction,
            string importSource,
            int internalSignatureId,
            XElement byteSequenceElement,
            int nextEspluqueSequenceId)
        {
            var connection = transaction?.Connection;
            int espluqueSequenceId = nextEspluqueSequenceId;
            nextEspluqueSequenceId++;

            string? reference = byteSequenceElement.Attribute("Reference")?.Value;
            string? endianness = byteSequenceElement.Attribute("Endianness")?.Value;

            var byteSequenceResult = await ImportSignaturesCommonRepository.InsertByteSequenceAsync(
                transaction,
                importSource,
                internalSignatureId,
                espluqueSequenceId,
                reference,
                endianness);

            if (!byteSequenceResult.IsSuccess) return Result<int>.Failure(byteSequenceResult.Error!.Code, byteSequenceResult.Error.Message);

            int subSequencePosition = 1;

            foreach (XElement subSequenceElement in byteSequenceElement
                .Elements()
                .Where(element => element.Name.LocalName == "SubSequence"))
            {
                var importSubSequenceResult = await ImportSubSequenceAsync(
                    transaction,
                    importSource,
                    espluqueSequenceId,
                    subSequencePosition,
                    subSequenceElement);

                if (!importSubSequenceResult.IsSuccess) return Result<int>.Failure(importSubSequenceResult.Error!.Code, importSubSequenceResult.Error.Message);

                subSequencePosition++;
            }

            return Result<int>.Success(nextEspluqueSequenceId);
        }

        private static async Task<Result> ImportSubSequenceAsync(
             SqliteTransaction transaction,
             string importSource,
             int espluqueSequenceId,
             int subSequencePosition,
             XElement subSequenceElement)
        {
            var connection = transaction?.Connection;
            int position = subSequencePosition;

            int? minFragLength = null;
            string? minFragLengthText = subSequenceElement.Attribute("MinFragLength")?.Value;

            if (!string.IsNullOrWhiteSpace(minFragLengthText))
            {
                if (!int.TryParse(minFragLengthText, out int minFragLengthValue))
                {
                    return Result.Failure("PRONOM_IMPORT_OPTIONAL_INTEGER_ATTRIBUTE_INVALID", $"The optional integer attribute MinFragLength is invalid on {subSequenceElement.Name.LocalName}: {minFragLengthText}");
                }

                minFragLength = minFragLengthValue;
            }

            int? subSeqMinOffset = null;
            string? subSeqMinOffsetText = subSequenceElement.Attribute("SubSeqMinOffset")?.Value;

            if (!string.IsNullOrWhiteSpace(subSeqMinOffsetText))
            {
                if (!int.TryParse(subSeqMinOffsetText, out int subSeqMinOffsetValue))
                {
                    return Result.Failure("PRONOM_IMPORT_OPTIONAL_INTEGER_ATTRIBUTE_INVALID", $"The optional integer attribute SubSeqMinOffset is invalid on {subSequenceElement.Name.LocalName}: {subSeqMinOffsetText}");
                }

                subSeqMinOffset = subSeqMinOffsetValue;
            }

            int? subSeqMaxOffset = null;
            string? subSeqMaxOffsetText = subSequenceElement.Attribute("SubSeqMaxOffset")?.Value;

            if (!string.IsNullOrWhiteSpace(subSeqMaxOffsetText))
            {
                if (!int.TryParse(subSeqMaxOffsetText, out int subSeqMaxOffsetValue))
                {
                    return Result.Failure("PRONOM_IMPORT_OPTIONAL_INTEGER_ATTRIBUTE_INVALID", $"The optional integer attribute SubSeqMaxOffset is invalid on {subSequenceElement.Name.LocalName}: {subSeqMaxOffsetText}");
                }

                subSeqMaxOffset = subSeqMaxOffsetValue;
            }

            string? sequence = subSequenceElement
                .Elements()
                .FirstOrDefault(element => element.Name.LocalName == "Sequence")
                ?.Value;

            if (string.IsNullOrWhiteSpace(sequence))
            {
                return Result.Failure("PRONOM_IMPORT_SEQUENCE_EMPTY", $"The sequence is empty for EspluqueSequenceId {espluqueSequenceId}.");
            }

            string normalizedSequence = string.Join(
                " ",
                sequence.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

            var subSequenceResult = await ImportSignaturesCommonRepository.InsertSubSequenceAsync(
                connection,
                transaction,
                importSource,
                espluqueSequenceId,
                minFragLength,
                position,
                subSeqMaxOffset,
                subSeqMinOffset,
                normalizedSequence);

            if (!subSequenceResult.IsSuccess) return Result.Failure(subSequenceResult.Error!.Code, subSequenceResult.Error.Message);

            foreach (XElement fragmentElement in subSequenceElement
                .Elements()
                .Where(element => element.Name.LocalName == "LeftFragment" || element.Name.LocalName == "RightFragment"))
            {
                var fragmentResult = await ImportFragmentAsync(
                    connection,
                    transaction,
                    importSource,
                    espluqueSequenceId,
                    position,
                    fragmentElement);

                if (!fragmentResult.IsSuccess) return Result.Failure(fragmentResult.Error!.Code, fragmentResult.Error.Message);
            }

            return Result.Success();
        }

        private static async Task<Result> ImportFragmentAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string importSource,
            int espluqueSequenceId,
            int subSequencePosition,
            XElement fragmentElement)
        {
            string? maxOffsetText = fragmentElement.Attribute("MaxOffset")?.Value;

            if (!int.TryParse(maxOffsetText, out int maxOffset))
            {
                return Result.Failure("PRONOM_IMPORT_REQUIRED_INTEGER_ATTRIBUTE_INVALID", $"The required integer attribute MaxOffset is invalid or missing on {fragmentElement.Name.LocalName}: {maxOffsetText}");
            }

            string? minOffsetText = fragmentElement.Attribute("MinOffset")?.Value;

            if (!int.TryParse(minOffsetText, out int minOffset))
            {
                return Result.Failure("PRONOM_IMPORT_REQUIRED_INTEGER_ATTRIBUTE_INVALID", $"The required integer attribute MinOffset is invalid or missing on {fragmentElement.Name.LocalName}: {minOffsetText}");
            }

            string? fragmentPositionText = fragmentElement.Attribute("Position")?.Value;

            if (!int.TryParse(fragmentPositionText, out int fragmentPosition))
            {
                return Result.Failure("PRONOM_IMPORT_REQUIRED_INTEGER_ATTRIBUTE_INVALID", $"The required integer attribute Position is invalid or missing on {fragmentElement.Name.LocalName}: {fragmentPositionText}");
            }

            if (string.IsNullOrWhiteSpace(fragmentElement.Value))
            {
                return Result.Failure("PRONOM_IMPORT_FRAGMENT_VALUE_EMPTY", $"The fragment value is empty for EspluqueSequenceId {espluqueSequenceId}, SubSequence position {subSequencePosition}.");
            }

            string leftRight = fragmentElement.Name.LocalName == "LeftFragment"
                ? "Left"
                : "Right";

            var fragmentResult = await ImportSignaturesCommonRepository.InsertFragmentAsync(
                connection,
                transaction,
                importSource,
                espluqueSequenceId,
                subSequencePosition,
                leftRight,
                maxOffset,
                minOffset,
                fragmentPosition,
                fragmentElement.Value.Trim());

            if (!fragmentResult.IsSuccess) return Result.Failure(fragmentResult.Error!.Code, fragmentResult.Error.Message);

            return Result.Success();
        }

    }
}
