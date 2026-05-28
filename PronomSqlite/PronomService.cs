using Espluque.Contracts.Ports;
using Microsoft.Extensions.Logging;
using Util;
using Espluque.Contracts.Interfaces;
using Espluque.Application.Services;
using Espluque.Application.Entities;
using PronomSqlite.Entities;

namespace PronomSqlite
{
    public class PronomService : IFileFormatService, IPronomImportService
    {
        private readonly Espluque.Contracts.Ports.ILogger _logger;
        private readonly IFileSignatureSource _fileSignatureSource;
        private readonly IImportFileSignatureRepository _importFileSignatureRepository;
        private readonly PronomRepository _pronomRepository;
        private readonly IEntityFactory _entityFactory;
        private readonly NodeBinaryReader _nodeBinaryReader = new();

        public PronomService(Espluque.Contracts.Ports.ILogger logger, IEntityFactory entityFactory, IFileSignatureSource fileSignatureSource, IImportFileSignatureRepository importFileSignatureRepository, PronomRepository pronomRepository)
        {
            _logger = logger;
            _fileSignatureSource = fileSignatureSource;
            _importFileSignatureRepository = importFileSignatureRepository;
            _pronomRepository = pronomRepository;
            _entityFactory = entityFactory;
        }

        public async Task<bool> ImportFileExtensionFromXmlAsync(string filePath)
        {
            Result<bool> canOpenReadResult = Util.File.CanOpenRead(filePath);

            if (!canOpenReadResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"File check failed: {canOpenReadResult.Error?.Code} - {canOpenReadResult.Error?.Message}");
                _logger.Log(LogLevel.Information, $"Analysis complete: {filePath}");
                return false;
            }
            else
            {
                _logger.Log(LogLevel.Information, $"File check succeeded: {filePath}");
            }

            Result<bool> importExtensionResult = await _fileSignatureSource.ImportFileExtensionFromXmlAsync(filePath);

            if (!importExtensionResult.IsSuccess)
            {
                _logger.Log(LogLevel.Error, $"Pronom Extension import failed: {importExtensionResult.Error?.Code} - {importExtensionResult.Error?.Message}");
                return false;
            }
            else
            {
                _logger.Log(LogLevel.Information, $"Pronom Extension import succeeded: {filePath}");
            }

            return true;
        }

        public async Task<Result<List<IFileFormat?>>> GetInfosFromExtensionAsync(
            string extension,
            bool withoutInternalSignature = false)
        {
            Result<List<KeyValuePair<string, string>>?> infosResult = withoutInternalSignature
                ? await _pronomRepository.GetInfosFromExtensionWithoutInternalSignatureAsync(extension)
                : await _pronomRepository.GetInfosFromExtensionAsync(extension);

            string lookupMode = withoutInternalSignature
                ? "Without signature"
                : "All";

            if (!infosResult.IsSuccess)
            {
                _logger.Log(
                    LogLevel.Error,
                    $"Pronom Extension info lookup failed ({lookupMode}): {infosResult.Error?.Code} - {infosResult.Error?.Message}");

                return Result<List<IFileFormat?>>.Failure(
                    infosResult.Error?.Code ?? "PRONOM_EXTENSION_INFO_LOOKUP_FAILED",
                    infosResult.Error?.Message ?? "PronomService.GetInfosFromExtensionAsync: failed to get extension infos.");
            }

            if (infosResult.Value is null)
            {
                _logger.Log(
                    LogLevel.Information,
                    $"Pronom Extension info not found ({lookupMode}): {extension}");

                return Result<List<IFileFormat?>>.Success([]);
            }

            List<string> candidateMessages = [];
            List<IFileFormat?> fileFormats = [];

            int candidateIndex = 1;

            while (true)
            {
                string nameKey = $"PronomFileFormat.Name[{candidateIndex}]";
                string versionKey = $"PronomFileFormat.Version[{candidateIndex}]";
                string mimeTypeKey = $"PronomFileFormat.MIMEType[{candidateIndex}]";

                string? name = infosResult.Value
                    .FirstOrDefault(info => info.Key == nameKey)
                    .Value;

                if (string.IsNullOrWhiteSpace(name))
                {
                    break;
                }

                string? version = infosResult.Value
                    .FirstOrDefault(info => info.Key == versionKey)
                    .Value;

                string? mimeType = infosResult.Value
                    .FirstOrDefault(info => info.Key == mimeTypeKey)
                    .Value;

                string displayName = string.IsNullOrWhiteSpace(version)
                    ? name
                    : $"{name} {version}";

                candidateMessages.Add($"Pronom FileFormat candidate ({lookupMode}) {extension}: {displayName}");

                fileFormats.Add(_entityFactory.CreateFileFormat(
                    string.Empty,
                    name,
                    version,
                    mimeType));

                candidateIndex++;
            }

            _logger.Log(
                LogLevel.Information,
                $"Pronom FileFormat candidates found ({lookupMode}): {extension} ({candidateMessages.Count})");

            foreach (string candidateMessage in candidateMessages)
            {
                _logger.Log(
                    LogLevel.Information,
                    candidateMessage);
            }

            return Result<List<IFileFormat?>>.Success(fileFormats);
        }

        #region Match

        public async Task<Result<IFileFormat?>> MatchInternalSignaturesAsync(IAnalysisNode node)
        {
            if (node is null)
            {
                return Result<IFileFormat?>.Failure(
                    "PRONOM_MATCH_NODE_MISSING",
                    "PronomService.MatchInternalSignaturesAsync: analysis node is missing.");
            }

            Result<List<int>> internalSignatureIdsResult =
                await _pronomRepository.ListInternalSignatureIdsAsync();

            if (!internalSignatureIdsResult.IsSuccess)
            {
                return Result<IFileFormat?>.Failure(
                    internalSignatureIdsResult.Error?.Code ?? "PRONOM_INTERNAL_SIGNATURE_IDS_LIST_FAILED",
                    internalSignatureIdsResult.Error?.Message ?? "PronomService.MatchInternalSignaturesAsync: failed to list internal signature IDs.");
            }

            List<int> matchedInternalSignatureIds = [];

            foreach (int internalSignatureId in internalSignatureIdsResult.Value)
            {
                Result<PronomInternalSignature?> internalSignatureResult =
                    await _pronomRepository.GetInternalSignatureAsync(internalSignatureId);

                if (!internalSignatureResult.IsSuccess)
                {
                    continue;
                }

                if (internalSignatureResult.Value is null)
                {
                    continue;
                }

                Result<bool> matchResult = await MatchInternalSignatureAsync(
                    node,
                    internalSignatureResult.Value);

                if (!matchResult.IsSuccess || !matchResult.Value)
                {
                    continue;
                }

                matchedInternalSignatureIds.Add(internalSignatureId);

                string byteSequenceReferences = string.Join(
                    ", ",
                    internalSignatureResult.Value.ByteSequences.Select(byteSequence =>
                        string.IsNullOrWhiteSpace(byteSequence.Reference)
                            ? "Variable"
                            : byteSequence.Reference));

                string matchedFormatDisplayName = "Unknown PRONOM format";

                Result<List<KeyValuePair<string, string>>?> matchedFormatInfosResult =
                    await _pronomRepository.GetInfosFromInternalSignatureAsync(internalSignatureId);

                if (matchedFormatInfosResult.IsSuccess && matchedFormatInfosResult.Value is not null)
                {
                    string? matchedName = matchedFormatInfosResult.Value
                        .FirstOrDefault(info => info.Key == "PronomFileFormat.Name[1]")
                        .Value;

                    string? matchedVersion = matchedFormatInfosResult.Value
                        .FirstOrDefault(info => info.Key == "PronomFileFormat.Version[1]")
                        .Value;

                    if (!string.IsNullOrWhiteSpace(matchedName))
                    {
                        matchedFormatDisplayName = string.IsNullOrWhiteSpace(matchedVersion)
                            ? matchedName
                            : $"{matchedName} {matchedVersion}";
                    }
                }

                _logger.Log(
                    LogLevel.Information,
                    $"Pronom internal signature matched: {matchedFormatDisplayName} (InternalSignatureId={internalSignatureId})");
            }

            if (matchedInternalSignatureIds.Count == 0)
            {
                return Result<IFileFormat?>.Success(null);
            }

            Result<List<KeyValuePair<string, string>>?> formatInfosResult =
                await _pronomRepository.GetHighestPriorityFileFormatInfosFromInternalSignatureIdsAsync(
                    matchedInternalSignatureIds);

            if (!formatInfosResult.IsSuccess || formatInfosResult.Value is null)
            {
                return Result<IFileFormat?>.Success(null);
            }

            string? name = formatInfosResult.Value
                .FirstOrDefault(info => info.Key == "PronomFileFormat.Name[1]")
                .Value;

            if (string.IsNullOrWhiteSpace(name))
            {
                return Result<IFileFormat?>.Success(null);
            }

            string? version = formatInfosResult.Value
                .FirstOrDefault(info => info.Key == "PronomFileFormat.Version[1]")
                .Value;

            string? mimeType = formatInfosResult.Value
                .FirstOrDefault(info => info.Key == "PronomFileFormat.MIMEType[1]")
                .Value;

            IFileFormat fileFormat = _entityFactory.CreateFileFormat(
                node.TargetRootFilePath,
                name,
                version,
                mimeType);

            string displayName = string.IsNullOrWhiteSpace(version)
                ? name
                : $"{name} {version}";

            _logger.Log(
                LogLevel.Information,
                $"Pronom selected format: {displayName} ({matchedInternalSignatureIds.Count} matched signatures)");

            return Result<IFileFormat?>.Success(fileFormat);
        }

        private async Task<Result<bool>> MatchInternalSignatureAsync(
            IAnalysisNode node,
            PronomInternalSignature internalSignature)
        {
            if (node is null)
            {
                return Result<bool>.Failure(
                    "PRONOM_MATCH_NODE_MISSING",
                    "PronomService.MatchInternalSignatureAsync: analysis node is missing.");
            }

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
                    node,
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
            IAnalysisNode node,
            PronomByteSequence byteSequence)
        {
            if (node is null)
            {
                return Result<bool>.Failure(
                    "PRONOM_MATCH_NODE_MISSING",
                    "PronomService.MatchByteSequenceAsync: analysis node is missing.");
            }

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

                Result<long> lengthResult = _nodeBinaryReader.GetLength(node);

                if (!lengthResult.IsSuccess)
                {
                    return Result<bool>.Success(false);
                }

                readOffset = Math.Max(0, lengthResult.Value - readSize);
            }
            else if (string.Equals(reference, "Variable", StringComparison.OrdinalIgnoreCase))
            {
                Result<long> lengthResult = _nodeBinaryReader.GetLength(node);

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

            Result<byte[]> byteWindowResult = _nodeBinaryReader.ReadBytes(
                node,
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

                byte checkedByte = byteWindow[checkedByteOffset];

                PronomShift? shift = subSequence.Shifts
                    .FirstOrDefault(shift => string.Equals(
                        shift.Byte,
                        checkedByte.ToString("X2"),
                        StringComparison.OrdinalIgnoreCase));

                int shiftValue = shift?.Value ?? subSequence.DefaultShift;

                if (shiftValue <= 0)
                {
                    shiftValue = 1;
                }

                currentOffset += shiftValue;
            }

            return Result<int?>.Success(null);

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
                                failedAttempts.Add( $"Boundary={boundary}, Offset={offset}, Start={fragmentStartOffset}, Expected={fragment.Value}, Actual=out-of-range");

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
                            failedAttempts.Add( $"Boundary={boundary}, Offset=invalid, Expected={fragment.Value}, Actual=invalid-length");

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

        #endregion

        #region size helpers

        private static int CalculateRequiredByteSequenceWindowSize(PronomByteSequence byteSequence)
        {
            int requiredSize = 0;

            foreach (PronomSubSequence subSequence in byteSequence.SubSequences)
            {
                int maxOffset = subSequence.SubSeqMaxOffset ?? subSequence.SubSeqMinOffset;
                int sequenceLength = CountHexBytes(subSequence.Sequence);

                int leftSpan = Math.Max(
                    subSequence.MinFragLength,
                    CalculateFragmentSpan(subSequence, "Left"));

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
    }
}
