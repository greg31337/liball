using System;

namespace webp;

internal static class WebpVp8FrameHeaderParser
{
    internal static WebpVp8FrameHeaderParseResult Parse(byte[] payload, WebpStrictness strictness)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return Parse(payload.AsSpan(), strictness);
    }

    internal static WebpVp8FrameHeaderParseResult Parse(ReadOnlySpan<byte> payload, WebpStrictness strictness)
    {
        WebpVp8FrameHeaderParseResult result = new WebpVp8FrameHeaderParseResult();
        if (payload.Length < WebpConstants.Vp8LossyKeyFrameHeaderSize)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 0, "VP8 payload is too short for a key-frame header");
            Finish(result);
            return result;
        }

        WebpVp8FrameHeader header = result.Header;
        header.FrameTag = WebpEndianReader.ReadUInt24(payload, 0);
        header.IsKeyFrame = (header.FrameTag & 1U) == 0;
        header.Version = (int)((header.FrameTag >> 1) & 7U);
        header.ShowFrame = ((header.FrameTag >> 4) & 1U) != 0;
        header.FirstPartitionSize = (int)(header.FrameTag >> 5);
        if (!header.IsKeyFrame)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 0, "VP8 payload must contain a key frame");
            Finish(result);
            return result;
        }

        ParseKeyFrameHeader(result, payload);
        Finish(result);
        return result;
    }

    internal static void ParseKeyFrameHeader(WebpVp8FrameHeaderParseResult result, ReadOnlySpan<byte> payload)
    {
        WebpVp8FrameHeader header = result.Header;
        header.HasValidKeyFrameSignature = payload[3] == WebpConstants.Vp8LossyKeyFrameSignature0
            && payload[4] == WebpConstants.Vp8LossyKeyFrameSignature1
            && payload[5] == WebpConstants.Vp8LossyKeyFrameSignature2;
        if (!header.HasValidKeyFrameSignature)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 3, "VP8 key-frame signature is invalid");
            return;
        }

        ushort widthBits = WebpEndianReader.ReadUInt16(payload, 6);
        ushort heightBits = WebpEndianReader.ReadUInt16(payload, 8);
        header.Width = widthBits & 0x3FFF;
        header.HorizontalScale = (widthBits >> 14) & 3;
        header.Height = heightBits & 0x3FFF;
        header.VerticalScale = (heightBits >> 14) & 3;
        if (header.Width == 0 || header.Height == 0)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 6, "VP8 dimensions must be non-zero");
        }

        if (payload.Length >= WebpConstants.Vp8LossyKeyFrameHeaderSize + 1)
        {
            header.ColorSpace = payload[10] & 1;
            header.PixelClampingType = (payload[10] >> 1) & 1;
        }
    }

    internal static void AddDiagnostic(WebpVp8FrameHeaderParseResult result, WebpDiagnosticSeverity severity, long offset, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(severity, offset, WebpConstants.FourCcVp8String, message));
    }

    internal static void Finish(WebpVp8FrameHeaderParseResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
