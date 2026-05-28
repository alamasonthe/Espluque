using Espluque.Contracts.Interfaces;
using Util;

namespace Espluque.Contracts.Ports;

public interface IFileFormatService
{
    Task<Result<List<IFileFormat?>>> GetInfosFromExtensionAsync(string extension, bool withoutInternalSignature = false);

    Task<Result<IFileFormat>> MatchInternalSignaturesAsync(IAnalysisNode node);
}