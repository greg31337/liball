using System;
using System.Collections.Generic;

namespace webp;

internal static class WebpPublicModelValidator
{
    internal static void ValidateDecodeOptions(WebpDecodeOptions options)
    {
        ValidateInspectionOptions(options);

        if (options.UnknownChunkPolicy != WebpUnknownChunkPolicy.Preserve
            && options.UnknownChunkPolicy != WebpUnknownChunkPolicy.Strip)
        {
            throw new ArgumentOutOfRangeException("UnknownChunkPolicy");
        }

        if (options.AnimationDecodeMode != WebpAnimationDecodeMode.IndependentFrames
            && options.AnimationDecodeMode != WebpAnimationDecodeMode.CompositedFrames)
        {
            throw new ArgumentOutOfRangeException("AnimationDecodeMode");
        }

        if (options.FrameIndex < -1)
        {
            throw new ArgumentOutOfRangeException("FrameIndex");
        }

        if (options.FrameIndex >= 0 && options.AnimationDecodeMode == WebpAnimationDecodeMode.CompositedFrames)
        {
            throw new ArgumentException("an explicit frame index cannot be combined with composited animation decoding", "options");
        }
    }

    internal static void ValidateInspectionOptions(WebpDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);
        WebpDecoder.ValidateSecurityLimits(options.Limits);

        if (options.Strictness != WebpStrictness.Permissive && options.Strictness != WebpStrictness.Strict)
        {
            throw new ArgumentOutOfRangeException("Strictness");
        }
    }

    internal static void ValidateStillFrameSelection(WebpDecodeOptions options)
    {
        if (options.FrameIndex > 0)
        {
            throw new ArgumentOutOfRangeException("FrameIndex");
        }
    }

    internal static void ValidateEncodeOptions(WebpEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);
        WebpEncoder.ValidateSecurityLimits(options.Limits);
        WebpEncoder.ValidateMode(options.Mode);
        WebpEncoder.ValidateUnknownChunkPolicy(options.UnknownChunkPolicy);
        WebpEncoder.ValidateMetadataCopyPolicy(options.MetadataCopyPolicy);
        WebpLossyEncoderSettings.ValidateOptions(options);
    }

    internal static void ValidateImageForEncoding(WebpImage image, WebpEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(image.Metadata);
        ArgumentNullException.ThrowIfNull(image.Frames);
        ArgumentNullException.ThrowIfNull(image.UnknownChunks);
        WebpContainerWriter.ValidateMetadata(image.Metadata);

        if (image.CanvasWidth < 0)
        {
            throw new ArgumentOutOfRangeException("CanvasWidth");
        }

        if (image.CanvasHeight < 0)
        {
            throw new ArgumentOutOfRangeException("CanvasHeight");
        }

        if ((image.CanvasWidth == 0) != (image.CanvasHeight == 0))
        {
            throw new ArgumentException("canvas width and height must both be zero or both be positive", "image");
        }

        bool animated = image.HasAnimation || image.Frames.Count > 1;
        if (animated && image.CanvasWidth == 0)
        {
            throw new ArgumentException("animated image encode requires positive canvas dimensions", "image");
        }

        ValidateUnknownChunks(image.UnknownChunks, "image");
        int index = 0;
        while (index < image.Frames.Count)
        {
            WebpFrame frame = image.Frames[index];
            if (frame == null)
            {
                throw new ArgumentException("image frame list contains a null frame", "image");
            }

            ValidateFrameForEncoding(frame, options, animated, image.CanvasWidth, image.CanvasHeight);
            index = index + 1;
        }
    }

    internal static void ValidateFrameForEncoding(WebpFrame frame, WebpEncodeOptions options, bool animated, int canvasWidth, int canvasHeight)
    {
        ArgumentNullException.ThrowIfNull(frame.UnknownChunks);
        ValidateUnknownChunks(frame.UnknownChunks, "image");

        if (frame.X < 0)
        {
            throw new ArgumentOutOfRangeException("X");
        }

        if (frame.Y < 0)
        {
            throw new ArgumentOutOfRangeException("Y");
        }

        if (frame.Width < 0)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (frame.Height < 0)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        if ((frame.Width == 0) != (frame.Height == 0))
        {
            throw new ArgumentException("frame width and height must both be zero or both be positive", "image");
        }

        if (frame.DurationMilliseconds < 0 || frame.DurationMilliseconds > 16777215)
        {
            throw new ArgumentOutOfRangeException("DurationMilliseconds");
        }

        if (frame.BlendMode != WebpFrameBlendMode.AlphaBlend && frame.BlendMode != WebpFrameBlendMode.Replace)
        {
            throw new ArgumentOutOfRangeException("BlendMode");
        }

        if (frame.DisposeMode != WebpFrameDisposeMode.DoNotDispose
            && frame.DisposeMode != WebpFrameDisposeMode.DisposeToBackground)
        {
            throw new ArgumentOutOfRangeException("DisposeMode");
        }

        if (frame.BitstreamKind != WebpBitstreamKind.Unknown
            && frame.BitstreamKind != WebpBitstreamKind.Lossy
            && frame.BitstreamKind != WebpBitstreamKind.Lossless)
        {
            throw new ArgumentOutOfRangeException("BitstreamKind");
        }

        if (frame.Pixels == null && frame.EncodedBitstream == null)
        {
            throw new ArgumentException("each image frame requires pixels or an encoded bitstream", "image");
        }

        if (frame.Pixels != null)
        {
            WebpPixelBufferHelper.ValidateRgbaBuffer(frame.Pixels);
            if (frame.Width > 0 && (frame.Width != frame.Pixels.Width || frame.Height != frame.Pixels.Height))
            {
                throw new ArgumentException("frame dimensions must match its pixel buffer", "image");
            }

            if (frame.EncodedBitstream == null && frame.EncodeOptions != null)
            {
                WebpEncodeOptions frameOptions = WebpEncoder.ResolveFrameEncodeOptions(frame, options);
                ValidateEncodeOptions(frameOptions);
            }
        }

        if (frame.EncodedBitstream != null && frame.EncodedBitstream.Length == 0)
        {
            throw new ArgumentException("encoded frame bitstream must not be empty", "image");
        }

        if (frame.AlphaData != null && frame.AlphaData.Length == 0)
        {
            throw new ArgumentException("frame alpha payload must not be empty", "image");
        }

        if (frame.AlphaData != null && frame.BitstreamKind == WebpBitstreamKind.Lossless)
        {
            throw new ArgumentException("lossless frames cannot contain a separate alpha payload", "image");
        }

        if (animated)
        {
            if ((frame.X & 1) != 0 || (frame.Y & 1) != 0)
            {
                throw new ArgumentException("animated frame positions must be even", "image");
            }

            int width = frame.Width;
            int height = frame.Height;
            if (width == 0 && frame.Pixels != null)
            {
                width = frame.Pixels.Width;
                height = frame.Pixels.Height;
            }

            if (width > 0 && ((long)frame.X + (long)width > canvasWidth || (long)frame.Y + (long)height > canvasHeight))
            {
                throw new ArgumentException("animated frame rectangle exceeds the canvas", "image");
            }
        }
        else if (frame.X != 0 || frame.Y != 0)
        {
            throw new ArgumentException("still-image frame offsets must be zero", "image");
        }
    }

    internal static void ValidateUnknownChunks(List<WebpUnknownChunk> chunks, string parameterName)
    {
        int index = 0;
        while (index < chunks.Count)
        {
            WebpUnknownChunk chunk = chunks[index];
            if (chunk == null)
            {
                throw new ArgumentException("unknown chunk list contains a null chunk", parameterName);
            }

            WebpUnknownChunkMux.ValidateFourCc(chunk.FourCc, parameterName);
            ArgumentNullException.ThrowIfNull(chunk.Data);
            if (WebpUnknownChunkMux.IsKnownWebpChunk(chunk.FourCc))
            {
                throw new ArgumentException("unknown chunks must not use known WebP FourCC values", parameterName);
            }

            index = index + 1;
        }
    }
}
