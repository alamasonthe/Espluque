using Espluque.Contracts.Interfaces;
using Util;

namespace Espluque.Contracts.Ports;

public interface IFileFormatService
{
    string Referentiel { get; }

    Task<Result<List<IFileFormat?>>> GetInfosFromExtensionAsync(string extension, bool withoutInternalSignature = false);

    Task<Result<IFileFormat>> IdentifyFileFormatAsync(string filePath);
}