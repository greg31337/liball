using System;

namespace webp;

internal static class WebpVp8XParser
{
    internal static WebpVp8XParseResult Parse(byte[] payload, WebpStrictness strictness)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return Parse(payload.AsSpan(), strictness);
    }

    internal static WebpVp8XParseResult Parse(ReadOnlySpan<byte> payload, WebpStrictness strictness)
    {
        WebpVp8XParseResult result = new WebpVp8XParseResult();

        if (payload.Length != WebpConstants.Vp8XChunkPayloadSize)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 0, WebpConstants.FourCcVp8XString, "VP8X payload must be exactly 10 bytes");
            Finish(result);
            return result;
        }

        WebpVp8XHeader header = result.Header;
        header.FeatureFlags = payload[0];
        header.ReservedFeatureBits = (byte)(header.FeatureFlags & WebpConstants.Vp8XFeatureReservedMask);
        header.HasReservedFeatureBits = header.ReservedFeatureBits != 0;
        header.HasIccProfile = (header.FeatureFlags & WebpConstants.Vp8XFeatureIccProfile) != 0;
        header.HasAlpha = (header.FeatureFlags & WebpConstants.Vp8XFeatureAlpha) != 0;
        header.HasExif = (header.FeatureFlags & WebpConstants.Vp8XFeatureExif) != 0;
        header.HasXmp = (header.FeatureFlags & WebpConstants.Vp8XFeatureXmp) != 0;
        header.HasAnimation = (header.FeatureFlags & WebpConstants.Vp8XFeatureAnimation) != 0;
        header.HasReservedBytes = payload[1] != 0 || payload[2] != 0 || payload[3] != 0;
        header.CanvasWidth = (int)WebpEndianReader.ReadUInt24(payload, 4) + 1;
        header.CanvasHeight = (int)WebpEndianReader.ReadUInt24(payload, 7) + 1;

        if (header.HasReservedFeatureBits)
        {
            AddReservedDiagnostic(result, strictness, 0, "VP8X reserved feature bits are set");
        }

        if (header.HasReservedBytes)
        {
            AddReservedDiagnostic(result, strictness, 1, "VP8X reserved bytes must be zero");
        }

        Finish(result);
        return result;
    }

    internal static void AddReservedDiagnostic(WebpVp8XParseResult result, WebpStrictness strictness, long offset, string message)
    {
        if (strictness == WebpStrictness.Strict)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, offset, WebpConstants.FourCcVp8XString, message);
        }
        else
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Warning, offset, WebpConstants.FourCcVp8XString, message);
        }
    }

    internal static void AddDiagnostic(WebpVp8XParseResult result, WebpDiagnosticSeverity severity, long offset, string fourCc, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(severity, offset, fourCc, message));
    }

    internal static void Finish(WebpVp8XParseResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
