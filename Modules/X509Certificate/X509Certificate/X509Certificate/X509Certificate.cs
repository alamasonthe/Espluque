namespace X509Certificate
{
    public class X509CertificateData
    {
        public string Filename { get; set; } = string.Empty;
        public string ExpirationDate { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public string SimpleName { get; set; } = string.Empty;
        public string IsVerified { get; set; } = string.Empty;
        public string SignatureAlgorithm { get; set; } = string.Empty;
        public string RelativeDistinguishedName { get; set; } = string.Empty;
        public string LengthOfRawData { get; set; } = string.Empty;
        public string RsaPublicKey { get; set; } = string.Empty;
        public string RsaPrivateKey { get; set; } = string.Empty;
    }
}