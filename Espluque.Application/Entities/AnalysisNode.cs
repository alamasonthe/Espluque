using Espluque.Contracts.Enums;
using Espluque.Contracts.Interfaces;

namespace Espluque.Application.Entities
{
    /// <summary>
    /// Represents one content node in the analysis tree.
    /// </summary>
    /// <remarks>
    /// An analysis node describes the content to analyze, stores the facts produced
    /// for this content, stores child contents discovered during analysis, and exposes
    /// both the local status and the consolidated status.
    /// </remarks>
    public class AnalysisNode : IAnalysisNode
    {
        public string Name { get; set; } = string.Empty;

        public string TargetRootFilePath { get; set; } = string.Empty;

        public List<(string Handler, string Value)> TargetInternalPath { get; set; } = [];

        public List<IFact> Facts { get; set; } = [];

        public List<IAnalysisNode> Children { get; set; } = [];

        public AnalysisStatusEnum LocalStatus { get; set; } = AnalysisStatusEnum.Pending;

        public AnalysisStatusEnum Status { get; }
    }
}