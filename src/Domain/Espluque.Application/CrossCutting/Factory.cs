using Espluque.Application.Contributions;
using Espluque.Application.CrossCutting.MessageBus;
using Espluque.Application.Modules;
using Espluque.Application.Thesaurus;
using Espluque.Application.Workflow;
using Espluque.Contracts.Contributions;
using Espluque.Contracts.CrossCutting;
using Espluque.Contracts.Modules;
using Espluque.Contracts.Thesaurus;
using Espluque.Contracts.Workflow;

namespace Espluque.Application.CrossCutting
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

        public IAssertion CreateAssertion(
            string sourceModule,
            string sourceContribution,
            string assertionType,
            string claimJson,
            List<KeyValuePair<string, string>>? summary)
        {
            return new Assertion
            {
                SourceModule = sourceModule,
                SourceContribution = sourceContribution,
                AssertionType = assertionType,
                ClaimJson = claimJson,
                Summary = summary ?? []
            };
        }

        public IModuleHealth CreateModuleHealth(
            string moduleName,
            ModuleHealthCheckEnum healthCheck,
            string? diag)
        {
            return new ModuleHealth
            {
                ModuleName = moduleName,
                HealthCheck = healthCheck,
                Diag = diag
            };
        }

        public IContributionHealth CreateContributionHealth(
            string moduleName,
            string contribInterfaceType,
            string contribClassName,
            ModuleHealthCheckEnum healthCheck,
            string? diag)
        {
            return new ContributionHealth
            {
                ModuleName = moduleName,
                ContribInterfaceType = contribInterfaceType,
                ContribClassName = contribClassName,
                HealthCheck = healthCheck,
                Diag = diag
            };
        }

        public IAnalysisContext CreateAnalysisContext(
            string? startingTag,
            List<string>? tagHistory,
            string? filePath,
            IFileFormat? currentFileFormat,
            List<IFileFormat>? fileFormatHistory,
            string? tempFolderPath,
            List<IGrabberResult>? observedData,
            List<IAssertion>? assertions)
        {
            return new AnalysisContext
            {
                StartingTag = startingTag,
                TagHistory = tagHistory ?? [],
                FilePath = filePath,
                CurrentFileFormat = currentFileFormat,
                FileFormatHistory = fileFormatHistory ?? [],
                TempFolderPath = tempFolderPath,
                ObservedData = observedData ?? [],
                Assertions = assertions ?? []
            };
        }
    }
}
