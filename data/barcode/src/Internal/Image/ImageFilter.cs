namespace barcode;

internal static class ImageFilter
{
    internal const int MaximumBoxRadius = 32;
    internal const double MaximumUnsharpAmount = 4.0;

    internal static ManagedGrayImage CopyGray(
        ManagedGrayImage? source)
    {
        HistogramAnalyzer.ValidateGrayImage(source);
        ManagedGrayImage image = source == null
            ? new ManagedGrayImage(1, 1)
            : source;
        ManagedGrayImage copy = new ManagedGrayImage(
            image.Width,
            image.Height);
        int index = 0;
        while (index < image.Samples.Length)
        {
            copy.Samples[index] = image.Samples[index];
            index = index + 1;
        }

        return copy;
    }

    internal static ManagedGrayImage BoxSmooth(
        ManagedGrayImage? source,
        int radius)
    {
        HistogramAnalyzer.ValidateGrayImage(source);
        ValidateRadius(radius);
        ManagedGrayImage image = source == null
            ? new ManagedGrayImage(1, 1)
            : source;
        if (radius == 0)
        {
            return CopyGray(image);
        }

        long[] integral = CreateIntegralSums(image);
        ManagedGrayImage result = new ManagedGrayImage(
            image.Width,
            image.Height);
        int row = 0;
        while (row < image.Height)
        {
            int column = 0;
            while (column < image.Width)
            {
                int left = column - radius;
                int top = row - radius;
                int right = column + radius + 1;
                int bottom = row + radius + 1;
                if (left < 0)
                {
                    left = 0;
                }

                if (top < 0)
                {
                    top = 0;
                }

                if (right > image.Width)
                {
                    right = image.Width;
                }

                if (bottom > image.Height)
                {
                    bottom = image.Height;
                }

                long sum = ReadIntegralSum(
                    integral,
                    image.Width,
                    left,
                    top,
                    right,
                    bottom);
                int count = (right - left) * (bottom - top);
                result.WriteSample(
                    column,
                    row,
                    (int)((sum + (count / 2)) / count));
                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }

    internal static ManagedGrayImage UnsharpEnhance(
        ManagedGrayImage? source,
        int radius,
        double amount)
    {
        HistogramAnalyzer.ValidateGrayImage(source);
        ValidateRadius(radius);
        if (double.IsNaN(amount)
            || double.IsInfinity(amount)
            || amount < 0.0
            || amount > MaximumUnsharpAmount)
        {
            throw new BarcodeValidationException(
                "The unsharp amount must be finite and within the supported range.");
        }

        ManagedGrayImage image = source == null
            ? new ManagedGrayImage(1, 1)
            : source;
        if (radius == 0 || amount == 0.0)
        {
            return CopyGray(image);
        }

        ManagedGrayImage smoothed = BoxSmooth(image, radius);
        ManagedGrayImage result = new ManagedGrayImage(
            image.Width,
            image.Height);
        int index = 0;
        while (index < image.Samples.Length)
        {
            double value = image.Samples[index]
                + ((image.Samples[index] - smoothed.Samples[index])
                    * amount);
            int rounded = value >= 0.0
                ? (int)(value + 0.5)
                : (int)(value - 0.5);
            result.Samples[index] = ClampSample(rounded);
            index = index + 1;
        }

        return result;
    }

    internal static int ClampSample(int value)
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

    static void ValidateRadius(int radius)
    {
        if (radius < 0 || radius > MaximumBoxRadius)
        {
            throw new BarcodeValidationException(
                "The box-filter radius is outside the supported range.");
        }
    }

    static long[] CreateIntegralSums(ManagedGrayImage source)
    {
        int integralWidth = CheckedMath.Add(source.Width, 1);
        int integralHeight = CheckedMath.Add(source.Height, 1);
        int length = CheckedMath.CalculateAllocationLength(
            integralWidth,
            integralHeight,
            1,
            int.MaxValue);
        long[] integral = new long[length];
        int row = 1;
        while (row <= source.Height)
        {
            long rowSum = 0L;
            int column = 1;
            while (column <= source.Width)
            {
                rowSum = rowSum
                    + source.ReadSample(column - 1, row - 1);
                integral[(row * integralWidth) + column]
                    = integral[((row - 1) * integralWidth) + column]
                    + rowSum;
                column = column + 1;
            }

            row = row + 1;
        }

        return integral;
    }

    static long ReadIntegralSum(
        long[] integral,
        int width,
        int left,
        int top,
        int right,
        int bottom)
    {
        int integralWidth = width + 1;
        return integral[(bottom * integralWidth) + right]
            - integral[(top * integralWidth) + right]
            - integral[(bottom * integralWidth) + left]
            + integral[(top * integralWidth) + left];
    }
}
