using Espluque.Application.MessageBus.Entities;
using Espluque.Application.ModuleManager.Entities;
using Espluque.Application.Thesaurus.Entities;
using Espluque.Contracts.Enums;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.MessageInterfaces;
using Espluque.Contracts.ModuleInterfaces;

namespace Espluque.Application.Entities
{
    public class Factory : IEntityFactory
    {
        public IFileFormat CreateFileFormat(
            string referentiel,
            string? type,
            string? version,
            string? mimeType)
        {
            return new FileFormat
            {
                Referentiel = referentiel,
                Label = type,
                Version = version,
                MIMEType = mimeType
            };
        }

        public IThesaurusTerm CreateThesaurusTerm(
            string? term,
            string? normalizedTerm,
            bool isPreferred,
            string? referenceName)
        {
            return new ThesaurusTerm
            {
                Term = term,
                NormalizedTerm = normalizedTerm,
                IsPreferred = isPreferred,
                ReferenceName = referenceName
            };
        }

        public IThesaurusConcept CreateThesaurusConcept(
            int? id,
            List<IThesaurusTerm>? terms,
            List<IThesaurusConcept>? parents,
            List<IThesaurusConcept>? children)
        {
            return new ThesaurusConcept
            {
                Id = id,
                Terms = terms ?? [],
                Parents = parents ?? [],
                Children = children ?? []
            };
        }

        public IAnalysisMessage CreateAnalysisMessage(
            AnalysisMessageTypeEnum messageType,
            bool isCompleted,
            IFileFormat? fileFormat,
            IFileInformationPack? information,
            string? label,
            object? viewerUC)
        {
            return new AnalysisMessage
            {
                MessageType = messageType,
                IsCompleted = isCompleted,
                FileFormat = fileFormat,
                Information = information,
                Label = label,
                ViewerUC = viewerUC
            };
        }

        public IFileInformationPack CreateFileInformationPack(
            string? label,
            List<KeyValuePair<string, string>>? information)
        {
            return new FileInformationPack
            {
                Label = label ?? string.Empty,
                Information = information
            };
        }

        public IModuleDiagnostic CreateModuleDiagnostic(
            string filePath, string name)
        {
            return new ModuleDiagnostic
            {
                FilePath = filePath,
                Name = name
            };
        }

        public IMessage CreateMessage(
            MessageTypeEnum messageType,
            string messageLabel,
            List<KeyValuePair<string, string>> payload)
        {
            return new Message
            {
                MessageType = messageType,
                MessageLabel = messageLabel,
                Payload = payload
            };
        }
    }
}
