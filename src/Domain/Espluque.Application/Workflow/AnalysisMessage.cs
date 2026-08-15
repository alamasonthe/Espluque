using Espluque.Contracts.Contributions;
using Espluque.Contracts.Workflow;

namespace Espluque.Application.Workflow
{
    /// <summary>
    /// Carries information emitted by the analysis workflow.
    /// </summary>
    /// <remarks>
    /// Emitted by AnalysisEngine and FusionEngine, then relayed by Orchestrator through AnalyserMessageEvent.
    /// MessageType identifies the message content and purpose.
    /// Information carries structured results; Label and ViewerUC are used for viewer contributions.
    /// </remarks>

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
