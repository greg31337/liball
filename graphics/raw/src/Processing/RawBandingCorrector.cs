namespace raw;

internal static class RawBandingCorrector
{
    internal static RawBandingCorrectionResult? Estimate(RawStoredImage storedImage, RawNormalizationOptions options, RawBlackLevel blackLevel)
    {
        bool rows = options.ApplyRowBandingCorrection;
        bool columns = options.ApplyColumnBandingCorrection;
        if (!rows && !columns)
        {
            return null;
        }
        if (options.MaskedAreas.Length == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Banding correction requires masked or optical-black calibration samples.", -1);
        }
        long calibrationSampleCount = CalibrationCount(storedImage, options);
        if (calibrationSampleCount <= 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Banding correction requires at least one masked logical sample.", -1);
        }
        double[] rowOffsets = rows ? EstimateAxis(storedImage, options, blackLevel, null, true) : System.Array.Empty<double>();
        double[] columnOffsets = columns ? EstimateAxis(storedImage, options, blackLevel, rowOffsets, false) : System.Array.Empty<double>();
        double maximum = MaximumCombined(rowOffsets, columnOffsets);
        if (maximum > options.MaximumBandingCorrection)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The estimated banding correction exceeds the configured maximum.", -1);
        }
        return new RawBandingCorrectionResult(rows, columns, rowOffsets, columnOffsets, calibrationSampleCount, maximum);
    }

    internal static double[] EstimateAxis(RawStoredImage storedImage, RawNormalizationOptions options, RawBlackLevel blackLevel, double[]? rowOffsets, bool rows)
    {
        int length = rows ? storedImage.Info.Height : storedImage.Info.Width;
        CheckedMath.DecodedSize(length, 16, options.Limits);
        CheckedMath.AllocationSize(length, 8, options.Limits);
        double[] sums = new double[length];
        long[] counts = new long[length];
        int channelCount = storedImage.Info.GetChannelCount();
        int areaIndex = 0;
        while (areaIndex < options.MaskedAreas.Length)
        {
            RawArea area = options.MaskedAreas[areaIndex];
            int y = area.Y;
            while (y < area.Y + area.Height)
            {
                options.CheckCancellation();
                int x = area.X;
                while (x < area.X + area.Width)
                {
                    int channel = 0;
                    while (channel < channelCount)
                    {
                        double linear = RawOpticalBlackEstimator.ReadLinearValue(storedImage, options, x, y, channel);
                        double residual = linear - blackLevel.GetValueUnchecked(x, y, channel);
                        if (!rows && rowOffsets != null && rowOffsets.Length != 0)
                        {
                            residual -= rowOffsets[y];
                        }
                        int index = rows ? y : x;
                        sums[index] += residual;
                        counts[index]++;
                        channel++;
                    }
                    x++;
                }
                y++;
            }
            areaIndex++;
        }
        string message = rows ? "Every stored row requires masked samples when row banding correction is requested." : "Every stored column requires masked samples when column banding correction is requested.";
        return RawOpticalBlackEstimator.Means(sums, counts, message);
    }

    internal static long CalibrationCount(RawStoredImage storedImage, RawNormalizationOptions options)
    {
        long count = 0;
        int areaIndex = 0;
        while (areaIndex < options.MaskedAreas.Length)
        {
            options.CheckCancellation();
            RawArea area = options.MaskedAreas[areaIndex];
            long pixels = CheckedMath.Multiply(area.Width, area.Height);
            long samples = CheckedMath.Multiply(pixels, storedImage.Info.GetChannelCount());
            count = CheckedMath.Add(count, samples);
            areaIndex++;
        }
        return count;
    }

    internal static double MaximumCombined(double[] rows, double[] columns)
    {
        double minimumRow = 0.0;
        double maximumRow = 0.0;
        double minimumColumn = 0.0;
        double maximumColumn = 0.0;
        int index = 0;
        while (index < rows.Length)
        {
            if (index == 0 || rows[index] < minimumRow)
            {
                minimumRow = rows[index];
            }
            if (index == 0 || rows[index] > maximumRow)
            {
                maximumRow = rows[index];
            }
            index++;
        }
        index = 0;
        while (index < columns.Length)
        {
            if (index == 0 || columns[index] < minimumColumn)
            {
                minimumColumn = columns[index];
            }
            if (index == 0 || columns[index] > maximumColumn)
            {
                maximumColumn = columns[index];
            }
            index++;
        }
        double maximum = System.Math.Abs(minimumRow + minimumColumn);
        maximum = System.Math.Max(maximum, System.Math.Abs(minimumRow + maximumColumn));
        maximum = System.Math.Max(maximum, System.Math.Abs(maximumRow + minimumColumn));
        return System.Math.Max(maximum, System.Math.Abs(maximumRow + maximumColumn));
    }

    internal static double Correction(RawBandingCorrectionResult? result, int x, int y)
    {
        if (result == null)
        {
            return 0.0;
        }
        double correction = result.RowCorrectionApplied ? result.RowOffsets[y] : 0.0;
        if (result.ColumnCorrectionApplied)
        {
            correction += result.ColumnOffsets[x];
        }
        return correction;
    }
}
