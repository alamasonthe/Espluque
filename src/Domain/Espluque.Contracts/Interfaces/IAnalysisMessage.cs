using Espluque.Contracts.Enums;

namespace Espluque.Contracts.Interfaces
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