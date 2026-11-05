using System;

namespace webp;

internal class WebpVp8IntraPredictionChoice
{
    internal int Mode;
    internal long Score;

    internal WebpVp8IntraPredictionChoice()
    {
        Mode = 0;
        Score = 0;
    }
}

internal static class WebpVp8IntraPredictionSearch
{
    internal static WebpVp8IntraPredictionChoice SearchLuma16X16(byte[] source, int sourceOffset, int sourceStride, byte[] top, byte[] left, byte topLeft)
    {
        return SearchBlock(source, sourceOffset, sourceStride, top, left, topLeft, 16);
    }

    internal static WebpVp8IntraPredictionChoice SearchChroma8X8(byte[] source, int sourceOffset, int sourceStride, byte[] top, byte[] left, byte topLeft)
    {
        return SearchBlock(source, sourceOffset, sourceStride, top, left, topLeft, 8);
    }

    internal static byte[] PredictBlock(int mode, byte[] top, byte[] left, byte topLeft, int size)
    {
        ValidateMode(mode);
        ValidateNeighbors(top, left, size);
        ValidateSize(size);

        byte[] prediction = new byte[size * size];
        int row = 0;
        while (row < size)
        {
            int column = 0;
            while (column < size)
            {
                prediction[(row * size) + column] = PredictPixel(mode, top, left, topLeft, row, column, size);
                column = column + 1;
            }

            row = row + 1;
        }

        return prediction;
    }

    internal static long ScoreBlock(byte[] source, int sourceOffset, int sourceStride, byte[] prediction, int size)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(prediction);
        ValidateSize(size);
        ValidateSource(source, sourceOffset, sourceStride, size);
        if (prediction.Length < size * size)
        {
            throw new ArgumentException("VP8 prediction block is too small", "prediction");
        }

        long score = 0;
        int row = 0;
        while (row < size)
        {
            int column = 0;
            while (column < size)
            {
                int diff = source[sourceOffset + (row * sourceStride) + column] - prediction[(row * size) + column];
                score = score + (diff * diff);
                column = column + 1;
            }

            row = row + 1;
        }

        return score;
    }

    internal static WebpVp8IntraPredictionChoice SearchBlock(byte[] source, int sourceOffset, int sourceStride, byte[] top, byte[] left, byte topLeft, int size)
    {
        ArgumentNullException.ThrowIfNull(source);
        ValidateSize(size);
        ValidateSource(source, sourceOffset, sourceStride, size);
        ValidateNeighbors(top, left, size);

        WebpVp8IntraPredictionChoice best = new WebpVp8IntraPredictionChoice();
        best.Mode = WebpVp8IntraModeParser.DcPred;
        best.Score = ScoreMode(source, sourceOffset, sourceStride, top, left, topLeft, size, best.Mode);

        CheckMode(source, sourceOffset, sourceStride, top, left, topLeft, size, WebpVp8IntraModeParser.VPred, best);
        CheckMode(source, sourceOffset, sourceStride, top, left, topLeft, size, WebpVp8IntraModeParser.HPred, best);
        CheckMode(source, sourceOffset, sourceStride, top, left, topLeft, size, WebpVp8IntraModeParser.TmPred, best);
        return best;
    }

    internal static void CheckMode(byte[] source, int sourceOffset, int sourceStride, byte[] top, byte[] left, byte topLeft, int size, int mode, WebpVp8IntraPredictionChoice best)
    {
        long score = ScoreMode(source, sourceOffset, sourceStride, top, left, topLeft, size, mode);
        if (score < best.Score)
        {
            best.Score = score;
            best.Mode = mode;
        }
    }

    internal static long ScoreMode(byte[] source, int sourceOffset, int sourceStride, byte[] top, byte[] left, byte topLeft, int size, int mode)
    {
        long score = 0;
        int row = 0;
        while (row < size)
        {
            int column = 0;
            while (column < size)
            {
                int predicted = PredictPixel(mode, top, left, topLeft, row, column, size);
                int actual = source[sourceOffset + (row * sourceStride) + column];
                int diff = actual - predicted;
                score = score + (diff * diff);
                column = column + 1;
            }

            row = row + 1;
        }

        return score;
    }

    internal static byte PredictPixel(int mode, byte[] top, byte[] left, byte topLeft, int row, int column, int size)
    {
        if (mode == WebpVp8IntraModeParser.VPred)
        {
            return top[column];
        }

        if (mode == WebpVp8IntraModeParser.HPred)
        {
            return left[row];
        }

        if (mode == WebpVp8IntraModeParser.TmPred)
        {
            return ClampToByte(left[row] + top[column] - topLeft);
        }

        return DcPrediction(top, left, size);
    }

    internal static byte DcPrediction(byte[] top, byte[] left, int size)
    {
        int sum = 0;
        int index = 0;
        while (index < size)
        {
            sum = sum + top[index] + left[index];
            index = index + 1;
        }

        if (size == 16)
        {
            return (byte)((sum + 16) >> 5);
        }

        return (byte)((sum + 8) >> 4);
    }

    internal static byte ClampToByte(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 255)
        {
            return 255;
        }

        return (byte)value;
    }

    internal static void ValidateMode(int mode)
    {
        if (mode < WebpVp8IntraModeParser.DcPred || mode > WebpVp8IntraModeParser.TmPred)
        {
            throw new ArgumentOutOfRangeException("mode");
        }
    }

    internal static void ValidateSize(int size)
    {
        if (size != 8 && size != 16)
        {
            throw new ArgumentOutOfRangeException("size");
        }
    }

    internal static void ValidateNeighbors(byte[] top, byte[] left, int size)
    {
        ArgumentNullException.ThrowIfNull(top);
        ArgumentNullException.ThrowIfNull(left);
        if (top.Length < size)
        {
            throw new ArgumentException("VP8 top predictor row is too small", "top");
        }

        if (left.Length < size)
        {
            throw new ArgumentException("VP8 left predictor column is too small", "left");
        }
    }

    internal static void ValidateSource(byte[] source, int sourceOffset, int sourceStride, int size)
    {
        if (sourceOffset < 0)
        {
            throw new ArgumentOutOfRangeException("sourceOffset");
        }

        if (sourceStride < size)
        {
            throw new ArgumentOutOfRangeException("sourceStride");
        }

        int lastIndex = sourceOffset + ((size - 1) * sourceStride) + size - 1;
        if (lastIndex >= source.Length)
        {
            throw new ArgumentException("VP8 source block exceeds source buffer", "source");
        }
    }
}
