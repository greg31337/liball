using System;
using System.Collections.Generic;

namespace webp;

internal static class WebpAnimationComposer
{
    internal static List<WebpFrame> ComposeFrames(WebpImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(image.Frames);
        return ComposeFrames(image.Frames, image.CanvasWidth, image.CanvasHeight, image.BackgroundColorBgra);
    }

    internal static List<WebpFrame> ComposeFrames(List<WebpFrame> frames, int canvasWidth, int canvasHeight, uint backgroundColorBgra)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ValidateCanvas(canvasWidth, canvasHeight);
        ValidateFrames(frames, canvasWidth, canvasHeight);

        WebpPixelBuffer canvas = WebpPixelBufferHelper.AllocateRgba(canvasWidth, canvasHeight);
        FillRectangle(canvas, 0, 0, canvasWidth, canvasHeight, backgroundColorBgra);

        List<WebpFrame> compositedFrames = new List<WebpFrame>();
        int index = 0;
        while (index < frames.Count)
        {
            WebpFrame frame = frames[index];
            ApplyFrame(canvas, frame);
            compositedFrames.Add(CreateCompositedFrame(canvas, frame));
            if (frame.DisposeMode == WebpFrameDisposeMode.DisposeToBackground)
            {
                FillRectangle(canvas, frame.X, frame.Y, GetFrameWidth(frame), GetFrameHeight(frame), backgroundColorBgra);
            }

            index = index + 1;
        }

        return compositedFrames;
    }

    internal static void ValidateCanvas(int canvasWidth, int canvasHeight)
    {
        if (canvasWidth <= 0 || canvasHeight <= 0)
        {
            throw new ArgumentOutOfRangeException("canvasWidth");
        }
    }

    internal static void ValidateFrames(List<WebpFrame> frames, int canvasWidth, int canvasHeight)
    {
        int index = 0;
        while (index < frames.Count)
        {
            WebpFrame frame = frames[index];
            if (frame == null)
            {
                throw new ArgumentException("frame list contains a null frame", "frames");
            }

            ValidateFrame(frame, canvasWidth, canvasHeight);
            index = index + 1;
        }
    }

    internal static void ValidateFrame(WebpFrame frame, int canvasWidth, int canvasHeight)
    {
        if (frame.Pixels == null)
        {
            throw new ArgumentException("animation frame composition requires decoded frame pixels", "frames");
        }

        if (frame.X < 0 || frame.Y < 0)
        {
            throw new ArgumentException("animation frame position is invalid", "frames");
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

        if (IsEmptyTimingFrame(frame))
        {
            if (frame.X > canvasWidth || frame.Y > canvasHeight)
            {
                throw new ArgumentException("empty animation frame position exceeds the canvas", "frames");
            }

            return;
        }

        WebpPixelBufferHelper.ValidateRgbaBuffer(frame.Pixels);

        int frameWidth = GetFrameWidth(frame);
        int frameHeight = GetFrameHeight(frame);
        if (frameWidth <= 0 || frameHeight <= 0)
        {
            throw new ArgumentException("animation frame dimensions must be non-zero", "frames");
        }

        if (frame.Width > 0 && frame.Width != frame.Pixels.Width)
        {
            throw new ArgumentException("animation frame width does not match pixel buffer width", "frames");
        }

        if (frame.Height > 0 && frame.Height != frame.Pixels.Height)
        {
            throw new ArgumentException("animation frame height does not match pixel buffer height", "frames");
        }

        long right = (long)frame.X + (long)frameWidth;
        long bottom = (long)frame.Y + (long)frameHeight;
        if (right > canvasWidth || bottom > canvasHeight)
        {
            throw new ArgumentException("animation frame rectangle exceeds the canvas", "frames");
        }
    }

    internal static bool IsEmptyTimingFrame(WebpFrame frame)
    {
        if (frame.Pixels == null || frame.Pixels.Rgba == null)
        {
            return false;
        }

        return frame.Width == 0
            && frame.Height == 0
            && frame.Pixels.Width == 0
            && frame.Pixels.Height == 0
            && frame.Pixels.Stride == 0
            && frame.Pixels.Rgba.Length == 0;
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

    internal static void ApplyFrame(WebpPixelBuffer canvas, WebpFrame frame)
    {
        if (frame.Pixels == null)
        {
            throw new ArgumentException("animation frame composition requires decoded frame pixels", "frames");
        }

        if (frame.BlendMode == WebpFrameBlendMode.Replace)
        {
            CopyFramePixels(canvas, frame, frame.Pixels);
        }
        else
        {
            BlendFramePixels(canvas, frame, frame.Pixels);
        }
    }

    internal static void CopyFramePixels(WebpPixelBuffer canvas, WebpFrame frame, WebpPixelBuffer source)
    {
        int row = 0;
        while (row < source.Height)
        {
            int sourceRow = row * source.Stride;
            int destinationRow = (frame.Y + row) * canvas.Stride + frame.X * 4;
            Array.Copy(source.Rgba, sourceRow, canvas.Rgba, destinationRow, source.Width * 4);
            row = row + 1;
        }
    }

    internal static void BlendFramePixels(WebpPixelBuffer canvas, WebpFrame frame, WebpPixelBuffer source)
    {
        int row = 0;
        while (row < source.Height)
        {
            int sourceRow = row * source.Stride;
            int destinationRow = (frame.Y + row) * canvas.Stride + frame.X * 4;
            int column = 0;
            while (column < source.Width)
            {
                int sourcePixel = sourceRow + column * 4;
                int destinationPixel = destinationRow + column * 4;
                BlendPixel(source.Rgba, sourcePixel, canvas.Rgba, destinationPixel);
                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal static void BlendPixel(byte[] source, int sourcePixel, byte[] destination, int destinationPixel)
    {
        int sourceAlpha = source[sourcePixel + 3];
        if (sourceAlpha == 0)
        {
            return;
        }

        if (sourceAlpha == 255)
        {
            destination[destinationPixel] = source[sourcePixel];
            destination[destinationPixel + 1] = source[sourcePixel + 1];
            destination[destinationPixel + 2] = source[sourcePixel + 2];
            destination[destinationPixel + 3] = source[sourcePixel + 3];
            return;
        }

        int destinationAlpha = destination[destinationPixel + 3];
        int inverseSourceAlpha = 255 - sourceAlpha;
        int outputAlpha = sourceAlpha + DivideBy255(destinationAlpha * inverseSourceAlpha);
        if (outputAlpha == 0)
        {
            destination[destinationPixel] = 0;
            destination[destinationPixel + 1] = 0;
            destination[destinationPixel + 2] = 0;
            destination[destinationPixel + 3] = 0;
            return;
        }

        destination[destinationPixel] = BlendChannel(source[sourcePixel], sourceAlpha, destination[destinationPixel], destinationAlpha, inverseSourceAlpha, outputAlpha);
        destination[destinationPixel + 1] = BlendChannel(source[sourcePixel + 1], sourceAlpha, destination[destinationPixel + 1], destinationAlpha, inverseSourceAlpha, outputAlpha);
        destination[destinationPixel + 2] = BlendChannel(source[sourcePixel + 2], sourceAlpha, destination[destinationPixel + 2], destinationAlpha, inverseSourceAlpha, outputAlpha);
        destination[destinationPixel + 3] = (byte)outputAlpha;
    }

    internal static byte BlendChannel(byte sourceChannel, int sourceAlpha, byte destinationChannel, int destinationAlpha, int inverseSourceAlpha, int outputAlpha)
    {
        int sourcePremultiplied = sourceChannel * sourceAlpha;
        int destinationPremultiplied = destinationChannel * destinationAlpha;
        int outputPremultiplied = sourcePremultiplied + DivideBy255(destinationPremultiplied * inverseSourceAlpha);
        return (byte)((outputPremultiplied + outputAlpha / 2) / outputAlpha);
    }

    internal static int DivideBy255(int value)
    {
        return (value + 127) / 255;
    }

    internal static WebpFrame CreateCompositedFrame(WebpPixelBuffer canvas, WebpFrame sourceFrame)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = 0;
        frame.Y = 0;
        frame.Width = canvas.Width;
        frame.Height = canvas.Height;
        frame.DurationMilliseconds = sourceFrame.DurationMilliseconds;
        frame.BlendMode = WebpFrameBlendMode.Replace;
        frame.DisposeMode = WebpFrameDisposeMode.DoNotDispose;
        frame.Pixels = WebpPixelBufferHelper.Copy(canvas);
        return frame;
    }

    internal static void FillRectangle(WebpPixelBuffer buffer, int x, int y, int width, int height, uint backgroundColorBgra)
    {
        byte red = (byte)((backgroundColorBgra >> 16) & 255U);
        byte green = (byte)((backgroundColorBgra >> 8) & 255U);
        byte blue = (byte)(backgroundColorBgra & 255U);
        byte alpha = (byte)((backgroundColorBgra >> 24) & 255U);

        int row = 0;
        while (row < height)
        {
            int rowOffset = (y + row) * buffer.Stride + x * 4;
            int column = 0;
            while (column < width)
            {
                int pixel = rowOffset + column * 4;
                buffer.Rgba[pixel] = red;
                buffer.Rgba[pixel + 1] = green;
                buffer.Rgba[pixel + 2] = blue;
                buffer.Rgba[pixel + 3] = alpha;
                column = column + 1;
            }

            row = row + 1;
        }
    }
}
