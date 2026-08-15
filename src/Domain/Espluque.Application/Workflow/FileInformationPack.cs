using Espluque.Contracts.Workflow;

namespace Espluque.Application.Workflow
{
    /// <summary>
    /// Carries labeled key/value information produced during analysis.
    /// </summary>
    /// <remarks>
    /// Used as the Information payload of AnalysisMessage.
    /// Label is used by the interface as the header of the corresponding result tab.
    /// </remarks>

    public class FileInformationPack: IFileInformationPack
    {
        public string Label { get; set; }

        public List<KeyValuePair<string, string>>? Information { get; set; }
    }
}
