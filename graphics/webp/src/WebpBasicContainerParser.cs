using System;

namespace webp;

internal static class WebpBasicContainerParser
{
    internal static WebpBasicContainerParseResult Parse(byte[] data, WebpStrictness strictness)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Parse(data.AsSpan(), strictness);
    }

    internal static WebpBasicContainerParseResult Parse(ReadOnlySpan<byte> data, WebpStrictness strictness)
    {
        WebpBasicContainerParseResult result = new WebpBasicContainerParseResult();
        WebpValidationResult signature = WebpSignature.ValidateRiffWebp(data);
        CopySignatureDiagnostics(result, signature, strictness);
        if (result.HasErrors())
        {
            Finish(result);
            return result;
        }

        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        CopyChunks(result, riff, data);
        InspectChunks(result, riff);

        if (!result.HasErrors())
        {
            ParseImagePayload(result);
        }

        Finish(result);
        return result;
    }

    internal static void InspectChunks(WebpBasicContainerParseResult result, WebpRiffParseResult riff)
    {
        int imageCount = 0;
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            if (chunk.FourCc == WebpConstants.FourCcVp8XString)
            {
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, chunk.Offset, chunk.FourCc, "VP8X chunk is not allowed in a basic still-image container");
            }
            else if (chunk.FourCc == WebpConstants.FourCcVp8String || chunk.FourCc == WebpConstants.FourCcVp8LString)
            {
                imageCount = imageCount + 1;
                if (imageCount == 1)
                {
                    result.ImageChunk = chunk;
                }
                else
                {
                    AddDiagnostic(result, WebpDiagnosticSeverity.Error, chunk.Offset, chunk.FourCc, "basic still-image containers must contain exactly one image chunk");
                }
            }
            else if (IsExtendedOnlyChunk(chunk.FourCc))
            {
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, chunk.Offset, chunk.FourCc, "extended chunk is not allowed in a basic still-image container");
            }

            index = index + 1;
        }

        if (imageCount == 0)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, WebpConstants.RiffHeaderSize, "", "basic still-image container is missing a VP8 or VP8L image chunk");
        }
    }

    internal static void ParseImagePayload(WebpBasicContainerParseResult result)
    {
        if (result.ImageChunk.FourCc == WebpConstants.FourCcVp8String)
        {
            ParseVp8Payload(result);
        }
        else if (result.ImageChunk.FourCc == WebpConstants.FourCcVp8LString)
        {
            ParseVp8LPayload(result);
        }
    }

    internal static void ParseVp8Payload(WebpBasicContainerParseResult result)
    {
        WebpVp8FrameHeaderParseResult vp8 = WebpVp8FrameHeaderParser.Parse(result.ImagePayload, WebpStrictness.Strict);
        CopyVp8Diagnostics(result, vp8);
        if (!vp8.IsValid)
        {
            return;
        }

        result.ContainerKind = WebpContainerKind.SimpleLossy;
        result.BitstreamKind = WebpBitstreamKind.Lossy;
        result.Width = vp8.Header.Width;
        result.Height = vp8.Header.Height;
        result.HasAlpha = false;
    }

    internal static void ParseVp8LPayload(WebpBasicContainerParseResult result)
    {
        if (result.ImagePayload.Length < WebpConstants.Vp8LHeaderSize)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, result.ImageChunk.PayloadOffset, result.ImageChunk.FourCc, "VP8L payload is too short for a lossless header");
            return;
        }

        if (result.ImagePayload[0] != WebpConstants.Vp8LSignature)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, result.ImageChunk.PayloadOffset, result.ImageChunk.FourCc, "VP8L signature is invalid");
            return;
        }

        uint bits = WebpEndianReader.ReadUInt32(result.ImagePayload, 1);
        int width = (int)(bits & 0x3FFFU) + 1;
        int height = (int)((bits >> 14) & 0x3FFFU) + 1;
        bool alpha = ((bits >> 28) & 1U) != 0;
        int version = (int)((bits >> 29) & 7U);
        if (version != WebpConstants.Vp8LVersion)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, result.ImageChunk.PayloadOffset + 4, result.ImageChunk.FourCc, "VP8L version must be zero");
            return;
        }

        result.ContainerKind = WebpContainerKind.SimpleLossless;
        result.BitstreamKind = WebpBitstreamKind.Lossless;
        result.Width = width;
        result.Height = height;
        result.HasAlpha = alpha;
    }

    internal static void CopyChunks(WebpBasicContainerParseResult result, WebpRiffParseResult riff, ReadOnlySpan<byte> data)
    {
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            result.Chunks.Add(chunk);
            if (chunk.FourCc == WebpConstants.FourCcVp8String || chunk.FourCc == WebpConstants.FourCcVp8LString)
            {
                if (result.ImagePayload.Length == 0)
                {
                    result.ImagePayload = WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength).ToArray();
                }
            }

            index = index + 1;
        }
    }

    internal static bool IsExtendedOnlyChunk(string fourCc)
    {
        return fourCc == WebpConstants.FourCcAlphaString
            || fourCc == WebpConstants.FourCcAnimString
            || fourCc == WebpConstants.FourCcAnimationFrameString
            || fourCc == WebpConstants.FourCcIccProfileString
            || fourCc == WebpConstants.FourCcExifString
            || fourCc == WebpConstants.FourCcXmpString;
    }

    internal static void CopySignatureDiagnostics(WebpBasicContainerParseResult result, WebpValidationResult signature, WebpStrictness strictness)
    {
        int index = 0;
        while (index < signature.Diagnostics.Count)
        {
            WebpDiagnostic diagnostic = signature.Diagnostics[index];
            WebpDiagnosticSeverity severity = diagnostic.Severity;
            if (diagnostic.Message == "chunk padding byte must be zero"
                || diagnostic.Message == "input contains trailing data after the RIFF payload")
            {
                severity = strictness == WebpStrictness.Strict ? WebpDiagnosticSeverity.Error : WebpDiagnosticSeverity.Warning;
            }

            AddDiagnostic(result, severity, diagnostic.Offset, diagnostic.FourCc, diagnostic.Message);
            index = index + 1;
        }
    }

    internal static void CopyVp8Diagnostics(WebpBasicContainerParseResult result, WebpVp8FrameHeaderParseResult vp8)
    {
        int index = 0;
        while (index < vp8.Diagnostics.Count)
        {
            WebpDiagnostic diagnostic = vp8.Diagnostics[index];
            AddDiagnostic(result, diagnostic.Severity, result.ImageChunk.PayloadOffset + diagnostic.Offset, result.ImageChunk.FourCc, diagnostic.Message);
            index = index + 1;
        }
    }

    internal static void AddDiagnostic(WebpBasicContainerParseResult result, WebpDiagnosticSeverity severity, long offset, string fourCc, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(severity, offset, fourCc, message));
    }

    internal static void Finish(WebpBasicContainerParseResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
