using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Util;

namespace X509Certificate
{
    public class X509Service
    {
        private readonly Espluque.Contracts.CrossCutting.ILogger _logger;

        public X509Service(Espluque.Contracts.CrossCutting.ILogger logger)
        {
            _logger = logger;
        }

        public async Task<List<KeyValuePair<string, string>>> GetInfos(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return [];

            Result<FileStream> fileStreamResult = Util.File.OpenRead(filename);
            if (!fileStreamResult.IsSuccess)
            {
                string formattedFileName = Path.GetFileName(filename).PadRight(35);
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{formattedFileName}\tX509 certificate reading error: {fileStreamResult.Error!.Code} - {fileStreamResult.Error.Message}");
                return [];
            }

            try
            {
                byte[] fileData;

                using (FileStream fileStream = fileStreamResult.Value!)
                using (MemoryStream memoryStream = new())
                {
                    await fileStream.CopyToAsync(memoryStream);
                    fileData = memoryStream.ToArray();
                }

                X509ContentType contentType = X509Certificate2.GetCertContentType(fileData);

                return contentType switch
                {
                    X509ContentType.Cert => GetCertificateInfos(filename, fileData),
                    X509ContentType.Pkcs7 => GetPkcs7Infos(filename, fileData),
                    X509ContentType.Pfx => GetPfxInfos(filename, fileData),
                    _ => []
                };
            }
            catch (Exception ex)
            {
                string formattedFileName = Path.GetFileName(filename).PadRight(35);
                _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"{formattedFileName}\tX509 certificate reading error: {ex.GetType().Name}: {ex.Message}");
                return [];
            }
        }

        private List<KeyValuePair<string, string>> GetCertificateInfos(string filename, byte[] fileData)
        {
            using X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(fileData);
            var data = CreateCertificateData(filename, certificate);
            return ToKeyValuePairs(data);
        }

        private List<KeyValuePair<string, string>> GetPkcs7Infos(string filename, byte[] fileData)
        {
            var infos = new List<KeyValuePair<string, string>>();

            byte[] cmsData = GetPkcs7Data(fileData);

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

            return infos;
        }

        private List<KeyValuePair<string, string>> GetPfxInfos(string filename, byte[] fileData)
        {
            var infos = new List<KeyValuePair<string, string>>();

            X509Certificate2Collection certificates = X509CertificateLoader.LoadPkcs12Collection(
                fileData,
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

            return infos;
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

        private static byte[] GetPkcs7Data(byte[] data)
        {
            try
            {
                string text = Encoding.UTF8.GetString(data);

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