namespace Pronom.Entities
{
    public class PronomContainerSignature
    {
        public int Id { get; set; }
        public string ContainerType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Puid { get; set; } = string.Empty;
        public List<PronomContainerFile> Files { get; set; } = [];
    }
}