using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography;

namespace X509Certificate
{
    public class X509Service
    {
        private readonly Espluque.Contracts.CrossCutting.ILogger _logger;

        public X509Service(Espluque.Contracts.CrossCutting.ILogger logger)
        {
            _logger = logger;
        }

        public Task<List<KeyValuePair<string, string>>> GetInfos(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
                return Task.FromResult(new List<KeyValuePair<string, string>>());

            try
            {
                X509ContentType contentType =
                    X509Certificate2.GetCertContentType(filename);

                return contentType switch
                {
                    X509ContentType.Cert => GetCertificateInfos(filename),
                    X509ContentType.Pkcs7 => GetPkcs7Infos(filename),
                    X509ContentType.Pfx => GetPfxInfos(filename),

                    _ => Task.FromResult(
                        new List<KeyValuePair<string, string>>())
                };
            }
            catch (Exception ex)
            {
                string formattedFileName = Path.GetFileName(filename).PadRight(35);
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{formattedFileName}\tX509 certificate reading error: {ex.GetType().Name}: {ex.Message}");
                return Task.FromResult(new List<KeyValuePair<string, string>>());
            }
        }

        private Task<List<KeyValuePair<string, string>>> GetCertificateInfos(string filename)
        {
            using X509Certificate2 certificate =
                X509CertificateLoader.LoadCertificateFromFile(filename);

            var data = CreateCertificateData(filename, certificate);

            return Task.FromResult(ToKeyValuePairs(data));
        }

        private Task<List<KeyValuePair<string, string>>> GetPkcs7Infos(string filename)
        {
            var infos = new List<KeyValuePair<string, string>>();

            byte[] cmsData = GetPkcs7Data(filename);

            var cms = new SignedCms();
            cms.Decode(cmsData);

            X509Certificate2Collection certificates = cms.Certificates;

            if (certificates.Count == 1)
            {
                var data = CreateCertificateData(filename, certificates[0]);
                infos.AddRange(ToKeyValuePairs(data));
            }
            else
            {
                infos.Add(new("CertificateCount", certificates.Count.ToString()));

                for (int i = 0; i < certificates.Count; i++)
                {
                    var data = CreateCertificateData(filename, certificates[i]);
                    infos.AddRange(ToKeyValuePairs(data, i));
                }
            }

            return Task.FromResult(infos);
        }

        private Task<List<KeyValuePair<string, string>>> GetPfxInfos(string filename)
        {
            var infos = new List<KeyValuePair<string, string>>();

            X509Certificate2Collection certificates =
                X509CertificateLoader.LoadPkcs12CollectionFromFile(
                    filename,
                    null,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);

            if (certificates.Count == 1)
            {
                var data = CreateCertificateData(filename, certificates[0]);
                infos.AddRange(ToKeyValuePairs(data));
            }
            else
            {
                infos.Add(new("CertificateCount", certificates.Count.ToString()));

                for (int i = 0; i < certificates.Count; i++)
                {
                    var data = CreateCertificateData(filename, certificates[i]);
                    infos.AddRange(ToKeyValuePairs(data, i));
                }
            }

            return Task.FromResult(infos);
        }

        private static X509CertificateData CreateCertificateData(
            string filename,
            X509Certificate2 certificate)
        {
            return new X509CertificateData
            {
                Filename = Path.GetFileName(filename),
                ExpirationDate = certificate.GetExpirationDateString(),
                FriendlyName = certificate.FriendlyName ?? string.Empty,
                SimpleName = certificate.GetNameInfo(X509NameType.SimpleName, false),
                IsVerified = certificate.Verify().ToString(),
                SignatureAlgorithm = certificate.SignatureAlgorithm?.FriendlyName ?? string.Empty,
                RelativeDistinguishedName =
                    string.Join('\n', certificate.Subject.Split(',')),
                LengthOfRawData = certificate.RawData.Length.ToString(),
                RsaPublicKey = GetRsaPublicKey(certificate),
                RsaPrivateKey = GetRsaPrivateKey(certificate)
            };
        }

        private static string GetRsaPublicKey(X509Certificate2 certificate)
        {
            try
            {
                using var rsa = certificate.GetRSAPublicKey();
                return rsa?.ExportRSAPublicKeyPem() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetRsaPrivateKey(X509Certificate2 certificate)
        {
            try
            {
                using var rsa = certificate.GetRSAPrivateKey();
                return rsa?.ExportRSAPrivateKeyPem() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static List<KeyValuePair<string, string>> ToKeyValuePairs( X509CertificateData data, int? index = null)
        {
            string prefix = index.HasValue
                ? $"Certificate[{index.Value}]."
                : string.Empty;

            return
            [
                new(prefix + "Filename", data.Filename),
                new(prefix + "ExpirationDate", data.ExpirationDate),
                new(prefix + "FriendlyName", data.FriendlyName),
                new(prefix + "SimpleName", data.SimpleName),
                new(prefix + "IsVerified", data.IsVerified),
                new(prefix + "SignatureAlgorithm", data.SignatureAlgorithm),
                new(prefix + "RelativeDistinguishedName", data.RelativeDistinguishedName),
                new(prefix + "LengthOfRawData", data.LengthOfRawData),
                new(prefix + "RsaPublicKey", data.RsaPublicKey),
                new(prefix + "RsaPrivateKey", data.RsaPrivateKey)
            ];
        }

        #region Helpers

        private static byte[] GetPkcs7Data(string filename)
        {
            byte[] data = File.ReadAllBytes(filename);

            try
            {
                string text = File.ReadAllText(filename);

                if (PemEncoding.TryFind(text, out PemFields fields))
                {
                    string base64 = text[fields.Base64Data];
                    return Convert.FromBase64String(base64);
                }
            }
            catch
            {
                // not PEM. Keep binary data
            }

            return data;
        }

        #endregion
    }
}