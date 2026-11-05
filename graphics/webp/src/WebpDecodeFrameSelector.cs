using System;
using System.Collections.Generic;

namespace webp;

internal static class WebpDecodeFrameSelector
{
    internal static List<WebpFrame> SelectFrames(List<WebpFrame> frames, WebpDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(options);
        ValidateFrameIndex(frames, options);

        List<WebpFrame> selectedFrames = new List<WebpFrame>();
        int index = 0;
        while (index < frames.Count)
        {
            WebpFrame frame = frames[index];
            if (frame == null)
            {
                throw new ArgumentException("frame list contains a null frame", "frames");
            }

            if (ShouldSelectFrame(index, options))
            {
                selectedFrames.Add(CopyFrame(frame));
            }

            index = index + 1;
        }

        return selectedFrames;
    }

    internal static WebpImage SelectFrames(WebpImage image, WebpDecodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(image.Metadata);
        ArgumentNullException.ThrowIfNull(image.Frames);
        ArgumentNullException.ThrowIfNull(image.UnknownChunks);
        ArgumentNullException.ThrowIfNull(options);

        WebpImage selected = new WebpImage();
        selected.CanvasWidth = image.CanvasWidth;
        selected.CanvasHeight = image.CanvasHeight;
        selected.HasAlpha = image.HasAlpha;
        selected.LoopCount = image.LoopCount;
        selected.BackgroundColorBgra = image.BackgroundColorBgra;
        selected.Metadata = CopyMetadata(image.Metadata);
        CopyUnknownChunks(selected.UnknownChunks, image.UnknownChunks);

        List<WebpFrame> frames = SelectFrames(image.Frames, options);
        int index = 0;
        while (index < frames.Count)
        {
            selected.Frames.Add(frames[index]);
            index = index + 1;
        }

        selected.HasAnimation = image.HasAnimation && selected.Frames.Count > 1;
        return selected;
    }

    internal static void ValidateFrameIndex(List<WebpFrame> frames, WebpDecodeOptions options)
    {
        if (options.FrameIndex < -1)
        {
            throw new ArgumentOutOfRangeException("FrameIndex");
        }

        if (options.FrameIndex >= frames.Count)
        {
            throw new ArgumentOutOfRangeException("FrameIndex");
        }
    }

    internal static bool ShouldSelectFrame(int index, WebpDecodeOptions options)
    {
        if (options.FrameIndex >= 0)
        {
            return index == options.FrameIndex;
        }

        if (options.FirstFrameOnly)
        {
            return index == 0;
        }

        return true;
    }

    internal static WebpFrame CopyFrame(WebpFrame source)
    {
        ArgumentNullException.ThrowIfNull(source.UnknownChunks);

        WebpFrame frame = new WebpFrame();
        frame.X = source.X;
        frame.Y = source.Y;
        frame.Width = source.Width;
        frame.Height = source.Height;
        frame.DurationMilliseconds = source.DurationMilliseconds;
        frame.BlendMode = source.BlendMode;
        frame.DisposeMode = source.DisposeMode;
        frame.BitstreamKind = source.BitstreamKind;
        frame.Pixels = CopyPixelBuffer(source.Pixels);
        frame.EncodedBitstream = CopyNullableBytes(source.EncodedBitstream);
        frame.AlphaData = CopyNullableBytes(source.AlphaData);
        CopyUnknownChunks(frame.UnknownChunks, source.UnknownChunks);
        return frame;
    }

    internal static WebpPixelBuffer? CopyPixelBuffer(WebpPixelBuffer? source)
    {
        if (source == null)
        {
            return null;
        }

        return WebpPixelBufferHelper.Copy(source);
    }

    internal static WebpMetadata CopyMetadata(WebpMetadata source)
    {
        WebpMetadata metadata = new WebpMetadata();
        metadata.IccProfile = CopyNullableBytes(source.IccProfile);
        metadata.Exif = CopyNullableBytes(source.Exif);
        metadata.Xmp = CopyNullableBytes(source.Xmp);
        return metadata;
    }

    internal static void CopyUnknownChunks(List<WebpUnknownChunk> destination, List<WebpUnknownChunk> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            WebpUnknownChunk unknown = source[index];
            if (unknown == null)
            {
                throw new ArgumentException("unknown chunk list contains a null chunk", "source");
            }

            destination.Add(new WebpUnknownChunk(unknown.FourCc, unknown.Data));
            index = index + 1;
        }
    }

    internal static byte[]? CopyNullableBytes(byte[]? data)
    {
        if (data == null)
        {
            return null;
        }

        return WebpUnknownChunk.CopyBytes(data);
    }
}
