using Util;

namespace Espluque.Contracts.Contributions;

public interface IFileFormatService
{
    string Referentiel { get; }

    Task<Result<List<IFileFormat?>>> GetInfosFromExtensionAsync(string extension, bool withoutInternalSignature = false);

    Task<Result<IFileFormat>> IdentifyFileFormatAsync(string filePath);
}