using System;

namespace webp;

internal static class WebpExtendedContainerParser
{
    internal static WebpExtendedContainerParseResult Parse(byte[] data, WebpStrictness strictness)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Parse(data.AsSpan(), strictness);
    }

    internal static WebpExtendedContainerParseResult Parse(ReadOnlySpan<byte> data, WebpStrictness strictness)
    {
        WebpExtendedContainerParseResult result = new WebpExtendedContainerParseResult();
        WebpValidationResult signature = WebpSignature.ValidateRiffWebp(data);
        CopySignatureDiagnostics(result, signature, strictness);
        if (result.HasErrors())
        {
            Finish(result);
            return result;
        }

        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        CopyChunks(result, riff);
        ParseTopLevelChunks(result, data, riff, strictness);
        ValidateExtendedShape(result);
        Finish(result);
        return result;
    }

    internal static void ParseTopLevelChunks(WebpExtendedContainerParseResult result, ReadOnlySpan<byte> data, WebpRiffParseResult riff, WebpStrictness strictness)
    {
        int iccCount = 0;
        int exifCount = 0;
        int xmpCount = 0;
        int animCount = 0;
        int alphaCount = 0;
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            ReadOnlySpan<byte> payload = WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength);

            if (chunk.FourCc == WebpConstants.FourCcVp8XString)
            {
                ParseVp8X(result, payload, chunk, strictness);
            }
            else if (chunk.FourCc == WebpConstants.FourCcIccProfileString)
            {
                iccCount = iccCount + 1;
                if (iccCount == 1)
                {
                    result.Metadata.IccProfile = payload.ToArray();
                }
                else
                {
                    AddDiagnostic(result, WebpDiagnosticSeverity.Error, chunk.Offset, chunk.FourCc, "extended container contains multiple ICCP chunks");
                }
            }
            else if (chunk.FourCc == WebpConstants.FourCcExifString)
            {
                exifCount = exifCount + 1;
                if (exifCount == 1)
                {
                    result.Metadata.Exif = payload.ToArray();
                }
                else
                {
                    AddDiagnostic(result, WebpDiagnosticSeverity.Error, chunk.Offset, chunk.FourCc, "extended container contains multiple EXIF chunks");
                }
            }
            else if (chunk.FourCc == WebpConstants.FourCcXmpString)
            {
                xmpCount = xmpCount + 1;
                if (xmpCount == 1)
                {
                    result.Metadata.Xmp = payload.ToArray();
                }
                else
                {
                    AddDiagnostic(result, WebpDiagnosticSeverity.Error, chunk.Offset, chunk.FourCc, "extended container contains multiple XMP chunks");
                }
            }
            else if (chunk.FourCc == WebpConstants.FourCcAnimString)
            {
                animCount = animCount + 1;
                if (animCount == 1)
                {
                    ParseAnimationHeader(result, payload, chunk);
                }
                else
                {
                    AddDiagnostic(result, WebpDiagnosticSeverity.Error, chunk.Offset, chunk.FourCc, "extended container contains multiple ANIM chunks");
                }
            }
            else if (chunk.FourCc == WebpConstants.FourCcAnimationFrameString)
            {
                ParseAnimationFrame(result, payload, chunk, strictness);
            }
            else if (chunk.FourCc == WebpConstants.FourCcAlphaString)
            {
                alphaCount = alphaCount + 1;
                if (alphaCount == 1)
                {
                    result.AlphaData = payload.ToArray();
                }
                else
                {
                    AddDiagnostic(result, WebpDiagnosticSeverity.Error, chunk.Offset, chunk.FourCc, "extended container contains multiple ALPH chunks");
                }
            }
            else if (chunk.FourCc == WebpConstants.FourCcVp8String)
            {
                SetTopLevelImage(result, payload, chunk, WebpBitstreamKind.Lossy);
            }
            else if (chunk.FourCc == WebpConstants.FourCcVp8LString)
            {
                SetTopLevelImage(result, payload, chunk, WebpBitstreamKind.Lossless);
            }
            else
            {
                result.UnknownChunks.Add(new WebpUnknownChunk(chunk.FourCc, payload.ToArray()));
            }

            index = index + 1;
        }
    }

    internal static void ParseVp8X(WebpExtendedContainerParseResult result, ReadOnlySpan<byte> payload, WebpChunkInfo chunk, WebpStrictness strictness)
    {
        if (result.HasVp8X)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, chunk.Offset, chunk.FourCc, "extended container contains multiple VP8X chunks");
            return;
        }

        WebpVp8XParseResult vp8x = WebpVp8XParser.Parse(payload, strictness);
        result.HasVp8X = true;
        result.Vp8X = vp8x.Header;
        CopyVp8XDiagnostics(result, vp8x, chunk.PayloadOffset);
    }

    internal static void ParseAnimationHeader(WebpExtendedContainerParseResult result, ReadOnlySpan<byte> payload, WebpChunkInfo chunk)
    {
        if (payload.Length != WebpConstants.AnimChunkPayloadSize)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, chunk.PayloadOffset, chunk.FourCc, "ANIM payload must be exactly 6 bytes");
            return;
        }

        result.AnimationBackgroundColorBgra = WebpEndianReader.ReadUInt32(payload, 0);
        result.AnimationLoopCount = WebpEndianReader.ReadUInt16(payload, 4);
    }

    internal static void ParseAnimationFrame(WebpExtendedContainerParseResult result, ReadOnlySpan<byte> payload, WebpChunkInfo chunk, WebpStrictness strictness)
    {
        if (payload.Length < WebpConstants.AnimFrameHeaderSize)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, chunk.PayloadOffset, chunk.FourCc, "ANMF payload is too short for a frame header");
            return;
        }

        WebpFrame frame = new WebpFrame();
        frame.X = (int)WebpEndianReader.ReadUInt24(payload, 0) * 2;
        frame.Y = (int)WebpEndianReader.ReadUInt24(payload, 3) * 2;
        frame.Width = (int)WebpEndianReader.ReadUInt24(payload, 6) + 1;
        frame.Height = (int)WebpEndianReader.ReadUInt24(payload, 9) + 1;
        frame.DurationMilliseconds = (int)WebpEndianReader.ReadUInt24(payload, 12);

        byte flags = payload[15];
        if ((flags & WebpConstants.AnimFrameDisposeBackgroundFlag) != 0)
        {
            frame.DisposeMode = WebpFrameDisposeMode.DisposeToBackground;
        }

        if ((flags & WebpConstants.AnimFrameNoBlendFlag) != 0)
        {
            frame.BlendMode = WebpFrameBlendMode.Replace;
        }

        if ((flags & WebpConstants.AnimFrameReservedMask) != 0)
        {
            AddReservedFrameDiagnostic(result, strictness, chunk.PayloadOffset + 15, chunk.FourCc, "ANMF reserved flag bits are set");
        }

        ParseFrameSubchunks(result, frame, payload, chunk.PayloadOffset + WebpConstants.AnimFrameHeaderSize, strictness);
        result.Frames.Add(frame);
    }

    internal static void ParseFrameSubchunks(WebpExtendedContainerParseResult result, WebpFrame frame, ReadOnlySpan<byte> anmfPayload, long absoluteSubchunkOffset, WebpStrictness strictness)
    {
        bool imageSeen = false;
        int offset = WebpConstants.AnimFrameHeaderSize;
        while (offset < anmfPayload.Length)
        {
            if (anmfPayload.Length - offset < WebpConstants.ChunkHeaderSize)
            {
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, absoluteSubchunkOffset + offset - WebpConstants.AnimFrameHeaderSize, WebpConstants.FourCcAnimationFrameString, "ANMF frame subchunk header is truncated");
                return;
            }

            string fourCc = WebpRiffParser.ReadFourCc(anmfPayload, offset);
            uint payloadLength = WebpEndianReader.ReadUInt32(anmfPayload, offset + WebpConstants.FourCcByteCount);
            int payloadOffset = offset + WebpConstants.ChunkHeaderSize;
            long payloadEnd = payloadOffset + payloadLength;
            long paddedEnd = payloadEnd + (payloadLength & 1U);
            if (paddedEnd > anmfPayload.Length)
            {
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, absoluteSubchunkOffset + offset - WebpConstants.AnimFrameHeaderSize, fourCc, "ANMF frame subchunk payload exceeds the frame payload");
                return;
            }

            ReadOnlySpan<byte> subchunkPayload = WebpEndianReader.ReadSlice(anmfPayload, payloadOffset, (int)payloadLength);
            if (fourCc == WebpConstants.FourCcAlphaString)
            {
                if (frame.AlphaData != null)
                {
                    AddDiagnostic(result, WebpDiagnosticSeverity.Error, absoluteSubchunkOffset + offset - WebpConstants.AnimFrameHeaderSize, fourCc, "ANMF frame contains multiple ALPH subchunks");
                }
                else
                {
                    frame.AlphaData = subchunkPayload.ToArray();
                }

                if (imageSeen)
                {
                    AddStrictnessDiagnostic(result, strictness, absoluteSubchunkOffset + offset - WebpConstants.AnimFrameHeaderSize, fourCc, "ANMF ALPH subchunk appears after the frame image subchunk");
                }
            }
            else if (fourCc == WebpConstants.FourCcVp8String)
            {
                if (frame.EncodedBitstream != null)
                {
                    AddDiagnostic(result, WebpDiagnosticSeverity.Error, absoluteSubchunkOffset + offset - WebpConstants.AnimFrameHeaderSize, fourCc, "ANMF frame contains multiple image subchunks");
                }
                else
                {
                    frame.EncodedBitstream = subchunkPayload.ToArray();
                    frame.BitstreamKind = WebpBitstreamKind.Lossy;
                }

                imageSeen = true;
            }
            else if (fourCc == WebpConstants.FourCcVp8LString)
            {
                if (frame.EncodedBitstream != null)
                {
                    AddDiagnostic(result, WebpDiagnosticSeverity.Error, absoluteSubchunkOffset + offset - WebpConstants.AnimFrameHeaderSize, fourCc, "ANMF frame contains multiple image subchunks");
                }
                else
                {
                    frame.EncodedBitstream = subchunkPayload.ToArray();
                    frame.BitstreamKind = WebpBitstreamKind.Lossless;
                }

                imageSeen = true;
            }
            else
            {
                frame.UnknownChunks.Add(new WebpUnknownChunk(fourCc, subchunkPayload.ToArray()));
            }

            if ((payloadLength & 1U) != 0 && anmfPayload[(int)payloadEnd] != 0)
            {
                AddStrictnessDiagnostic(result, strictness, absoluteSubchunkOffset + payloadEnd - WebpConstants.AnimFrameHeaderSize, fourCc, "ANMF frame subchunk padding byte must be zero");
            }

            offset = (int)paddedEnd;
        }

        if (frame.EncodedBitstream == null)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, absoluteSubchunkOffset, WebpConstants.FourCcAnimationFrameString, "ANMF frame is missing a VP8 or VP8L image subchunk");
        }

        if (frame.BitstreamKind == WebpBitstreamKind.Lossless && frame.AlphaData != null)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, absoluteSubchunkOffset, WebpConstants.FourCcAnimationFrameString, "ALPH subchunk is not allowed with a VP8L frame image");
        }
    }

    internal static void SetTopLevelImage(WebpExtendedContainerParseResult result, ReadOnlySpan<byte> payload, WebpChunkInfo chunk, WebpBitstreamKind bitstreamKind)
    {
        if (result.ImagePayload != null)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, chunk.Offset, chunk.FourCc, "extended still container contains multiple top-level image chunks");
            return;
        }

        result.ImagePayload = payload.ToArray();
        result.BitstreamKind = bitstreamKind;
    }

    internal static void ValidateExtendedShape(WebpExtendedContainerParseResult result)
    {
        if (!result.HasVp8X)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, WebpConstants.RiffHeaderSize, "", "extended WebP container is missing VP8X");
        }

        if (result.BitstreamKind == WebpBitstreamKind.Lossless && result.AlphaData != null)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, WebpConstants.RiffHeaderSize, WebpConstants.FourCcAlphaString, "ALPH chunk is not allowed with a VP8L image chunk");
        }
    }

    internal static void CopyChunks(WebpExtendedContainerParseResult result, WebpRiffParseResult riff)
    {
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            result.Chunks.Add(riff.Chunks[index]);
            index = index + 1;
        }
    }

    internal static void CopySignatureDiagnostics(WebpExtendedContainerParseResult result, WebpValidationResult signature, WebpStrictness strictness)
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

    internal static void CopyVp8XDiagnostics(WebpExtendedContainerParseResult result, WebpVp8XParseResult vp8x, long baseOffset)
    {
        int index = 0;
        while (index < vp8x.Diagnostics.Count)
        {
            WebpDiagnostic source = vp8x.Diagnostics[index];
            AddDiagnostic(result, source.Severity, baseOffset + source.Offset, source.FourCc, source.Message);
            index = index + 1;
        }
    }

    internal static void AddReservedFrameDiagnostic(WebpExtendedContainerParseResult result, WebpStrictness strictness, long offset, string fourCc, string message)
    {
        if (strictness == WebpStrictness.Strict)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, offset, fourCc, message);
        }
        else
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Warning, offset, fourCc, message);
        }
    }

    internal static void AddStrictnessDiagnostic(WebpExtendedContainerParseResult result, WebpStrictness strictness, long offset, string fourCc, string message)
    {
        if (strictness == WebpStrictness.Strict)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, offset, fourCc, message);
        }
        else
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Warning, offset, fourCc, message);
        }
    }

    internal static void AddDiagnostic(WebpExtendedContainerParseResult result, WebpDiagnosticSeverity severity, long offset, string fourCc, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(severity, offset, fourCc, message));
    }

    internal static void Finish(WebpExtendedContainerParseResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
