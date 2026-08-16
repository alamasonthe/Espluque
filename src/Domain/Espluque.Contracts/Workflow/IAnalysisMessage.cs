using Espluque.Contracts.Contributions;

namespace Espluque.Contracts.Workflow
{
    public interface IAnalysisMessage
    {
        AnalysisMessageTypeEnum MessageType { get; set; }
        bool IsCompleted { get; set; }
        IFileFormat? FileFormat { get; set; }
        IFileInformationPack? Information { get; set; }
        string? Label { get; set; }
        object? ViewerUC { get; set; }
    }
}