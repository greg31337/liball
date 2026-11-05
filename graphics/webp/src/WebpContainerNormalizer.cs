using System;
using System.Collections.Generic;

namespace webp;

internal static class WebpContainerNormalizer
{
    internal static WebpNormalizationResult Normalize(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return NormalizeSpan(data);
    }

    internal static WebpNormalizationResult NormalizeSpan(ReadOnlySpan<byte> data)
    {
        WebpNormalizationResult result = new WebpNormalizationResult();
        result.Data = data.ToArray();

        WebpSecurityLimits limits = new WebpSecurityLimits();
        string? limitError = WebpSecurityLimitValidator.GetContainerLimitError(data, limits);
        if (limitError != null)
        {
            AddUnsafe(result, 0, "RIFF", limitError);
            return result;
        }

        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        if (!CanRepairRiff(result, riff, data.Length))
        {
            return result;
        }

        List<WebpRiffChunk> sourceChunks = CopyChunks(data, riff);
        if (!ValidateUniqueChunks(result, sourceChunks))
        {
            return result;
        }

        bool hasAnimation = GetChunkCount(sourceChunks, WebpConstants.FourCcAnimString) != 0
            || GetChunkCount(sourceChunks, WebpConstants.FourCcAnimationFrameString) != 0;
        byte[]? normalized;
        if (hasAnimation)
        {
            normalized = NormalizeAnimation(result, sourceChunks);
        }
        else
        {
            normalized = NormalizeStill(result, sourceChunks);
        }

        if (normalized == null)
        {
            return result;
        }

        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Strictness = WebpStrictness.Strict;
        WebpValidationResult validation = WebpValidator.Validate(normalized, options);
        if (!validation.IsValid)
        {
            CopyUnsafeDiagnostics(result, validation.Diagnostics);
            return result;
        }

        result.Data = normalized;
        result.IsNormalized = true;
        result.WasChanged = !BytesEqual(data, normalized);
        return result;
    }

    internal static bool CanRepairRiff(WebpNormalizationResult result, WebpRiffParseResult riff, int inputLength)
    {
        if (riff.RiffFourCc != WebpConstants.FourCcRiffString || riff.FormatFourCc != WebpConstants.FourCcWebpString)
        {
            AddUnsafe(result, 0, "RIFF", "normalization requires a RIFF WEBP container");
            return false;
        }

        bool safe = true;
        int index = 0;
        while (index < riff.Diagnostics.Count)
        {
            WebpDiagnostic diagnostic = riff.Diagnostics[index];
            bool repairable = diagnostic.Message == "chunk padding byte must be zero";
            if (diagnostic.Message == "chunk padding byte is missing")
            {
                repairable = IsRepairableMissingPadding(riff, inputLength);
            }

            if (diagnostic.Message == "input contains trailing data after the RIFF payload")
            {
                repairable = IsRepairableTrailingPadding(riff, inputLength);
            }

            if (diagnostic.Message == "RIFF payload extends beyond the input buffer")
            {
                repairable = IsRepairableMissingPadding(riff, inputLength);
            }

            if (!repairable)
            {
                AddUnsafe(result, diagnostic.Offset, diagnostic.FourCc, diagnostic.Message);
                safe = false;
            }

            index = index + 1;
        }

        return safe;
    }

    internal static bool IsRepairableMissingPadding(WebpRiffParseResult riff, int inputLength)
    {
        if (riff.Chunks.Count == 0)
        {
            return false;
        }

        WebpChunkInfo chunk = riff.Chunks[riff.Chunks.Count - 1];
        if (!chunk.HasPadding || chunk.PaddingOffset != inputLength)
        {
            return false;
        }

        return riff.DeclaredFileLength == inputLength || riff.DeclaredFileLength == inputLength + 1L;
    }

    internal static bool IsRepairableTrailingPadding(WebpRiffParseResult riff, int inputLength)
    {
        if (riff.Chunks.Count == 0 || riff.DeclaredFileLength + 1L != inputLength)
        {
            return false;
        }

        WebpChunkInfo chunk = riff.Chunks[riff.Chunks.Count - 1];
        return chunk.HasPadding && chunk.PaddingOffset + 1L == inputLength && chunk.PaddedEndOffset == inputLength;
    }

    internal static List<WebpRiffChunk> CopyChunks(ReadOnlySpan<byte> data, WebpRiffParseResult riff)
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            ReadOnlySpan<byte> payload = WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength);
            chunks.Add(new WebpRiffChunk(chunk.FourCc, payload.ToArray()));
            index = index + 1;
        }

        return chunks;
    }

    internal static bool ValidateUniqueChunks(WebpNormalizationResult result, List<WebpRiffChunk> chunks)
    {
        bool valid = true;
        valid = ValidateMaximumCount(result, chunks, WebpConstants.FourCcVp8XString, 1) && valid;
        valid = ValidateMaximumCount(result, chunks, WebpConstants.FourCcIccProfileString, 1) && valid;
        valid = ValidateMaximumCount(result, chunks, WebpConstants.FourCcAnimString, 1) && valid;
        valid = ValidateMaximumCount(result, chunks, WebpConstants.FourCcAlphaString, 1) && valid;
        valid = ValidateMaximumCount(result, chunks, WebpConstants.FourCcExifString, 1) && valid;
        valid = ValidateMaximumCount(result, chunks, WebpConstants.FourCcXmpString, 1) && valid;
        return valid;
    }

    internal static bool ValidateMaximumCount(WebpNormalizationResult result, List<WebpRiffChunk> chunks, string fourCc, int maximum)
    {
        if (GetChunkCount(chunks, fourCc) <= maximum)
        {
            return true;
        }

        AddUnsafe(result, -1, fourCc, "normalization cannot safely choose between duplicate " + fourCc + " chunks");
        return false;
    }

    internal static byte[]? NormalizeStill(WebpNormalizationResult result, List<WebpRiffChunk> source)
    {
        int vp8Count = GetChunkCount(source, WebpConstants.FourCcVp8String);
        int vp8LCount = GetChunkCount(source, WebpConstants.FourCcVp8LString);
        if (vp8Count + vp8LCount != 1)
        {
            AddUnsafe(result, -1, "", "still-image normalization requires exactly one VP8 or VP8L image chunk");
            return null;
        }

        if (GetChunkCount(source, WebpConstants.FourCcAnimString) != 0 || GetChunkCount(source, WebpConstants.FourCcAnimationFrameString) != 0)
        {
            AddUnsafe(result, -1, "", "still-image normalization cannot retain animation chunks");
            return null;
        }

        WebpRiffChunk image = vp8Count == 1 ? FindChunk(source, WebpConstants.FourCcVp8String) : FindChunk(source, WebpConstants.FourCcVp8LString);
        WebpBitstreamKind kind = vp8Count == 1 ? WebpBitstreamKind.Lossy : WebpBitstreamKind.Lossless;
        WebpBasicContainerParseResult parsed = ParseImagePayload(image.Payload, kind);
        if (parsed.HasErrors())
        {
            AddUnsafe(result, -1, image.FourCc, "image bitstream is not valid and cannot be normalized without recompression");
            return null;
        }

        WebpRiffChunk? alpha = FindOptionalChunk(source, WebpConstants.FourCcAlphaString);
        if (alpha != null && (kind != WebpBitstreamKind.Lossy || alpha.Payload.Length == 0))
        {
            AddUnsafe(result, -1, WebpConstants.FourCcAlphaString, "ALPH is only safe with a non-empty lossy VP8 image");
            return null;
        }

        WebpRiffChunk? icc = FindOptionalChunk(source, WebpConstants.FourCcIccProfileString);
        WebpRiffChunk? exif = FindOptionalChunk(source, WebpConstants.FourCcExifString);
        WebpRiffChunk? xmp = FindOptionalChunk(source, WebpConstants.FourCcXmpString);
        bool extended = alpha != null || icc != null || exif != null || xmp != null;
        List<WebpRiffChunk> output = new List<WebpRiffChunk>();
        if (extended)
        {
            WebpMetadata metadata = CreateMetadata(icc, exif, xmp);
            bool hasAlpha = alpha != null || parsed.HasAlpha;
            output.Add(new WebpRiffChunk(WebpConstants.FourCcVp8XString, WebpContainerWriter.CreateVp8XPayload(parsed.Width, parsed.Height, metadata, hasAlpha, false)));
            AddOptionalChunk(output, icc);
            AddOptionalChunk(output, alpha);
            output.Add(new WebpRiffChunk(image.FourCc, image.Payload));
            AddOptionalChunk(output, exif);
            AddOptionalChunk(output, xmp);
        }
        else
        {
            output.Add(new WebpRiffChunk(image.FourCc, image.Payload));
        }

        AddUnknownChunks(output, source);
        return WebpUnknownChunkMux.WritePreservingOrder(WebpConstants.FourCcWebpString, output);
    }

    internal static byte[]? NormalizeAnimation(WebpNormalizationResult result, List<WebpRiffChunk> source)
    {
        WebpRiffChunk? vp8x = FindOptionalChunk(source, WebpConstants.FourCcVp8XString);
        if (vp8x == null || vp8x.Payload.Length != WebpConstants.Vp8XChunkPayloadSize)
        {
            AddUnsafe(result, -1, WebpConstants.FourCcVp8XString, "animation normalization requires a valid VP8X canvas");
            return null;
        }

        WebpVp8XParseResult header = WebpVp8XParser.Parse(vp8x.Payload, WebpStrictness.Permissive);
        if (header.HasErrors() || header.Header.CanvasWidth <= 0 || header.Header.CanvasHeight <= 0)
        {
            AddUnsafe(result, -1, WebpConstants.FourCcVp8XString, "animation canvas dimensions cannot be recovered safely");
            return null;
        }

        WebpRiffChunk? anim = FindOptionalChunk(source, WebpConstants.FourCcAnimString);
        if (anim == null || anim.Payload.Length != WebpConstants.AnimChunkPayloadSize)
        {
            AddUnsafe(result, -1, WebpConstants.FourCcAnimString, "animation normalization requires one valid ANIM chunk");
            return null;
        }

        if (GetChunkCount(source, WebpConstants.FourCcAnimationFrameString) == 0)
        {
            AddUnsafe(result, -1, WebpConstants.FourCcAnimationFrameString, "animation normalization requires at least one ANMF frame");
            return null;
        }

        if (GetChunkCount(source, WebpConstants.FourCcVp8String) != 0
            || GetChunkCount(source, WebpConstants.FourCcVp8LString) != 0
            || GetChunkCount(source, WebpConstants.FourCcAlphaString) != 0)
        {
            AddUnsafe(result, -1, "", "animation normalization cannot safely retain top-level image or alpha chunks");
            return null;
        }

        List<WebpFrame> frames = new List<WebpFrame>();
        int index = 0;
        while (index < source.Count)
        {
            if (source[index].FourCc == WebpConstants.FourCcAnimationFrameString)
            {
                WebpFrame? frame = ParseAnimationFrame(result, source[index].Payload, header.Header.CanvasWidth, header.Header.CanvasHeight);
                if (frame == null)
                {
                    return null;
                }

                frames.Add(frame);
            }

            index = index + 1;
        }

        WebpRiffChunk? icc = FindOptionalChunk(source, WebpConstants.FourCcIccProfileString);
        WebpRiffChunk? exif = FindOptionalChunk(source, WebpConstants.FourCcExifString);
        WebpRiffChunk? xmp = FindOptionalChunk(source, WebpConstants.FourCcXmpString);
        WebpMetadata metadata = CreateMetadata(icc, exif, xmp);
        bool hasAlpha = WebpContainerWriter.HasAnimationAlpha(frames);
        List<WebpRiffChunk> output = new List<WebpRiffChunk>();
        output.Add(new WebpRiffChunk(WebpConstants.FourCcVp8XString, WebpContainerWriter.CreateVp8XPayload(header.Header.CanvasWidth, header.Header.CanvasHeight, metadata, hasAlpha, true)));
        AddOptionalChunk(output, icc);
        output.Add(new WebpRiffChunk(WebpConstants.FourCcAnimString, anim.Payload));
        index = 0;
        while (index < frames.Count)
        {
            output.Add(new WebpRiffChunk(WebpConstants.FourCcAnimationFrameString, WebpContainerWriter.CreateAnmfPayload(frames[index])));
            index = index + 1;
        }

        AddOptionalChunk(output, exif);
        AddOptionalChunk(output, xmp);
        AddUnknownChunks(output, source);
        return WebpUnknownChunkMux.WritePreservingOrder(WebpConstants.FourCcWebpString, output);
    }

    internal static WebpFrame? ParseAnimationFrame(WebpNormalizationResult result, byte[] payload, int canvasWidth, int canvasHeight)
    {
        if (payload.Length < WebpConstants.AnimFrameHeaderSize)
        {
            AddUnsafe(result, -1, WebpConstants.FourCcAnimationFrameString, "ANMF header is truncated");
            return null;
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

        int imageCount = 0;
        int alphaCount = 0;
        int offset = WebpConstants.AnimFrameHeaderSize;
        while (offset < payload.Length)
        {
            if (payload.Length - offset < WebpConstants.ChunkHeaderSize)
            {
                AddUnsafe(result, -1, WebpConstants.FourCcAnimationFrameString, "ANMF subchunk header is truncated");
                return null;
            }

            string fourCc = WebpRiffParser.ReadFourCc(payload, offset);
            uint length = WebpEndianReader.ReadUInt32(payload, offset + WebpConstants.FourCcByteCount);
            long payloadOffset = offset + WebpConstants.ChunkHeaderSize;
            long payloadEnd = payloadOffset + length;
            long paddedEnd = payloadEnd + (length & 1U);
            if (payloadEnd > payload.Length || (paddedEnd > payload.Length && payloadEnd != payload.Length))
            {
                AddUnsafe(result, -1, fourCc, "ANMF subchunk bounds cannot be repaired safely");
                return null;
            }

            byte[] subchunk = WebpEndianReader.ReadSlice(payload, (int)payloadOffset, (int)length).ToArray();
            if (fourCc == WebpConstants.FourCcAlphaString)
            {
                alphaCount = alphaCount + 1;
                frame.AlphaData = subchunk;
            }
            else if (fourCc == WebpConstants.FourCcVp8String || fourCc == WebpConstants.FourCcVp8LString)
            {
                imageCount = imageCount + 1;
                frame.EncodedBitstream = subchunk;
                frame.BitstreamKind = fourCc == WebpConstants.FourCcVp8String ? WebpBitstreamKind.Lossy : WebpBitstreamKind.Lossless;
            }
            else
            {
                if (WebpContainerWriter.IsKnownChunk(fourCc))
                {
                    AddUnsafe(result, -1, fourCc, "known top-level chunks cannot be normalized inside ANMF");
                    return null;
                }

                frame.UnknownChunks.Add(new WebpUnknownChunk(fourCc, subchunk));
            }

            offset = paddedEnd > payload.Length ? payload.Length : (int)paddedEnd;
        }

        if (imageCount != 1 || alphaCount > 1 || frame.EncodedBitstream == null)
        {
            AddUnsafe(result, -1, WebpConstants.FourCcAnimationFrameString, "ANMF normalization requires one image and at most one alpha subchunk");
            return null;
        }

        WebpBasicContainerParseResult parsed = ParseImagePayload(frame.EncodedBitstream, frame.BitstreamKind);
        if (parsed.HasErrors() || parsed.Width != frame.Width || parsed.Height != frame.Height)
        {
            AddUnsafe(result, -1, WebpConstants.FourCcAnimationFrameString, "ANMF image dimensions or bitstream are invalid");
            return null;
        }

        if (frame.AlphaData != null && (frame.AlphaData.Length == 0 || frame.BitstreamKind != WebpBitstreamKind.Lossy))
        {
            AddUnsafe(result, -1, WebpConstants.FourCcAlphaString, "ANMF alpha is incompatible with its image bitstream");
            return null;
        }

        long right = (long)frame.X + frame.Width;
        long bottom = (long)frame.Y + frame.Height;
        if (right > canvasWidth || bottom > canvasHeight)
        {
            AddUnsafe(result, -1, WebpConstants.FourCcAnimationFrameString, "ANMF rectangle exceeds the animation canvas");
            return null;
        }

        return frame;
    }

    internal static WebpBasicContainerParseResult ParseImagePayload(byte[] payload, WebpBitstreamKind kind)
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk(WebpContainerWriter.GetImageFourCc(kind), payload));
        byte[] data = WebpRiffWriter.Write(WebpConstants.FourCcWebpString, chunks);
        return WebpBasicContainerParser.Parse(data, WebpStrictness.Strict);
    }

    internal static int GetChunkCount(List<WebpRiffChunk> chunks, string fourCc)
    {
        int count = 0;
        int index = 0;
        while (index < chunks.Count)
        {
            if (chunks[index].FourCc == fourCc)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static WebpRiffChunk FindChunk(List<WebpRiffChunk> chunks, string fourCc)
    {
        int index = 0;
        while (index < chunks.Count)
        {
            if (chunks[index].FourCc == fourCc)
            {
                return chunks[index];
            }

            index = index + 1;
        }

        throw new InvalidOperationException("required chunk is missing");
    }

    internal static WebpRiffChunk? FindOptionalChunk(List<WebpRiffChunk> chunks, string fourCc)
    {
        int index = 0;
        while (index < chunks.Count)
        {
            if (chunks[index].FourCc == fourCc)
            {
                return chunks[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static WebpMetadata CreateMetadata(WebpRiffChunk? icc, WebpRiffChunk? exif, WebpRiffChunk? xmp)
    {
        WebpMetadata metadata = new WebpMetadata();
        if (icc != null)
        {
            metadata.IccProfile = icc.Payload;
        }

        if (exif != null)
        {
            metadata.Exif = exif.Payload;
        }

        if (xmp != null)
        {
            metadata.Xmp = xmp.Payload;
        }

        return metadata;
    }

    internal static void AddOptionalChunk(List<WebpRiffChunk> output, WebpRiffChunk? chunk)
    {
        if (chunk != null)
        {
            output.Add(new WebpRiffChunk(chunk.FourCc, chunk.Payload));
        }
    }

    internal static void AddUnknownChunks(List<WebpRiffChunk> output, List<WebpRiffChunk> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            if (!WebpUnknownChunkMux.IsKnownWebpChunk(source[index].FourCc))
            {
                output.Add(new WebpRiffChunk(source[index].FourCc, source[index].Payload));
            }

            index = index + 1;
        }
    }

    internal static void CopyUnsafeDiagnostics(WebpNormalizationResult result, List<WebpDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            WebpDiagnostic diagnostic = diagnostics[index];
            if (diagnostic.Severity == WebpDiagnosticSeverity.Error)
            {
                AddUnsafe(result, diagnostic.Offset, diagnostic.FourCc, diagnostic.Message);
            }

            index = index + 1;
        }
    }

    internal static bool BytesEqual(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void AddUnsafe(WebpNormalizationResult result, long offset, string fourCc, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(WebpDiagnosticSeverity.Error, offset, fourCc, message));
    }
}
