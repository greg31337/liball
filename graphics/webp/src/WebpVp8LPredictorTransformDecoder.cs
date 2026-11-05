using System;

namespace webp;

internal static class WebpVp8LPredictorTransformDecoder
{
    internal static WebpVp8LPredictorTransformParseResult Parse(WebpVp8LBitReader reader, int imageWidth, int imageHeight, Func<WebpVp8LBitReader, int, int, WebpPixelBuffer> predictorImageDecoder)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(predictorImageDecoder);
        ValidateDimensions(imageWidth, imageHeight);

        WebpVp8LPredictorTransformParseResult result = new WebpVp8LPredictorTransformParseResult();
        WebpVp8LPredictorTransform transform = result.Transform;
        transform.SizeBits = (int)reader.ReadBits(3) + 2;
        transform.Width = DivRoundUp(imageWidth, 1 << transform.SizeBits);
        transform.Height = DivRoundUp(imageHeight, 1 << transform.SizeBits);

        WebpPixelBuffer predictorImage = predictorImageDecoder(reader, transform.Width, transform.Height);
        ArgumentNullException.ThrowIfNull(predictorImage);
        if (predictorImage.Width == 0 && predictorImage.Height == 0 && predictorImage.Rgba.Length == 0)
        {
            AddDiagnostic(result, "VP8L predictor image decode exceeded the recursion depth limit");
            Finish(result);
            return result;
        }

        WebpPixelBufferHelper.ValidateRgbaBuffer(predictorImage);
        ValidatePredictorImageDimensions(predictorImage, transform.Width, transform.Height);
        ExtractModes(result, predictorImage);
        Finish(result);
        return result;
    }

    internal static WebpVp8LColor[] ApplyInverse(WebpVp8LColor[] residuals, int width, int height, WebpVp8LPredictorTransform transform)
    {
        ArgumentNullException.ThrowIfNull(residuals);
        ArgumentNullException.ThrowIfNull(transform);
        ValidateDimensions(width, height);
        ValidateResiduals(residuals, width, height);
        ValidateTransform(transform, width, height);

        WebpVp8LColor[] output = new WebpVp8LColor[residuals.Length];
        int index = 0;
        while (index < residuals.Length)
        {
            int x = index % width;
            int y = index / width;
            WebpVp8LColor predictor = PredictForPixel(output, width, height, x, y, transform);
            output[index] = AddColors(residuals[index], predictor);
            index = index + 1;
        }

        return output;
    }

    internal static WebpVp8LColor Predict(int mode, WebpVp8LColor left, WebpVp8LColor top, WebpVp8LColor topRight, WebpVp8LColor topLeft)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(top);
        ArgumentNullException.ThrowIfNull(topRight);
        ArgumentNullException.ThrowIfNull(topLeft);

        if (mode == 0)
        {
            return Black();
        }

        if (mode == 1)
        {
            return left.Copy();
        }

        if (mode == 2)
        {
            return top.Copy();
        }

        if (mode == 3)
        {
            return topRight.Copy();
        }

        if (mode == 4)
        {
            return topLeft.Copy();
        }

        return PredictComposite(mode, left, top, topRight, topLeft);
    }

    internal static WebpVp8LColor PredictComposite(int mode, WebpVp8LColor left, WebpVp8LColor top, WebpVp8LColor topRight, WebpVp8LColor topLeft)
    {
        if (mode == 5)
        {
            return AverageColor(AverageColor(left, topRight), top);
        }

        if (mode == 6)
        {
            return AverageColor(left, topLeft);
        }

        if (mode == 7)
        {
            return AverageColor(left, top);
        }

        if (mode == 8)
        {
            return AverageColor(topLeft, top);
        }

        if (mode == 9)
        {
            return AverageColor(top, topRight);
        }

        if (mode == 10)
        {
            return AverageColor(AverageColor(left, topLeft), AverageColor(top, topRight));
        }

        if (mode == 11)
        {
            return SelectPredictor(left, top, topLeft);
        }

        if (mode == 12)
        {
            return ClampAddSubtractFull(left, top, topLeft);
        }

        if (mode == 13)
        {
            return ClampAddSubtractHalf(AverageColor(left, top), topLeft);
        }

        throw new ArgumentOutOfRangeException("mode");
    }

    internal static WebpVp8LColor PredictForPixel(WebpVp8LColor[] output, int width, int height, int x, int y, WebpVp8LPredictorTransform transform)
    {
        if (x == 0 && y == 0)
        {
            return Black();
        }

        if (y == 0)
        {
            return output[y * width + x - 1].Copy();
        }

        if (x == 0)
        {
            return output[(y - 1) * width + x].Copy();
        }

        int mode = GetMode(transform, x, y);
        WebpVp8LColor left = output[y * width + x - 1];
        WebpVp8LColor top = output[(y - 1) * width + x];
        WebpVp8LColor topLeft = output[(y - 1) * width + x - 1];
        WebpVp8LColor topRight = GetTopRight(output, width, height, x, y);
        return Predict(mode, left, top, topRight, topLeft);
    }

    internal static WebpVp8LColor GetTopRight(WebpVp8LColor[] output, int width, int height, int x, int y)
    {
        if (x == width - 1)
        {
            return output[y * width].Copy();
        }

        return output[(y - 1) * width + x + 1].Copy();
    }

    internal static int GetMode(WebpVp8LPredictorTransform transform, int x, int y)
    {
        int blockX = x >> transform.SizeBits;
        int blockY = y >> transform.SizeBits;
        return transform.Modes[blockY * transform.Width + blockX];
    }

    internal static void ExtractModes(WebpVp8LPredictorTransformParseResult result, WebpPixelBuffer predictorImage)
    {
        WebpVp8LPredictorTransform transform = result.Transform;
        transform.Modes = new byte[predictorImage.Width * predictorImage.Height];
        int row = 0;
        while (row < predictorImage.Height)
        {
            int rowOffset = row * predictorImage.Stride;
            int column = 0;
            while (column < predictorImage.Width)
            {
                int pixel = rowOffset + column * 4;
                int index = row * predictorImage.Width + column;
                byte mode = predictorImage.Rgba[pixel + 1];
                if (mode > 13)
                {
                    AddDiagnostic(result, "VP8L predictor mode must be between 0 and 13");
                    return;
                }

                transform.Modes[index] = mode;
                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal static WebpVp8LColor AddColors(WebpVp8LColor residual, WebpVp8LColor predictor)
    {
        ArgumentNullException.ThrowIfNull(residual);
        WebpVp8LColor color = new WebpVp8LColor();
        color.Red = (byte)((residual.Red + predictor.Red) & 255);
        color.Green = (byte)((residual.Green + predictor.Green) & 255);
        color.Blue = (byte)((residual.Blue + predictor.Blue) & 255);
        color.Alpha = (byte)((residual.Alpha + predictor.Alpha) & 255);
        return color;
    }

    internal static WebpVp8LColor AverageColor(WebpVp8LColor a, WebpVp8LColor b)
    {
        WebpVp8LColor color = new WebpVp8LColor();
        color.Red = (byte)((a.Red + b.Red) / 2);
        color.Green = (byte)((a.Green + b.Green) / 2);
        color.Blue = (byte)((a.Blue + b.Blue) / 2);
        color.Alpha = (byte)((a.Alpha + b.Alpha) / 2);
        return color;
    }

    internal static WebpVp8LColor SelectPredictor(WebpVp8LColor left, WebpVp8LColor top, WebpVp8LColor topLeft)
    {
        int estimateAlpha = left.Alpha + top.Alpha - topLeft.Alpha;
        int estimateRed = left.Red + top.Red - topLeft.Red;
        int estimateGreen = left.Green + top.Green - topLeft.Green;
        int estimateBlue = left.Blue + top.Blue - topLeft.Blue;
        int leftDistance = ChannelDistance(estimateAlpha, left.Alpha) + ChannelDistance(estimateRed, left.Red) + ChannelDistance(estimateGreen, left.Green) + ChannelDistance(estimateBlue, left.Blue);
        int topDistance = ChannelDistance(estimateAlpha, top.Alpha) + ChannelDistance(estimateRed, top.Red) + ChannelDistance(estimateGreen, top.Green) + ChannelDistance(estimateBlue, top.Blue);
        if (leftDistance < topDistance)
        {
            return left.Copy();
        }

        return top.Copy();
    }

    internal static int ChannelDistance(int estimate, byte value)
    {
        int distance = estimate - value;
        if (distance < 0)
        {
            return -distance;
        }

        return distance;
    }

    internal static WebpVp8LColor ClampAddSubtractFull(WebpVp8LColor left, WebpVp8LColor top, WebpVp8LColor topLeft)
    {
        WebpVp8LColor color = new WebpVp8LColor();
        color.Red = (byte)Clamp(left.Red + top.Red - topLeft.Red);
        color.Green = (byte)Clamp(left.Green + top.Green - topLeft.Green);
        color.Blue = (byte)Clamp(left.Blue + top.Blue - topLeft.Blue);
        color.Alpha = (byte)Clamp(left.Alpha + top.Alpha - topLeft.Alpha);
        return color;
    }

    internal static WebpVp8LColor ClampAddSubtractHalf(WebpVp8LColor average, WebpVp8LColor topLeft)
    {
        WebpVp8LColor color = new WebpVp8LColor();
        color.Red = (byte)Clamp(average.Red + (average.Red - topLeft.Red) / 2);
        color.Green = (byte)Clamp(average.Green + (average.Green - topLeft.Green) / 2);
        color.Blue = (byte)Clamp(average.Blue + (average.Blue - topLeft.Blue) / 2);
        color.Alpha = (byte)Clamp(average.Alpha + (average.Alpha - topLeft.Alpha) / 2);
        return color;
    }

    internal static int Clamp(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 255)
        {
            return 255;
        }

        return value;
    }

    internal static WebpVp8LColor Black()
    {
        WebpVp8LColor color = new WebpVp8LColor();
        color.Alpha = 255;
        return color;
    }

    internal static int DivRoundUp(int value, int divisor)
    {
        return (value + divisor - 1) / divisor;
    }

    internal static void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }
    }

    internal static void ValidatePredictorImageDimensions(WebpPixelBuffer image, int width, int height)
    {
        if (image.Width != width || image.Height != height)
        {
            throw new ArgumentException("predictor image dimensions must match derived transform dimensions", "predictorImage");
        }
    }

    internal static void ValidateResiduals(WebpVp8LColor[] residuals, int width, int height)
    {
        if (residuals.Length != width * height)
        {
            throw new ArgumentException("residual pixel count must equal width * height", "residuals");
        }
    }

    internal static void ValidateTransform(WebpVp8LPredictorTransform transform, int width, int height)
    {
        int expectedWidth = DivRoundUp(width, 1 << transform.SizeBits);
        int expectedHeight = DivRoundUp(height, 1 << transform.SizeBits);
        if (transform.Width != expectedWidth || transform.Height != expectedHeight)
        {
            throw new ArgumentException("predictor transform dimensions do not match the image dimensions", "transform");
        }

        if (transform.Modes.Length != transform.Width * transform.Height)
        {
            throw new ArgumentException("predictor mode count does not match transform dimensions", "transform");
        }
    }

    internal static void AddDiagnostic(WebpVp8LPredictorTransformParseResult result, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(WebpDiagnosticSeverity.Error, -1, WebpConstants.FourCcVp8LString, message));
    }

    internal static void Finish(WebpVp8LPredictorTransformParseResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
