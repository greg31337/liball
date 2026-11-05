using System;
using System.Collections.Generic;

namespace webp;

/// <summary>Reads and rewrites WebP metadata and animation container data without recompressing image bitstreams.</summary>
/// <remarks>Returned files are newly allocated RIFF containers. Image payload bytes are preserved while affected headers and chunk ordering are rebuilt.</remarks>
internal static class WebpMux
{
    /// <summary>Extracts copies of the raw ICC, Exif, and XMP payloads from a WebP file.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>A metadata object whose absent payloads are null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>data</c> is not a valid WebP container.</exception>
    internal static WebpMetadata ExtractMetadata(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return WebpMetadataMux.Extract(data);
    }

    /// <summary>Gets copies of the raw ICC, Exif, and XMP payloads from a WebP file.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>A metadata object whose absent payloads are null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>data</c> is not a valid WebP container.</exception>
    internal static WebpMetadata GetMetadata(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return GetMetadataSpan(data.AsSpan());
    }

    internal static WebpMetadata GetMetadataSpan(ReadOnlySpan<byte> data)
    {
        return WebpMetadataMux.ExtractSpan(data);
    }

    /// <summary>Replaces all ICC, Exif, and XMP chunks with the non-null payloads in a metadata object.</summary>
    /// <remarks>Null metadata fields remove the corresponding chunks. A basic still container is promoted to VP8X when metadata is added.</remarks>
    /// <param name="data">The complete source RIFF WebP file.</param>
    /// <param name="metadata">The complete desired metadata state.</param>
    /// <returns>A newly allocated WebP file with updated metadata flags and chunks.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> or <c>metadata</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source is invalid or cannot be represented as a metadata-bearing WebP container.</exception>
    internal static byte[] ReplaceMetadata(byte[] data, WebpMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(metadata);
        return ReplaceMetadataSpan(data.AsSpan(), metadata);
    }

    internal static byte[] ReplaceMetadataSpan(ReadOnlySpan<byte> data, WebpMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return WebpMetadataMux.ReplaceSpan(data, metadata);
    }

    /// <summary>Removes ICC, Exif, and XMP chunks and clears their VP8X feature flags.</summary>
    /// <param name="data">The complete source RIFF WebP file.</param>
    /// <returns>A newly allocated WebP file without metadata chunks.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>data</c> is not a valid WebP container.</exception>
    internal static byte[] StripMetadata(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return StripMetadataSpan(data.AsSpan());
    }

    internal static byte[] StripMetadataSpan(ReadOnlySpan<byte> data)
    {
        return WebpMetadataMux.StripSpan(data);
    }

    /// <summary>Sets the ICC profile while preserving existing Exif and XMP payloads.</summary>
    /// <param name="data">The complete source RIFF WebP file.</param>
    /// <param name="iccProfile">The raw ICC payload to copy, or null to remove the profile.</param>
    /// <returns>A newly allocated WebP file with the requested ICC state.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>data</c> is not a valid WebP container.</exception>
    internal static byte[] SetIccProfile(byte[] data, byte[]? iccProfile)
    {
        ArgumentNullException.ThrowIfNull(data);
        return SetIccProfileSpan(data.AsSpan(), iccProfile);
    }

    internal static byte[] SetIccProfileSpan(ReadOnlySpan<byte> data, byte[]? iccProfile)
    {
        WebpMetadataMux.ValidateOptionalMetadataPayload(iccProfile, "iccProfile");
        WebpMetadata metadata = WebpMetadataMux.ExtractSpan(data);
        metadata.IccProfile = CopyNullableBytes(iccProfile);
        return WebpMetadataMux.ReplaceSpan(data, metadata);
    }

    /// <summary>Sets the Exif payload while preserving existing ICC and XMP payloads.</summary>
    /// <param name="data">The complete source RIFF WebP file.</param>
    /// <param name="exif">The raw Exif payload to copy, or null to remove it.</param>
    /// <returns>A newly allocated WebP file with the requested Exif state.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>data</c> is not a valid WebP container.</exception>
    internal static byte[] SetExif(byte[] data, byte[]? exif)
    {
        ArgumentNullException.ThrowIfNull(data);
        return SetExifSpan(data.AsSpan(), exif);
    }

    internal static byte[] SetExifSpan(ReadOnlySpan<byte> data, byte[]? exif)
    {
        WebpMetadataMux.ValidateOptionalMetadataPayload(exif, "exif");
        WebpMetadata metadata = WebpMetadataMux.ExtractSpan(data);
        metadata.Exif = CopyNullableBytes(exif);
        return WebpMetadataMux.ReplaceSpan(data, metadata);
    }

    /// <summary>Sets the XMP payload while preserving existing ICC and Exif payloads.</summary>
    /// <param name="data">The complete source RIFF WebP file.</param>
    /// <param name="xmp">The raw XMP payload to copy, or null to remove it.</param>
    /// <returns>A newly allocated WebP file with the requested XMP state.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>data</c> is not a valid WebP container.</exception>
    internal static byte[] SetXmp(byte[] data, byte[]? xmp)
    {
        ArgumentNullException.ThrowIfNull(data);
        return SetXmpSpan(data.AsSpan(), xmp);
    }

    internal static byte[] SetXmpSpan(ReadOnlySpan<byte> data, byte[]? xmp)
    {
        WebpMetadataMux.ValidateOptionalMetadataPayload(xmp, "xmp");
        WebpMetadata metadata = WebpMetadataMux.ExtractSpan(data);
        metadata.Xmp = CopyNullableBytes(xmp);
        return WebpMetadataMux.ReplaceSpan(data, metadata);
    }

    internal static List<WebpUnknownChunk> EnumerateUnknownChunks(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return WebpUnknownChunkMux.EnumerateSpan(data);
    }

    internal static List<WebpUnknownChunk> EnumerateUnknownChunksSpan(ReadOnlySpan<byte> data)
    {
        return WebpUnknownChunkMux.EnumerateSpan(data);
    }

    internal static byte[] ReplaceUnknownChunks(byte[] data, List<WebpUnknownChunk> unknownChunks, WebpUnknownChunkPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(data);
        return WebpUnknownChunkMux.ReplaceSpan(data, unknownChunks, placement);
    }

    internal static byte[] ReplaceUnknownChunksSpan(ReadOnlySpan<byte> data, List<WebpUnknownChunk> unknownChunks, WebpUnknownChunkPlacement placement)
    {
        return WebpUnknownChunkMux.ReplaceSpan(data, unknownChunks, placement);
    }

    internal static byte[] StripUnknownChunks(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return WebpUnknownChunkMux.StripSpan(data);
    }

    internal static byte[] StripUnknownChunksSpan(ReadOnlySpan<byte> data)
    {
        return WebpUnknownChunkMux.StripSpan(data);
    }

    /// <summary>Extracts every animation frame as geometry, timing, encoded image payload, alpha data, and preserved subchunks.</summary>
    /// <remarks>Pixel buffers are not decoded. Returned payload arrays are copies and may be changed independently of the input file.</remarks>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <returns>The frames in playback order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source is invalid or is not an animation.</exception>
    internal static List<WebpFrame> ExtractFrames(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return ExtractFramesSpan(data.AsSpan());
    }

    internal static List<WebpFrame> ExtractFramesSpan(ReadOnlySpan<byte> data)
    {
        WebpExtendedContainerParseResult parsed = RequireAnimatedWebpSpan(data);
        List<WebpFrame> frames = new List<WebpFrame>();
        int index = 0;
        while (index < parsed.Frames.Count)
        {
            frames.Add(CopyFrame(parsed.Frames[index]));
            index = index + 1;
        }

        return frames;
    }

    /// <summary>Extracts one animation frame without decoding its pixels.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>A copied frame containing geometry, timing, and encoded payload data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source is invalid or is not an animation.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>frameIndex</c> does not identify an existing frame.</exception>
    internal static WebpFrame ExtractFrame(byte[] data, int frameIndex)
    {
        ArgumentNullException.ThrowIfNull(data);
        return ExtractFrameSpan(data.AsSpan(), frameIndex);
    }

    internal static WebpFrame ExtractFrameSpan(ReadOnlySpan<byte> data, int frameIndex)
    {
        ValidateNonNegativeFrameIndex(frameIndex);
        WebpExtendedContainerParseResult parsed = RequireAnimatedWebpSpan(data);
        if (frameIndex >= parsed.Frames.Count)
        {
            throw new ArgumentOutOfRangeException("frameIndex");
        }

        return CopyFrame(parsed.Frames[frameIndex]);
    }

    internal static byte[] InsertAnimationFrame(byte[] data, int frameIndex, WebpFrame frame)
    {
        ArgumentNullException.ThrowIfNull(data);
        return InsertAnimationFrameSpan(data, frameIndex, frame);
    }

    internal static byte[] InsertAnimationFrameSpan(ReadOnlySpan<byte> data, int frameIndex, WebpFrame frame)
    {
        ValidateNonNegativeFrameIndex(frameIndex);
        ArgumentNullException.ThrowIfNull(frame);
        WebpExtendedContainerParseResult parsed = RequireAnimatedWebpSpan(data);
        if (frameIndex > parsed.Frames.Count)
        {
            throw new ArgumentOutOfRangeException("frameIndex");
        }

        ValidateEncodedAnimationFrame(frame, parsed.Vp8X.CanvasWidth, parsed.Vp8X.CanvasHeight, frameIndex);
        List<WebpFrame> frames = CopyFrames(parsed.Frames);
        frames.Insert(frameIndex, CopyFrame(frame));
        byte[] framePayload = WebpContainerWriter.CreateAnmfPayload(frame);
        return RewriteAnimationFrames(data, frames, frameIndex, framePayload, true, false);
    }

    internal static byte[] ReplaceAnimationFrame(byte[] data, int frameIndex, WebpFrame frame)
    {
        ArgumentNullException.ThrowIfNull(data);
        return ReplaceAnimationFrameSpan(data, frameIndex, frame);
    }

    internal static byte[] ReplaceAnimationFrameSpan(ReadOnlySpan<byte> data, int frameIndex, WebpFrame frame)
    {
        ValidateNonNegativeFrameIndex(frameIndex);
        ArgumentNullException.ThrowIfNull(frame);
        WebpExtendedContainerParseResult parsed = RequireAnimatedWebpSpan(data);
        ValidateExistingFrameIndex(frameIndex, parsed.Frames.Count);
        ValidateEncodedAnimationFrame(frame, parsed.Vp8X.CanvasWidth, parsed.Vp8X.CanvasHeight, frameIndex);
        List<WebpFrame> frames = CopyFrames(parsed.Frames);
        frames[frameIndex] = CopyFrame(frame);
        byte[] framePayload = WebpContainerWriter.CreateAnmfPayload(frame);
        return RewriteAnimationFrames(data, frames, frameIndex, framePayload, false, false);
    }

    internal static byte[] RemoveAnimationFrame(byte[] data, int frameIndex)
    {
        ArgumentNullException.ThrowIfNull(data);
        return RemoveAnimationFrameSpan(data, frameIndex);
    }

    internal static byte[] RemoveAnimationFrameSpan(ReadOnlySpan<byte> data, int frameIndex)
    {
        ValidateNonNegativeFrameIndex(frameIndex);
        WebpExtendedContainerParseResult parsed = RequireAnimatedWebpSpan(data);
        ValidateExistingFrameIndex(frameIndex, parsed.Frames.Count);
        if (parsed.Frames.Count == 1)
        {
            throw new ArgumentException("animation must retain at least one frame", "data");
        }

        List<WebpFrame> frames = CopyFrames(parsed.Frames);
        frames.RemoveAt(frameIndex);
        return RewriteAnimationFrames(data, frames, frameIndex, Array.Empty<byte>(), false, true);
    }

    internal static void ValidateExistingFrameIndex(int frameIndex, int frameCount)
    {
        if (frameIndex < 0 || frameIndex >= frameCount)
        {
            throw new ArgumentOutOfRangeException("frameIndex");
        }
    }

    internal static void ValidateNonNegativeFrameIndex(int frameIndex)
    {
        if (frameIndex < 0)
        {
            throw new ArgumentOutOfRangeException("frameIndex");
        }
    }

    internal static void ValidateEncodedAnimationFrame(WebpFrame frame, int canvasWidth, int canvasHeight, int frameIndex)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.BlendMode != WebpFrameBlendMode.AlphaBlend && frame.BlendMode != WebpFrameBlendMode.Replace)
        {
            throw new ArgumentOutOfRangeException("BlendMode");
        }

        if (frame.DisposeMode != WebpFrameDisposeMode.DoNotDispose && frame.DisposeMode != WebpFrameDisposeMode.DisposeToBackground)
        {
            throw new ArgumentOutOfRangeException("DisposeMode");
        }

        WebpContainerWriter.ValidateAnimationFrame(frame, canvasWidth, canvasHeight, frameIndex);
    }

    internal static List<WebpFrame> CopyFrames(List<WebpFrame> source)
    {
        List<WebpFrame> frames = new List<WebpFrame>();
        int index = 0;
        while (index < source.Count)
        {
            frames.Add(CopyFrame(source[index]));
            index = index + 1;
        }

        return frames;
    }

    internal static byte[] RewriteAnimationFrames(ReadOnlySpan<byte> data, List<WebpFrame> frames, int frameIndex, byte[] framePayload, bool insertFrame, bool removeFrame)
    {
        bool hasAlpha = WebpContainerWriter.HasAnimationAlpha(frames);
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        int frameOccurrence = 0;
        bool inserted = false;
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            ReadOnlySpan<byte> sourcePayload = WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength);
            if (chunk.FourCc == WebpConstants.FourCcAnimationFrameString)
            {
                if (insertFrame && frameOccurrence == frameIndex)
                {
                    chunks.Add(new WebpRiffChunk(WebpConstants.FourCcAnimationFrameString, framePayload));
                    inserted = true;
                }

                if (!removeFrame || frameOccurrence != frameIndex)
                {
                    if (!insertFrame && frameOccurrence == frameIndex)
                    {
                        chunks.Add(new WebpRiffChunk(chunk.FourCc, framePayload));
                    }
                    else
                    {
                        chunks.Add(new WebpRiffChunk(chunk.FourCc, sourcePayload.ToArray()));
                    }
                }

                frameOccurrence = frameOccurrence + 1;
                if (insertFrame && !inserted && frameOccurrence == frameIndex && frameIndex == frames.Count - 1)
                {
                    chunks.Add(new WebpRiffChunk(WebpConstants.FourCcAnimationFrameString, framePayload));
                    inserted = true;
                }
            }
            else if (chunk.FourCc == WebpConstants.FourCcVp8XString)
            {
                chunks.Add(new WebpRiffChunk(chunk.FourCc, UpdateVp8XAlphaFlag(sourcePayload, hasAlpha)));
            }
            else
            {
                chunks.Add(new WebpRiffChunk(chunk.FourCc, sourcePayload.ToArray()));
            }

            index = index + 1;
        }

        if (insertFrame && !inserted)
        {
            chunks.Add(new WebpRiffChunk(WebpConstants.FourCcAnimationFrameString, framePayload));
        }

        byte[] rewritten = WebpUnknownChunkMux.WritePreservingOrder(WebpConstants.FourCcWebpString, chunks);
        RequireAnimatedWebp(rewritten);
        return rewritten;
    }

    internal static byte[] UpdateVp8XAlphaFlag(ReadOnlySpan<byte> payload, bool hasAlpha)
    {
        if (payload.Length != WebpConstants.Vp8XChunkPayloadSize)
        {
            throw new ArgumentException("VP8X payload must be exactly 10 bytes", "data");
        }

        byte[] updated = payload.ToArray();
        if (hasAlpha)
        {
            updated[0] = (byte)(updated[0] | WebpConstants.Vp8XFeatureAlpha);
        }
        else
        {
            updated[0] = (byte)(updated[0] & 0xEF);
        }

        return updated;
    }

    /// <summary>Changes an animation's loop count without recompressing its frames.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="loopCount">The repetition count, where <c>0</c> means repeat forever.</param>
    /// <returns>A newly allocated WebP file with the updated ANIM header.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source is invalid or is not an animation.</exception>
    internal static byte[] SetAnimationLoopCount(byte[] data, ushort loopCount)
    {
        ArgumentNullException.ThrowIfNull(data);
        return SetAnimationLoopCountSpan(data.AsSpan(), loopCount);
    }

    internal static byte[] SetAnimationLoopCountSpan(ReadOnlySpan<byte> data, ushort loopCount)
    {
        return RewriteAnimationHeaderSpan(data, loopCount, 0, true, false);
    }

    /// <summary>Changes an animation's background color without recompressing its frames.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file with the updated ANIM header.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source is invalid or is not an animation.</exception>
    internal static byte[] SetAnimationBackgroundColor(byte[] data, uint backgroundColorBgra)
    {
        ArgumentNullException.ThrowIfNull(data);
        return SetAnimationBackgroundColorSpan(data.AsSpan(), backgroundColorBgra);
    }

    internal static byte[] SetAnimationBackgroundColorSpan(ReadOnlySpan<byte> data, uint backgroundColorBgra)
    {
        return RewriteAnimationHeaderSpan(data, 0, backgroundColorBgra, false, true);
    }

    /// <summary>Changes an animation's loop count and background color in one container rewrite.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="loopCount">The repetition count, where <c>0</c> means repeat forever.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file with the updated ANIM header.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source is invalid or is not an animation.</exception>
    internal static byte[] SetAnimationLoopAndBackground(byte[] data, ushort loopCount, uint backgroundColorBgra)
    {
        ArgumentNullException.ThrowIfNull(data);
        return SetAnimationLoopAndBackgroundSpan(data.AsSpan(), loopCount, backgroundColorBgra);
    }

    internal static byte[] SetAnimationLoopAndBackgroundSpan(ReadOnlySpan<byte> data, ushort loopCount, uint backgroundColorBgra)
    {
        return RewriteAnimationHeaderSpan(data, loopCount, backgroundColorBgra, true, true);
    }

    internal static byte[]? CopyNullableBytes(byte[]? data)
    {
        if (data == null)
        {
            return null;
        }

        return WebpUnknownChunk.CopyBytes(data);
    }

    internal static WebpExtendedContainerParseResult RequireAnimatedWebp(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return RequireAnimatedWebpSpan(data.AsSpan());
    }

    internal static WebpExtendedContainerParseResult RequireAnimatedWebpSpan(ReadOnlySpan<byte> data)
    {
        WebpValidationResult validation = WebpValidator.Validate(data);
        if (validation.HasErrors())
        {
            throw new ArgumentException("input is not a valid animated WebP container", "data");
        }

        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);
        if (parsed.HasErrors() || !parsed.Vp8X.HasAnimation || parsed.Frames.Count == 0)
        {
            throw new ArgumentException("input is not an animated WebP container", "data");
        }

        return parsed;
    }

    internal static byte[] RewriteAnimationHeader(byte[] data, ushort loopCount, uint backgroundColorBgra, bool replaceLoopCount, bool replaceBackgroundColor)
    {
        ArgumentNullException.ThrowIfNull(data);
        return RewriteAnimationHeaderSpan(data.AsSpan(), loopCount, backgroundColorBgra, replaceLoopCount, replaceBackgroundColor);
    }

    internal static byte[] RewriteAnimationHeaderSpan(ReadOnlySpan<byte> data, ushort loopCount, uint backgroundColorBgra, bool replaceLoopCount, bool replaceBackgroundColor)
    {
        RequireAnimatedWebpSpan(data);

        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            ReadOnlySpan<byte> sourcePayload = WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength);
            byte[] payload = sourcePayload.ToArray();
            if (chunk.FourCc == WebpConstants.FourCcAnimString)
            {
                RewriteAnimPayload(payload, loopCount, backgroundColorBgra, replaceLoopCount, replaceBackgroundColor);
            }

            chunks.Add(new WebpRiffChunk(chunk.FourCc, payload));
            index = index + 1;
        }

        byte[] rewritten = WebpRiffWriter.Write(WebpConstants.FourCcWebpString, chunks);
        RequireAnimatedWebp(rewritten);
        return rewritten;
    }

    internal static void RewriteAnimPayload(byte[] payload, ushort loopCount, uint backgroundColorBgra, bool replaceLoopCount, bool replaceBackgroundColor)
    {
        if (payload.Length != WebpConstants.AnimChunkPayloadSize)
        {
            throw new ArgumentException("ANIM payload must be exactly 6 bytes", "data");
        }

        if (replaceBackgroundColor)
        {
            WebpEndianWriter.WriteUInt32(payload, 0, backgroundColorBgra);
        }

        if (replaceLoopCount)
        {
            WebpEndianWriter.WriteUInt16(payload, 4, loopCount);
        }
    }

    internal static WebpFrame CopyFrame(WebpFrame source)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = source.X;
        frame.Y = source.Y;
        frame.Width = source.Width;
        frame.Height = source.Height;
        frame.DurationMilliseconds = source.DurationMilliseconds;
        frame.BlendMode = source.BlendMode;
        frame.DisposeMode = source.DisposeMode;
        frame.BitstreamKind = source.BitstreamKind;
        frame.EncodedBitstream = CopyNullableBytes(source.EncodedBitstream);
        frame.AlphaData = CopyNullableBytes(source.AlphaData);
        CopyUnknownChunks(frame, source);
        return frame;
    }

    internal static void CopyUnknownChunks(WebpFrame target, WebpFrame source)
    {
        int index = 0;
        while (index < source.UnknownChunks.Count)
        {
            WebpUnknownChunk unknown = source.UnknownChunks[index];
            target.UnknownChunks.Add(new WebpUnknownChunk(unknown.FourCc, unknown.Data));
            index = index + 1;
        }
    }
}
