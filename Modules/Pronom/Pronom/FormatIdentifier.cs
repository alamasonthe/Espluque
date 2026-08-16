using Microsoft.Extensions.Logging;
using Pronom.Entities;
using Util;
using System.IO;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Contributions;

namespace Pronom
{
    public class FormatIdentifier
    {
        private readonly Espluque.Contracts.CrossCutting.ILogger _logger;
        private readonly IEntityFactory _entityFactory;
        private readonly string _referentiel = "Pronom";

        public FormatIdentifier(
            Espluque.Contracts.CrossCutting.ILogger logger,
            IEntityFactory entityFactory)
        {
            _logger = logger;
            _entityFactory = entityFactory;
        }

        public async Task<Result<IFileFormat?>> MatchSignaturesAsync(string filePath, string dbFilePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Result<IFileFormat?>.Failure("PRONOM_MATCH_FILEPATH_MISSING", "PronomService.MatchInternalSignaturesAsync: filePath is missing.");
            }

            var fileName = Path.GetFileName(filePath).PadRight(35);

            Result<List<int>> internalSignatureIdsResult = await PronomRepository.ListInternalSignatureIdsAsync(dbFilePath);

            if (!internalSignatureIdsResult.IsSuccess)
            {
                return Result<IFileFormat?>.Failure(internalSignatureIdsResult.Error?.Code ?? "PRONOM_INTERNAL_SIGNATURE_IDS_LIST_FAILED", internalSignatureIdsResult.Error?.Message ?? "PronomService.MatchInternalSignaturesAsync: failed to list internal signature IDs.");
            }

            List<int> matchedInternalSignatureIds = [];

            foreach (int internalSignatureId in internalSignatureIdsResult.Value)
            {
                Result<PronomInternalSignature?> internalSignatureResult = await PronomRepository.GetInternalSignatureAsync(internalSignatureId, dbFilePath);

                if (!internalSignatureResult.IsSuccess)
                {

                    continue;
                }

                if (internalSignatureResult.Value is null)
                {

                    continue;
                }

                Result<bool> matchResult = await MatchInternalSignatureAsync(filePath, internalSignatureResult.Value);

                if (!matchResult.IsSuccess || !matchResult.Value)
                {
                    continue;
                }

                matchedInternalSignatureIds.Add(internalSignatureId);

                string matchedFormatDisplayName = "Unknown PRONOM format";

                // only for log
                Result<List<PronomFileFormatInfo>> matchedFormatInfosResult = await PronomRepository.GetInfosFromInternalSignatureAsync(internalSignatureId, dbFilePath);
                if (matchedFormatInfosResult.IsSuccess && matchedFormatInfosResult.Value is not null)
                {

                    PronomFileFormatInfo? matchedFormatInfo = matchedFormatInfosResult.Value.FirstOrDefault();

                    if (matchedFormatInfo is not null && !string.IsNullOrWhiteSpace(matchedFormatInfo.Name))
                    {
                        matchedFormatDisplayName = string.IsNullOrWhiteSpace(matchedFormatInfo.Version) ? matchedFormatInfo.Name : $"{matchedFormatInfo.Name} {matchedFormatInfo.Version}";
                    }
                }

                _logger.Log(LogLevel.Information, $"{fileName}\tPronom internal signature matched: {matchedFormatDisplayName} (InternalSignatureId={internalSignatureId})");
            }

            if (matchedInternalSignatureIds.Count == 0)
            {
                return Result<IFileFormat?>.Success(null);
            }

            PronomFileFormatInfo? formatInfo = null;

            Result<PronomFileFormatInfo?> formatInfoResult = await PronomRepository.GetHighestPriorityFileFormatInfosFromInternalSignatureIdsAsync(matchedInternalSignatureIds, dbFilePath);
            if (formatInfoResult.IsSuccess && formatInfoResult.Value is not null)
            {
                formatInfo = formatInfoResult.Value;
            }
            else
            {
                Result<List<PronomFileFormatInfo>> fallbackFormatInfosResult = await PronomRepository.GetInfosFromInternalSignatureAsync(matchedInternalSignatureIds[0], dbFilePath);
                if (fallbackFormatInfosResult.IsSuccess)
                {
                    formatInfo = fallbackFormatInfosResult.Value.FirstOrDefault();
                }
            }

            PronomFileFormatInfo? mainFormatInfo = formatInfo;

            string? puid = formatInfo?.Puid;

            if (!string.IsNullOrWhiteSpace(puid))
            {
                Result<string?> containerTypeResult = await PronomRepository.GetTriggerByPuidAsync(puid, dbFilePath);

                if (containerTypeResult.IsSuccess && !string.IsNullOrWhiteSpace(containerTypeResult.Value))
                {
                    Result<string?> containerPuidResult = await MatchContainerSignaturesAsync(filePath, containerTypeResult.Value, dbFilePath);

                    if (containerPuidResult.IsSuccess && !string.IsNullOrWhiteSpace(containerPuidResult.Value))
                    {
                        Result<PronomFileFormatInfo?> containerFormatInfoResult = await PronomRepository.GetInfosFromPuidAsync(containerPuidResult.Value, dbFilePath);

                        if (containerFormatInfoResult.IsSuccess && containerFormatInfoResult.Value is not null)
                        {
                            mainFormatInfo = containerFormatInfoResult.Value;
                        }
                    }
                }
            }

            IFileFormat? format = null;

            if (mainFormatInfo is not null)
            {
                format = _entityFactory.CreateFileFormat(
                    _referentiel,
                    mainFormatInfo.Name,
                    mainFormatInfo.Version,
                    mainFormatInfo.MimeType);

            }

            string? displayName = mainFormatInfo is null
                    ? "Unknown PRONOM format"
                    : string.IsNullOrWhiteSpace(mainFormatInfo.Version) ? mainFormatInfo.Name : $"{mainFormatInfo.Name} {mainFormatInfo.Version}";

            _logger.Log(LogLevel.Information, $"{fileName}\tPronom selected format: {displayName} ({matchedInternalSignatureIds.Count} matched signatures)");

            return Result<IFileFormat?>.Success(format);
        }

        private async Task<Result<bool>> MatchInternalSignatureAsync(
            string filePath,
            PronomInternalSignature internalSignature)
        {
            if (internalSignature is null)
            {
                return Result<bool>.Failure(
                    "PRONOM_MATCH_INTERNAL_SIGNATURE_MISSING",
                    "PronomService.MatchInternalSignatureAsync: internal signature is missing.");
            }

            if (internalSignature.ByteSequences.Count == 0)
            {
                return Result<bool>.Success(false);
            }

            foreach (PronomByteSequence byteSequence in internalSignature.ByteSequences)
            {
                Result<bool> byteSequenceResult = await MatchByteSequenceAsync(
                    filePath,
                    byteSequence);

                if (!byteSequenceResult.IsSuccess)
                {
                    return Result<bool>.Success(false);
                }

                if (!byteSequenceResult.Value)
                {
                    return Result<bool>.Success(false);
                }
            }

            return Result<bool>.Success(true);
        }

        private async Task<Result<bool>> MatchByteSequenceAsync(
            string filePath,
            PronomByteSequence byteSequence)
        {
            if (byteSequence is null)
            {
                return Result<bool>.Failure(
                    "PRONOM_MATCH_BYTE_SEQUENCE_MISSING",
                    "PronomService.MatchByteSequenceAsync: byte sequence is missing.");
            }

            if (byteSequence.SubSequences.Count == 0)
            {
                return Result<bool>.Success(false);
            }

            string reference = string.IsNullOrWhiteSpace(byteSequence.Reference)
                ? "Variable"
                : byteSequence.Reference;

            long readOffset = 0;
            int readSize;

            if (string.Equals(reference, "BOFoffset", StringComparison.OrdinalIgnoreCase))
            {
                readSize = CalculateRequiredByteSequenceWindowSize(byteSequence);
            }
            else if (string.Equals(reference, "EOFoffset", StringComparison.OrdinalIgnoreCase))
            {
                readSize = CalculateRequiredByteSequenceWindowSize(byteSequence);

                
                Result<long> lengthResult = Util.File.GetLength(filePath);

                if (!lengthResult.IsSuccess)
                {
                    return Result<bool>.Success(false);
                }

                readOffset = Math.Max(0, lengthResult.Value - readSize);
            }
            else if (string.Equals(reference, "Variable", StringComparison.OrdinalIgnoreCase))
            {
                Result<long> lengthResult = Util.File.GetLength(filePath);

                if (!lengthResult.IsSuccess)
                {
                    return Result<bool>.Success(false);
                }

                if (lengthResult.Value <= 0 || lengthResult.Value > int.MaxValue)
                {
                    return Result<bool>.Success(false);
                }

                readSize = (int)lengthResult.Value;
            }
            else
            {
                return Result<bool>.Success(false);
            }

            if (readSize <= 0)
            {
                return Result<bool>.Success(false);
            }

            Result<byte[]> byteWindowResult = Bin.ReadBytesFromFile(
                filePath,
                readOffset,
                readSize);

            if (!byteWindowResult.IsSuccess || byteWindowResult.Value is null)
            {
                return Result<bool>.Success(false);
            }

            int currentOffset = 0;

            foreach (PronomSubSequence subSequence in byteSequence.SubSequences.OrderBy(subSequence => subSequence.Position))
            {
                Result<int?> subSequenceResult = await MatchSubSequenceAsync(
                    byteWindowResult.Value,
                    subSequence,
                    currentOffset);

                if (!subSequenceResult.IsSuccess)
                {
                    return Result<bool>.Success(false);
                }

                if (!subSequenceResult.Value.HasValue)
                {
                    return Result<bool>.Success(false);
                }

                currentOffset = subSequenceResult.Value.Value;
            }

            return Result<bool>.Success(true);
        }

        private async Task<Result<int?>> MatchSubSequenceAsync(
            byte[] byteWindow,
            PronomSubSequence subSequence,
            int startOffset)
        {
            if (byteWindow is null)
            {
                return Result<int?>.Failure(
                    "PRONOM_MATCH_BYTE_WINDOW_MISSING",
                    "PronomService.MatchSubSequenceAsync: byte window is missing.");
            }

            if (subSequence is null)
            {
                return Result<int?>.Failure(
                    "PRONOM_MATCH_SUB_SEQUENCE_MISSING",
                    "PronomService.MatchSubSequenceAsync: sub-sequence is missing.");
            }

            if (startOffset < 0)
            {
                return Result<int?>.Failure(
                    "PRONOM_MATCH_START_OFFSET_INVALID",
                    $"PronomService.MatchSubSequenceAsync: start offset is invalid: {startOffset}.");
            }

            await Task.CompletedTask;

            byte[] sequenceBytes = ConvertHexToBytes(subSequence.Sequence);

            if (sequenceBytes.Length == 0)
            {
                return Result<int?>.Success(null);
            }

            int minLeftFragmentSpan = CalculateMinimumFragmentSpan(subSequence, "Left");
            int maxLeftFragmentSpan = CalculateMaximumFragmentSpan(subSequence, "Left");

            int minOffset =
                startOffset
                + subSequence.SubSeqMinOffset
                + minLeftFragmentSpan;

            int maxOffset =
                startOffset
                + (subSequence.SubSeqMaxOffset ?? byteWindow.Length)
                + maxLeftFragmentSpan;

            if (minOffset < 0 || maxOffset < minOffset)
            {
                return Result<int?>.Success(null);
            }

            int lastStartOffset = Math.Min(
                maxOffset,
                byteWindow.Length - sequenceBytes.Length);

            if (lastStartOffset < minOffset)
            {
                return Result<int?>.Success(null);
            }

            int currentOffset = minOffset;

            while (currentOffset <= lastStartOffset)
            {
                bool sequenceMatches = true;

                for (int i = 0; i < sequenceBytes.Length; i++)
                {
                    if (byteWindow[currentOffset + i] != sequenceBytes[i])
                    {
                        sequenceMatches = false;
                        break;
                    }
                }

                if (sequenceMatches)
                {
                    /*
                    _logger.Log(
                        LogLevel.Information,
                        $"Pronom sub-sequence sequence matched: Position={subSequence.Position}, Offset={currentOffset}, Sequence={subSequence.Sequence}, FragmentCount={subSequence.Fragments.Count}");
                    */

                    if (subSequence.Fragments.Count == 0)
                    {
                        return Result<int?>.Success(currentOffset + sequenceBytes.Length);
                    }

                    Result<bool> fragmentsResult = await MatchFragmentsAsync(
                        byteWindow,
                        currentOffset,
                        subSequence);

                    /*
                    _logger.Log(
                        LogLevel.Information,
                        $"Pronom sub-sequence fragments result: Position={subSequence.Position}, Offset={currentOffset}, IsSuccess={fragmentsResult.IsSuccess}, Matched={fragmentsResult.Value}");
                    */

                    if (fragmentsResult.IsSuccess && fragmentsResult.Value)
                    {
                        return Result<int?>.Success(currentOffset + sequenceBytes.Length);
                    }

                    currentOffset++;
                    continue;
                }

                int checkedByteOffset = currentOffset + sequenceBytes.Length;

                if (checkedByteOffset >= byteWindow.Length)
                {
                    currentOffset++;
                    continue;
                }

                currentOffset++;
            }

            return Result<int?>.Success(null);

                    }

        private async Task<Result<bool>> MatchFragmentsAsync(
            byte[] byteWindow,
            int subSequenceMatchOffset,
            PronomSubSequence subSequence)
        {
            if (byteWindow is null)
            {
                return Result<bool>.Failure(
                    "PRONOM_MATCH_BYTE_WINDOW_MISSING",
                    "PronomService.MatchFragmentsAsync: byte window is missing.");
            }

            if (subSequenceMatchOffset < 0)
            {
                return Result<bool>.Failure(
                    "PRONOM_MATCH_SUB_SEQUENCE_OFFSET_INVALID",
                    $"PronomService.MatchFragmentsAsync: sub-sequence match offset is invalid: {subSequenceMatchOffset}.");
            }

            if (subSequence is null)
            {
                return Result<bool>.Failure(
                    "PRONOM_MATCH_SUB_SEQUENCE_MISSING",
                    "PronomService.MatchFragmentsAsync: sub-sequence is missing.");
            }

            await Task.CompletedTask;

            byte[] sequenceBytes = ConvertHexToBytes(subSequence.Sequence);

            if (sequenceBytes.Length == 0)
            {
                return Result<bool>.Success(false);
            }

            List<int> leftBoundaries = [subSequenceMatchOffset];

            foreach (IGrouping<int, PronomFragment> fragmentGroup in subSequence.Fragments
                .Where(fragment => fragment.LeftRight == "Left")
                .GroupBy(fragment => fragment.Position)
                .OrderByDescending(group => group.Key))
            {
                List<int> nextLeftBoundaries = [];
                List<string> failedAttempts = [];

                foreach (int boundary in leftBoundaries)
                {
                    foreach (PronomFragment fragment in fragmentGroup)
                    {
                        int fragmentLength = GetFragmentLength(fragment.Value);

                        if (fragmentLength <= 0)
                        {
                            failedAttempts.Add(
                                $"Boundary={boundary}, Offset=invalid, Expected={fragment.Value}, Actual=invalid-length");

                            continue;
                        }

                        for (int offset = fragment.MinOffset; offset <= fragment.MaxOffset; offset++)
                        {
                            int fragmentStartOffset = boundary - offset - fragmentLength;

                            if (fragmentStartOffset < 0)
                            {
                                failedAttempts.Add($"Boundary={boundary}, Offset={offset}, Start={fragmentStartOffset}, Expected={fragment.Value}, Actual=out-of-range");

                                continue;
                            }

                            if (FragmentMatches(byteWindow, fragmentStartOffset, fragment.Value))
                            {
                                nextLeftBoundaries.Add(fragmentStartOffset);
                            }
                            else
                            {
                                failedAttempts.Add(
                                    $"Boundary={boundary}, Offset={offset}, Start={fragmentStartOffset}, Expected={fragment.Value}, Actual={ReadHex(byteWindow, fragmentStartOffset, fragmentLength)}");
                            }
                        }
                    }
                }

                if (nextLeftBoundaries.Count == 0)
                {
                    /*
                    _logger.Log(
                        LogLevel.Information,
                        $"Pronom fragment group failed: Side=Left, AnchorOffset={subSequenceMatchOffset}, GroupPosition={fragmentGroup.Key}, Attempts={string.Join(" | ", failedAttempts)}");
                    */

                    return Result<bool>.Success(false);
                }

                leftBoundaries = nextLeftBoundaries.Distinct().ToList();
            }

            int sequenceEndOffset = subSequenceMatchOffset + sequenceBytes.Length;

            List<int> rightBoundaries = [sequenceEndOffset];

            foreach (IGrouping<int, PronomFragment> fragmentGroup in subSequence.Fragments
                .Where(fragment => fragment.LeftRight == "Right")
                .GroupBy(fragment => fragment.Position)
                .OrderBy(group => group.Key))
            {
                List<int> nextRightBoundaries = [];
                List<string> failedAttempts = [];

                foreach (int boundary in rightBoundaries)
                {
                    foreach (PronomFragment fragment in fragmentGroup)
                    {
                        int fragmentLength = GetFragmentLength(fragment.Value);

                        if (fragmentLength <= 0)
                        {
                            failedAttempts.Add($"Boundary={boundary}, Offset=invalid, Expected={fragment.Value}, Actual=invalid-length");

                            continue;
                        }

                        for (int offset = fragment.MinOffset; offset <= fragment.MaxOffset; offset++)
                        {
                            int fragmentStartOffset = boundary + offset;

                            if (FragmentMatches(byteWindow, fragmentStartOffset, fragment.Value))
                            {
                                nextRightBoundaries.Add(fragmentStartOffset + fragmentLength);
                            }
                            else
                            {
                                failedAttempts.Add(
                                    $"Boundary={boundary}, Offset={offset}, Start={fragmentStartOffset}, Expected={fragment.Value}, Actual={ReadHex(byteWindow, fragmentStartOffset, fragmentLength)}");
                            }
                        }
                    }
                }

                if (nextRightBoundaries.Count == 0)
                {
                    /*
                    _logger.Log(
                        LogLevel.Information,
                        $"Pronom fragment group failed: Side=Right, AnchorOffset={subSequenceMatchOffset}, GroupPosition={fragmentGroup.Key}, Attempts={string.Join(" | ", failedAttempts)}");
                    */

                    return Result<bool>.Success(false);
                }

                rightBoundaries = nextRightBoundaries.Distinct().ToList();
            }

            return Result<bool>.Success(true);

            static bool FragmentMatches(byte[] byteWindow, int startOffset, string fragmentValue)
            {
                if (startOffset < 0 || string.IsNullOrWhiteSpace(fragmentValue))
                {
                    return false;
                }

                string value = fragmentValue.Trim();

                if (value.StartsWith("[", StringComparison.Ordinal) &&
                    value.EndsWith("]", StringComparison.Ordinal) &&
                    value.Contains(':'))
                {
                    string rangeContent = value[1..^1];
                    string[] rangeParts = rangeContent.Split(':');

                    if (rangeParts.Length != 2)
                    {
                        return false;
                    }

                    byte[] minBytes = ConvertHexToBytes(rangeParts[0]);
                    byte[] maxBytes = ConvertHexToBytes(rangeParts[1]);

                    if (minBytes.Length == 0 ||
                        maxBytes.Length == 0 ||
                        minBytes.Length != maxBytes.Length)
                    {
                        return false;
                    }

                    if (startOffset + minBytes.Length > byteWindow.Length)
                    {
                        return false;
                    }

                    byte[] actualBytes = new byte[minBytes.Length];

                    Array.Copy(
                        byteWindow,
                        startOffset,
                        actualBytes,
                        0,
                        minBytes.Length);

                    return CompareBytes(actualBytes, minBytes) >= 0 &&
                           CompareBytes(actualBytes, maxBytes) <= 0;
                }

                byte[] fragmentBytes = ConvertHexToBytes(value);

                if (fragmentBytes.Length == 0)
                {
                    return false;
                }

                return BytesMatch(byteWindow, startOffset, fragmentBytes);
            }

            static bool BytesMatch(byte[] byteWindow, int startOffset, byte[] expectedBytes)
            {
                if (byteWindow is null || expectedBytes is null)
                {
                    return false;
                }

                if (startOffset < 0)
                {
                    return false;
                }

                if (startOffset + expectedBytes.Length > byteWindow.Length)
                {
                    return false;
                }

                for (int i = 0; i < expectedBytes.Length; i++)
                {
                    if (byteWindow[startOffset + i] != expectedBytes[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            static int GetFragmentLength(string fragmentValue)
            {
                if (string.IsNullOrWhiteSpace(fragmentValue))
                {
                    return 0;
                }

                string value = fragmentValue.Trim();

                if (value.StartsWith("[", StringComparison.Ordinal) &&
                    value.EndsWith("]", StringComparison.Ordinal) &&
                    value.Contains(':'))
                {
                    string rangeContent = value[1..^1];
                    string[] rangeParts = rangeContent.Split(':');

                    if (rangeParts.Length != 2)
                    {
                        return 0;
                    }

                    byte[] minBytes = ConvertHexToBytes(rangeParts[0]);
                    byte[] maxBytes = ConvertHexToBytes(rangeParts[1]);

                    if (minBytes.Length == 0 ||
                        maxBytes.Length == 0 ||
                        minBytes.Length != maxBytes.Length)
                    {
                        return 0;
                    }

                    return minBytes.Length;
                }

                return ConvertHexToBytes(value).Length;
            }

            static string ReadHex(byte[] byteWindow, int startOffset, int length)
            {
                if (byteWindow is null || startOffset < 0 || length <= 0)
                {
                    return "out-of-range";
                }

                if (startOffset >= byteWindow.Length)
                {
                    return "out-of-range";
                }

                int readableLength = Math.Min(length, byteWindow.Length - startOffset);

                return Convert.ToHexString(
                    byteWindow
                        .Skip(startOffset)
                        .Take(readableLength)
                        .ToArray());
            }

            static int CompareBytes(byte[] leftBytes, byte[] rightBytes)
            {
                int length = Math.Min(leftBytes.Length, rightBytes.Length);

                for (int i = 0; i < length; i++)
                {
                    int comparison = leftBytes[i].CompareTo(rightBytes[i]);

                    if (comparison != 0)
                    {
                        return comparison;
                    }
                }

                return leftBytes.Length.CompareTo(rightBytes.Length);
            }

            static byte[] ConvertHexToBytes(string hexValue)
            {
                if (string.IsNullOrWhiteSpace(hexValue))
                {
                    return [];
                }

                string normalizedHex = new(
                    hexValue
                        .Where(Uri.IsHexDigit)
                        .ToArray());

                if (normalizedHex.Length == 0 || normalizedHex.Length % 2 != 0)
                {
                    return [];
                }

                byte[] bytes = new byte[normalizedHex.Length / 2];

                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(
                        normalizedHex.Substring(i * 2, 2),
                        16);
                }

                return bytes;
            }
        }

        #region containers

        private async Task<Result<string?>> MatchContainerSignaturesAsync(string filePath, string containerType, string dbFilePath)
        {
            Result<List<PronomContainerSignature>> containerSignaturesResult = await PronomRepository.GetContainerSignaturesByTypeAsync(containerType, dbFilePath);

            if (!containerSignaturesResult.IsSuccess)
            {
                return Result<string?>.Failure(
                    containerSignaturesResult.Error?.Code ?? "PRONOM_CONTAINER_SIGNATURES_GET_FAILED",
                    containerSignaturesResult.Error?.Message ?? "FormatIdentifier.MatchContainerSignaturesAsync: failed to get container signatures.");
            }

            foreach (PronomContainerSignature containerSignature in containerSignaturesResult.Value)
            {
                if (containerSignature.Files.Count == 0)
                {
                    continue;
                }

                bool containerSignatureMatches = true;

                foreach (PronomContainerFile containerFile in containerSignature.Files)
                {
                    Result<bool> fileExistsResult = SevenZip.Services.SevenZipService.EntryExists(filePath, containerFile.Path);

                    bool containerFileMatches = fileExistsResult.IsSuccess && fileExistsResult.Value;

                    if (containerFileMatches && containerFile.InternalSignatureIds.Count > 0)
                    {
                        containerFileMatches = false;

                        foreach (int internalSignatureId in containerFile.InternalSignatureIds)
                        {

                            Result<bool> internalSignatureResult = await MatchContainerInternalSignatureAsync(
                                filePath,
                                containerFile.Path,
                                internalSignatureId,
                                dbFilePath);

                            if (internalSignatureResult.IsSuccess && internalSignatureResult.Value)
                            {
                                containerFileMatches = true;
                            }
                        }
                    }

                    if (!containerFileMatches)
                    {
                        containerSignatureMatches = false;
                    }
                }

                if (containerSignatureMatches)
                {
                    return Result<string?>.Success(containerSignature.Puid);
                }
            }

            return Result<string?>.Success(null);
        }

        private async Task<Result<bool>> MatchContainerInternalSignatureAsync(string filePath, string containerFilePath, int internalSignatureId, string dbFilePath)
        {
            Result<bool> entryExistsResult = SevenZip.Services.SevenZipService.EntryExists(filePath, containerFilePath);

            string tempFilePath = Util.File.CreateTempFilePath();

            try
            {
                Result<bool> extractResult = SevenZip.Services.SevenZipService.ExtractEntryToFile(filePath, containerFilePath, tempFilePath);

                if (!extractResult.IsSuccess)
                {
                    if (extractResult.Error is not null)
                    {
                        return Result<bool>.Failure(extractResult.Error.Code, extractResult.Error.Message);
                    }

                    return Result<bool>.Failure("PRONOM_CONTAINER_ENTRY_EXTRACT_FAILED", "SevenZipService.ExtractEntryToFile failed without error details.");
                }

                if (!extractResult.Value)
                {
                    return Result<bool>.Success(false);
                }

                Result<PronomInternalSignature?> internalSignatureResult = await PronomRepository.GetContainerInternalSignatureAsync(internalSignatureId, dbFilePath);

                if (!internalSignatureResult.IsSuccess)
                {
                    if (internalSignatureResult.Error is not null)
                    {
                        return Result<bool>.Failure(internalSignatureResult.Error.Code, internalSignatureResult.Error.Message);
                    }

                    return Result<bool>.Failure("PRONOM_CONTAINER_INTERNAL_SIGNATURE_READ_FAILED", $"Failed to read PRONOM container internal signature from database. InternalSignatureId={internalSignatureId}.");
                }

                if (internalSignatureResult.Value is null)
                {
                    return Result<bool>.Success(false);
                }

                var matchResult =  await MatchInternalSignatureAsync(tempFilePath, internalSignatureResult.Value);
                return matchResult;
            }
            finally
            {
                try
                {
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                }
                catch
                {
                }
            }
        }

        #endregion

        #region size helpers

        private static int CalculateRequiredByteSequenceWindowSize(PronomByteSequence byteSequence)
        {
            int requiredSize = 0;

            foreach (PronomSubSequence subSequence in byteSequence.SubSequences)
            {
                int maxOffset = subSequence.SubSeqMaxOffset ?? subSequence.SubSeqMinOffset;
                int sequenceLength = CountHexBytes(subSequence.Sequence);

                int leftSpan = CalculateFragmentSpan(subSequence, "Left");

                int rightSpan = CalculateFragmentSpan(subSequence, "Right");

                int subSequenceRequiredSize =
                    maxOffset
                    + leftSpan
                    + sequenceLength
                    + rightSpan;

                if (subSequenceRequiredSize > requiredSize)
                {
                    requiredSize = subSequenceRequiredSize;
                }
            }

            return requiredSize;
        }

        private static int CalculateFragmentSpan(
            PronomSubSequence subSequence,
            string leftRight)
        {
            return subSequence.Fragments
                .Where(fragment => fragment.LeftRight == leftRight)
                .GroupBy(fragment => fragment.Position)
                .Sum(group =>
                    group.Max(fragment =>
                        fragment.MaxOffset + CountHexBytes(fragment.Value)));
        }

        private static int CountHexBytes(string value)
        {
            string normalizedValue = new(
                value
                    .Where(Uri.IsHexDigit)
                    .ToArray());

            return normalizedValue.Length / 2;
        }

        private static int CalculateMinimumFragmentSpan(
            PronomSubSequence subSequence,
            string leftRight)
        {
            int span = 0;

            foreach (IGrouping<int, PronomFragment> fragmentGroup in subSequence.Fragments
                .Where(fragment => fragment.LeftRight == leftRight)
                .GroupBy(fragment => fragment.Position)
                .OrderBy(group => group.Key))
            {
                int minimumPositionSpan = fragmentGroup.Min(fragment =>
                    fragment.MinOffset + CountHexBytes(fragment.Value));

                span += minimumPositionSpan;
            }

            return span;
        }

        private static int CalculateMaximumFragmentSpan(
            PronomSubSequence subSequence,
            string leftRight)
        {
            int span = 0;

            foreach (IGrouping<int, PronomFragment> fragmentGroup in subSequence.Fragments
                .Where(fragment => fragment.LeftRight == leftRight)
                .GroupBy(fragment => fragment.Position)
                .OrderBy(group => group.Key))
            {
                int maximumPositionSpan = fragmentGroup.Max(fragment =>
                    fragment.MaxOffset + CountHexBytes(fragment.Value));

                span += maximumPositionSpan;
            }

            return span;
        }

        #endregion

        private static bool IsSamePronomFormat(PronomFileFormatInfo left, PronomFileFormatInfo right)
        {
            if (!string.IsNullOrWhiteSpace(left.Puid) && !string.IsNullOrWhiteSpace(right.Puid))
            {
                return string.Equals(left.Puid, right.Puid, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.Version, right.Version, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.MimeType, right.MimeType, StringComparison.OrdinalIgnoreCase);
        }

        private static byte[] ConvertHexToBytes(string sequenceValue)
        {
            if (string.IsNullOrWhiteSpace(sequenceValue))
            {
                return [];
            }

            List<byte> bytes = [];
            int index = 0;

            while (index < sequenceValue.Length)
            {
                char currentChar = sequenceValue[index];

                if (char.IsWhiteSpace(currentChar))
                {
                    index++;
                    continue;
                }

                if (currentChar == '\'')
                {
                    int closingQuoteIndex = sequenceValue.IndexOf('\'', index + 1);

                    if (closingQuoteIndex < 0)
                    {
                        return [];
                    }

                    string textValue = sequenceValue.Substring(index + 1, closingQuoteIndex - index - 1);
                    bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(textValue));
                    index = closingQuoteIndex + 1;
                    continue;
                }

                if (Uri.IsHexDigit(currentChar))
                {
                    int nextHexDigitIndex = index + 1;

                    while (nextHexDigitIndex < sequenceValue.Length && char.IsWhiteSpace(sequenceValue[nextHexDigitIndex]))
                    {
                        nextHexDigitIndex++;
                    }

                    if (nextHexDigitIndex >= sequenceValue.Length || !Uri.IsHexDigit(sequenceValue[nextHexDigitIndex]))
                    {
                        return [];
                    }

                    bytes.Add(Convert.ToByte($"{currentChar}{sequenceValue[nextHexDigitIndex]}", 16));
                    index = nextHexDigitIndex + 1;
                    continue;
                }

                index++;
            }

            return bytes.ToArray();
        }

    }
}