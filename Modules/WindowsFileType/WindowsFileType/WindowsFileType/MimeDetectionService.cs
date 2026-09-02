using System.Runtime.InteropServices;

namespace WindowsFileType
{
    public static class MimeDetectionService
    {
        private const int FMFD_ENABLEMIMESNIFFING = 0x00000002;
        private const int FMFD_RETURNUPDATEDIMGMIMES = 0x00000020;

        [DllImport(
            "urlmon.dll",
            CharSet = CharSet.Unicode,
            ExactSpelling = true,
            SetLastError = false)]
        private static extern int FindMimeFromData(
            IntPtr pBC,
            string? pwzUrl,
            byte[] pBuffer,
            int cbSize,
            string? pwzMimeProposed,
            int dwMimeFlags,
            out IntPtr ppwzMimeOut,
            int dwReserved);

        public static string? DetectMimeType(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            byte[] buffer;

            using (FileStream stream = File.OpenRead(filePath))
            {
                int length = (int)Math.Min(stream.Length, 256);

                if (length == 0)
                    return null;

                buffer = new byte[length];
                stream.ReadExactly(buffer);
            }

            IntPtr mimePointer = IntPtr.Zero;

            try
            {
                int result = FindMimeFromData(
                    IntPtr.Zero,
                    null,
                    buffer,
                    buffer.Length,
                    null,
                    FMFD_ENABLEMIMESNIFFING | FMFD_RETURNUPDATEDIMGMIMES,
                    out mimePointer,
                    0);

                if (result != 0 || mimePointer == IntPtr.Zero)
                    return null;

                return Marshal.PtrToStringUni(mimePointer);
            }
            finally
            {
                if (mimePointer != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(mimePointer);
            }
        }
    }
}