using Espluque.Contracts.Interfaces;

namespace Espluque.Application.Contributions
{
    /// <summary>
    /// Represents a consolidated assertion about the analyzed file.
    /// </summary>
    /// <remarks>
    /// Assertions are produced by IFusioner contributions by combining information available in the analysis context.
    /// Each assertion records its source module and contribution, its semantic type, and a serialized claim in ClaimJson.
    ///
    /// Assertions are added to AnalysisContext.Assertions by the fusion workflow.
    /// Summary provides a key/value representation of the assertion intended for concise presentation of its main information.
    /// </remarks>
    
    public class Assertion : IAssertion
    {
        public string SourceModule { get; set; }
        public string SourceContribution { get; set; }
        public string AssertionType { get; set; }
        public string ClaimJson { get; set; }

        public List<KeyValuePair<string, string>>? Summary { get; set; } = [];
    }
}
