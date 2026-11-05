using System;
using System.Collections.Generic;
using System.IO;

namespace webp;

/// <summary>Decodes WebP containers into RGBA still images or animation frame models.</summary>
/// <remarks>Malformed file content is normally reported through <see cref="WebpDecodeResult.Diagnostics"/>. Argument, stream, path, and invalid option problems are reported as exceptions.</remarks>
internal static class WebpDecoder
{
    /// <summary>Decodes a WebP byte array with default options.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>The decoded image and any format diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    internal static WebpDecodeResult Decode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Decode(data.AsSpan(), new WebpDecodeOptions());
    }

    /// <summary>Decodes a WebP byte array using the supplied frame, metadata, strictness, and security settings.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="options">The settings that control parsing and decoded output.</param>
    /// <returns>The decoded image and any format diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c>, <c>options</c>, or <c>options.Limits</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a security limit is not positive or a frame selection is outside the available frames.</exception>
    internal static WebpDecodeResult Decode(byte[] data, WebpDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Decode(data.AsSpan(), options);
    }

    /// <summary>Decodes WebP data from a read-only span with default options.</summary>
    /// <param name="data">A span containing the complete RIFF WebP file.</param>
    /// <returns>The decoded image and any format diagnostics.</returns>
    internal static WebpDecodeResult Decode(ReadOnlySpan<byte> data)
    {
        return Decode(data, new WebpDecodeOptions());
    }

    /// <summary>Reads and decodes a WebP file from disk with default options.</summary>
    /// <param name="path">The path of the file to read.</param>
    /// <returns>The decoded image and any format diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>path</c> is null.</exception>
    /// <exception cref="IOException">Thrown when the file cannot be read.</exception>
    internal static WebpDecodeResult Decode(string path)
    {
        return Decode(path, new WebpDecodeOptions());
    }

    /// <summary>Reads and decodes a WebP file from disk using the supplied options.</summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="options">The settings that control parsing and decoded output.</param>
    /// <returns>The decoded image and any format diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>path</c>, <c>options</c>, or <c>options.Limits</c> is null.</exception>
    /// <exception cref="IOException">Thrown when the file cannot be read.</exception>
    internal static WebpDecodeResult Decode(string path, WebpDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(path);
        WebpDecodeTransformProcessor.ValidateOptions(options);
        return Decode(WebpInputReader.ReadFileWithLimit(path, WebpSecurityLimitValidator.GetInputByteLimit(options.Limits)), options);
    }

    /// <summary>Reads the remaining bytes from a stream and decodes them with default options.</summary>
    /// <remarks>Reading starts at the stream's current position. The stream remains open after decoding.</remarks>
    /// <param name="stream">A readable stream positioned at the start of a RIFF WebP file.</param>
    /// <returns>The decoded image and any format diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>stream</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>stream</c> is not readable.</exception>
    /// <exception cref="IOException">Thrown when bytes cannot be read from the stream.</exception>
    internal static WebpDecodeResult Decode(Stream stream)
    {
        return Decode(stream, new WebpDecodeOptions());
    }

    /// <summary>Reads the remaining bytes from a stream and decodes them using the supplied options.</summary>
    /// <remarks>Reading starts at the stream's current position. Seekable and non-seekable streams are supported, and the stream remains open.</remarks>
    /// <param name="stream">A readable stream positioned at the start of a RIFF WebP file.</param>
    /// <param name="options">The settings that control parsing and decoded output.</param>
    /// <returns>The decoded image and any format diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>stream</c>, <c>options</c>, or <c>options.Limits</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>stream</c> is not readable.</exception>
    /// <exception cref="IOException">Thrown when bytes cannot be read from the stream.</exception>
    internal static WebpDecodeResult Decode(Stream stream, WebpDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        WebpDecodeTransformProcessor.ValidateOptions(options);
        return Decode(WebpInputReader.ReadStreamWithLimit(stream, WebpSecurityLimitValidator.GetInputByteLimit(options.Limits)), options);
    }

    /// <summary>Decodes WebP data from a read-only span using the supplied options.</summary>
    /// <remarks>Set <c>IdentifyOnly</c> to retain dimensions, frame metadata, and encoded frame payloads without allocating decoded pixel buffers.</remarks>
    /// <param name="data">A span containing the complete RIFF WebP file.</param>
    /// <param name="options">The settings that control parsing and decoded output.</param>
    /// <returns>The decoded image and any format diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>options</c> or <c>options.Limits</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a security limit is not positive or a frame selection is outside the available frames.</exception>
    internal static WebpDecodeResult Decode(ReadOnlySpan<byte> data, WebpDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);
        ValidateSecurityLimits(options.Limits);
        WebpDecodeTransformProcessor.ValidateOptions(options);
        WebpDecodeResult limitResult = new WebpDecodeResult();
        ValidateInputRiffSize(limitResult, data.Length, options.Limits);
        string? retainedPayloadError = WebpSecurityLimitValidator.GetContainerLimitError(data, options.Limits);
        if (retainedPayloadError != null)
        {
            AddDiagnostic(limitResult, "RIFF", retainedPayloadError);
        }

        if (limitResult.HasErrors())
        {
            Finish(limitResult);
            return limitResult;
        }

        if (HasVp8XChunk(data))
        {
            return DecodeExtendedStill(data, options);
        }

        return DecodeBasicStill(data, options);
    }

    internal static byte[] ReadAllBytes(Stream stream)
    {
        return WebpInputReader.ReadStream(stream);
    }

    internal static byte[] ReadSeekableStream(Stream stream)
    {
        return WebpInputReader.ReadSeekableStream(stream);
    }

    internal static byte[] ReadNonSeekableStream(Stream stream)
    {
        return WebpInputReader.ReadNonSeekableStream(stream);
    }

    internal static WebpDecodeResult DecodeBasicStill(ReadOnlySpan<byte> data, WebpDecodeOptions options)
    {
        WebpPublicModelValidator.ValidateStillFrameSelection(options);
        WebpDecodeResult result = new WebpDecodeResult();
        WebpBasicContainerParseResult parsed = WebpBasicContainerParser.Parse(data, options.Strictness);
        CopyBasicDiagnostics(result, parsed);
        if (!parsed.IsValid)
        {
            Finish(result);
            return result;
        }

        ValidateBasicSecurityLimits(result, parsed, options);
        if (result.HasErrors())
        {
            Finish(result);
            return result;
        }

        WebpFrame frame = NewStillFrame(parsed.Width, parsed.Height, parsed.BitstreamKind, parsed.ImagePayload);
        if (!options.IdentifyOnly)
        {
            WebpPixelBuffer? pixels = DecodeImagePayload(result, parsed.ImagePayload, parsed.BitstreamKind, options.Strictness, options.Limits.MaxRecursionDepth);
            frame.Pixels = pixels;
        }

        result.Image.CanvasWidth = parsed.Width;
        result.Image.CanvasHeight = parsed.Height;
        result.Image.HasAlpha = parsed.HasAlpha;
        if (options.UnknownChunkPolicy == WebpUnknownChunkPolicy.Preserve)
        {
            CopyBasicUnknownChunks(result.Image, parsed, data);
        }

        result.Image.Frames.Add(frame);
        if (!result.HasErrors())
        {
            WebpDecodeTransformProcessor.ApplyStill(result, options, parsed.Width, parsed.Height);
        }

        Finish(result);
        return result;
    }

    internal static WebpDecodeResult DecodeExtendedStill(ReadOnlySpan<byte> data, WebpDecodeOptions options)
    {
        WebpDecodeResult result = new WebpDecodeResult();
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, options.Strictness);
        CopyExtendedDiagnostics(result, parsed);
        if (!parsed.IsValid)
        {
            Finish(result);
            return result;
        }

        ValidateExtendedSecurityLimits(result, parsed, options);
        if (result.HasErrors())
        {
            Finish(result);
            return result;
        }

        if (parsed.Vp8X.HasAnimation || parsed.Frames.Count != 0)
        {
            DecodeAnimatedFrames(result, parsed, options);
            Finish(result);
            return result;
        }

        WebpPublicModelValidator.ValidateStillFrameSelection(options);

        if (parsed.ImagePayload == null)
        {
            AddDiagnostic(result, "", "extended still container is missing an image payload");
            Finish(result);
            return result;
        }

        int width = parsed.Vp8X.CanvasWidth;
        int height = parsed.Vp8X.CanvasHeight;
        WebpFrame frame = NewStillFrame(width, height, parsed.BitstreamKind, parsed.ImagePayload);
        if (!options.IdentifyOnly)
        {
            WebpPixelBuffer? pixels = DecodeImagePayload(result, parsed.ImagePayload, parsed.BitstreamKind, options.Strictness, options.Limits.MaxRecursionDepth);
            if (pixels != null && parsed.AlphaData != null && parsed.BitstreamKind == WebpBitstreamKind.Lossy)
            {
                ApplyAlpha(result, pixels, parsed.AlphaData, options.Strictness, options.Limits.MaxRecursionDepth);
            }

            frame.Pixels = pixels;
        }

        result.Image.CanvasWidth = width;
        result.Image.CanvasHeight = height;
        result.Image.HasAlpha = parsed.Vp8X.HasAlpha
            || parsed.AlphaData != null
            || WebpInfoReader.HasLosslessBitstreamAlpha(parsed.ImagePayload, parsed.BitstreamKind);
        if (options.DecodeMetadata)
        {
            result.Image.Metadata = CopyMetadata(parsed.Metadata);
        }

        if (options.UnknownChunkPolicy == WebpUnknownChunkPolicy.Preserve)
        {
            CopyUnknownChunks(result.Image, parsed);
        }

        result.Image.Frames.Add(frame);
        if (!result.HasErrors())
        {
            WebpDecodeTransformProcessor.ApplyStill(result, options, width, height);
        }

        Finish(result);
        return result;
    }

    internal static void DecodeAnimatedFrames(WebpDecodeResult result, WebpExtendedContainerParseResult parsed, WebpDecodeOptions options)
    {
        if (options.AnimationDecodeMode != WebpAnimationDecodeMode.IndependentFrames && options.AnimationDecodeMode != WebpAnimationDecodeMode.CompositedFrames)
        {
            AddDiagnostic(result, WebpConstants.FourCcAnimString, "unsupported animated WebP decode mode");
            return;
        }

        result.Image.CanvasWidth = parsed.Vp8X.CanvasWidth;
        result.Image.CanvasHeight = parsed.Vp8X.CanvasHeight;
        result.Image.HasAlpha = parsed.Vp8X.HasAlpha || HasFrameAlpha(parsed);
        List<WebpFrame> selectedFrames = WebpDecodeFrameSelector.SelectFrames(parsed.Frames, options);
        ValidateSelectedAnimationPixelLimits(result, selectedFrames, parsed.Vp8X.CanvasWidth, parsed.Vp8X.CanvasHeight, options);
        if (result.HasErrors())
        {
            return;
        }

        result.Image.HasAnimation = selectedFrames.Count > 1;
        result.Image.LoopCount = parsed.AnimationLoopCount;
        result.Image.BackgroundColorBgra = parsed.AnimationBackgroundColorBgra;
        if (options.DecodeMetadata)
        {
            result.Image.Metadata = CopyMetadata(parsed.Metadata);
        }

        if (options.UnknownChunkPolicy == WebpUnknownChunkPolicy.Preserve)
        {
            CopyUnknownChunks(result.Image, parsed);
        }

        int index = 0;
        while (index < selectedFrames.Count)
        {
            result.Image.Frames.Add(DecodeAnimationFrame(result, selectedFrames[index], options));
            index = index + 1;
        }

        if (options.AnimationDecodeMode == WebpAnimationDecodeMode.CompositedFrames && !options.IdentifyOnly && !result.HasErrors())
        {
            ComposeDecodedAnimationFrames(result);
        }

        if (!result.HasErrors())
        {
            bool composited = options.AnimationDecodeMode == WebpAnimationDecodeMode.CompositedFrames;
            WebpDecodeTransformProcessor.ApplyAnimation(result, options, parsed.Vp8X.CanvasWidth, parsed.Vp8X.CanvasHeight, composited);
        }
    }

    internal static void ComposeDecodedAnimationFrames(WebpDecodeResult result)
    {
        List<WebpFrame> compositedFrames = WebpAnimationComposer.ComposeFrames(result.Image);
        result.Image.Frames.Clear();

        int index = 0;
        while (index < compositedFrames.Count)
        {
            result.Image.Frames.Add(compositedFrames[index]);
            index = index + 1;
        }
    }

    internal static WebpFrame DecodeAnimationFrame(WebpDecodeResult result, WebpFrame source, WebpDecodeOptions options)
    {
        WebpFrame frame = CopyFrameMetadata(source);
        if (source.EncodedBitstream != null)
        {
            frame.EncodedBitstream = WebpUnknownChunk.CopyBytes(source.EncodedBitstream);
        }

        if (!options.IdentifyOnly && source.EncodedBitstream != null)
        {
            WebpPixelBuffer? pixels = DecodeImagePayload(result, source.EncodedBitstream, source.BitstreamKind, options.Strictness, options.Limits.MaxRecursionDepth);
            if (pixels != null && source.AlphaData != null && source.BitstreamKind == WebpBitstreamKind.Lossy)
            {
                ApplyAlpha(result, pixels, source.AlphaData, options.Strictness, options.Limits.MaxRecursionDepth);
            }

            frame.Pixels = pixels;
            ValidateDecodedFrameDimensions(result, frame, pixels);
        }

        if (options.UnknownChunkPolicy == WebpUnknownChunkPolicy.Preserve)
        {
            CopyFrameUnknownChunks(frame, source);
        }

        return frame;
    }

    internal static WebpFrame CopyFrameMetadata(WebpFrame source)
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
        return frame;
    }

    internal static void ValidateDecodedFrameDimensions(WebpDecodeResult result, WebpFrame frame, WebpPixelBuffer? pixels)
    {
        if (pixels == null)
        {
            return;
        }

        if (pixels.Width != frame.Width || pixels.Height != frame.Height)
        {
            AddDiagnostic(result, WebpConstants.FourCcAnimationFrameString, "decoded animation frame dimensions do not match the ANMF rectangle");
        }
    }

    internal static WebpFrame NewStillFrame(int width, int height, WebpBitstreamKind bitstreamKind, byte[] encodedBitstream)
    {
        WebpFrame frame = new WebpFrame();
        frame.Width = width;
        frame.Height = height;
        frame.BitstreamKind = bitstreamKind;
        frame.EncodedBitstream = WebpUnknownChunk.CopyBytes(encodedBitstream);
        return frame;
    }

    internal static WebpPixelBuffer? DecodeImagePayload(WebpDecodeResult result, byte[] payload, WebpBitstreamKind bitstreamKind, WebpStrictness strictness, int maxRecursionDepth)
    {
        if (bitstreamKind == WebpBitstreamKind.Lossy)
        {
            WebpVp8DecodeResult decoded = WebpVp8Decoder.Decode(payload, strictness);
            CopyVp8Diagnostics(result, decoded);
            if (!decoded.IsValid)
            {
                return null;
            }

            return decoded.PixelBuffer;
        }

        if (bitstreamKind == WebpBitstreamKind.Lossless)
        {
            WebpVp8LDecodeResult decoded = WebpVp8LDecoder.DecodeWithLimit(payload, strictness, maxRecursionDepth);
            CopyVp8LDiagnostics(result, decoded);
            if (!decoded.IsValid)
            {
                return null;
            }

            return decoded.PixelBuffer;
        }

        AddDiagnostic(result, "", "unsupported WebP image bitstream kind");
        return null;
    }

    internal static void ApplyAlpha(WebpDecodeResult result, WebpPixelBuffer pixels, byte[] alphaData, WebpStrictness strictness, int maxRecursionDepth)
    {
        int nestedDepth = maxRecursionDepth - 1;
        WebpAlphaDecodeResult alpha = WebpAlphaDecoder.Decode(
            alphaData,
            pixels.Width,
            pixels.Height,
            strictness,
            (imageStream, width, height) => DecodeAlphaLosslessImageWithLimit(imageStream, width, height, nestedDepth));
        CopyAlphaDiagnostics(result, alpha);
        if (!alpha.IsValid)
        {
            return;
        }

        int y = 0;
        while (y < pixels.Height)
        {
            int x = 0;
            while (x < pixels.Width)
            {
                int pixelOffset = (y * pixels.Stride) + (x * 4);
                pixels.Rgba[pixelOffset + 3] = alpha.Alpha[(y * pixels.Width) + x];
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal static WebpPixelBuffer DecodeAlphaLosslessImageWithLimit(byte[] imageStream, int width, int height, int maxRecursionDepth)
    {
        WebpVp8LDecodeResult decoded = WebpVp8LDecoder.DecodeWithLimit(imageStream, WebpStrictness.Strict, maxRecursionDepth);
        if (decoded.PixelBuffer.Width != width || decoded.PixelBuffer.Height != height)
        {
            return new WebpPixelBuffer();
        }

        return decoded.PixelBuffer;
    }

    internal static bool HasVp8XChunk(ReadOnlySpan<byte> data)
    {
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        if (!riff.IsValid)
        {
            return false;
        }

        int index = 0;
        while (index < riff.Chunks.Count)
        {
            if (riff.Chunks[index].FourCc == WebpConstants.FourCcVp8XString)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static WebpMetadata CopyMetadata(WebpMetadata metadata)
    {
        WebpMetadata copy = new WebpMetadata();
        copy.IccProfile = CopyNullableBytes(metadata.IccProfile);
        copy.Exif = CopyNullableBytes(metadata.Exif);
        copy.Xmp = CopyNullableBytes(metadata.Xmp);
        return copy;
    }

    internal static byte[]? CopyNullableBytes(byte[]? source)
    {
        if (source == null)
        {
            return null;
        }

        return WebpUnknownChunk.CopyBytes(source);
    }

    internal static void CopyUnknownChunks(WebpImage image, WebpExtendedContainerParseResult parsed)
    {
        int index = 0;
        while (index < parsed.UnknownChunks.Count)
        {
            image.UnknownChunks.Add(new WebpUnknownChunk(parsed.UnknownChunks[index].FourCc, parsed.UnknownChunks[index].Data));
            index = index + 1;
        }
    }

    internal static void CopyBasicUnknownChunks(WebpImage image, WebpBasicContainerParseResult parsed, ReadOnlySpan<byte> data)
    {
        int index = 0;
        while (index < parsed.Chunks.Count)
        {
            WebpChunkInfo chunk = parsed.Chunks[index];
            if (!chunk.IsKnown)
            {
                byte[] payload = WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength).ToArray();
                image.UnknownChunks.Add(new WebpUnknownChunk(chunk.FourCc, payload));
            }

            index = index + 1;
        }
    }

    internal static void CopyFrameUnknownChunks(WebpFrame frame, WebpFrame source)
    {
        int index = 0;
        while (index < source.UnknownChunks.Count)
        {
            frame.UnknownChunks.Add(new WebpUnknownChunk(source.UnknownChunks[index].FourCc, source.UnknownChunks[index].Data));
            index = index + 1;
        }
    }

    internal static bool HasFrameAlpha(WebpExtendedContainerParseResult parsed)
    {
        int index = 0;
        while (index < parsed.Frames.Count)
        {
            WebpFrame frame = parsed.Frames[index];
            if (frame.AlphaData != null
                || WebpInfoReader.HasLosslessBitstreamAlpha(frame.EncodedBitstream, frame.BitstreamKind))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void ValidateSecurityLimits(WebpSecurityLimits limits)
    {
        if (limits.MaxCanvasWidth <= 0)
        {
            throw new ArgumentOutOfRangeException("MaxCanvasWidth");
        }

        if (limits.MaxCanvasHeight <= 0)
        {
            throw new ArgumentOutOfRangeException("MaxCanvasHeight");
        }

        if (limits.MaxCanvasPixels <= 0)
        {
            throw new ArgumentOutOfRangeException("MaxCanvasPixels");
        }

        if (limits.MaxTotalPixels <= 0)
        {
            throw new ArgumentOutOfRangeException("MaxTotalPixels");
        }

        if (limits.MaxRiffSize <= 0)
        {
            throw new ArgumentOutOfRangeException("MaxRiffSize");
        }

        if (limits.MaxChunkCount <= 0)
        {
            throw new ArgumentOutOfRangeException("MaxChunkCount");
        }

        if (limits.MaxFrameCount <= 0)
        {
            throw new ArgumentOutOfRangeException("MaxFrameCount");
        }

        if (limits.MaxRecursionDepth <= 0)
        {
            throw new ArgumentOutOfRangeException("MaxRecursionDepth");
        }

        WebpSecurityLimitValidator.ValidateLimits(limits);
    }

    internal static void ValidateInputRiffSize(WebpDecodeResult result, int dataLength, WebpSecurityLimits limits)
    {
        if (dataLength > limits.MaxRiffSize)
        {
            AddDiagnostic(result, "RIFF", "input RIFF size exceeds the configured security limit");
        }
    }

    internal static void ValidateBasicSecurityLimits(WebpDecodeResult result, WebpBasicContainerParseResult parsed, WebpDecodeOptions options)
    {
        WebpSecurityLimits limits = options.Limits;
        ValidateChunkCount(result, parsed.Chunks.Count, limits);
        ValidatePixelRectangle(result, parsed.Width, parsed.Height, limits, parsed.ImageChunk.FourCc, "basic image dimensions exceed configured security limits");
        ValidateTotalPixels(result, (long)parsed.Width * (long)parsed.Height, limits);
        if (options.IdentifyOnly)
        {
            ValidateDecodedAllocation(result, parsed.ImagePayload.Length, 0, 0, 0, limits);
        }
        else
        {
            ValidateDecodedAllocation(result, parsed.ImagePayload.Length, parsed.Width, parsed.Height, 1, limits);
        }
    }

    internal static void ValidateExtendedSecurityLimits(WebpDecodeResult result, WebpExtendedContainerParseResult parsed, WebpDecodeOptions options)
    {
        WebpSecurityLimits limits = options.Limits;
        ValidateChunkCount(result, CountExtendedChunks(parsed), limits);
        if (parsed.Frames.Count > limits.MaxFrameCount)
        {
            AddDiagnostic(result, WebpConstants.FourCcAnimationFrameString, "animation frame count exceeds the configured security limit");
        }

        if (parsed.HasVp8X)
        {
            ValidatePixelRectangle(result, parsed.Vp8X.CanvasWidth, parsed.Vp8X.CanvasHeight, limits, WebpConstants.FourCcVp8XString, "canvas dimensions exceed configured security limits");
        }

        long totalPixels = 0;
        if (parsed.Frames.Count == 0 && parsed.HasVp8X)
        {
            totalPixels = (long)parsed.Vp8X.CanvasWidth * (long)parsed.Vp8X.CanvasHeight;
        }

        int index = 0;
        while (index < parsed.Frames.Count)
        {
            WebpFrame frame = parsed.Frames[index];
            ValidatePixelRectangle(result, frame.Width, frame.Height, limits, WebpConstants.FourCcAnimationFrameString, "animation frame dimensions exceed configured security limits");
            totalPixels = AddPixelCount(totalPixels, (long)frame.Width * (long)frame.Height);
            ValidateAlphaRecursionLimit(result, frame.AlphaData, limits);
            index = index + 1;
        }

        ValidateAlphaRecursionLimit(result, parsed.AlphaData, limits);
        ValidateTotalPixels(result, totalPixels, limits);
        if (parsed.Frames.Count == 0 && !options.IdentifyOnly)
        {
            ValidateDecodedAllocation(result, GetExtendedRetainedPayloadBytes(parsed), parsed.Vp8X.CanvasWidth, parsed.Vp8X.CanvasHeight, 1, limits);
        }
        else
        {
            ValidateDecodedAllocation(result, GetExtendedRetainedPayloadBytes(parsed), 0, 0, 0, limits);
        }
    }

    internal static long GetExtendedRetainedPayloadBytes(WebpExtendedContainerParseResult parsed)
    {
        long bytes = GetMetadataBytes(parsed.Metadata);
        if (parsed.AlphaData != null)
        {
            bytes = AddPixelCount(bytes, parsed.AlphaData.Length);
        }

        if (parsed.ImagePayload != null)
        {
            bytes = AddPixelCount(bytes, parsed.ImagePayload.Length);
        }

        int index = 0;
        while (index < parsed.UnknownChunks.Count)
        {
            bytes = AddPixelCount(bytes, parsed.UnknownChunks[index].Data.Length);
            index = index + 1;
        }

        index = 0;
        while (index < parsed.Frames.Count)
        {
            WebpFrame frame = parsed.Frames[index];
            if (frame.AlphaData != null)
            {
                bytes = AddPixelCount(bytes, frame.AlphaData.Length);
            }

            if (frame.EncodedBitstream != null)
            {
                bytes = AddPixelCount(bytes, frame.EncodedBitstream.Length);
            }

            int unknownIndex = 0;
            while (unknownIndex < frame.UnknownChunks.Count)
            {
                bytes = AddPixelCount(bytes, frame.UnknownChunks[unknownIndex].Data.Length);
                unknownIndex = unknownIndex + 1;
            }

            index = index + 1;
        }

        return bytes;
    }

    internal static long GetMetadataBytes(WebpMetadata metadata)
    {
        long bytes = 0;
        if (metadata.IccProfile != null)
        {
            bytes = AddPixelCount(bytes, metadata.IccProfile.Length);
        }

        if (metadata.Exif != null)
        {
            bytes = AddPixelCount(bytes, metadata.Exif.Length);
        }

        if (metadata.Xmp != null)
        {
            bytes = AddPixelCount(bytes, metadata.Xmp.Length);
        }

        return bytes;
    }

    internal static void ValidateDecodedAllocation(WebpDecodeResult result, long retainedPayloadBytes, int width, int height, int frameCount, WebpSecurityLimits limits)
    {
        long allocationBytes = retainedPayloadBytes;
        if (width > 0 && height > 0 && frameCount > 0)
        {
            long pixelBytes = (long)width * (long)height * 4L;
            allocationBytes = AddPixelCount(allocationBytes, pixelBytes * frameCount);
        }

        if (allocationBytes > limits.MaxAggregateAllocationBytes)
        {
            AddDiagnostic(result, "RIFF", "aggregate allocation size exceeds the configured security limit");
        }
    }

    internal static int CountExtendedChunks(WebpExtendedContainerParseResult parsed)
    {
        int count = parsed.Chunks.Count;
        int index = 0;
        while (index < parsed.Frames.Count)
        {
            WebpFrame frame = parsed.Frames[index];
            if (frame.AlphaData != null)
            {
                count = count + 1;
            }

            if (frame.EncodedBitstream != null)
            {
                count = count + 1;
            }

            count = count + frame.UnknownChunks.Count;
            index = index + 1;
        }

        return count;
    }

    internal static void ValidateSelectedAnimationPixelLimits(WebpDecodeResult result, List<WebpFrame> selectedFrames, int canvasWidth, int canvasHeight, WebpDecodeOptions options)
    {
        long totalPixels = 0;
        if (options.AnimationDecodeMode == WebpAnimationDecodeMode.CompositedFrames)
        {
            long canvasPixels = (long)canvasWidth * (long)canvasHeight;
            int index = 0;
            while (index < selectedFrames.Count)
            {
                totalPixels = AddPixelCount(totalPixels, canvasPixels);
                index = index + 1;
            }
        }
        else
        {
            int index = 0;
            while (index < selectedFrames.Count)
            {
                WebpFrame frame = selectedFrames[index];
                totalPixels = AddPixelCount(totalPixels, (long)frame.Width * (long)frame.Height);
                index = index + 1;
            }
        }

        ValidateTotalPixels(result, totalPixels, options.Limits);
        if (!options.IdentifyOnly)
        {
            long allocationBytes = AddPixelCount(totalPixels, totalPixels);
            allocationBytes = AddPixelCount(allocationBytes, allocationBytes);
            if (allocationBytes > options.Limits.MaxAggregateAllocationBytes)
            {
                AddDiagnostic(result, "RIFF", "aggregate allocation size exceeds the configured security limit");
            }
        }
    }

    internal static void ValidateChunkCount(WebpDecodeResult result, int chunkCount, WebpSecurityLimits limits)
    {
        if (chunkCount > limits.MaxChunkCount)
        {
            AddDiagnostic(result, "RIFF", "chunk count exceeds the configured security limit");
        }
    }

    internal static void ValidatePixelRectangle(WebpDecodeResult result, int width, int height, WebpSecurityLimits limits, string fourCc, string message)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (width > limits.MaxCanvasWidth || height > limits.MaxCanvasHeight)
        {
            AddDiagnostic(result, fourCc, message);
        }

        long pixels = (long)width * (long)height;
        if (pixels > limits.MaxCanvasPixels)
        {
            AddDiagnostic(result, fourCc, message);
        }
    }

    internal static void ValidateTotalPixels(WebpDecodeResult result, long totalPixels, WebpSecurityLimits limits)
    {
        if (totalPixels > limits.MaxTotalPixels)
        {
            AddDiagnostic(result, "", "total pixel count exceeds the configured security limit");
        }
    }

    internal static void ValidateAlphaRecursionLimit(WebpDecodeResult result, byte[]? alphaData, WebpSecurityLimits limits)
    {
        if (alphaData == null || alphaData.Length == 0)
        {
            return;
        }

        WebpAlphaHeader header = WebpAlphaDecoder.ParseHeader(alphaData[0]);
        if (header.CompressionMethod == WebpConstants.AlphaCompressionLossless && limits.MaxRecursionDepth < 2)
        {
            AddDiagnostic(result, WebpConstants.FourCcAlphaString, "compressed alpha decode exceeds the configured recursion depth limit");
        }
    }

    internal static long AddPixelCount(long totalPixels, long pixels)
    {
        if (totalPixels > long.MaxValue - pixels)
        {
            return long.MaxValue;
        }

        return totalPixels + pixels;
    }

    internal static void CopyBasicDiagnostics(WebpDecodeResult result, WebpBasicContainerParseResult parsed)
    {
        int index = 0;
        while (index < parsed.Diagnostics.Count)
        {
            result.Diagnostics.Add(parsed.Diagnostics[index]);
            index = index + 1;
        }
    }

    internal static void CopyExtendedDiagnostics(WebpDecodeResult result, WebpExtendedContainerParseResult parsed)
    {
        int index = 0;
        while (index < parsed.Diagnostics.Count)
        {
            result.Diagnostics.Add(parsed.Diagnostics[index]);
            index = index + 1;
        }
    }

    internal static void CopyVp8Diagnostics(WebpDecodeResult result, WebpVp8DecodeResult decoded)
    {
        int index = 0;
        while (index < decoded.Diagnostics.Count)
        {
            result.Diagnostics.Add(decoded.Diagnostics[index]);
            index = index + 1;
        }
    }

    internal static void CopyVp8LDiagnostics(WebpDecodeResult result, WebpVp8LDecodeResult decoded)
    {
        int index = 0;
        while (index < decoded.Diagnostics.Count)
        {
            result.Diagnostics.Add(decoded.Diagnostics[index]);
            index = index + 1;
        }
    }

    internal static void CopyAlphaDiagnostics(WebpDecodeResult result, WebpAlphaDecodeResult decoded)
    {
        int index = 0;
        while (index < decoded.Diagnostics.Count)
        {
            result.Diagnostics.Add(decoded.Diagnostics[index]);
            index = index + 1;
        }
    }

    internal static void AddDiagnostic(WebpDecodeResult result, string fourCc, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(WebpDiagnosticSeverity.Error, -1, fourCc, message));
    }

    internal static void Finish(WebpDecodeResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
