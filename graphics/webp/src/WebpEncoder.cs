using System;
using System.Collections.Generic;
using System.IO;

namespace webp;

internal class WebpAutomaticEncodeDecision
{
    internal WebpEncodeMode Mode;
    internal WebpBitstreamKind BitstreamKind;
    internal WebpContainerKind ContainerKind;
    internal bool HasAlpha;
    internal bool UsesExtendedContainer;

    internal WebpAutomaticEncodeDecision()
    {
        Mode = WebpEncodeMode.Lossless;
        BitstreamKind = WebpBitstreamKind.Lossless;
        ContainerKind = WebpContainerKind.SimpleLossless;
        HasAlpha = false;
        UsesExtendedContainer = false;
    }
}

/// <summary>Encodes RGBA pixel buffers and still or animated image models as RIFF WebP files.</summary>
/// <remarks>The encoder writes deterministic managed-code output. Automatic mode chooses between the library's supported lossy and lossless paths from the supplied pixels and options.</remarks>
internal static class WebpEncoder
{
    /// <summary>Encodes one RGBA image to a WebP byte array with default options.</summary>
    /// <param name="buffer">The RGBA pixels to encode.</param>
    /// <returns>A complete RIFF WebP file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>buffer</c> or its byte array is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the buffer shape is inconsistent.</exception>
    internal static byte[] Encode(WebpPixelBuffer buffer)
    {
        return Encode(buffer, new WebpEncodeOptions());
    }

    /// <summary>Encodes one RGBA image to a WebP byte array using the supplied compression settings.</summary>
    /// <param name="buffer">The RGBA pixels to encode.</param>
    /// <param name="options">The compression, alpha, preservation, and safety settings.</param>
    /// <returns>A complete RIFF WebP file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>buffer</c>, its byte array, <c>options</c>, or <c>options.Limits</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the pixel layout or an option combination cannot be encoded.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when dimensions, limits, or numeric encoding settings are outside their supported ranges.</exception>
    internal static byte[] Encode(WebpPixelBuffer buffer, WebpEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        WebpPublicModelValidator.ValidateEncodeOptions(options);
        WebpPixelBufferHelper.ValidateRgbaBuffer(buffer);
        ValidatePixelBufferLimits(buffer, options.Limits);

        WebpAutomaticEncodeDecision decision = SelectAutomaticMode(buffer, options);
        byte[] data;
        if (decision.Mode == WebpEncodeMode.Lossy)
        {
            ValidateEncodeAlphaRecursion(buffer, options);
            data = WebpLossyAlphaEncoder.EncodeStill(buffer, options);
        }
        else
        {
            byte[] payload = WebpVp8LEncoder.Encode(buffer, options);
            data = WebpContainerWriter.WriteBasicStill(payload, WebpBitstreamKind.Lossless);
        }

        data = ApplyDeterministicSerialization(data, options);
        ValidateEncodedOutputLimits(data, options.Limits);
        return data;
    }

    /// <summary>Encodes a still or animated image model to a WebP byte array with default options.</summary>
    /// <param name="image">The image model containing at least one frame.</param>
    /// <returns>A complete RIFF WebP file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the image or a required model field is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the image model is internally inconsistent.</exception>
    internal static byte[] Encode(WebpImage image)
    {
        return Encode(image, new WebpEncodeOptions());
    }

    /// <summary>Encodes a still or animated image model using the supplied compression and preservation settings.</summary>
    /// <remarks>Animation frames with pixels are encoded; frames with a compatible retained bitstream can be muxed without decoding those pixels.</remarks>
    /// <param name="image">The image model containing at least one frame.</param>
    /// <param name="options">The compression, alpha, metadata, unknown-chunk, and safety settings.</param>
    /// <returns>A complete RIFF WebP file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the image, options, limits, or a required model field is null.</exception>
    /// <exception cref="ArgumentException">Thrown when frame geometry, payloads, metadata, or options form an invalid WebP model.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when dimensions, limits, or numeric encoding settings are outside their supported ranges.</exception>
    internal static byte[] Encode(WebpImage image, WebpEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);
        WebpPublicModelValidator.ValidateEncodeOptions(options);
        WebpPublicModelValidator.ValidateImageForEncoding(image, options);
        ValidateImageModelLimits(image, options.Limits);
        ValidateRetainedPayloadLimits(image, options);

        byte[] data;
        if (image.HasAnimation || image.Frames.Count > 1)
        {
            data = EncodeAnimatedImage(image, options);
        }
        else if (image.Frames.Count == 1)
        {
            data = EncodeStillImage(image, options);
        }
        else
        {
            throw new ArgumentException("image encode requires at least one frame", "image");
        }

        data = ApplyDeterministicSerialization(data, options);
        ValidateEncodedOutputLimits(data, options.Limits);
        return data;
    }

    /// <summary>Encodes one RGBA image with default options and appends the WebP bytes to a stream.</summary>
    /// <remarks>Writing starts at the stream's current position and leaves the stream open.</remarks>
    /// <param name="buffer">The RGBA pixels to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>buffer</c>, its bytes, or <c>stream</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not writable or the buffer is invalid.</exception>
    /// <exception cref="IOException">Thrown when the encoded bytes cannot be written.</exception>
    internal static void Encode(WebpPixelBuffer buffer, Stream stream)
    {
        Encode(buffer, stream, new WebpEncodeOptions());
    }

    /// <summary>Encodes one RGBA image and appends the WebP bytes to a stream.</summary>
    /// <remarks>Writing starts at the stream's current position and leaves the stream open.</remarks>
    /// <param name="buffer">The RGBA pixels to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="options">The compression, alpha, preservation, and safety settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument, byte array, or limits object is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not writable or the input cannot be encoded.</exception>
    /// <exception cref="IOException">Thrown when the encoded bytes cannot be written.</exception>
    internal static void Encode(WebpPixelBuffer buffer, Stream stream, WebpEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite)
        {
            throw new ArgumentException("WebP encode stream must be writable", "stream");
        }

        byte[] data = Encode(buffer, options);
        stream.Write(data, 0, data.Length);
    }

    /// <summary>Encodes one RGBA image with default options and writes or replaces a file.</summary>
    /// <param name="buffer">The RGBA pixels to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>buffer</c>, its bytes, or <c>path</c> is null.</exception>
    /// <exception cref="IOException">Thrown when the destination file cannot be written.</exception>
    internal static void Encode(WebpPixelBuffer buffer, string path)
    {
        Encode(buffer, path, new WebpEncodeOptions());
    }

    /// <summary>Encodes one RGBA image and writes or replaces a file.</summary>
    /// <param name="buffer">The RGBA pixels to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">The compression, alpha, preservation, and safety settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument, byte array, or limits object is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the input cannot be encoded.</exception>
    /// <exception cref="IOException">Thrown when the destination file cannot be written.</exception>
    internal static void Encode(WebpPixelBuffer buffer, string path, WebpEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(path);
        byte[] data = Encode(buffer, options);
        File.WriteAllBytes(path, data);
    }

    /// <summary>Encodes a still or animated image model with default options and appends it to a stream.</summary>
    /// <remarks>Writing starts at the stream's current position and leaves the stream open.</remarks>
    /// <param name="image">The image model containing at least one frame.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <exception cref="ArgumentNullException">Thrown when the image, stream, or a required model field is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not writable or the model is invalid.</exception>
    /// <exception cref="IOException">Thrown when the encoded bytes cannot be written.</exception>
    internal static void Encode(WebpImage image, Stream stream)
    {
        Encode(image, stream, new WebpEncodeOptions());
    }

    /// <summary>Encodes a still or animated image model and appends it to a stream.</summary>
    /// <remarks>Writing starts at the stream's current position and leaves the stream open.</remarks>
    /// <param name="image">The image model containing at least one frame.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="options">The compression, preservation, and safety settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument, model field, or limits object is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not writable or the model cannot be encoded.</exception>
    /// <exception cref="IOException">Thrown when the encoded bytes cannot be written.</exception>
    internal static void Encode(WebpImage image, Stream stream, WebpEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite)
        {
            throw new ArgumentException("WebP encode stream must be writable", "stream");
        }

        byte[] data = Encode(image, options);
        stream.Write(data, 0, data.Length);
    }

    /// <summary>Encodes a still or animated image model with default options and writes or replaces a file.</summary>
    /// <param name="image">The image model containing at least one frame.</param>
    /// <param name="path">The destination file path.</param>
    /// <exception cref="ArgumentNullException">Thrown when the image, path, or a required model field is null.</exception>
    /// <exception cref="IOException">Thrown when the destination file cannot be written.</exception>
    internal static void Encode(WebpImage image, string path)
    {
        Encode(image, path, new WebpEncodeOptions());
    }

    /// <summary>Encodes a still or animated image model and writes or replaces a file.</summary>
    /// <param name="image">The image model containing at least one frame.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">The compression, preservation, and safety settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument, model field, or limits object is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the model cannot be encoded.</exception>
    /// <exception cref="IOException">Thrown when the destination file cannot be written.</exception>
    internal static void Encode(WebpImage image, string path, WebpEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(path);
        byte[] data = Encode(image, options);
        File.WriteAllBytes(path, data);
    }

    internal static WebpAutomaticEncodeDecision SelectAutomaticMode(WebpPixelBuffer buffer, WebpEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(options);
        WebpPixelBufferHelper.ValidateRgbaBuffer(buffer);
        ValidateMode(options.Mode);
        WebpLossyAlphaEncoder.ValidateLossyAlphaMode(options.LossyAlphaMode);

        WebpAutomaticEncodeDecision decision = new WebpAutomaticEncodeDecision();
        bool sourceHasAlpha = WebpLossyAlphaEncoder.HasNonOpaqueAlpha(buffer);
        decision.Mode = SelectMode(buffer, options, sourceHasAlpha);
        decision.HasAlpha = sourceHasAlpha;
        if (decision.Mode == WebpEncodeMode.Lossy && options.LossyAlphaMode != WebpLossyAlphaMode.Preserve)
        {
            decision.HasAlpha = false;
        }
        if (decision.Mode == WebpEncodeMode.Lossy)
        {
            decision.BitstreamKind = WebpBitstreamKind.Lossy;
            if (decision.HasAlpha)
            {
                decision.ContainerKind = WebpContainerKind.ExtendedStill;
                decision.UsesExtendedContainer = true;
            }
            else
            {
                decision.ContainerKind = WebpContainerKind.SimpleLossy;
                decision.UsesExtendedContainer = false;
            }
        }
        else
        {
            decision.BitstreamKind = WebpBitstreamKind.Lossless;
            decision.ContainerKind = WebpContainerKind.SimpleLossless;
            decision.UsesExtendedContainer = false;
        }

        return decision;
    }

    internal static byte[] EncodeAnimatedImage(WebpImage image, WebpEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(image.Metadata);
        ArgumentNullException.ThrowIfNull(image.UnknownChunks);

        WebpImage encoded = new WebpImage();
        encoded.CanvasWidth = image.CanvasWidth;
        encoded.CanvasHeight = image.CanvasHeight;
        encoded.HasAnimation = true;
        encoded.HasAlpha = image.HasAlpha;
        encoded.LoopCount = image.LoopCount;
        encoded.BackgroundColorBgra = image.BackgroundColorBgra;
        encoded.Metadata = CopyMetadataForEncode(image.Metadata, options.MetadataCopyPolicy);
        CopyUnknownChunksForEncode(encoded.UnknownChunks, image.UnknownChunks, options.UnknownChunkPolicy);

        int index = 0;
        while (index < image.Frames.Count)
        {
            encoded.Frames.Add(EncodeAnimationFrame(image.Frames[index], options));
            index = index + 1;
        }

        encoded.HasAlpha = WebpContainerWriter.HasAnimationAlpha(encoded.Frames);

        return WebpContainerWriter.WriteAnimated(encoded);
    }

    internal static byte[] EncodeStillImage(WebpImage image, WebpEncodeOptions options)
    {
        byte[] data = EncodeStillFrame(image.Frames[0], options);
        WebpMetadata metadata = CopyMetadataForEncode(image.Metadata, options.MetadataCopyPolicy);
        if (WebpMetadataMux.HasAnyMetadata(metadata))
        {
            data = AddStillMetadata(data, metadata);
        }

        List<WebpUnknownChunk> unknownChunks = new List<WebpUnknownChunk>();
        CopyUnknownChunksForEncode(unknownChunks, image.UnknownChunks, options.UnknownChunkPolicy);
        if (unknownChunks.Count != 0)
        {
            data = WebpUnknownChunkMux.Replace(data, unknownChunks);
        }

        return data;
    }

    internal static byte[] AddStillMetadata(byte[] data, WebpMetadata metadata)
    {
        if (WebpInfoReader.HasVp8X(data))
        {
            WebpExtendedContainerParseResult extended = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);
            if (extended.ImagePayload == null)
            {
                throw new InvalidOperationException("encoded extended still image is missing its image payload");
            }

            return WebpContainerWriter.WriteExtendedStill(
                extended.ImagePayload,
                extended.BitstreamKind,
                extended.Vp8X.CanvasWidth,
                extended.Vp8X.CanvasHeight,
                metadata,
                extended.AlphaData);
        }

        WebpBasicContainerParseResult basic = WebpBasicContainerParser.Parse(data, WebpStrictness.Strict);
        return WebpContainerWriter.WriteExtendedStill(
            basic.ImagePayload,
            basic.BitstreamKind,
            basic.Width,
            basic.Height,
            metadata,
            null);
    }

    internal static byte[] EncodeStillFrame(WebpFrame frame, WebpEncodeOptions options)
    {
        if (frame == null)
        {
            throw new ArgumentException("image encode requires a frame", "image");
        }

        if (frame.Pixels == null)
        {
            throw new ArgumentException("image encode from RGBA frames requires frame pixels", "image");
        }

        WebpBitstreamKind bitstreamKind = SelectFrameBitstreamKind(frame, frame.Pixels, options);
        if (bitstreamKind == WebpBitstreamKind.Lossy)
        {
            ValidateEncodeAlphaRecursion(frame.Pixels, options);
            return WebpLossyAlphaEncoder.EncodeStill(frame.Pixels, options);
        }

        byte[] payload = WebpVp8LEncoder.Encode(frame.Pixels, options);
        return WebpContainerWriter.WriteBasicStill(payload, WebpBitstreamKind.Lossless);
    }

    internal static WebpFrame EncodeAnimationFrame(WebpFrame source, WebpEncodeOptions options)
    {
        if (source == null)
        {
            throw new ArgumentException("animated image encode frame list contains a null frame", "image");
        }

        if (source.EncodedBitstream != null)
        {
            WebpBitstreamKind encodedBitstreamKind = SelectEncodedBitstreamKind(source);
            return CopyEncodedAnimationFrame(source, encodedBitstreamKind, options);
        }

        if (source.Pixels == null)
        {
            throw new ArgumentException("animated image encode from RGBA frames requires each frame to have pixels", "image");
        }

        WebpPixelBufferHelper.ValidateRgbaBuffer(source.Pixels);
        WebpEncodeOptions frameOptions = ResolveFrameEncodeOptions(source, options);

        WebpFrame frame = new WebpFrame();
        frame.X = source.X;
        frame.Y = source.Y;
        frame.Width = GetFrameWidth(source);
        frame.Height = GetFrameHeight(source);
        frame.DurationMilliseconds = source.DurationMilliseconds;
        frame.BlendMode = source.BlendMode;
        frame.DisposeMode = source.DisposeMode;
        frame.BitstreamKind = SelectFrameBitstreamKind(source, source.Pixels, frameOptions);
        CopyUnknownChunksForEncode(frame.UnknownChunks, source.UnknownChunks, options.UnknownChunkPolicy);
        EncodeFramePixels(frame, source.Pixels, frameOptions);
        return frame;
    }

    internal static WebpEncodeOptions ResolveFrameEncodeOptions(WebpFrame frame, WebpEncodeOptions globalOptions)
    {
        if (frame.EncodeOptions == null)
        {
            return globalOptions;
        }

        WebpFrameEncodeOptions source = frame.EncodeOptions;
        WebpLossyAlphaEncoder.ValidateLossyAlphaMode(source.LossyAlphaMode);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = source.Mode;
        options.Preset = globalOptions.Preset;
        options.Quality = source.Quality;
        options.Method = globalOptions.Method;
        options.FilterStrength = globalOptions.FilterStrength;
        options.FilterSharpness = globalOptions.FilterSharpness;
        options.AutoFilter = globalOptions.AutoFilter;
        options.SpatialNoiseShaping = globalOptions.SpatialNoiseShaping;
        options.Preprocessing = globalOptions.Preprocessing;
        options.TargetSize = globalOptions.TargetSize;
        options.TargetPsnr = globalOptions.TargetPsnr;
        options.PassCount = globalOptions.PassCount;
        options.NearLosslessQuality = source.NearLosslessQuality;
        options.ExactTransparentRgb = source.ExactTransparentRgb;
        options.LosslessAlpha = source.LosslessAlpha;
        options.AlphaQuality = source.AlphaQuality;
        options.AlphaCompression = source.AlphaCompression;
        options.AlphaFilter = source.AlphaFilter;
        options.LossyAlphaMode = source.LossyAlphaMode;
        options.AlphaBackgroundColorBgra = source.AlphaBackgroundColorBgra;
        options.Deterministic = globalOptions.Deterministic;
        options.MetadataCopyPolicy = globalOptions.MetadataCopyPolicy;
        options.UnknownChunkPolicy = globalOptions.UnknownChunkPolicy;
        options.Limits = globalOptions.Limits;
        return options;
    }

    internal static WebpFrame CopyEncodedAnimationFrame(WebpFrame source, WebpBitstreamKind bitstreamKind, WebpEncodeOptions options)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = source.X;
        frame.Y = source.Y;
        frame.Width = source.Width;
        frame.Height = source.Height;
        frame.DurationMilliseconds = source.DurationMilliseconds;
        frame.BlendMode = source.BlendMode;
        frame.DisposeMode = source.DisposeMode;
        frame.BitstreamKind = bitstreamKind;
        if (source.EncodedBitstream != null)
        {
            frame.EncodedBitstream = WebpUnknownChunk.CopyBytes(source.EncodedBitstream);
        }

        frame.AlphaData = CopyNullableBytes(source.AlphaData);
        CopyUnknownChunksForEncode(frame.UnknownChunks, source.UnknownChunks, options.UnknownChunkPolicy);
        return frame;
    }

    internal static WebpBitstreamKind SelectEncodedBitstreamKind(WebpFrame frame)
    {
        if (frame.BitstreamKind == WebpBitstreamKind.Lossy || frame.BitstreamKind == WebpBitstreamKind.Lossless)
        {
            ValidateEncodedBitstream(frame, frame.BitstreamKind);
            return frame.BitstreamKind;
        }

        if (frame.BitstreamKind == WebpBitstreamKind.Unknown)
        {
            return InferEncodedBitstreamKind(frame);
        }

        throw new ArgumentOutOfRangeException("BitstreamKind");
    }

    internal static WebpBitstreamKind InferEncodedBitstreamKind(WebpFrame frame)
    {
        if (IsEncodedBitstreamValid(frame, WebpBitstreamKind.Lossless))
        {
            return WebpBitstreamKind.Lossless;
        }

        if (IsEncodedBitstreamValid(frame, WebpBitstreamKind.Lossy))
        {
            return WebpBitstreamKind.Lossy;
        }

        throw new ArgumentException("pre-encoded animation frame bitstream is not a valid VP8 or VP8L payload", "image");
    }

    internal static void ValidateEncodedBitstream(WebpFrame frame, WebpBitstreamKind bitstreamKind)
    {
        if (!IsEncodedBitstreamValid(frame, bitstreamKind))
        {
            throw new ArgumentException("pre-encoded animation frame bitstream is not valid for its bitstream kind", "image");
        }
    }

    internal static bool IsEncodedBitstreamValid(WebpFrame frame, WebpBitstreamKind bitstreamKind)
    {
        if (frame.EncodedBitstream == null)
        {
            return false;
        }

        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        if (bitstreamKind == WebpBitstreamKind.Lossless)
        {
            chunks.Add(new WebpRiffChunk(WebpConstants.FourCcVp8LString, frame.EncodedBitstream));
        }
        else if (bitstreamKind == WebpBitstreamKind.Lossy)
        {
            chunks.Add(new WebpRiffChunk(WebpConstants.FourCcVp8String, frame.EncodedBitstream));
        }
        else
        {
            return false;
        }

        WebpBasicContainerParseResult parsed = WebpBasicContainerParser.Parse(WebpRiffWriter.Write(WebpConstants.FourCcWebpString, chunks), WebpStrictness.Strict);
        return parsed.IsValid;
    }

    internal static void EncodeFramePixels(WebpFrame frame, WebpPixelBuffer pixels, WebpEncodeOptions options)
    {
        if (frame.BitstreamKind == WebpBitstreamKind.Lossy)
        {
            ValidateEncodeAlphaRecursion(pixels, options);
            WebpPixelBuffer colorBuffer = WebpLossyAlphaEncoder.PrepareLossyColorBuffer(pixels, options);
            frame.EncodedBitstream = WebpVp8KeyFrameEncoder.Encode(colorBuffer, options);
            frame.AlphaData = WebpLossyAlphaEncoder.EncodeAlpha(pixels, options);
            return;
        }

        frame.EncodedBitstream = WebpVp8LEncoder.Encode(pixels, options);
    }

    internal static WebpBitstreamKind SelectFrameBitstreamKind(WebpFrame frame, WebpPixelBuffer pixels, WebpEncodeOptions options)
    {
        if (frame.EncodeOptions != null && frame.EncodeOptions.Mode != WebpEncodeMode.Automatic)
        {
            ValidateMode(frame.EncodeOptions.Mode);
            if (frame.EncodeOptions.Mode == WebpEncodeMode.Lossy)
            {
                return WebpBitstreamKind.Lossy;
            }

            return WebpBitstreamKind.Lossless;
        }

        if (frame.BitstreamKind == WebpBitstreamKind.Lossy || frame.BitstreamKind == WebpBitstreamKind.Lossless)
        {
            return frame.BitstreamKind;
        }

        if (frame.BitstreamKind == WebpBitstreamKind.Unknown)
        {
            return SelectAutomaticMode(pixels, options).BitstreamKind;
        }

        throw new ArgumentOutOfRangeException("BitstreamKind");
    }

    internal static int GetFrameWidth(WebpFrame frame)
    {
        if (frame.Width > 0)
        {
            return frame.Width;
        }

        if (frame.Pixels == null)
        {
            return 0;
        }

        return frame.Pixels.Width;
    }

    internal static int GetFrameHeight(WebpFrame frame)
    {
        if (frame.Height > 0)
        {
            return frame.Height;
        }

        if (frame.Pixels == null)
        {
            return 0;
        }

        return frame.Pixels.Height;
    }

    internal static WebpMetadata CopyMetadataForEncode(WebpMetadata metadata, WebpMetadataCopyPolicy policy)
    {
        WebpMetadata copy = new WebpMetadata();
        if (policy == WebpMetadataCopyPolicy.None)
        {
            return copy;
        }

        if (policy == WebpMetadataCopyPolicy.All || policy == WebpMetadataCopyPolicy.Icc || policy == WebpMetadataCopyPolicy.Selected)
        {
            copy.IccProfile = CopyNullableBytes(metadata.IccProfile);
        }

        if (policy == WebpMetadataCopyPolicy.All || policy == WebpMetadataCopyPolicy.Exif || policy == WebpMetadataCopyPolicy.Selected)
        {
            copy.Exif = CopyNullableBytes(metadata.Exif);
        }

        if (policy == WebpMetadataCopyPolicy.All || policy == WebpMetadataCopyPolicy.Xmp || policy == WebpMetadataCopyPolicy.Selected)
        {
            copy.Xmp = CopyNullableBytes(metadata.Xmp);
        }

        return copy;
    }

    internal static void CopyUnknownChunksForEncode(List<WebpUnknownChunk> destination, List<WebpUnknownChunk> source, WebpUnknownChunkPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (policy == WebpUnknownChunkPolicy.Strip)
        {
            return;
        }

        int index = 0;
        while (index < source.Count)
        {
            WebpUnknownChunk unknown = source[index];
            if (unknown == null)
            {
                throw new ArgumentException("unknown chunk list contains a null chunk", "image");
            }

            destination.Add(new WebpUnknownChunk(unknown.FourCc, unknown.Data));
            index = index + 1;
        }
    }

    internal static byte[]? CopyNullableBytes(byte[]? source)
    {
        if (source == null)
        {
            return null;
        }

        return WebpUnknownChunk.CopyBytes(source);
    }

    internal static byte[] ApplyDeterministicSerialization(byte[] data, WebpEncodeOptions options)
    {
        if (!options.Deterministic)
        {
            return data;
        }

        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        if (riff.HasErrors() || riff.FormatFourCc != WebpConstants.FourCcWebpString)
        {
            throw new InvalidOperationException("deterministic encode produced an invalid RIFF WEBP container");
        }

        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            ReadOnlySpan<byte> payload = WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength);
            chunks.Add(new WebpRiffChunk(chunk.FourCc, payload.ToArray()));
            index = index + 1;
        }

        return WebpUnknownChunkMux.WritePreservingOrder(WebpConstants.FourCcWebpString, chunks);
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

    internal static void ValidatePixelBufferLimits(WebpPixelBuffer buffer, WebpSecurityLimits limits)
    {
        ValidatePixelRectangle(buffer.Width, buffer.Height, limits, "pixel buffer dimensions exceed configured security limits");
        ValidateTotalPixels((long)buffer.Width * (long)buffer.Height, limits);
        WebpSecurityLimitValidator.ValidateOutputBytes((long)buffer.Width * (long)buffer.Height * 4L, limits, "buffer");
    }

    internal static void ValidateImageModelLimits(WebpImage image, WebpSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(image.Metadata);
        ArgumentNullException.ThrowIfNull(image.UnknownChunks);

        if (image.Frames.Count > limits.MaxFrameCount)
        {
            throw new ArgumentException("animation frame count exceeds the configured security limit", "image");
        }

        if (image.CanvasWidth > 0 || image.CanvasHeight > 0)
        {
            ValidatePixelRectangle(image.CanvasWidth, image.CanvasHeight, limits, "canvas dimensions exceed configured security limits");
        }

        int chunkCount = CountImageModelChunks(image);
        if (chunkCount > limits.MaxChunkCount)
        {
            throw new ArgumentException("chunk count exceeds the configured security limit", "image");
        }

        long totalPixels = 0;
        int index = 0;
        while (index < image.Frames.Count)
        {
            WebpFrame frame = image.Frames[index];
            if (frame == null)
            {
                throw new ArgumentException("image frame list contains a null frame", "image");
            }

            int width = GetFrameLimitWidth(frame);
            int height = GetFrameLimitHeight(frame);
            if (width > 0 || height > 0)
            {
                ValidatePixelRectangle(width, height, limits, "frame dimensions exceed configured security limits");
                totalPixels = AddPixelCount(totalPixels, (long)width * (long)height);
            }

            index = index + 1;
        }

        ValidateTotalPixels(totalPixels, limits);
    }

    internal static void ValidateRetainedPayloadLimits(WebpImage image, WebpEncodeOptions options)
    {
        long metadataBytes = GetCopiedMetadataBytes(image.Metadata, options.MetadataCopyPolicy);
        long unknownBytes = 0;
        long aggregateBytes = metadataBytes;
        int index = 0;
        while (options.UnknownChunkPolicy == WebpUnknownChunkPolicy.Preserve && index < image.UnknownChunks.Count)
        {
            WebpUnknownChunk unknown = image.UnknownChunks[index];
            if (unknown != null)
            {
                unknownBytes = AddPixelCount(unknownBytes, unknown.Data.Length);
                aggregateBytes = AddPixelCount(aggregateBytes, unknown.Data.Length);
            }

            index = index + 1;
        }

        index = 0;
        while (index < image.Frames.Count)
        {
            WebpFrame frame = image.Frames[index];
            if (frame != null)
            {
                if (frame.EncodedBitstream != null)
                {
                    aggregateBytes = AddPixelCount(aggregateBytes, frame.EncodedBitstream.Length);
                }

                if (frame.AlphaData != null)
                {
                    aggregateBytes = AddPixelCount(aggregateBytes, frame.AlphaData.Length);
                }

                if (frame.Pixels != null)
                {
                    aggregateBytes = AddPixelCount(aggregateBytes, (long)frame.Pixels.Width * (long)frame.Pixels.Height * 4L);
                }

                int unknownIndex = 0;
                while (options.UnknownChunkPolicy == WebpUnknownChunkPolicy.Preserve && unknownIndex < frame.UnknownChunks.Count)
                {
                    WebpUnknownChunk unknown = frame.UnknownChunks[unknownIndex];
                    if (unknown != null)
                    {
                        unknownBytes = AddPixelCount(unknownBytes, unknown.Data.Length);
                        aggregateBytes = AddPixelCount(aggregateBytes, unknown.Data.Length);
                    }

                    unknownIndex = unknownIndex + 1;
                }
            }

            index = index + 1;
        }

        if (metadataBytes > options.Limits.MaxMetadataBytes)
        {
            throw new ArgumentException("metadata payload size exceeds the configured security limit", "image");
        }

        if (unknownBytes > options.Limits.MaxUnknownChunkBytes)
        {
            throw new ArgumentException("unknown chunk payload size exceeds the configured security limit", "image");
        }

        WebpSecurityLimitValidator.ValidateOutputBytes(aggregateBytes, options.Limits, "image");
    }

    internal static long GetCopiedMetadataBytes(WebpMetadata metadata, WebpMetadataCopyPolicy policy)
    {
        long bytes = 0;
        if ((policy == WebpMetadataCopyPolicy.All || policy == WebpMetadataCopyPolicy.Icc || policy == WebpMetadataCopyPolicy.Selected)
            && metadata.IccProfile != null)
        {
            bytes = AddPixelCount(bytes, metadata.IccProfile.Length);
        }

        if ((policy == WebpMetadataCopyPolicy.All || policy == WebpMetadataCopyPolicy.Exif || policy == WebpMetadataCopyPolicy.Selected)
            && metadata.Exif != null)
        {
            bytes = AddPixelCount(bytes, metadata.Exif.Length);
        }

        if ((policy == WebpMetadataCopyPolicy.All || policy == WebpMetadataCopyPolicy.Xmp || policy == WebpMetadataCopyPolicy.Selected)
            && metadata.Xmp != null)
        {
            bytes = AddPixelCount(bytes, metadata.Xmp.Length);
        }

        return bytes;
    }

    internal static int CountImageModelChunks(WebpImage image)
    {
        int count = 0;
        if (image.Metadata.IccProfile != null)
        {
            count = count + 1;
        }

        if (image.Metadata.Exif != null)
        {
            count = count + 1;
        }

        if (image.Metadata.Xmp != null)
        {
            count = count + 1;
        }

        count = count + image.UnknownChunks.Count;
        int index = 0;
        while (index < image.Frames.Count)
        {
            WebpFrame frame = image.Frames[index];
            if (frame != null)
            {
                if (frame.AlphaData != null)
                {
                    count = count + 1;
                }

                if (frame.EncodedBitstream != null)
                {
                    count = count + 1;
                }

                count = count + frame.UnknownChunks.Count;
            }

            index = index + 1;
        }

        return count;
    }

    internal static int GetFrameLimitWidth(WebpFrame frame)
    {
        if (frame.Width > 0)
        {
            return frame.Width;
        }

        if (frame.Pixels != null)
        {
            return frame.Pixels.Width;
        }

        return 0;
    }

    internal static int GetFrameLimitHeight(WebpFrame frame)
    {
        if (frame.Height > 0)
        {
            return frame.Height;
        }

        if (frame.Pixels != null)
        {
            return frame.Pixels.Height;
        }

        return 0;
    }

    internal static void ValidatePixelRectangle(int width, int height, WebpSecurityLimits limits, string message)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (width > limits.MaxCanvasWidth || height > limits.MaxCanvasHeight)
        {
            throw new ArgumentException(message, "image");
        }

        long pixels = (long)width * (long)height;
        if (pixels > limits.MaxCanvasPixels)
        {
            throw new ArgumentException(message, "image");
        }
    }

    internal static void ValidateTotalPixels(long totalPixels, WebpSecurityLimits limits)
    {
        if (totalPixels > limits.MaxTotalPixels)
        {
            throw new ArgumentException("total pixel count exceeds the configured security limit", "image");
        }
    }

    internal static void ValidateEncodeAlphaRecursion(WebpPixelBuffer pixels, WebpEncodeOptions options)
    {
        if (options.AlphaCompression == WebpAlphaCompressionMethod.Lossless
            && options.LossyAlphaMode == WebpLossyAlphaMode.Preserve
            && options.Limits.MaxRecursionDepth < 2
            && WebpLossyAlphaEncoder.HasNonOpaqueAlpha(pixels))
        {
            throw new ArgumentException("compressed alpha encode exceeds the configured recursion depth limit", "options");
        }
    }

    internal static void ValidateEncodedOutputLimits(byte[] data, WebpSecurityLimits limits)
    {
        if (data.Length > limits.MaxRiffSize)
        {
            throw new ArgumentException("encoded RIFF size exceeds the configured security limit", "image");
        }

        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        if (riff.Chunks.Count > limits.MaxChunkCount)
        {
            throw new ArgumentException("encoded chunk count exceeds the configured security limit", "image");
        }

        WebpSecurityLimitValidator.ValidateOutputBytes(data.Length, limits, "image");
    }

    internal static long AddPixelCount(long totalPixels, long pixels)
    {
        if (totalPixels > long.MaxValue - pixels)
        {
            return long.MaxValue;
        }

        return totalPixels + pixels;
    }

    internal static WebpEncodeMode SelectMode(WebpPixelBuffer buffer, WebpEncodeOptions options, bool hasAlpha)
    {
        if (options.Mode != WebpEncodeMode.Automatic)
        {
            return options.Mode;
        }

        if (options.NearLosslessQuality < 100)
        {
            return WebpEncodeMode.NearLossless;
        }

        if (CanUseSimpleLossy(buffer) && options.Quality < WebpConstants.DefaultQuality)
        {
            return WebpEncodeMode.Lossy;
        }

        if (hasAlpha && !options.LosslessAlpha && CanUseSimpleLossy(buffer))
        {
            return WebpEncodeMode.Lossy;
        }

        return WebpEncodeMode.Lossless;
    }

    internal static bool CanUseSimpleLossy(WebpPixelBuffer buffer)
    {
        return buffer.Width <= 16 && buffer.Height <= 16;
    }

    internal static void ValidateMode(WebpEncodeMode mode)
    {
        if (mode < WebpEncodeMode.Automatic || mode > WebpEncodeMode.NearLossless)
        {
            throw new ArgumentOutOfRangeException("Mode");
        }
    }

    internal static void ValidateUnknownChunkPolicy(WebpUnknownChunkPolicy policy)
    {
        if (policy < WebpUnknownChunkPolicy.Preserve || policy > WebpUnknownChunkPolicy.Strip)
        {
            throw new ArgumentOutOfRangeException("UnknownChunkPolicy");
        }
    }

    internal static void ValidateMetadataCopyPolicy(WebpMetadataCopyPolicy policy)
    {
        if (policy < WebpMetadataCopyPolicy.None || policy > WebpMetadataCopyPolicy.Selected)
        {
            throw new ArgumentOutOfRangeException("MetadataCopyPolicy");
        }
    }
}
