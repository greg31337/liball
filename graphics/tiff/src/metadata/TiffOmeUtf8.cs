using System.Text;

namespace tiff;

internal class TiffOmeUtf8
{
    internal static string Decode(byte[] bytes, TiffDiagnostics diagnostics)
    {
        if (bytes == null || bytes.Length == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidAsciiValue, "OME ImageDescription must contain a NUL-terminated UTF-8 value.");
            return "";
        }

        int byteCount = bytes.Length;
        if (bytes[bytes.Length - 1] == 0)
        {
            byteCount = byteCount - 1;
        }
        else
        {
            diagnostics.AddError(TiffErrorCode.InvalidAsciiValue, "OME ImageDescription is not NUL terminated.");
        }

        int byteIndex = 0;
        while (byteIndex < byteCount)
        {
            if (bytes[byteIndex] == 0)
            {
                diagnostics.AddError(TiffErrorCode.InvalidAsciiValue, "OME ImageDescription contains an embedded NUL byte.");
                return "";
            }

            byteIndex = byteIndex + 1;
        }

        string text = Encoding.UTF8.GetString(bytes, 0, byteCount);
        int characterIndex = 0;
        while (characterIndex < text.Length)
        {
            if (text[characterIndex] == '\uFFFD')
            {
                diagnostics.AddError(TiffErrorCode.InvalidAsciiValue, "OME ImageDescription contains invalid UTF-8 data.");
                return "";
            }

            characterIndex = characterIndex + 1;
        }

        return text;
    }
}
