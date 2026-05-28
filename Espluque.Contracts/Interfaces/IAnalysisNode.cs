using Espluque.Contracts.Enums;

namespace Espluque.Contracts.Interfaces
{
    public interface IAnalysisNode
    {
        List<IAnalysisNode> Children { get; set; }
        List<IFact> Facts { get; set; }
        AnalysisStatusEnum LocalStatus { get; set; }
        string Name { get; set; }
        AnalysisStatusEnum Status { get; }
        List<(string Handler, string Value)> TargetInternalPath { get; set; }
        string TargetRootFilePath { get; set; }
    }
}