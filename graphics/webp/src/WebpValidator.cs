using System;
using System.Collections.Generic;

namespace webp;

/// <summary>Checks RIFF structure, WebP chunk ordering, feature flags, and dimensions without decoding pixels.</summary>
internal static class WebpValidator
{
    /// <summary>Validates a WebP byte array using permissive rules.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>A validity flag and all discovered diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    internal static WebpValidationResult Validate(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        WebpDecodeOptions options = new WebpDecodeOptions();
        return Validate(data.AsSpan(), options);
    }

    /// <summary>Validates a WebP byte array using the strictness from the supplied options.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls warnings and errors.</param>
    /// <returns>A validity flag and all discovered diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> or <c>options</c> is null.</exception>
    internal static WebpValidationResult Validate(byte[] data, WebpDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(options);
        return Validate(data.AsSpan(), options);
    }

    /// <summary>Validates WebP data from a read-only span using permissive rules.</summary>
    /// <param name="data">A span containing the complete RIFF WebP file.</param>
    /// <returns>A validity flag and all discovered diagnostics.</returns>
    internal static WebpValidationResult Validate(ReadOnlySpan<byte> data)
    {
        WebpDecodeOptions options = new WebpDecodeOptions();
        return Validate(data, options);
    }

    /// <summary>Validates WebP data from a read-only span using the strictness from the supplied options.</summary>
    /// <remarks>Strict mode promotes recoverable conformance issues, such as inconsistent feature flags, from warnings to errors.</remarks>
    /// <param name="data">A span containing the complete RIFF WebP file.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls warnings and errors.</param>
    /// <returns>A validity flag and all discovered diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>options</c> is null.</exception>
    internal static WebpValidationResult Validate(ReadOnlySpan<byte> data, WebpDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        WebpPublicModelValidator.ValidateInspectionOptions(options);

        WebpValidationResult result = new WebpValidationResult();
        string? limitError = WebpInfoReader.GetLimitError(data, options.Limits);
        if (limitError != null)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 0, "RIFF", limitError);
            Finish(result);
            return result;
        }

        if (HasVp8X(data))
        {
            ValidateExtended(result, data, options.Strictness);
        }
        else
        {
            ValidateBasic(result, data, options.Strictness);
        }

        Finish(result);
        return result;
    }

    internal static bool HasVp8X(ReadOnlySpan<byte> data)
    {
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
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

    internal static void ValidateBasic(WebpValidationResult result, ReadOnlySpan<byte> data, WebpStrictness strictness)
    {
        WebpBasicContainerParseResult basic = WebpBasicContainerParser.Parse(data, strictness);
        CopyDiagnostics(result, basic.Diagnostics);
        ValidateChunkOrder(result, basic.Chunks, strictness);
        ValidateBasicDimensions(result, basic);
    }

    internal static void ValidateExtended(WebpValidationResult result, ReadOnlySpan<byte> data, WebpStrictness strictness)
    {
        WebpExtendedContainerParseResult extended = WebpExtendedContainerParser.Parse(data, strictness);
        CopyDiagnostics(result, extended.Diagnostics);
        ValidateChunkOrder(result, extended.Chunks, strictness);
        ValidateVp8XDimensions(result, extended);
        ValidateExtendedBitstreams(result, extended, strictness);
        ValidateAlphaPayloads(result, extended, strictness);
        ValidateFeatureFlags(result, extended, strictness);
        ValidateRequiredExtendedChunks(result, extended);
        ValidateFrameDimensions(result, extended);
    }

    internal static void ValidateChunkOrder(WebpValidationResult result, List<WebpChunkInfo> chunks, WebpStrictness strictness)
    {
        if (HasChunkTableErrors(result))
        {
            return;
        }

        WebpValidationResult order = WebpContainerOrderValidator.ValidateChunks(chunks, strictness);
        CopyDiagnostics(result, order.Diagnostics);
    }

    internal static bool HasChunkTableErrors(WebpValidationResult result)
    {
        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            WebpDiagnostic diagnostic = result.Diagnostics[index];
            if (diagnostic.Severity == WebpDiagnosticSeverity.Error
                && (diagnostic.Message.Contains("RIFF") || diagnostic.Message.Contains("chunk")))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void ValidateBasicDimensions(WebpValidationResult result, WebpBasicContainerParseResult basic)
    {
        if (basic.Width < 0 || basic.Height < 0)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, basic.ImageChunk.PayloadOffset, basic.ImageChunk.FourCc, "basic image dimensions are invalid");
            return;
        }

        if (basic.ContainerKind == WebpContainerKind.SimpleLossy)
        {
            if (basic.Width > WebpConstants.Vp8LossyMaxDimension || basic.Height > WebpConstants.Vp8LossyMaxDimension)
            {
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, basic.ImageChunk.PayloadOffset, basic.ImageChunk.FourCc, "VP8 dimensions exceed the lossy maximum");
            }
        }

        if (basic.ContainerKind == WebpContainerKind.SimpleLossless)
        {
            if (basic.Width > WebpConstants.Vp8LMaxDimension || basic.Height > WebpConstants.Vp8LMaxDimension)
            {
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, basic.ImageChunk.PayloadOffset, basic.ImageChunk.FourCc, "VP8L dimensions exceed the lossless maximum");
            }
        }
    }

    internal static void ValidateVp8XDimensions(WebpValidationResult result, WebpExtendedContainerParseResult extended)
    {
        if (!extended.HasVp8X)
        {
            return;
        }

        int width = extended.Vp8X.CanvasWidth;
        int height = extended.Vp8X.CanvasHeight;
        if (width <= 0 || height <= 0)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, FindChunkOffset(extended.Chunks, WebpConstants.FourCcVp8XString), WebpConstants.FourCcVp8XString, "VP8X canvas dimensions must be non-zero");
            return;
        }

        if (width > WebpConstants.Vp8XMaxCanvasDimension || height > WebpConstants.Vp8XMaxCanvasDimension)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, FindChunkOffset(extended.Chunks, WebpConstants.FourCcVp8XString), WebpConstants.FourCcVp8XString, "VP8X canvas dimensions exceed the maximum");
        }

        long pixels = (long)width * (long)height;
        if (pixels > WebpConstants.Vp8XMaxCanvasPixels)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, FindChunkOffset(extended.Chunks, WebpConstants.FourCcVp8XString), WebpConstants.FourCcVp8XString, "VP8X canvas dimensions exceed the maximum pixel count");
        }
    }

    internal static void ValidateFeatureFlags(WebpValidationResult result, WebpExtendedContainerParseResult extended, WebpStrictness strictness)
    {
        if (!extended.HasVp8X)
        {
            return;
        }

        ValidateDeclaredChunk(result, extended, strictness, extended.Vp8X.HasIccProfile, HasChunk(extended.Chunks, WebpConstants.FourCcIccProfileString), WebpConstants.FourCcIccProfileString, "ICC profile");
        ValidateDeclaredChunk(result, extended, strictness, extended.Vp8X.HasExif, HasChunk(extended.Chunks, WebpConstants.FourCcExifString), WebpConstants.FourCcExifString, "Exif metadata");
        ValidateDeclaredChunk(result, extended, strictness, extended.Vp8X.HasXmp, HasChunk(extended.Chunks, WebpConstants.FourCcXmpString), WebpConstants.FourCcXmpString, "XMP metadata");

        bool hasAlphaChunk = HasChunk(extended.Chunks, WebpConstants.FourCcAlphaString)
            || HasTopLevelLosslessAlpha(extended)
            || HasFrameAlpha(extended);
        if (hasAlphaChunk && !extended.Vp8X.HasAlpha)
        {
            AddStrictnessDiagnostic(result, strictness, FindChunkOffset(extended.Chunks, WebpConstants.FourCcAlphaString), WebpConstants.FourCcAlphaString, "VP8X alpha feature flag is not set but alpha data is present");
        }

        if (!hasAlphaChunk && extended.Vp8X.HasAlpha)
        {
            AddStrictnessDiagnostic(result, strictness, FindChunkOffset(extended.Chunks, WebpConstants.FourCcVp8XString), WebpConstants.FourCcVp8XString, "VP8X declares alpha but no alpha data is present");
        }

        bool hasAnimationChunks = HasChunk(extended.Chunks, WebpConstants.FourCcAnimString) || HasChunk(extended.Chunks, WebpConstants.FourCcAnimationFrameString);
        if (hasAnimationChunks && !extended.Vp8X.HasAnimation)
        {
            AddStrictnessDiagnostic(result, strictness, FindFirstChunkOffset(extended.Chunks, WebpConstants.FourCcAnimString, WebpConstants.FourCcAnimationFrameString), WebpConstants.FourCcVp8XString, "VP8X animation feature flag is not set but animation chunks are present");
        }
    }

    internal static void ValidateDeclaredChunk(WebpValidationResult result, WebpExtendedContainerParseResult extended, WebpStrictness strictness, bool flagSet, bool chunkPresent, string fourCc, string featureName)
    {
        if (flagSet && !chunkPresent)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, FindChunkOffset(extended.Chunks, WebpConstants.FourCcVp8XString), fourCc, "VP8X declares " + featureName + " but the chunk is missing");
        }

        if (!flagSet && chunkPresent)
        {
            AddStrictnessDiagnostic(result, strictness, FindChunkOffset(extended.Chunks, fourCc), fourCc, "VP8X does not declare " + featureName + " but the chunk is present");
        }
    }

    internal static void ValidateRequiredExtendedChunks(WebpValidationResult result, WebpExtendedContainerParseResult extended)
    {
        if (!extended.HasVp8X)
        {
            return;
        }

        bool hasAnimation = extended.Vp8X.HasAnimation
            || HasChunk(extended.Chunks, WebpConstants.FourCcAnimString)
            || HasChunk(extended.Chunks, WebpConstants.FourCcAnimationFrameString);
        if (hasAnimation)
        {
            if (!HasChunk(extended.Chunks, WebpConstants.FourCcAnimString))
            {
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, WebpConstants.RiffHeaderSize, WebpConstants.FourCcAnimString, "animated WebP container is missing ANIM");
            }

            if (extended.Frames.Count == 0)
            {
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, WebpConstants.RiffHeaderSize, WebpConstants.FourCcAnimationFrameString, "animated WebP container is missing ANMF frames");
            }

            if (extended.ImagePayload != null || extended.AlphaData != null)
            {
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, WebpConstants.RiffHeaderSize, "", "animated WebP container must not contain top-level image or ALPH chunks");
            }
        }
        else if (extended.ImagePayload == null)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, WebpConstants.RiffHeaderSize, "", "extended still-image container is missing a VP8 or VP8L image chunk");
        }
    }

    internal static void ValidateFrameDimensions(WebpValidationResult result, WebpExtendedContainerParseResult extended)
    {
        int index = 0;
        while (index < extended.Frames.Count)
        {
            WebpFrame frame = extended.Frames[index];
            if (frame.Width <= 0 || frame.Height <= 0)
            {
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, FindChunkOffset(extended.Chunks, WebpConstants.FourCcAnimationFrameString), WebpConstants.FourCcAnimationFrameString, "ANMF frame dimensions must be non-zero");
            }

            if (frame.X < 0 || frame.Y < 0)
            {
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, FindChunkOffset(extended.Chunks, WebpConstants.FourCcAnimationFrameString), WebpConstants.FourCcAnimationFrameString, "ANMF frame position is invalid");
            }

            if (extended.Vp8X.CanvasWidth > 0 && extended.Vp8X.CanvasHeight > 0)
            {
                long frameRight = (long)frame.X + (long)frame.Width;
                long frameBottom = (long)frame.Y + (long)frame.Height;
                if (frameRight > extended.Vp8X.CanvasWidth || frameBottom > extended.Vp8X.CanvasHeight)
                {
                    AddDiagnostic(result, WebpDiagnosticSeverity.Error, FindChunkOffset(extended.Chunks, WebpConstants.FourCcAnimationFrameString), WebpConstants.FourCcAnimationFrameString, "ANMF frame rectangle exceeds the VP8X canvas");
                }
            }

            index = index + 1;
        }
    }

    internal static bool HasChunk(List<WebpChunkInfo> chunks, string fourCc)
    {
        int index = 0;
        while (index < chunks.Count)
        {
            if (chunks[index].FourCc == fourCc)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool HasFrameAlpha(WebpExtendedContainerParseResult extended)
    {
        int index = 0;
        while (index < extended.Frames.Count)
        {
            WebpFrame frame = extended.Frames[index];
            if (frame.AlphaData != null)
            {
                return true;
            }

            if (frame.BitstreamKind == WebpBitstreamKind.Lossless && frame.EncodedBitstream != null)
            {
                WebpVp8LHeaderParseResult header = WebpVp8LBitReader.ParseHeader(frame.EncodedBitstream, WebpStrictness.Permissive);
                if (header.IsValid && header.Header.HasAlpha)
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool HasTopLevelLosslessAlpha(WebpExtendedContainerParseResult extended)
    {
        if (extended.BitstreamKind != WebpBitstreamKind.Lossless || extended.ImagePayload == null)
        {
            return false;
        }

        WebpVp8LHeaderParseResult header = WebpVp8LBitReader.ParseHeader(extended.ImagePayload, WebpStrictness.Permissive);
        return header.IsValid && header.Header.HasAlpha;
    }

    internal static void ValidateExtendedBitstreams(WebpValidationResult result, WebpExtendedContainerParseResult extended, WebpStrictness strictness)
    {
        if (extended.ImagePayload != null)
        {
            long offset = FindChunkPayloadOffset(extended.Chunks, GetImageFourCc(extended.BitstreamKind));
            ValidateImageBitstream(result, extended.ImagePayload, extended.BitstreamKind, extended.Vp8X.CanvasWidth, extended.Vp8X.CanvasHeight, offset, "top-level image", strictness);
        }

        int index = 0;
        while (index < extended.Frames.Count)
        {
            WebpFrame frame = extended.Frames[index];
            if (frame.EncodedBitstream != null)
            {
                long offset = FindChunkOffset(extended.Chunks, WebpConstants.FourCcAnimationFrameString);
                ValidateImageBitstream(result, frame.EncodedBitstream, frame.BitstreamKind, frame.Width, frame.Height, offset, "ANMF frame image", strictness);
            }

            index = index + 1;
        }
    }

    internal static void ValidateImageBitstream(WebpValidationResult result, byte[] payload, WebpBitstreamKind bitstreamKind, int expectedWidth, int expectedHeight, long offset, string description, WebpStrictness strictness)
    {
        if (bitstreamKind == WebpBitstreamKind.Lossy)
        {
            WebpVp8FrameHeaderParseResult header = WebpVp8FrameHeaderParser.Parse(payload, strictness);
            CopyImageDiagnostics(result, header.Diagnostics, offset);
            if (header.IsValid && (header.Header.Width != expectedWidth || header.Header.Height != expectedHeight))
            {
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, offset, WebpConstants.FourCcVp8String, description + " dimensions do not match the declared dimensions");
            }
        }
        else if (bitstreamKind == WebpBitstreamKind.Lossless)
        {
            WebpVp8LHeaderParseResult header = WebpVp8LBitReader.ParseHeader(payload, strictness);
            CopyImageDiagnostics(result, header.Diagnostics, offset);
            if (header.IsValid && (header.Header.Width != expectedWidth || header.Header.Height != expectedHeight))
            {
                AddDiagnostic(result, WebpDiagnosticSeverity.Error, offset, WebpConstants.FourCcVp8LString, description + " dimensions do not match the declared dimensions");
            }
        }
        else
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, offset, "", description + " has an unsupported bitstream kind");
        }
    }

    internal static void CopyImageDiagnostics(WebpValidationResult result, List<WebpDiagnostic> diagnostics, long offset)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            WebpDiagnostic diagnostic = diagnostics[index];
            AddDiagnostic(result, diagnostic.Severity, offset + diagnostic.Offset, diagnostic.FourCc, diagnostic.Message);
            index = index + 1;
        }
    }

    internal static void ValidateAlphaPayloads(WebpValidationResult result, WebpExtendedContainerParseResult extended, WebpStrictness strictness)
    {
        if (extended.AlphaData != null)
        {
            ValidateAlphaPayload(result, extended.AlphaData, FindChunkPayloadOffset(extended.Chunks, WebpConstants.FourCcAlphaString), strictness);
        }

        int index = 0;
        while (index < extended.Frames.Count)
        {
            byte[]? alphaData = extended.Frames[index].AlphaData;
            if (alphaData != null)
            {
                ValidateAlphaPayload(result, alphaData, FindChunkOffset(extended.Chunks, WebpConstants.FourCcAnimationFrameString), strictness);
            }

            index = index + 1;
        }
    }

    internal static void ValidateAlphaPayload(WebpValidationResult result, byte[] payload, long offset, WebpStrictness strictness)
    {
        if (payload.Length == 0)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, offset, WebpConstants.FourCcAlphaString, "ALPH payload must contain a header byte");
            return;
        }

        int compression = payload[0] & WebpConstants.AlphaCompressionMask;
        if (compression > WebpConstants.AlphaCompressionLossless)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, offset, WebpConstants.FourCcAlphaString, "ALPH compression method is invalid");
        }

        if ((payload[0] & WebpConstants.AlphaReservedMask) != 0)
        {
            AddStrictnessDiagnostic(result, strictness, offset, WebpConstants.FourCcAlphaString, "ALPH reserved bits are set");
        }
    }

    internal static string GetImageFourCc(WebpBitstreamKind bitstreamKind)
    {
        if (bitstreamKind == WebpBitstreamKind.Lossy)
        {
            return WebpConstants.FourCcVp8String;
        }

        return WebpConstants.FourCcVp8LString;
    }

    internal static long FindChunkPayloadOffset(List<WebpChunkInfo> chunks, string fourCc)
    {
        int index = 0;
        while (index < chunks.Count)
        {
            if (chunks[index].FourCc == fourCc)
            {
                return chunks[index].PayloadOffset;
            }

            index = index + 1;
        }

        return WebpConstants.RiffHeaderSize;
    }

    internal static long FindChunkOffset(List<WebpChunkInfo> chunks, string fourCc)
    {
        int index = 0;
        while (index < chunks.Count)
        {
            if (chunks[index].FourCc == fourCc)
            {
                return chunks[index].Offset;
            }

            index = index + 1;
        }

        return WebpConstants.RiffHeaderSize;
    }

    internal static long FindFirstChunkOffset(List<WebpChunkInfo> chunks, string firstFourCc, string secondFourCc)
    {
        int index = 0;
        while (index < chunks.Count)
        {
            if (chunks[index].FourCc == firstFourCc || chunks[index].FourCc == secondFourCc)
            {
                return chunks[index].Offset;
            }

            index = index + 1;
        }

        return WebpConstants.RiffHeaderSize;
    }

    internal static void AddStrictnessDiagnostic(WebpValidationResult result, WebpStrictness strictness, long offset, string fourCc, string message)
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

    internal static void CopyDiagnostics(WebpValidationResult result, List<WebpDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            result.Diagnostics.Add(diagnostics[index]);
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
