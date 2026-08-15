using Espluque.Contracts.Interfaces;

namespace Espluque.Application.Contributions
{
    /// <summary>
    /// Represents a file format identified during analysis.
    /// </summary>
    /// <remarks>
    /// File formats are produced by IDetector contributions and describe an identified format through
    /// a reference system, label, optional version and MIME type.
    ///
    /// During analysis, several detectors may identify candidate formats.
    /// The analysis workflow uses the thesaurus to compare their specificity and determine the current format.
    /// Identified formats are retained in AnalysisContext.FileFormatHistory while the selected format is exposed
    /// through AnalysisContext.CurrentFileFormat.
    ///
    /// The current format is also resolved against the thesaurus to determine the tags used to select
    /// the contributions that can continue the analysis.
    /// </remarks>

    public class FileFormat : IFileFormat
    {
        public string? Referentiel { get; set; }

        public string? Label { get; set; }

        public string? Version { get; set; }

        public string? MIMEType { get; set; }
    }
}
