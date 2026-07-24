using Espluque.Contracts.Interfaces;
using Espluque.Contracts.Enums;

namespace Espluque.Application.Entities
{
    public class AnalysisMessage: IAnalysisMessage
    {
        public AnalysisMessageTypeEnum MessageType { get; set; }
        public bool IsCompleted { get; set; }
        public IFileFormat? FileFormat { get; set; }
        public IFileInformationPack? Information { get; set; }
        public string? Label { get; set; }
        public Object? ViewerUC { get; set; }
    }
}
