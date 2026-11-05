using System;

namespace webp;

internal static class WebpDecodeTransformProcessor
{
    internal static void ValidateOptions(WebpDecodeOptions options)
    {
        WebpPublicModelValidator.ValidateDecodeOptions(options);

        if (options.AspectRatioMode != WebpAspectRatioMode.Stretch && options.AspectRatioMode != WebpAspectRatioMode.Preserve)
        {
            throw new ArgumentOutOfRangeException("AspectRatioMode");
        }

        if (options.CropX < 0 || options.CropY < 0 || options.CropWidth < 0 || options.CropHeight < 0)
        {
            throw new ArgumentOutOfRangeException("CropWidth");
        }

        if ((options.CropWidth == 0) != (options.CropHeight == 0))
        {
            throw new ArgumentException("crop width and height must both be zero or both be positive", "options");
        }

        if (options.CropWidth == 0 && (options.CropX != 0 || options.CropY != 0))
        {
            throw new ArgumentException("crop offsets require a non-zero crop rectangle", "options");
        }

        if (options.TargetWidth < 0 || options.TargetHeight < 0)
        {
            throw new ArgumentOutOfRangeException("TargetWidth");
        }

        if (options.AspectRatioMode == WebpAspectRatioMode.Stretch
            && ((options.TargetWidth == 0) != (options.TargetHeight == 0)))
        {
            throw new ArgumentException("stretch scaling requires both target dimensions", "options");
        }

        if (options.IdentifyOnly && HasRequestedTransform(options))
        {
            throw new ArgumentException("cropping and scaling require pixel decoding", "options");
        }
    }

    internal static bool HasRequestedTransform(WebpDecodeOptions options)
    {
        return options.CropWidth != 0 || options.CropHeight != 0 || options.TargetWidth != 0 || options.TargetHeight != 0;
    }

    internal static WebpDecodeTransform CreateTransform(int sourceCanvasWidth, int sourceCanvasHeight, WebpDecodeOptions options)
    {
        WebpDecodeTransform transform = new WebpDecodeTransform();
        transform.SourceCanvasWidth = sourceCanvasWidth;
        transform.SourceCanvasHeight = sourceCanvasHeight;
        transform.SourceX = options.CropWidth == 0 ? 0 : options.CropX;
        transform.SourceY = options.CropHeight == 0 ? 0 : options.CropY;
        transform.SourceWidth = options.CropWidth == 0 ? sourceCanvasWidth : options.CropWidth;
        transform.SourceHeight = options.CropHeight == 0 ? sourceCanvasHeight : options.CropHeight;
        transform.AspectRatioMode = options.AspectRatioMode;

        long right = (long)transform.SourceX + (long)transform.SourceWidth;
        long bottom = (long)transform.SourceY + (long)transform.SourceHeight;
        if (transform.SourceWidth <= 0 || transform.SourceHeight <= 0 || right > sourceCanvasWidth || bottom > sourceCanvasHeight)
        {
            throw new ArgumentException("crop rectangle exceeds the source canvas", "options");
        }

        SetOutputDimensions(transform, options);
        ValidateOutputDimensions(transform, options.Limits);
        return transform;
    }

    internal static void SetOutputDimensions(WebpDecodeTransform transform, WebpDecodeOptions options)
    {
        if (options.TargetWidth == 0 && options.TargetHeight == 0)
        {
            transform.OutputWidth = transform.SourceWidth;
            transform.OutputHeight = transform.SourceHeight;
            return;
        }

        if (options.AspectRatioMode == WebpAspectRatioMode.Stretch)
        {
            transform.OutputWidth = options.TargetWidth;
            transform.OutputHeight = options.TargetHeight;
            return;
        }

        if (options.TargetWidth == 0)
        {
            transform.OutputHeight = options.TargetHeight;
            transform.OutputWidth = ScaleDimension(transform.SourceWidth, transform.SourceHeight, options.TargetHeight);
            return;
        }

        if (options.TargetHeight == 0)
        {
            transform.OutputWidth = options.TargetWidth;
            transform.OutputHeight = ScaleDimension(transform.SourceHeight, transform.SourceWidth, options.TargetWidth);
            return;
        }

        long widthScale = (long)options.TargetWidth * (long)transform.SourceHeight;
        long heightScale = (long)options.TargetHeight * (long)transform.SourceWidth;
        if (widthScale <= heightScale)
        {
            transform.OutputWidth = options.TargetWidth;
            transform.OutputHeight = ScaleDimension(transform.SourceHeight, transform.SourceWidth, options.TargetWidth);
        }
        else
        {
            transform.OutputHeight = options.TargetHeight;
            transform.OutputWidth = ScaleDimension(transform.SourceWidth, transform.SourceHeight, options.TargetHeight);
        }
    }

    internal static int ScaleDimension(int sourceDimension, int sourceReference, int targetReference)
    {
        long scaled = (long)sourceDimension * (long)targetReference;
        long rounded = (scaled + (sourceReference / 2)) / sourceReference;
        if (rounded < 1)
        {
            return 1;
        }

        if (rounded > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("TargetWidth");
        }

        return (int)rounded;
    }

    internal static void ValidateOutputDimensions(WebpDecodeTransform transform, WebpSecurityLimits limits)
    {
        if (transform.OutputWidth <= 0 || transform.OutputHeight <= 0)
        {
            throw new ArgumentOutOfRangeException("TargetWidth");
        }

        long pixels = (long)transform.OutputWidth * (long)transform.OutputHeight;
        if (transform.OutputWidth > limits.MaxCanvasWidth
            || transform.OutputHeight > limits.MaxCanvasHeight
            || pixels > limits.MaxCanvasPixels)
        {
            throw new ArgumentException("decoded output dimensions exceed configured security limits", "options");
        }
    }

    internal static void ApplyStill(WebpDecodeResult result, WebpDecodeOptions options, int sourceCanvasWidth, int sourceCanvasHeight)
    {
        WebpDecodeTransform transform = CreateTransform(sourceCanvasWidth, sourceCanvasHeight, options);
        result.Image.Transform = transform;
        if (IsIdentity(transform))
        {
            return;
        }

        ValidateTotalOutputPixels(transform, result.Image.Frames.Count, options.Limits);
        result.Image.CanvasWidth = transform.OutputWidth;
        result.Image.CanvasHeight = transform.OutputHeight;
        if (result.Image.Frames.Count == 0)
        {
            return;
        }

        WebpFrame frame = result.Image.Frames[0];
        if (frame.Pixels != null)
        {
            frame.Pixels = TransformPixels(frame.Pixels, transform.SourceX, transform.SourceY, transform.SourceWidth, transform.SourceHeight, transform.OutputWidth, transform.OutputHeight);
        }

        frame.X = 0;
        frame.Y = 0;
        frame.Width = transform.OutputWidth;
        frame.Height = transform.OutputHeight;
        frame.EncodedBitstream = null;
        frame.AlphaData = null;
    }

    internal static void ApplyAnimation(WebpDecodeResult result, WebpDecodeOptions options, int sourceCanvasWidth, int sourceCanvasHeight, bool composited)
    {
        WebpDecodeTransform transform = CreateTransform(sourceCanvasWidth, sourceCanvasHeight, options);
        result.Image.Transform = transform;
        if (IsIdentity(transform))
        {
            return;
        }

        if (composited)
        {
            ValidateTotalOutputPixels(transform, result.Image.Frames.Count, options.Limits);
            ApplyCompositedFrames(result, transform);
        }
        else
        {
            ValidateIndependentOutputPixels(result, transform, options.Limits);
            ApplyIndependentFrames(result, transform);
        }

        result.Image.CanvasWidth = transform.OutputWidth;
        result.Image.CanvasHeight = transform.OutputHeight;
    }

    internal static bool IsIdentity(WebpDecodeTransform transform)
    {
        return transform.SourceX == 0
            && transform.SourceY == 0
            && transform.SourceWidth == transform.SourceCanvasWidth
            && transform.SourceHeight == transform.SourceCanvasHeight
            && transform.OutputWidth == transform.SourceCanvasWidth
            && transform.OutputHeight == transform.SourceCanvasHeight;
    }

    internal static void ApplyCompositedFrames(WebpDecodeResult result, WebpDecodeTransform transform)
    {
        int index = 0;
        while (index < result.Image.Frames.Count)
        {
            WebpFrame frame = result.Image.Frames[index];
            if (frame.Pixels != null)
            {
                frame.Pixels = TransformPixels(frame.Pixels, transform.SourceX, transform.SourceY, transform.SourceWidth, transform.SourceHeight, transform.OutputWidth, transform.OutputHeight);
            }

            frame.X = 0;
            frame.Y = 0;
            frame.Width = transform.OutputWidth;
            frame.Height = transform.OutputHeight;
            frame.EncodedBitstream = null;
            frame.AlphaData = null;
            index = index + 1;
        }
    }

    internal static void ApplyIndependentFrames(WebpDecodeResult result, WebpDecodeTransform transform)
    {
        int index = 0;
        while (index < result.Image.Frames.Count)
        {
            ApplyIndependentFrame(result.Image.Frames[index], transform);
            index = index + 1;
        }
    }

    internal static void ApplyIndependentFrame(WebpFrame frame, WebpDecodeTransform transform)
    {
        int left = Maximum(frame.X, transform.SourceX);
        int top = Maximum(frame.Y, transform.SourceY);
        int right = Minimum(frame.X + frame.Width, transform.SourceX + transform.SourceWidth);
        int bottom = Minimum(frame.Y + frame.Height, transform.SourceY + transform.SourceHeight);
        if (right <= left || bottom <= top)
        {
            frame.X = 0;
            frame.Y = 0;
            frame.Width = 0;
            frame.Height = 0;
            frame.Pixels = new WebpPixelBuffer();
            frame.EncodedBitstream = null;
            frame.AlphaData = null;
            return;
        }

        int outputLeft = ScaleFloor(left - transform.SourceX, transform.SourceWidth, transform.OutputWidth);
        int outputTop = ScaleFloor(top - transform.SourceY, transform.SourceHeight, transform.OutputHeight);
        int outputRight = ScaleCeiling(right - transform.SourceX, transform.SourceWidth, transform.OutputWidth);
        int outputBottom = ScaleCeiling(bottom - transform.SourceY, transform.SourceHeight, transform.OutputHeight);
        int outputWidth = outputRight - outputLeft;
        int outputHeight = outputBottom - outputTop;
        if (frame.Pixels != null)
        {
            frame.Pixels = TransformPixels(frame.Pixels, left - frame.X, top - frame.Y, right - left, bottom - top, outputWidth, outputHeight);
        }

        frame.X = outputLeft;
        frame.Y = outputTop;
        frame.Width = outputWidth;
        frame.Height = outputHeight;
        frame.EncodedBitstream = null;
        frame.AlphaData = null;
    }

    internal static WebpPixelBuffer TransformPixels(WebpPixelBuffer source, int sourceX, int sourceY, int sourceWidth, int sourceHeight, int outputWidth, int outputHeight)
    {
        WebpPixelBufferHelper.ValidateRgbaBuffer(source);
        WebpPixelBuffer output = WebpPixelBufferHelper.AllocateRgba(outputWidth, outputHeight);
        int outputY = 0;
        while (outputY < outputHeight)
        {
            int inputY = sourceY + (int)(((long)outputY * (long)sourceHeight) / outputHeight);
            int outputX = 0;
            while (outputX < outputWidth)
            {
                int inputX = sourceX + (int)(((long)outputX * (long)sourceWidth) / outputWidth);
                int sourceOffset = inputY * source.Stride + inputX * 4;
                int outputOffset = outputY * output.Stride + outputX * 4;
                output.Rgba[outputOffset] = source.Rgba[sourceOffset];
                output.Rgba[outputOffset + 1] = source.Rgba[sourceOffset + 1];
                output.Rgba[outputOffset + 2] = source.Rgba[sourceOffset + 2];
                output.Rgba[outputOffset + 3] = source.Rgba[sourceOffset + 3];
                outputX = outputX + 1;
            }

            outputY = outputY + 1;
        }

        return output;
    }

    internal static void ValidateIndependentOutputPixels(WebpDecodeResult result, WebpDecodeTransform transform, WebpSecurityLimits limits)
    {
        long totalPixels = 0;
        int index = 0;
        while (index < result.Image.Frames.Count)
        {
            WebpFrame frame = result.Image.Frames[index];
            int left = Maximum(frame.X, transform.SourceX);
            int top = Maximum(frame.Y, transform.SourceY);
            int right = Minimum(frame.X + frame.Width, transform.SourceX + transform.SourceWidth);
            int bottom = Minimum(frame.Y + frame.Height, transform.SourceY + transform.SourceHeight);
            if (right > left && bottom > top)
            {
                int outputLeft = ScaleFloor(left - transform.SourceX, transform.SourceWidth, transform.OutputWidth);
                int outputTop = ScaleFloor(top - transform.SourceY, transform.SourceHeight, transform.OutputHeight);
                int outputRight = ScaleCeiling(right - transform.SourceX, transform.SourceWidth, transform.OutputWidth);
                int outputBottom = ScaleCeiling(bottom - transform.SourceY, transform.SourceHeight, transform.OutputHeight);
                totalPixels = AddPixels(totalPixels, (long)(outputRight - outputLeft) * (long)(outputBottom - outputTop));
            }

            index = index + 1;
        }

        if (totalPixels > limits.MaxTotalPixels)
        {
            throw new ArgumentException("decoded output pixels exceed configured security limits", "options");
        }
    }

    internal static void ValidateTotalOutputPixels(WebpDecodeTransform transform, int frameCount, WebpSecurityLimits limits)
    {
        long framePixels = (long)transform.OutputWidth * (long)transform.OutputHeight;
        long totalPixels = 0;
        int index = 0;
        while (index < frameCount)
        {
            totalPixels = AddPixels(totalPixels, framePixels);
            index = index + 1;
        }

        if (totalPixels > limits.MaxTotalPixels)
        {
            throw new ArgumentException("decoded output pixels exceed configured security limits", "options");
        }
    }

    internal static long AddPixels(long total, long pixels)
    {
        if (total > long.MaxValue - pixels)
        {
            return long.MaxValue;
        }

        return total + pixels;
    }

    internal static int ScaleFloor(int value, int sourceDimension, int outputDimension)
    {
        return (int)(((long)value * (long)outputDimension) / sourceDimension);
    }

    internal static int ScaleCeiling(int value, int sourceDimension, int outputDimension)
    {
        long scaled = (long)value * (long)outputDimension;
        return (int)((scaled + sourceDimension - 1L) / sourceDimension);
    }

    internal static int Maximum(int first, int second)
    {
        return first > second ? first : second;
    }

    internal static int Minimum(int first, int second)
    {
        return first < second ? first : second;
    }
}
