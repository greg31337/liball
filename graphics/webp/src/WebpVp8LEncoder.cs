using System;
using System.Collections.Generic;

namespace webp;

internal static class WebpVp8LEncoder
{
    internal static byte[] Encode(WebpPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        WebpPixelBufferHelper.ValidateRgbaBuffer(buffer);
        ValidateDimensions(buffer.Width, buffer.Height);

        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        WebpVp8LColor[] pixels = CopyPixels(buffer);
        WebpVp8LColorIndexingEncodePlan colorIndexing = CreateColorIndexingPlan(pixels, buffer.Width, buffer.Height);
        if (colorIndexing.UseColorIndexing)
        {
            WriteHeader(writer, buffer);
            WriteColorIndexingTransform(writer, colorIndexing);
            writer.WriteBits(0, 1);
            WriteLiteralOnlyImageData(writer, colorIndexing.PackedPixels, colorIndexing.PackedWidth, colorIndexing.Height);
            return writer.ToArray();
        }

        bool useSubtractGreen = ShouldUseSubtractGreen(pixels);
        if (useSubtractGreen)
        {
            pixels = ApplyForwardSubtractGreen(pixels);
        }

        int predictorMode = ChoosePredictorMode(pixels, buffer.Width, buffer.Height);
        bool usePredictor = predictorMode >= 0;
        if (usePredictor)
        {
            pixels = ApplyForwardPredictor(pixels, buffer.Width, buffer.Height, predictorMode);
        }

        WriteHeader(writer, buffer);
        WriteTransformHeaders(writer, useSubtractGreen, usePredictor, predictorMode, buffer.Width, buffer.Height);
        WriteLiteralOnlyImageData(writer, pixels, buffer.Width, buffer.Height);
        return writer.ToArray();
    }

    internal static byte[] Encode(WebpPixelBuffer buffer, WebpEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        WebpPixelBuffer prepared = PrepareForEncoding(buffer, options);
        return Encode(prepared);
    }

    internal static WebpPixelBuffer PrepareForEncoding(WebpPixelBuffer buffer, WebpEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        WebpPixelBufferHelper.ValidateRgbaBuffer(buffer);
        ValidateDimensions(buffer.Width, buffer.Height);
        WebpNearLosslessPreprocessor.ComputeQuantizationStep(options.NearLosslessQuality);
        if (options.Mode == WebpEncodeMode.NearLossless || options.NearLosslessQuality < 100)
        {
            return WebpNearLosslessPreprocessor.Apply(buffer, options.NearLosslessQuality, options.ExactTransparentRgb);
        }

        return WebpPixelBufferHelper.Copy(buffer);
    }

    internal static void WriteHeader(WebpBitWriter writer, WebpPixelBuffer buffer)
    {
        writer.WriteBits(WebpConstants.Vp8LSignature, 8);
        writer.WriteBits((uint)(buffer.Width - 1), 14);
        writer.WriteBits((uint)(buffer.Height - 1), 14);
        if (HasAlpha(buffer))
        {
            writer.WriteBits(1, 1);
        }
        else
        {
            writer.WriteBits(0, 1);
        }

        writer.WriteBits(WebpConstants.Vp8LVersion, 3);
    }

    internal static void WriteTransformHeaders(WebpBitWriter writer, bool useSubtractGreen, bool usePredictor, int predictorMode, int width, int height)
    {
        if (useSubtractGreen)
        {
            writer.WriteBits(1, 1);
            writer.WriteBits(WebpVp8LDecoder.SubtractGreenTransformType, 2);
        }

        if (usePredictor)
        {
            writer.WriteBits(1, 1);
            writer.WriteBits(WebpVp8LDecoder.PredictorTransformType, 2);
            WritePredictorTransformData(writer, width, height, predictorMode);
        }

        writer.WriteBits(0, 1);
    }

    internal static void WritePredictorTransformData(WebpBitWriter writer, int width, int height, int predictorMode)
    {
        int sizeBits = 2;
        int transformWidth = DivRoundUp(width, 1 << sizeBits);
        int transformHeight = DivRoundUp(height, 1 << sizeBits);
        WebpVp8LColor[] predictorPixels = new WebpVp8LColor[transformWidth * transformHeight];
        int index = 0;
        while (index < predictorPixels.Length)
        {
            WebpVp8LColor color = new WebpVp8LColor();
            color.Green = (byte)predictorMode;
            color.Alpha = 255;
            predictorPixels[index] = color;
            index = index + 1;
        }

        writer.WriteBits((uint)(sizeBits - 2), 3);
        WriteLiteralOnlySubresolutionImageData(writer, predictorPixels, transformWidth, transformHeight);
    }

    internal static void WriteColorIndexingTransform(WebpBitWriter writer, WebpVp8LColorIndexingEncodePlan plan)
    {
        writer.WriteBits(1, 1);
        writer.WriteBits(WebpVp8LDecoder.ColorIndexingTransformType, 2);
        writer.WriteBits((uint)(plan.ColorTableSize - 1), 8);
        WriteLiteralOnlySubresolutionImageData(writer, CreateColorTableDeltas(plan.ColorTable), plan.ColorTableSize, 1);
    }

    internal static void WriteLiteralOnlyImageData(WebpBitWriter writer, WebpVp8LColor[] pixels, int width, int height)
    {
        WebpVp8LEntropyWriter.WriteImageData(writer, CreateLiteralTokens(pixels, width, height), width, height);
    }

    internal static void WriteLiteralOnlySubresolutionImageData(WebpBitWriter writer, WebpVp8LColor[] pixels, int width, int height)
    {
        WebpVp8LEntropyWriter.WriteSubresolutionImageData(writer, CreateLiteralTokens(pixels, width, height), width, height);
    }

    internal static List<WebpVp8LLz77Token> CreateLiteralTokens(WebpVp8LColor[] pixels, int width, int height)
    {
        ValidatePixelCount(pixels, width, height);
        List<WebpVp8LLz77Token> tokens = new List<WebpVp8LLz77Token>();
        int index = 0;
        while (index < width * height)
        {
            WebpVp8LLz77Token token = new WebpVp8LLz77Token();
            token.IsLiteral = true;
            token.Color = pixels[index].Copy();
            tokens.Add(token);
            index = index + 1;
        }

        return tokens;
    }

    internal static WebpVp8LColor[] CopyPixels(WebpPixelBuffer buffer)
    {
        WebpVp8LColor[] pixels = new WebpVp8LColor[buffer.Width * buffer.Height];
        int row = 0;
        while (row < buffer.Height)
        {
            int rowOffset = row * buffer.Stride;
            int column = 0;
            while (column < buffer.Width)
            {
                int sourcePixel = rowOffset + column * 4;
                int index = row * buffer.Width + column;
                WebpVp8LColor color = new WebpVp8LColor();
                color.Red = buffer.Rgba[sourcePixel];
                color.Green = buffer.Rgba[sourcePixel + 1];
                color.Blue = buffer.Rgba[sourcePixel + 2];
                color.Alpha = buffer.Rgba[sourcePixel + 3];
                pixels[index] = color;
                column = column + 1;
            }

            row = row + 1;
        }

        return pixels;
    }

    internal static bool ShouldUseSubtractGreen(WebpVp8LColor[] pixels)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        return ScorePixels(ApplyForwardSubtractGreen(pixels)) < ScorePixels(pixels);
    }

    internal static int ChoosePredictorMode(WebpVp8LColor[] pixels, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        ValidatePixelCount(pixels, width, height);
        if (width == 1 && height == 1)
        {
            return -1;
        }

        long bestScore = ScorePixels(pixels);
        int bestMode = -1;
        int[] candidateModes = new int[] { 1, 2, 7 };
        int index = 0;
        while (index < candidateModes.Length)
        {
            int mode = candidateModes[index];
            long score = ScorePixels(ApplyForwardPredictor(pixels, width, height, mode));
            if (score < bestScore)
            {
                bestScore = score;
                bestMode = mode;
            }

            index = index + 1;
        }

        return bestMode;
    }

    internal static WebpVp8LColorIndexingEncodePlan CreateColorIndexingPlan(WebpVp8LColor[] pixels, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        ValidatePixelCount(pixels, width, height);

        WebpVp8LColorIndexingEncodePlan plan = new WebpVp8LColorIndexingEncodePlan();
        plan.Height = height;
        WebpVp8LColor[] colorTable = BuildColorTable(pixels);
        plan.ColorTableSize = colorTable.Length;
        plan.ColorTable = colorTable;
        if (!ShouldUseColorIndexing(pixels, colorTable.Length))
        {
            return plan;
        }

        plan.UseColorIndexing = true;
        plan.WidthBits = WebpVp8LColorIndexingTransformDecoder.ComputeWidthBits(plan.ColorTableSize);
        plan.PackedWidth = DivRoundUp(width, 1 << plan.WidthBits);
        plan.PackedPixels = PackColorIndices(pixels, width, height, plan.ColorTable, plan.WidthBits, plan.PackedWidth);
        return plan;
    }

    internal static WebpVp8LColor[] BuildColorTable(WebpVp8LColor[] pixels)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        WebpVp8LColor[] table = new WebpVp8LColor[257];
        int count = 0;
        int index = 0;
        while (index < pixels.Length)
        {
            WebpVp8LColor color = pixels[index];
            if (FindColorIndex(table, count, color) < 0)
            {
                if (count == 256)
                {
                    table[count] = color.Copy();
                    return CopyColorTable(table, count + 1);
                }

                table[count] = color.Copy();
                count = count + 1;
            }

            index = index + 1;
        }

        return CopyColorTable(table, count);
    }

    internal static WebpVp8LColor[] ApplyForwardSubtractGreen(WebpVp8LColor[] pixels)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        WebpVp8LColor[] output = new WebpVp8LColor[pixels.Length];
        int index = 0;
        while (index < pixels.Length)
        {
            WebpVp8LColor color = pixels[index];
            WebpVp8LColor transformed = new WebpVp8LColor();
            transformed.Red = (byte)((color.Red - color.Green) & 255);
            transformed.Green = color.Green;
            transformed.Blue = (byte)((color.Blue - color.Green) & 255);
            transformed.Alpha = color.Alpha;
            output[index] = transformed;
            index = index + 1;
        }

        return output;
    }

    internal static WebpVp8LColor[] ApplyForwardPredictor(WebpVp8LColor[] pixels, int width, int height, int mode)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        ValidatePixelCount(pixels, width, height);
        WebpVp8LColor[] residuals = new WebpVp8LColor[pixels.Length];
        int index = 0;
        while (index < pixels.Length)
        {
            int x = index % width;
            int y = index / width;
            WebpVp8LColor predictor = PredictForPixel(pixels, width, x, y, mode);
            residuals[index] = SubtractColors(pixels[index], predictor);
            index = index + 1;
        }

        return residuals;
    }

    internal static bool ShouldUseColorIndexing(WebpVp8LColor[] pixels, int colorTableSize)
    {
        if (colorTableSize <= 0 || colorTableSize > 256)
        {
            return false;
        }

        if (pixels.Length < 8)
        {
            return false;
        }

        return colorTableSize < pixels.Length;
    }

    internal static WebpVp8LColor[] PackColorIndices(WebpVp8LColor[] pixels, int width, int height, WebpVp8LColor[] colorTable, int widthBits, int packedWidth)
    {
        WebpVp8LColor[] packedPixels = new WebpVp8LColor[packedWidth * height];
        int index = 0;
        while (index < packedPixels.Length)
        {
            WebpVp8LColor color = new WebpVp8LColor();
            color.Alpha = 255;
            packedPixels[index] = color;
            index = index + 1;
        }

        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                int sourceIndex = y * width + x;
                int colorIndex = FindColorIndex(colorTable, colorTable.Length, pixels[sourceIndex]);
                SetPackedIndex(packedPixels, packedWidth, x, y, widthBits, colorIndex);
                x = x + 1;
            }

            y = y + 1;
        }

        return packedPixels;
    }

    internal static void SetPackedIndex(WebpVp8LColor[] packedPixels, int packedWidth, int x, int y, int widthBits, int colorIndex)
    {
        int packedX = x >> widthBits;
        int packedIndex = y * packedWidth + packedX;
        if (widthBits == 0)
        {
            packedPixels[packedIndex].Green = (byte)colorIndex;
            return;
        }

        int bitsPerPixel = 8 >> widthBits;
        int pixelsPerPackedPixel = 1 << widthBits;
        int position = x & (pixelsPerPackedPixel - 1);
        int shift = position * bitsPerPixel;
        packedPixels[packedIndex].Green = (byte)(packedPixels[packedIndex].Green | (colorIndex << shift));
    }

    internal static WebpVp8LColor[] CreateColorTableDeltas(WebpVp8LColor[] colorTable)
    {
        WebpVp8LColor[] deltas = new WebpVp8LColor[colorTable.Length];
        WebpVp8LColor previous = new WebpVp8LColor();
        int index = 0;
        while (index < colorTable.Length)
        {
            WebpVp8LColor color = colorTable[index];
            WebpVp8LColor delta = new WebpVp8LColor();
            delta.Red = (byte)((color.Red - previous.Red) & 255);
            delta.Green = (byte)((color.Green - previous.Green) & 255);
            delta.Blue = (byte)((color.Blue - previous.Blue) & 255);
            delta.Alpha = (byte)((color.Alpha - previous.Alpha) & 255);
            deltas[index] = delta;
            previous = color;
            index = index + 1;
        }

        return deltas;
    }

    internal static WebpVp8LColor[] CopyColorTable(WebpVp8LColor[] table, int count)
    {
        WebpVp8LColor[] copy = new WebpVp8LColor[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = table[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    internal static int FindColorIndex(WebpVp8LColor[] colorTable, int count, WebpVp8LColor color)
    {
        int index = 0;
        while (index < count)
        {
            if (ColorsEqual(colorTable[index], color))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool ColorsEqual(WebpVp8LColor a, WebpVp8LColor b)
    {
        return a.Red == b.Red && a.Green == b.Green && a.Blue == b.Blue && a.Alpha == b.Alpha;
    }

    internal static WebpVp8LColor PredictForPixel(WebpVp8LColor[] pixels, int width, int x, int y, int mode)
    {
        if (x == 0 && y == 0)
        {
            return Black();
        }

        if (y == 0)
        {
            return pixels[y * width + x - 1].Copy();
        }

        if (x == 0)
        {
            return pixels[(y - 1) * width + x].Copy();
        }

        WebpVp8LColor left = pixels[y * width + x - 1];
        WebpVp8LColor top = pixels[(y - 1) * width + x];
        WebpVp8LColor topLeft = pixels[(y - 1) * width + x - 1];
        WebpVp8LColor topRight = GetTopRight(pixels, width, x, y);
        return WebpVp8LPredictorTransformDecoder.Predict(mode, left, top, topRight, topLeft);
    }

    internal static WebpVp8LColor GetTopRight(WebpVp8LColor[] pixels, int width, int x, int y)
    {
        if (x == width - 1)
        {
            return pixels[y * width].Copy();
        }

        return pixels[(y - 1) * width + x + 1].Copy();
    }

    internal static WebpVp8LColor SubtractColors(WebpVp8LColor color, WebpVp8LColor predictor)
    {
        WebpVp8LColor residual = new WebpVp8LColor();
        residual.Red = (byte)((color.Red - predictor.Red) & 255);
        residual.Green = (byte)((color.Green - predictor.Green) & 255);
        residual.Blue = (byte)((color.Blue - predictor.Blue) & 255);
        residual.Alpha = (byte)((color.Alpha - predictor.Alpha) & 255);
        return residual;
    }

    internal static WebpVp8LColor Black()
    {
        WebpVp8LColor color = new WebpVp8LColor();
        color.Alpha = 255;
        return color;
    }

    internal static long ScorePixels(WebpVp8LColor[] pixels)
    {
        long score = 0;
        int index = 0;
        while (index < pixels.Length)
        {
            score = score + ScoreByte(pixels[index].Red);
            score = score + ScoreByte(pixels[index].Green);
            score = score + ScoreByte(pixels[index].Blue);
            score = score + ScoreByte(pixels[index].Alpha);
            index = index + 1;
        }

        return score;
    }

    internal static int ScoreByte(byte value)
    {
        int unsignedValue = value;
        int negativeValue = 256 - unsignedValue;
        if (negativeValue < unsignedValue)
        {
            return negativeValue;
        }

        return unsignedValue;
    }

    internal static int DivRoundUp(int value, int divisor)
    {
        return (value + divisor - 1) / divisor;
    }

    internal static bool HasAlpha(WebpPixelBuffer buffer)
    {
        int row = 0;
        while (row < buffer.Height)
        {
            int rowOffset = row * buffer.Stride;
            int column = 0;
            while (column < buffer.Width)
            {
                int pixel = rowOffset + column * 4;
                if (buffer.Rgba[pixel + 3] != 255)
                {
                    return true;
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return false;
    }

    internal static void ValidateDimensions(int width, int height)
    {
        if (width > WebpConstants.Vp8LMaxDimension)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (height > WebpConstants.Vp8LMaxDimension)
        {
            throw new ArgumentOutOfRangeException("height");
        }
    }

    internal static void ValidatePixelCount(WebpVp8LColor[] pixels, int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }

        if (pixels.Length != width * height)
        {
            throw new ArgumentException("pixel count must equal width * height", "pixels");
        }
    }
}
