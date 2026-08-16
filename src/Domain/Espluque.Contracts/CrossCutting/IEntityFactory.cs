using Espluque.Contracts.Contributions;
using Espluque.Contracts.Modules;
using Espluque.Contracts.Thesaurus;
using Espluque.Contracts.Workflow;

namespace Espluque.Contracts.CrossCutting;

public interface IEntityFactory
{
    IFileFormat CreateFileFormat(string referentiel, string type, string? version, string? mimeType);
    IThesaurusConcept CreateThesaurusConcept(int? id, List<IThesaurusTerm>? terms, List<IThesaurusConcept>? parents, List<IThesaurusConcept>? children);
    IThesaurusTerm CreateThesaurusTerm(string? term, string? normalizedTerm, bool isPreferred, string? referenceName);
    IAnalysisMessage CreateAnalysisMessage(AnalysisMessageTypeEnum messageType, bool isCompleted, IFileFormat? fileFormat, IFileInformationPack? information, string? label, object? viewerUC);
    IFileInformationPack CreateFileInformationPack( string? label, List<KeyValuePair<string, string>>? information);
    IMessage CreateMessage( MessageTypeEnum messageType, string messageLabel, List<KeyValuePair<string, string>> payload);
    IAssertion CreateAssertion(string sourceModule, string sourceContribution, string assertionType, string claimJson, List<KeyValuePair<string, string>>? summary);
    IModuleHealth CreateModuleHealth(string moduleName, ModuleHealthCheckEnum healthCheck, string? diag);
    IContributionHealth CreateContributionHealth(string moduleName, string contribInterfaceType, string contribClassName, ModuleHealthCheckEnum healthCheck, string? diag);
    IAnalysisContext CreateAnalysisContext(string? startingTag, List<string>? tagHistory, string? filePath, IFileFormat? currentFileFormat, List<IFileFormat>? fileFormatHistory, string? tempFolderPath, List<IGrabberResult>? observedData, List<IAssertion>? assertions);
}
