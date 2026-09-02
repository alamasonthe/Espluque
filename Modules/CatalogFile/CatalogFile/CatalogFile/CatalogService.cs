using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace CatalogFile
{
    public class CatalogService
    {
        private readonly Espluque.Contracts.CrossCutting.ILogger _logger;

        public CatalogService(Espluque.Contracts.CrossCutting.ILogger logger)
        {
            _logger = logger;
        }

        public List<KeyValuePair<string, string>> GetInfos(string filename)
        {
            CatalogFileInfo? catalog = Read(filename);

            if (catalog == null)
                return new();

            List<KeyValuePair<string, string>> infos = new();

            infos.Add(new("Signer", catalog.Signer ?? string.Empty));
            infos.Add(new("Issuer", catalog.Issuer ?? string.Empty));
            infos.Add(new("Certificate valid from", catalog.CertificateValidFrom?.ToString("O") ?? string.Empty));
            infos.Add(new("Certificate valid to", catalog.CertificateValidTo?.ToString("O") ?? string.Empty));
            infos.Add(new("OSAttr", catalog.OSAttr ?? string.Empty));
            infos.Add(new("Member count", catalog.Members.Count.ToString()));

            for (int i = 0; i < catalog.Members.Count; i++)
            {
                CatalogMemberInfo member = catalog.Members[i];
                int index = i + 1;

                infos.Add(new(
                    $"Member [{index}] ReferenceTag",
                    member.ReferenceTag ?? string.Empty));

                infos.Add(new(
                    $"Member [{index}] DigestAlgorithm",
                    member.DigestAlgorithm ?? string.Empty));

                infos.Add(new(
                    $"Member [{index}] Digest",
                    member.Digest ?? string.Empty));
            }

            return infos;
        }

        public CatalogFileInfo? Read(string filename)
        {
            string formattedFileName = Path.GetFileName(filename).PadRight(35);

            if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
            {
                _logger.Log(LogLevel.Error, $"{formattedFileName}\tCatalog file not found: {filename}");
                return null;
            }

            IntPtr hCatalog = CryptCATOpen(filename, 0, IntPtr.Zero, 0, 0);

            if (hCatalog == new IntPtr(INVALID_HANDLE_VALUE))
            {
                int error = Marshal.GetLastWin32Error();

                _logger.Log( LogLevel.Error, $"{formattedFileName}\tUnable to open catalog file: {filename} (Win32 error {error})");
                return null;
            }

            try
            {
                CatalogFileInfo result = new();

                ReadSignature(filename, result);
                ReadCatalogAttributes(hCatalog, result);
                ReadMembers(hCatalog, result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"{formattedFileName}\tError reading catalog file {filename}: {ex.Message}");

                return null;
            }
            finally
            {
                if (!CryptCATClose(hCatalog))
                {
                    int error = Marshal.GetLastWin32Error();

                    _logger.Log(LogLevel.Warning, $"{formattedFileName}\tUnable to close catalog file {filename} (Win32 error {error})");
                }
            }
        }

        private static void ReadCatalogAttributes(IntPtr hCatalog, CatalogFileInfo result)
        {
            IntPtr pAttr = IntPtr.Zero;

            while ((pAttr = CryptCATEnumerateCatAttr(hCatalog, pAttr)) != IntPtr.Zero)
            {
                CRYPTCATATTRIBUTE attr =
                    Marshal.PtrToStructure<CRYPTCATATTRIBUTE>(pAttr);

                string? name = Marshal.PtrToStringUni(attr.pwszReferenceTag);

                if (!string.Equals(name, "OSAttr", StringComparison.OrdinalIgnoreCase))
                    continue;

                byte[] data = new byte[attr.cbValue];
                Marshal.Copy(attr.pbValue, data, 0, data.Length);

                result.OSAttr = Encoding.ASCII
                    .GetString(data)
                    .TrimEnd('\0');

                break;
            }
        }

        private void ReadMembers(IntPtr hCatalog, CatalogFileInfo result)
        {
            IntPtr pMember = IntPtr.Zero;

            while ((pMember = CryptCATEnumerateMember(hCatalog, pMember)) != IntPtr.Zero)
            {
                CRYPTCATMEMBER nativeMember =
                    Marshal.PtrToStructure<CRYPTCATMEMBER>(pMember);

                ReadMemberAttributes(hCatalog, pMember, result);

                if (nativeMember.pIndirectData == IntPtr.Zero)
                    continue;

                SIP_INDIRECT_DATA indirectData =
                    Marshal.PtrToStructure<SIP_INDIRECT_DATA>(
                        nativeMember.pIndirectData);

                result.Members.Add(new CatalogMemberInfo
                {
                    ReferenceTag =
                        Marshal.PtrToStringUni(nativeMember.pwszReferenceTag),

                    DigestAlgorithm =
                        GetAlgorithmName(indirectData.DigestAlgorithm.pszObjId),

                    Digest =
                        ReadHex(
                            indirectData.Digest.pbData,
                            indirectData.Digest.cbData)
                });
            }
        }

        private void ReadMemberAttributes( IntPtr hCatalog, IntPtr pMember, CatalogFileInfo result)
        {
            if (!string.IsNullOrWhiteSpace(result.OSAttr))
                return;

            IntPtr pAttr = IntPtr.Zero;

            while ((pAttr = CryptCATEnumerateAttr(
                hCatalog,
                pMember,
                pAttr)) != IntPtr.Zero)
            {
                CRYPTCATATTRIBUTE attr =
                    Marshal.PtrToStructure<CRYPTCATATTRIBUTE>(pAttr);

                string? name =
                    Marshal.PtrToStringUni(attr.pwszReferenceTag);

                if (!string.Equals(
                    name,
                    "OSAttr",
                    StringComparison.OrdinalIgnoreCase))
                    continue;

                byte[] data = new byte[attr.cbValue];
                Marshal.Copy(attr.pbValue, data, 0, data.Length);

                result.OSAttr = Encoding.Unicode
                    .GetString(data)
                    .TrimEnd('\0');

                return;
            }
        }

        private void ReadSignature(string filename, CatalogFileInfo result)
        {
            try
            {
                SignedCms signedCms = new();
                signedCms.Decode(File.ReadAllBytes(filename));

                if (signedCms.SignerInfos.Count == 0)
                    return;

                X509Certificate2? certificate =
                    signedCms.SignerInfos[0].Certificate;

                if (certificate == null)
                    return;

                result.Signer =
                    certificate.GetNameInfo(X509NameType.SimpleName, false);

                result.Issuer =
                    certificate.GetNameInfo(X509NameType.SimpleName, true);

                result.CertificateValidFrom = certificate.NotBefore;
                result.CertificateValidTo = certificate.NotAfter;
            }
            catch (Exception ex)
            {
                _logger.Log(
                    LogLevel.Warning,
                    $"Unable to read catalog signature {filename}: {ex.Message}");
            }
        }

        private static string? GetAlgorithmName(IntPtr pszObjId)
        {
            string? oid = Marshal.PtrToStringAnsi(pszObjId);

            if (string.IsNullOrWhiteSpace(oid))
                return null;

            try
            {
                return Oid
                    .FromOidValue(oid, OidGroup.HashAlgorithm)
                    .FriendlyName?
                    .ToUpperInvariant() ?? oid;
            }
            catch
            {
                return oid;
            }
        }

        private static string ReadHex(IntPtr pointer, int length)
        {
            byte[] data = new byte[length];
            Marshal.Copy(pointer, data, 0, length);

            return Convert.ToHexString(data);
        }

        private const int INVALID_HANDLE_VALUE = -1;

        #region dll import

        [DllImport("Wintrust.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CryptCATOpen(
            string pwszFileName,
            int fdwOpenFlags,
            IntPtr hProv,
            int dwPublicVersion,
            int dwEncodingType);

        [DllImport("Wintrust.dll", SetLastError = true)]
        private static extern bool CryptCATClose(IntPtr hCatalog);

        [DllImport("Wintrust.dll", SetLastError = true)]
        private static extern IntPtr CryptCATEnumerateMember(
            IntPtr hCatalog,
            IntPtr pPrevMember);

        [DllImport("Wintrust.dll", SetLastError = true)]
        private static extern IntPtr CryptCATEnumerateCatAttr(
            IntPtr hCatalog,
            IntPtr pPrevAttr);

        [DllImport("Wintrust.dll", SetLastError = true)]
        private static extern IntPtr CryptCATEnumerateAttr(
            IntPtr hCatalog,
            IntPtr pCatMember,
            IntPtr pPrevAttr);

        #endregion

        #region StructLayout

        [StructLayout(LayoutKind.Sequential)]
        private struct CRYPTCATMEMBER
        {
            public int cbStruct;
            public IntPtr pwszReferenceTag;
            public IntPtr pwszFileName;
            public Guid gSubjectType;
            public int fdwMemberFlags;
            public IntPtr pIndirectData;
            public int dwCertVersion;
            public int dwReserved;
            public IntPtr hReserved;
            public CRYPT_ATTR_BLOB sEncodedIndirectData;
            public CRYPT_ATTR_BLOB sEncodedMemberInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CRYPTCATATTRIBUTE
        {
            public int cbStruct;
            public IntPtr pwszReferenceTag;
            public int dwAttrTypeAndAction;
            public int cbValue;
            public IntPtr pbValue;
            public int dwReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIP_INDIRECT_DATA
        {
            public CRYPT_ATTRIBUTE_TYPE_VALUE Data;
            public CRYPT_ALGORITHM_IDENTIFIER DigestAlgorithm;
            public CRYPT_HASH_BLOB Digest;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CRYPT_ATTRIBUTE_TYPE_VALUE
        {
            public IntPtr pszObjId;
            public CRYPT_OBJID_BLOB Value;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CRYPT_ALGORITHM_IDENTIFIER
        {
            public IntPtr pszObjId;
            public CRYPT_OBJID_BLOB Parameters;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CRYPT_ATTR_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CRYPT_OBJID_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CRYPT_HASH_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        #endregion
    }
}