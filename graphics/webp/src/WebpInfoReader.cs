using System;

namespace webp;

/// <summary>Reads WebP dimensions, features, chunk layout, frame timing, and diagnostics without decoding pixels.</summary>
internal static class WebpInfoReader
{
    /// <summary>Identifies a WebP byte array using permissive parsing.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>Container information and any format diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    internal static WebpInfo Read(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        WebpDecodeOptions options = new WebpDecodeOptions();
        return Read(data.AsSpan(), options);
    }

    /// <summary>Identifies a WebP byte array using the strictness from the supplied decode options.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls format diagnostics.</param>
    /// <returns>Container information and any format diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> or <c>options</c> is null.</exception>
    internal static WebpInfo Read(byte[] data, WebpDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(options);
        return Read(data.AsSpan(), options);
    }

    /// <summary>Identifies WebP data from a read-only span using permissive parsing.</summary>
    /// <param name="data">A span containing the complete RIFF WebP file.</param>
    /// <returns>Container information and any format diagnostics.</returns>
    internal static WebpInfo Read(ReadOnlySpan<byte> data)
    {
        WebpDecodeOptions options = new WebpDecodeOptions();
        return Read(data, options);
    }

    /// <summary>Identifies WebP data from a read-only span using the strictness from the supplied options.</summary>
    /// <remarks>Frame entries contain geometry and timing only; pixel and encoded-payload fields are not populated.</remarks>
    /// <param name="data">A span containing the complete RIFF WebP file.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls format diagnostics.</param>
    /// <returns>Container information and any format diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>options</c> is null.</exception>
    internal static WebpInfo Read(ReadOnlySpan<byte> data, WebpDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        WebpPublicModelValidator.ValidateInspectionOptions(options);

        string? limitError = GetLimitError(data, options.Limits);
        if (limitError != null)
        {
            WebpInfo limited = new WebpInfo();
            limited.Diagnostics.Add(new WebpDiagnostic(WebpDiagnosticSeverity.Error, 0, "RIFF", limitError));
            return limited;
        }

        if (HasVp8X(data))
        {
            WebpExtendedContainerParseResult extended = WebpExtendedContainerParser.Parse(data, options.Strictness);
            WebpInfo extendedInfo = FromExtended(extended);
            PopulateMissingChunkTable(extendedInfo, data);
            return extendedInfo;
        }

        WebpBasicContainerParseResult basic = WebpBasicContainerParser.Parse(data, options.Strictness);
        WebpInfo basicInfo = FromBasic(basic);
        PopulateMissingChunkTable(basicInfo, data);
        return basicInfo;
    }

    internal static string? GetLimitError(ReadOnlySpan<byte> data, WebpSecurityLimits limits)
    {
        if (data.Length > limits.MaxRiffSize)
        {
            return "input RIFF size exceeds the configured security limit";
        }

        return WebpSecurityLimitValidator.GetContainerLimitError(data, limits);
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

    internal static WebpInfo FromBasic(WebpBasicContainerParseResult source)
    {
        WebpInfo info = new WebpInfo();
        info.Width = source.Width;
        info.Height = source.Height;
        info.HasAlpha = source.HasAlpha;
        info.HasAnimation = false;
        info.HasIccProfile = false;
        info.HasExif = false;
        info.HasXmp = false;
        info.ContainerKind = source.ContainerKind;
        info.BitstreamKind = source.BitstreamKind;
        CopyChunks(info, source.Chunks);
        CopyDiagnostics(info, source.Diagnostics);
        return info;
    }

    internal static WebpInfo FromExtended(WebpExtendedContainerParseResult source)
    {
        WebpInfo info = new WebpInfo();
        info.Width = source.Vp8X.CanvasWidth;
        info.Height = source.Vp8X.CanvasHeight;
        info.HasAlpha = source.Vp8X.HasAlpha
            || source.AlphaData != null
            || HasLosslessBitstreamAlpha(source.ImagePayload, source.BitstreamKind)
            || HasFrameAlpha(source);
        info.HasAnimation = source.Vp8X.HasAnimation || source.Frames.Count > 0;
        info.HasIccProfile = source.Metadata.IccProfile != null;
        info.HasExif = source.Metadata.Exif != null;
        info.HasXmp = source.Metadata.Xmp != null;
        info.LoopCount = source.AnimationLoopCount;
        info.BackgroundColorBgra = source.AnimationBackgroundColorBgra;
        info.DeclaredFeatureFlags = GetDeclaredFeatureFlags(source.Vp8X.FeatureFlags);
        info.ReservedFeatureBits = source.Vp8X.ReservedFeatureBits;
        info.HasReservedFeatureBits = source.Vp8X.HasReservedFeatureBits;
        info.HasReservedBytes = source.Vp8X.HasReservedBytes;
        info.ContainerKind = GetExtendedContainerKind(info.HasAnimation);
        info.BitstreamKind = GetExtendedBitstreamKind(source);
        CopyChunks(info, source.Chunks);
        CopyFrames(info, source);
        CopyDiagnostics(info, source.Diagnostics);
        return info;
    }

    internal static WebpVp8XFeatureFlags GetDeclaredFeatureFlags(byte featureFlags)
    {
        byte knownFlags = (byte)(featureFlags & WebpConstants.Vp8XFeatureKnownMask);
        return (WebpVp8XFeatureFlags)knownFlags;
    }

    internal static WebpContainerKind GetExtendedContainerKind(bool hasAnimation)
    {
        if (hasAnimation)
        {
            return WebpContainerKind.ExtendedAnimation;
        }

        return WebpContainerKind.ExtendedStill;
    }

    internal static WebpBitstreamKind GetExtendedBitstreamKind(WebpExtendedContainerParseResult source)
    {
        if (source.BitstreamKind != WebpBitstreamKind.Unknown)
        {
            return source.BitstreamKind;
        }

        if (source.Frames.Count == 0)
        {
            return WebpBitstreamKind.Unknown;
        }

        WebpBitstreamKind kind = source.Frames[0].BitstreamKind;
        int index = 1;
        while (index < source.Frames.Count)
        {
            if (source.Frames[index].BitstreamKind != kind)
            {
                return WebpBitstreamKind.Unknown;
            }

            index = index + 1;
        }

        return kind;
    }

    internal static bool HasFrameAlpha(WebpExtendedContainerParseResult source)
    {
        int index = 0;
        while (index < source.Frames.Count)
        {
            WebpFrame frame = source.Frames[index];
            if (frame.AlphaData != null || HasLosslessBitstreamAlpha(frame.EncodedBitstream, frame.BitstreamKind))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool HasLosslessBitstreamAlpha(byte[]? payload, WebpBitstreamKind bitstreamKind)
    {
        if (payload == null
            || bitstreamKind != WebpBitstreamKind.Lossless
            || payload.Length < WebpConstants.Vp8LHeaderSize
            || payload[0] != WebpConstants.Vp8LSignature)
        {
            return false;
        }

        uint bits = WebpEndianReader.ReadUInt32(payload, 1);
        int version = (int)((bits >> 29) & 7U);
        return version == WebpConstants.Vp8LVersion && ((bits >> 28) & 1U) != 0;
    }

    internal static void CopyChunks(WebpInfo info, System.Collections.Generic.List<WebpChunkInfo> chunks)
    {
        int index = 0;
        while (index < chunks.Count)
        {
            info.Chunks.Add(CopyChunk(chunks[index]));
            index = index + 1;
        }
    }

    internal static void PopulateMissingChunkTable(WebpInfo info, ReadOnlySpan<byte> data)
    {
        if (info.Chunks.Count != 0 || data.Length < WebpConstants.RiffHeaderSize)
        {
            return;
        }

        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        CopyChunks(info, riff.Chunks);
    }

    internal static WebpChunkInfo CopyChunk(WebpChunkInfo source)
    {
        WebpChunkInfo chunk = new WebpChunkInfo();
        chunk.FourCc = source.FourCc;
        chunk.Offset = source.Offset;
        chunk.PayloadOffset = source.PayloadOffset;
        chunk.PayloadLength = source.PayloadLength;
        chunk.HasPadding = source.HasPadding;
        chunk.PaddingOffset = source.PaddingOffset;
        chunk.PaddedEndOffset = source.PaddedEndOffset;
        chunk.IsPaddingValid = source.IsPaddingValid;
        chunk.IsKnown = source.IsKnown;
        return chunk;
    }

    internal static void CopyFrames(WebpInfo info, WebpExtendedContainerParseResult source)
    {
        int index = 0;
        while (index < source.Frames.Count)
        {
            info.Frames.Add(CopyFrame(source.Frames[index]));
            index = index + 1;
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
        return frame;
    }

    internal static void CopyDiagnostics(WebpInfo info, System.Collections.Generic.List<WebpDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            WebpDiagnostic diagnostic = diagnostics[index];
            info.Diagnostics.Add(new WebpDiagnostic(diagnostic.Severity, diagnostic.Offset, diagnostic.FourCc, diagnostic.Message));
            index = index + 1;
        }
    }
}
