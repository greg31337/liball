using System;

namespace webp;

internal static class WebpAlphaFilter
{
    internal static byte[] Filter(byte[] alpha, int width, int height, byte filterMethod)
    {
        ArgumentNullException.ThrowIfNull(alpha);
        ValidateAlphaPlane(alpha, width, height);
        ValidateFilterMethod(filterMethod);

        byte[] filteredAlpha = new byte[alpha.Length];
        int index = 0;
        while (index < alpha.Length)
        {
            int predictor = Predict(alpha, width, index, filterMethod);
            filteredAlpha[index] = (byte)((alpha[index] - predictor) & 255);
            index = index + 1;
        }

        return filteredAlpha;
    }

    internal static byte[] Unfilter(byte[] filteredAlpha, int width, int height, byte filterMethod)
    {
        ArgumentNullException.ThrowIfNull(filteredAlpha);
        ValidateAlphaPlane(filteredAlpha, width, height);
        ValidateFilterMethod(filterMethod);

        byte[] alpha = new byte[filteredAlpha.Length];
        int index = 0;
        while (index < filteredAlpha.Length)
        {
            int predictor = Predict(alpha, width, index, filterMethod);
            alpha[index] = (byte)((predictor + filteredAlpha[index]) & 255);
            index = index + 1;
        }

        return alpha;
    }

    internal static int Predict(byte[] alpha, int width, int index, byte filterMethod)
    {
        if (filterMethod == WebpConstants.AlphaFilterNone)
        {
            return 0;
        }

        int x = index % width;
        int y = index / width;
        if (filterMethod == WebpConstants.AlphaFilterHorizontal)
        {
            return PredictHorizontal(alpha, width, x, y, index);
        }

        if (filterMethod == WebpConstants.AlphaFilterVertical)
        {
            return PredictVertical(alpha, width, x, y, index);
        }

        return PredictGradient(alpha, width, x, y, index);
    }

    internal static int PredictHorizontal(byte[] alpha, int width, int x, int y, int index)
    {
        if (x != 0)
        {
            return alpha[index - 1];
        }

        if (y != 0)
        {
            return alpha[index - width];
        }

        return 0;
    }

    internal static int PredictVertical(byte[] alpha, int width, int x, int y, int index)
    {
        if (y != 0)
        {
            return alpha[index - width];
        }

        if (x != 0)
        {
            return alpha[index - 1];
        }

        return 0;
    }

    internal static int PredictGradient(byte[] alpha, int width, int x, int y, int index)
    {
        if (x == 0 && y == 0)
        {
            return 0;
        }

        if (x == 0)
        {
            return alpha[index - width];
        }

        if (y == 0)
        {
            return alpha[index - 1];
        }

        int left = alpha[index - 1];
        int above = alpha[index - width];
        int upperLeft = alpha[index - width - 1];
        return Clip(left + above - upperLeft);
    }

    internal static int Clip(int value)
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

    internal static void ValidateAlphaPlane(byte[] alpha, int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }

        long expectedLength = (long)width * (long)height;
        if (expectedLength > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("height");
        }

        if (alpha.Length != expectedLength)
        {
            throw new ArgumentException("alpha plane length must equal width * height", "filteredAlpha");
        }
    }

    internal static void ValidateFilterMethod(byte filterMethod)
    {
        if (filterMethod > WebpConstants.AlphaFilterGradient)
        {
            throw new ArgumentOutOfRangeException("filterMethod");
        }
    }
}
