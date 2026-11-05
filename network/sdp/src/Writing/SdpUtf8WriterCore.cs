using System;

namespace sdp;

internal static class SdpUtf8WriterCore
{
    internal static SdpWriteResult WriteUtf8Bytes(SdpSessionDescription description, SdpWriteOptions options)
    {
        SdpWriteResult result = SdpWriterCore.WriteText(description, options);
        if (!result.Success)
        {
            return result;
        }

        int requiredBytes = SdpLineReader.Utf8.GetByteCount(result.Text);
        if (requiredBytes > options.MaximumOutputBytes)
        {
            result.Success = false;
            result.Text = string.Empty;
            result.CharactersWritten = 0;
            SdpWriterCore.AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteOutputByteLimitExceeded, SdpScope.Session, '\0', string.Empty, -1, "The generated UTF-8 SDP description exceeds the configured byte limit.", new SdpSourceLocation());
            return result;
        }

        result.Utf8Bytes = SdpLineReader.Utf8.GetBytes(result.Text);
        result.BytesWritten = requiredBytes;
        result.Text = string.Empty;
        result.CharactersWritten = 0;
        return result;
    }

    internal static SdpBufferWriteResult WriteUtf8Buffer(SdpSessionDescription description, Span<byte> destination, SdpWriteOptions options)
    {
        SdpBufferWriteResult result = new SdpBufferWriteResult();
        SdpWriteResult generated = SdpWriterCore.WriteText(description, options);
        if (!generated.Success)
        {
            CopyGenerationDiagnostics(result, generated, options.MaximumDiagnosticCount);
            return result;
        }

        int requiredBytes = SdpLineReader.Utf8.GetByteCount(generated.Text);
        result.RequiredBytes = requiredBytes;
        if (requiredBytes > options.MaximumOutputBytes)
        {
            AddBufferDiagnostic(result, options.MaximumDiagnosticCount, SdpDiagnosticCodes.WriteOutputByteLimitExceeded, "The generated UTF-8 SDP description exceeds the configured byte limit.");
            return result;
        }
        if (destination.Length < requiredBytes)
        {
            result.BufferTooSmall = true;
            AddBufferDiagnostic(result, options.MaximumDiagnosticCount, SdpDiagnosticCodes.WriteBufferTooSmall, "The caller-provided UTF-8 output buffer is too small for the complete SDP description.");
            return result;
        }

        result.BytesWritten = SdpLineReader.Utf8.GetBytes(generated.Text.AsSpan(), destination);
        result.Success = true;
        return result;
    }

    internal static void CopyGenerationDiagnostics(SdpBufferWriteResult target, SdpWriteResult source, int maximumDiagnosticCount)
    {
        SdpDiagnosticLimiter limiter = new SdpDiagnosticLimiter(target.Diagnostics, maximumDiagnosticCount);
        int index = 0;
        while (index < source.Diagnostics.Count && !limiter.DiagnosticLimitReached)
        {
            limiter.AddDiagnostic(source.Diagnostics[index]);
            index = index + 1;
        }
        target.DiagnosticLimitReached = limiter.DiagnosticLimitReached || source.DiagnosticLimitReached;
    }

    internal static void AddBufferDiagnostic(SdpBufferWriteResult result, int maximumDiagnosticCount, string code, string detail)
    {
        SdpSourcePath path = new SdpSourcePath();
        path.Scope = SdpScope.Session;
        path.MediaIndex = -1;
        SdpDiagnostic diagnostic = SdpDiagnosticBuilder.CreateDiagnostic(code, SdpValidationSeverity.Error, SdpScope.Session, '\0', string.Empty, -1, detail, path, new SdpSourceLocation());
        SdpDiagnosticLimiter limiter = new SdpDiagnosticLimiter(result.Diagnostics, maximumDiagnosticCount);
        limiter.AddDiagnostic(diagnostic);
        result.DiagnosticLimitReached = limiter.DiagnosticLimitReached;
    }
}
