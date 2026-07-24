namespace Pronom.Entities
{
    public class PronomContainerFile
    {
        public int ContainerSignatureId { get; set; }
        public string Path { get; set; } = string.Empty;
        public List<int> InternalSignatureIds { get; set; } = [];
    }
}