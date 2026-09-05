using Espluque.Contracts.CrossCutting;
using PE.Entities;
using Util;

namespace PE.Services
{
    /// <summary>
    /// Calculates and exposes the main structural offsets used to navigate a Portable Executable file.
    /// </summary>
    /// <remarks>
    /// Reads PE header locations, optional-header boundaries, section-table location,
    /// and resource-section offsets directly from the file.
    /// </remarks>
    public class PeOffsetsService
    {
        private readonly ILogger _logger;

        public PeOffsetsService(ILogger logger)
        {
            _logger = logger;
        }

        public Result<PeOffsets> GetPeOffsets(string filePath)
        {
            string formattedFileName = Path.GetFileName(filePath).PadRight(35);
            PeOffsets peOffsets = new();

            var ntHeaderResult = Bin.ReadBytesFromFile(filePath, 0x3C, 4).ToUInt32();
            if (!ntHeaderResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{formattedFileName}\t{ntHeaderResult.Error.Code} {ntHeaderResult.Error.Message}");
                return Result<PeOffsets>.Failure(ntHeaderResult.Error.Code, ntHeaderResult.Error.Message);
            }

            peOffsets.NtHeader = ntHeaderResult.Value;
            peOffsets.FileHeader = peOffsets.NtHeader + 4;

            var numberOfSectionsResult = Bin.ReadBytesFromFile(filePath, peOffsets.FileHeader + 0x02, 2).ToUInt16();
            if (!numberOfSectionsResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{formattedFileName}\t{numberOfSectionsResult.Error.Code} {numberOfSectionsResult.Error.Message}");
                return Result<PeOffsets>.Failure(numberOfSectionsResult.Error.Code, numberOfSectionsResult.Error.Message);
            }

            var sizeOfOptionalHeaderResult = Bin.ReadBytesFromFile(filePath, peOffsets.FileHeader + 0x10, 2).ToUInt16();
            if (!sizeOfOptionalHeaderResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{formattedFileName}\t{sizeOfOptionalHeaderResult.Error.Code} {sizeOfOptionalHeaderResult.Error.Message}");
                return Result<PeOffsets>.Failure(sizeOfOptionalHeaderResult.Error.Code, sizeOfOptionalHeaderResult.Error.Message);
            }

            peOffsets.OptionalHeader = peOffsets.FileHeader + 0x14;

            var optionalMagicResult = Bin.ReadBytesFromFile(filePath, peOffsets.OptionalHeader, 2).ToUInt16();
            if (!optionalMagicResult.IsSuccess)
            {
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{formattedFileName}\t{optionalMagicResult.Error.Code} {optionalMagicResult.Error.Message}");
                return Result<PeOffsets>.Failure(optionalMagicResult.Error.Code, optionalMagicResult.Error.Message);
            }

            if (optionalMagicResult.Value == 0x10B)
                peOffsets.DataDirectory = peOffsets.OptionalHeader + 0x60;
            else if (optionalMagicResult.Value == 0x20B)
                peOffsets.DataDirectory = peOffsets.OptionalHeader + 0x70;
            else
            {
                const string errorCode = "PE_OPTIONAL_HEADER_UNSUPPORTED";
                string errorMessage = $"Unsupported PE optional header: 0x{optionalMagicResult.Value:X}";
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{formattedFileName}\t{errorCode} {errorMessage}");
                return Result<PeOffsets>.Failure(errorCode, errorMessage);
            }

            peOffsets.SectionHeaders = peOffsets.OptionalHeader + sizeOfOptionalHeaderResult.Value;

            for (int sectionIndex = 0; sectionIndex < numberOfSectionsResult.Value; sectionIndex++)
            {
                long sectionOffset = peOffsets.SectionHeaders + 0x28L * sectionIndex;

                var sectionNameResult = Bin.ReadBytesFromFile(filePath, sectionOffset, 8).ToAsciiString();
                if (!sectionNameResult.IsSuccess)
                {
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{formattedFileName}\t{sectionNameResult.Error.Code} {sectionNameResult.Error.Message}");
                    return Result<PeOffsets>.Failure(sectionNameResult.Error.Code, sectionNameResult.Error.Message);
                }

                if (sectionNameResult.Value != ".rsrc")
                    continue;

                var resourceSectionRvaResult = Bin.ReadBytesFromFile(filePath, sectionOffset + 0x0C, 4).ToUInt32();
                if (!resourceSectionRvaResult.IsSuccess)
                {
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{formattedFileName}\t{resourceSectionRvaResult.Error.Code} {resourceSectionRvaResult.Error.Message}");
                    return Result<PeOffsets>.Failure(resourceSectionRvaResult.Error.Code, resourceSectionRvaResult.Error.Message);
                }

                var resourceSectionResult = Bin.ReadBytesFromFile(filePath, sectionOffset + 0x14, 4).ToUInt32();
                if (!resourceSectionResult.IsSuccess)
                {
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{formattedFileName}\t{resourceSectionResult.Error.Code} {resourceSectionResult.Error.Message}");
                    return Result<PeOffsets>.Failure(resourceSectionResult.Error.Code, resourceSectionResult.Error.Message);
                }

                peOffsets.ResourceSectionRva = resourceSectionRvaResult.Value;
                peOffsets.ResourceSection = resourceSectionResult.Value;
                break;
            }

            return Result<PeOffsets>.Success(peOffsets);
        }
    }
}