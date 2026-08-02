namespace Espluquer.Entities
{
    public sealed class ContributionGraphNode
    {
        public int ConceptId { get; init; }
        public string Label { get; init; } = string.Empty;

        public int Column { get; set; }
        public int Row { get; set; }

        public double X { get; set; }
        public double Y { get; set; }

        public bool HasViewer { get; set; }
        public bool HasGrabber { get; set; }
        public bool HasDetector { get; set; }
        public bool HasFusioner { get; set; }
    }
}
