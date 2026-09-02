namespace CatalogFile
{
    public class CatalogFileInfo
    {
        public string? Signer { get; set; }

        public string? Issuer { get; set; }

        public DateTime? CertificateValidFrom { get; set; }

        public DateTime? CertificateValidTo { get; set; }

        public string? OSAttr { get; set; }

        public List<CatalogMemberInfo> Members { get; set; } = new();
    }
}
