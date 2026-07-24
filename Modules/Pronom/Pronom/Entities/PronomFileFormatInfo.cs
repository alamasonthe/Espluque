namespace Pronom.Entities
{
    public class PronomFileFormatInfo
    {
        public string Puid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Version { get; set; }
        public string? MimeType { get; set; }
    }
}
