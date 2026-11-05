using System;

namespace webp;

internal static class WebpSignature
{
    internal static bool HasRiffWebpSignature(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return HasRiffWebpSignature(data.AsSpan());
    }

    internal static bool HasRiffWebpSignature(ReadOnlySpan<byte> data)
    {
        if (data.Length < WebpConstants.RiffHeaderSize)
        {
            return false;
        }

        return data[0] == (byte)'R'
            && data[1] == (byte)'I'
            && data[2] == (byte)'F'
            && data[3] == (byte)'F'
            && data[8] == (byte)'W'
            && data[9] == (byte)'E'
            && data[10] == (byte)'B'
            && data[11] == (byte)'P';
    }

    internal static bool IsWebpMimeType(string mimeType)
    {
        ArgumentNullException.ThrowIfNull(mimeType);
        return string.Equals(mimeType, WebpConstants.MimeType, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsWebpFileExtension(string extension)
    {
        ArgumentNullException.ThrowIfNull(extension);
        if (string.Equals(extension, WebpConstants.FileExtension, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(extension, "webp", StringComparison.OrdinalIgnoreCase);
    }

    internal static WebpValidationResult ValidateRiffWebp(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return ValidateRiffWebp(data.AsSpan());
    }

    internal static WebpValidationResult ValidateRiffWebp(ReadOnlySpan<byte> data)
    {
        WebpValidationResult result = new WebpValidationResult();

        if (data.Length < WebpConstants.RiffHeaderSize)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 0, "", "RIFF WEBP signature is truncated");
            Finish(result);
            return result;
        }

        WebpRiffParseResult riffResult = WebpRiffParser.Parse(data);
        CopyDiagnostics(result, riffResult);

        if (riffResult.RiffFourCc == WebpConstants.FourCcRiffString && riffResult.FormatFourCc != WebpConstants.FourCcWebpString)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 8, riffResult.FormatFourCc, "RIFF form type is not WEBP");
        }

        Finish(result);
        return result;
    }

    internal static void CopyDiagnostics(WebpValidationResult result, WebpRiffParseResult riffResult)
    {
        int index = 0;
        while (index < riffResult.Diagnostics.Count)
        {
            result.Diagnostics.Add(riffResult.Diagnostics[index]);
            index = index + 1;
        }
    }

    internal static void AddDiagnostic(WebpValidationResult result, WebpDiagnosticSeverity severity, long offset, string fourCc, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(severity, offset, fourCc, message));
    }

    internal static void Finish(WebpValidationResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
