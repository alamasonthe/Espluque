using Espluque.Contracts.DetectionResult;
using Espluque.Contracts.Enums;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces;
using System.Reflection.Emit;

namespace Espluque.Contracts.Interfaces;

public interface IEntityFactory
{
    IFileFormat CreateFileFormat(string referentiel, string type, string? version, string? mimeType);
    IThesaurusConcept CreateThesaurusConcept(int? id, List<IThesaurusTerm>? terms, List<IThesaurusConcept>? parents, List<IThesaurusConcept>? children);
    IThesaurusTerm CreateThesaurusTerm(string? term, string? normalizedTerm, bool isPreferred, string? referenceName);
    IAnalysisMessage CreateAnalysisMessage(AnalysisMessageTypeEnum messageType, bool isCompleted, IFileFormat? fileFormat, IFileInformationPack? information, string? label, object? viewerUC);
    IFileInformationPack CreateFileInformationPack( string? label, List<KeyValuePair<string, string>>? information);
    IModuleDiagnostic CreateModuleDiagnostic( string filePath, string name);
    IMessage CreateMessage( MessageTypeEnum messageType, string messageLabel, List<KeyValuePair<string, string>> payload);
    IResultModelDefinition CreateResultModelDefinition(int? id, string name, string? thesaurusTag, List<string>? properties, List<ResultPropertyLink>? propertyLinks);
}
