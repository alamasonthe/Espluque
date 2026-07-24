using ImageMagick;
using Util;

namespace Magick
{
    internal class Analyzer
    {
        public static async Task<Result<bool>> CanReadMimeType(string mimeType)
        {
            try
            {
                var supportedFormats = MagickNET.SupportedFormats;

                var readableFormats = supportedFormats
                    .Where(format => format.SupportsReading);

                bool canRead = readableFormats.Any(format =>
                    string.Equals(format.MimeType, mimeType, StringComparison.OrdinalIgnoreCase));

                return Result<bool>.Success(canRead);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure("MAGICK_MIME_TYPE_READ_CHECK_FAILED", ex.Message);
            }
        }
    }
}
