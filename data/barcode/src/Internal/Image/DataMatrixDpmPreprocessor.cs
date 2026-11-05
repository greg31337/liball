namespace barcode;

internal class DataMatrixDpmBinaryCandidate
{
    internal ModuleMatrix Modules = new ModuleMatrix(1, 1);
    internal string Method = "";
    internal int ConnectionRadius;
    internal double ForegroundFraction;
}

internal class DataMatrixDpmPreprocessingResult
{
    internal ManagedGrayImage Luminance = new ManagedGrayImage(1, 1);
    internal ManagedGrayImage Smoothed = new ManagedGrayImage(1, 1);
    internal ManagedGrayImage Flattened = new ManagedGrayImage(1, 1);
    internal DataMatrixDpmBinaryCandidate[] Candidates = new DataMatrixDpmBinaryCandidate[0];
    internal int GlobalThreshold;
    internal int BackgroundRadius;
    internal double MeanLocalContrast;
}

internal static class DataMatrixDpmPreprocessor
{
    internal const int MinimumBackgroundRadius = 4;
    internal const int MaximumBackgroundRadius = 24;
    internal const int LocalContrastGain = 4;
    internal const double MinimumAdaptiveBias = 2.0;
    internal const double AdaptiveDeviationFactor = 0.12;
    internal const int MaximumConnectionRadius = 3;

    internal static DataMatrixDpmPreprocessingResult Preprocess(
        BarcodeImage image,
        BarcodeEffort effort)
    {
        ValidateEffort(effort);
        ManagedGrayImage luminance = ConvertToLuminance(image);
        int smoothingRadius = effort == BarcodeEffort.Fast ? 0 : 1;
        ManagedGrayImage smoothed = smoothingRadius == 0
            ? ImageFilter.CopyGray(luminance)
            : ImageFilter.BoxSmooth(luminance, smoothingRadius);
        int minimumDimension = luminance.Width < luminance.Height
            ? luminance.Width
            : luminance.Height;
        int backgroundRadius = minimumDimension / 10;
        if (backgroundRadius < MinimumBackgroundRadius)
        {
            backgroundRadius = MinimumBackgroundRadius;
        }

        if (backgroundRadius > MaximumBackgroundRadius)
        {
            backgroundRadius = MaximumBackgroundRadius;
        }

        long[] sums = CreateIntegralSums(smoothed);
        long[] squares = CreateIntegralSquares(smoothed);
        ManagedGrayImage flattened = ImageNormalizer.NormalizeIllumination(
            smoothed,
            backgroundRadius,
            LocalContrastGain);
        int globalThreshold = HistogramAnalyzer.Analyze(
            flattened).OtsuThreshold;
        double meanLocalContrast = CalculateMeanLocalContrast(
            smoothed,
            sums,
            squares,
            backgroundRadius);
        global::System.Collections.Generic.List<DataMatrixDpmBinaryCandidate> candidates =
            new global::System.Collections.Generic.List<DataMatrixDpmBinaryCandidate>();
        ModuleMatrix global = GlobalBinarizer.BinarizeThreshold(
            flattened,
            globalThreshold,
            BarcodePolarity.DarkOnLight);
        AddCandidate(candidates, global, "FlattenedOtsu", 0);
        ModuleMatrix adaptiveDark = ThresholdAdaptiveDark(
            smoothed,
            sums,
            squares,
            backgroundRadius);
        AddCandidate(candidates, adaptiveDark, "AdaptiveDark", 0);
        if (effort != BarcodeEffort.Fast)
        {
            ModuleMatrix adaptiveLight = ThresholdAdaptiveLight(
                smoothed,
                sums,
                squares,
                backgroundRadius);
            AddCandidate(candidates, adaptiveLight, "AdaptiveLight", 0);
            ModuleMatrix majorityDark = Morphology.MajorityFilter(
                adaptiveDark,
                1);
            AddCandidate(candidates, majorityDark, "TextureSuppressedDark", 0);
            ModuleMatrix majorityLightForeground = Morphology.Invert(
                Morphology.MajorityFilter(
                    Morphology.Invert(adaptiveLight),
                    1));
            AddCandidate(candidates, majorityLightForeground, "TextureSuppressedLight", 0);
            int maximumRadius = effort == BarcodeEffort.Thorough
                ? MaximumConnectionRadius
                : MaximumConnectionRadius - 1;
            int radius = 1;
            while (radius <= maximumRadius)
            {
                AddCandidate(
                    candidates,
                    Morphology.Dilate(adaptiveDark, radius),
                    $"DotConnectDark{radius}",
                    radius);
                ModuleMatrix lightMarks = Morphology.Invert(adaptiveLight);
                ModuleMatrix connectedLightMarks = Morphology.Dilate(
                    lightMarks,
                    radius);
                AddCandidate(
                    candidates,
                    Morphology.Invert(connectedLightMarks),
                    $"DotConnectLight{radius}",
                    radius);
                radius = radius + 1;
            }
        }

        DataMatrixDpmPreprocessingResult result = new DataMatrixDpmPreprocessingResult();
        result.Luminance = luminance;
        result.Smoothed = smoothed;
        result.Flattened = flattened;
        result.Candidates = candidates.ToArray();
        result.GlobalThreshold = globalThreshold;
        result.BackgroundRadius = backgroundRadius;
        result.MeanLocalContrast = meanLocalContrast;
        return result;
    }

    internal static ManagedGrayImage ConvertToLuminance(BarcodeImage image)
    {
        return ImageIngestion.ConvertToGray8(image);
    }

    static ModuleMatrix ThresholdAdaptiveDark(
        ManagedGrayImage source,
        long[] sums,
        long[] squares,
        int radius)
    {
        ModuleMatrix result = new ModuleMatrix(source.Width, source.Height);
        int row = 0;
        while (row < source.Height)
        {
            int column = 0;
            while (column < source.Width)
            {
                double[] statistics = CalculateWindowStatistics(
                    sums,
                    squares,
                    source.Width,
                    source.Height,
                    column,
                    row,
                    radius);
                double bias = statistics[1] * AdaptiveDeviationFactor;
                if (bias < MinimumAdaptiveBias)
                {
                    bias = MinimumAdaptiveBias;
                }

                bool foreground = source.ReadSample(column, row) < statistics[0] - bias;
                result.SetModule(column, row, foreground);
                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }

    static ModuleMatrix ThresholdAdaptiveLight(
        ManagedGrayImage source,
        long[] sums,
        long[] squares,
        int radius)
    {
        ModuleMatrix result = new ModuleMatrix(source.Width, source.Height);
        int row = 0;
        while (row < source.Height)
        {
            int column = 0;
            while (column < source.Width)
            {
                double[] statistics = CalculateWindowStatistics(
                    sums,
                    squares,
                    source.Width,
                    source.Height,
                    column,
                    row,
                    radius);
                double bias = statistics[1] * AdaptiveDeviationFactor;
                if (bias < MinimumAdaptiveBias)
                {
                    bias = MinimumAdaptiveBias;
                }

                bool brightMark = source.ReadSample(column, row) > statistics[0] + bias;
                result.SetModule(column, row, !brightMark);
                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }

    static long[] CreateIntegralSums(ManagedGrayImage source)
    {
        int integralWidth = CheckedMath.Add(source.Width, 1);
        int integralHeight = CheckedMath.Add(source.Height, 1);
        int integralLength = CheckedMath.CalculateAllocationLength(
            integralWidth,
            integralHeight,
            1,
            int.MaxValue);
        long[] integral = new long[integralLength];
        int row = 1;
        while (row <= source.Height)
        {
            long rowSum = 0L;
            int column = 1;
            while (column <= source.Width)
            {
                rowSum = rowSum + source.ReadSample(column - 1, row - 1);
                integral[(row * integralWidth) + column]
                    = integral[((row - 1) * integralWidth) + column] + rowSum;
                column = column + 1;
            }

            row = row + 1;
        }

        return integral;
    }

    static long[] CreateIntegralSquares(ManagedGrayImage source)
    {
        int integralWidth = CheckedMath.Add(source.Width, 1);
        int integralHeight = CheckedMath.Add(source.Height, 1);
        int integralLength = CheckedMath.CalculateAllocationLength(
            integralWidth,
            integralHeight,
            1,
            int.MaxValue);
        long[] integral = new long[integralLength];
        int row = 1;
        while (row <= source.Height)
        {
            long rowSum = 0L;
            int column = 1;
            while (column <= source.Width)
            {
                long value = source.ReadSample(column - 1, row - 1);
                rowSum = rowSum + (value * value);
                integral[(row * integralWidth) + column]
                    = integral[((row - 1) * integralWidth) + column] + rowSum;
                column = column + 1;
            }

            row = row + 1;
        }

        return integral;
    }

    static long ReadIntegralWindow(long[] integral, int width, int[] bounds)
    {
        int integralWidth = width + 1;
        int left = bounds[0];
        int top = bounds[1];
        int right = bounds[2];
        int bottom = bounds[3];
        return integral[(bottom * integralWidth) + right]
            - integral[(top * integralWidth) + right]
            - integral[(bottom * integralWidth) + left]
            + integral[(top * integralWidth) + left];
    }

    static int[] CalculateWindowBounds(
        int column,
        int row,
        int radius,
        int width,
        int height)
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

        if (right > width)
        {
            right = width;
        }

        if (bottom > height)
        {
            bottom = height;
        }

        return new int[] { left, top, right, bottom };
    }

    static double[] CalculateWindowStatistics(
        long[] sums,
        long[] squares,
        int width,
        int height,
        int column,
        int row,
        int radius)
    {
        int[] bounds = CalculateWindowBounds(column, row, radius, width, height);
        long sum = ReadIntegralWindow(sums, width, bounds);
        long squareSum = ReadIntegralWindow(squares, width, bounds);
        int count = (bounds[2] - bounds[0]) * (bounds[3] - bounds[1]);
        double mean = (double)sum / (double)count;
        double variance = ((double)squareSum / (double)count) - (mean * mean);
        if (variance < 0.0)
        {
            variance = 0.0;
        }

        return new double[] { mean, global::System.Math.Sqrt(variance) };
    }

    static double CalculateMeanLocalContrast(
        ManagedGrayImage source,
        long[] sums,
        long[] squares,
        int radius)
    {
        double total = 0.0;
        int row = 0;
        while (row < source.Height)
        {
            int column = 0;
            while (column < source.Width)
            {
                double[] statistics = CalculateWindowStatistics(
                    sums,
                    squares,
                    source.Width,
                    source.Height,
                    column,
                    row,
                    radius);
                total = total + statistics[1];
                column = column + 1;
            }

            row = row + 1;
        }

        return total / (double)(source.Width * source.Height);
    }

    static void AddCandidate(
        global::System.Collections.Generic.List<DataMatrixDpmBinaryCandidate> candidates,
        ModuleMatrix modules,
        string method,
        int connectionRadius)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            if (MatricesEqual(candidates[index].Modules, modules))
            {
                return;
            }

            index = index + 1;
        }

        int foregroundCount = 0;
        int row = 0;
        while (row < modules.Height)
        {
            int column = 0;
            while (column < modules.Width)
            {
                if (modules.GetModule(column, row))
                {
                    foregroundCount = foregroundCount + 1;
                }

                column = column + 1;
            }

            row = row + 1;
        }

        DataMatrixDpmBinaryCandidate candidate = new DataMatrixDpmBinaryCandidate();
        candidate.Modules = CopyMatrix(modules);
        candidate.Method = method;
        candidate.ConnectionRadius = connectionRadius;
        candidate.ForegroundFraction = (double)foregroundCount
            / (double)(modules.Width * modules.Height);
        candidates.Add(candidate);
    }

    static bool MatricesEqual(ModuleMatrix first, ModuleMatrix second)
    {
        if (first.Width != second.Width || first.Height != second.Height)
        {
            return false;
        }

        int row = 0;
        while (row < first.Height)
        {
            int column = 0;
            while (column < first.Width)
            {
                if (first.GetModule(column, row) != second.GetModule(column, row))
                {
                    return false;
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return true;
    }

    static ModuleMatrix CopyMatrix(ModuleMatrix source)
    {
        ModuleMatrix copy = new ModuleMatrix(source.Width, source.Height);
        copy.LoadModules(source.CopyModules());
        return copy;
    }

    static void ValidateEffort(BarcodeEffort effort)
    {
        if (effort != BarcodeEffort.Fast
            && effort != BarcodeEffort.Balanced
            && effort != BarcodeEffort.Thorough)
        {
            throw new BarcodeValidationException("DPM preprocessing effort is invalid.");
        }
    }
}
