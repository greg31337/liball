using System;

namespace webp;

internal static class WebpRiffParser
{
    internal static WebpRiffParseResult Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Parse(data.AsSpan());
    }

    internal static WebpRiffParseResult Parse(ReadOnlySpan<byte> data)
    {
        WebpRiffParseResult result = new WebpRiffParseResult();

        if (data.Length < WebpConstants.RiffHeaderSize)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 0, "", "RIFF header is truncated");
            Finish(result);
            return result;
        }

        result.RiffFourCc = ReadFourCc(data, 0);
        result.DeclaredSize = WebpEndianReader.ReadUInt32(data, 4);
        result.DeclaredFileLength = 8L + result.DeclaredSize;
        result.FormatFourCc = ReadFourCc(data, 8);

        if (result.RiffFourCc != WebpConstants.FourCcRiffString)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 0, result.RiffFourCc, "RIFF signature is invalid");
            Finish(result);
            return result;
        }

        if (result.DeclaredSize > WebpConstants.MaxRiffSizeField)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 4, WebpConstants.FourCcRiffString, "RIFF size exceeds the WebP maximum");
        }

        if (result.DeclaredFileLength < WebpConstants.RiffHeaderSize)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 4, WebpConstants.FourCcRiffString, "RIFF size is smaller than the form type field");
            Finish(result);
            return result;
        }

        if (result.DeclaredFileLength > data.Length)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 4, WebpConstants.FourCcRiffString, "RIFF payload extends beyond the input buffer");
        }

        if (result.DeclaredFileLength < data.Length)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Warning, result.DeclaredFileLength, "", "input contains trailing data after the RIFF payload");
        }

        ParseChunks(data, result);
        Finish(result);
        return result;
    }

    internal static string ReadFourCc(ReadOnlySpan<byte> data, int offset)
    {
        WebpEndianReader.ReadSlice(data, offset, WebpConstants.FourCcByteCount);

        char[] chars = new char[WebpConstants.FourCcByteCount];
        chars[0] = (char)data[offset];
        chars[1] = (char)data[offset + 1];
        chars[2] = (char)data[offset + 2];
        chars[3] = (char)data[offset + 3];
        return new string(chars);
    }

    internal static void ParseChunks(ReadOnlySpan<byte> data, WebpRiffParseResult result)
    {
        long containerEnd = result.DeclaredFileLength;
        if (containerEnd > data.Length)
        {
            containerEnd = data.Length;
        }

        long offset = WebpConstants.RiffHeaderSize;
        while (offset < containerEnd)
        {
            if (containerEnd - offset < WebpConstants.ChunkHeaderSize)
            {
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, offset, "", "chunk header is truncated");
                return;
            }

            int intOffset = (int)offset;
            string fourCc = ReadFourCc(data, intOffset);
            uint payloadLength = WebpEndianReader.ReadUInt32(data, intOffset + WebpConstants.FourCcByteCount);
            long payloadOffset = offset + WebpConstants.ChunkHeaderSize;
            long payloadEnd = payloadOffset + payloadLength;
            long paddingLength = payloadLength & 1U;
            long paddedEnd = payloadEnd + paddingLength;

            WebpChunkInfo chunk = new WebpChunkInfo();
            chunk.FourCc = fourCc;
            chunk.Offset = offset;
            chunk.PayloadOffset = payloadOffset;
            chunk.PayloadLength = payloadLength;
            chunk.HasPadding = paddingLength != 0;
            chunk.PaddingOffset = payloadEnd;
            chunk.PaddedEndOffset = paddedEnd;
            chunk.IsPaddingValid = true;
            chunk.IsKnown = IsKnownChunk(fourCc);
            result.Chunks.Add(chunk);

            if (payloadEnd > containerEnd)
            {
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, offset, fourCc, "chunk payload exceeds the RIFF payload");
                return;
            }

            if (paddedEnd > containerEnd)
            {
                chunk.IsPaddingValid = false;
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, payloadEnd, fourCc, "chunk padding byte is missing");
                return;
            }

            if (chunk.HasPadding && data[(int)payloadEnd] != 0)
            {
                chunk.IsPaddingValid = false;
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, payloadEnd, fourCc, "chunk padding byte must be zero");
            }

            offset = paddedEnd;
        }
    }

    internal static bool IsKnownChunk(string fourCc)
    {
        return fourCc == WebpConstants.FourCcVp8String
            || fourCc == WebpConstants.FourCcVp8LString
            || fourCc == WebpConstants.FourCcVp8XString
            || fourCc == WebpConstants.FourCcAlphaString
            || fourCc == WebpConstants.FourCcAnimString
            || fourCc == WebpConstants.FourCcAnimationFrameString
            || fourCc == WebpConstants.FourCcIccProfileString
            || fourCc == WebpConstants.FourCcExifString
            || fourCc == WebpConstants.FourCcXmpString;
    }

    internal static void AddDiagnostic(WebpRiffParseResult result, WebpDiagnosticSeverity severity, long offset, string fourCc, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(severity, offset, fourCc, message));
    }

    internal static void Finish(WebpRiffParseResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
