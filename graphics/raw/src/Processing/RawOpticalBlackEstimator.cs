namespace raw;

internal static class RawOpticalBlackEstimator
{
    internal static RawBlackLevel EstimateValidated(RawStoredImage storedImage, RawNormalizationOptions options)
    {
        if (!RawNormalizer.HasOpticalBlack(options.MaskedAreas))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Optical-black estimation requires at least one optical-black area.", -1);
        }
        RawImageInfo info = storedImage.Info;
        int channelCount = info.GetChannelCount();
        long cellCountLong = CheckedMath.Multiply(options.OpticalBlackRepeatWidth, options.OpticalBlackRepeatHeight);
        cellCountLong = CheckedMath.Multiply(cellCountLong, channelCount);
        CheckedMath.DecodedSize(cellCountLong, 16, options.Limits);
        CheckedMath.AllocationSize(cellCountLong, 8, options.Limits);
        int cellCount = CheckedMath.ToInt32(cellCountLong);
        double[] sums = new double[cellCount];
        long[] counts = new long[cellCount];
        AccumulateCells(storedImage, options, sums, counts);
        double[] values = Means(sums, counts, "Every repeating optical-black cell and channel requires at least one sample.");
        double[] rowDeltas = options.EstimateBlackRowDeltas ? EstimateRows(storedImage, options, values) : System.Array.Empty<double>();
        double[] columnDeltas = options.EstimateBlackColumnDeltas ? EstimateColumns(storedImage, options, values, rowDeltas) : System.Array.Empty<double>();
        return new RawBlackLevel(options.OpticalBlackRepeatWidth, options.OpticalBlackRepeatHeight, channelCount, values, rowDeltas, columnDeltas);
    }

    internal static void AccumulateCells(RawStoredImage storedImage, RawNormalizationOptions options, double[] sums, long[] counts)
    {
        int areaIndex = 0;
        while (areaIndex < options.MaskedAreas.Length)
        {
            RawArea area = options.MaskedAreas[areaIndex];
            if (area.Kind == RawAreaKind.OpticalBlack)
            {
                int y = area.Y;
                while (y < area.Y + area.Height)
                {
                    options.CheckCancellation();
                    int x = area.X;
                    while (x < area.X + area.Width)
                    {
                        int channel = 0;
                        while (channel < storedImage.Info.GetChannelCount())
                        {
                            double value = ReadLinearValue(storedImage, options, x, y, channel);
                            int cell = CellIndex(x, y, channel, storedImage.Info.GetChannelCount(), options);
                            sums[cell] += value;
                            counts[cell]++;
                            channel++;
                        }
                        x++;
                    }
                    y++;
                }
            }
            areaIndex++;
        }
    }

    internal static double[] EstimateRows(RawStoredImage storedImage, RawNormalizationOptions options, double[] values)
    {
        int height = storedImage.Info.Height;
        CheckedMath.DecodedSize(height, 16, options.Limits);
        CheckedMath.AllocationSize(height, 8, options.Limits);
        double[] sums = new double[height];
        long[] counts = new long[height];
        AccumulateResiduals(storedImage, options, values, null, sums, counts, true);
        return Means(sums, counts, "Every stored row requires optical-black samples when row deltas are requested.");
    }

    internal static double[] EstimateColumns(RawStoredImage storedImage, RawNormalizationOptions options, double[] values, double[] rowDeltas)
    {
        int width = storedImage.Info.Width;
        CheckedMath.DecodedSize(width, 16, options.Limits);
        CheckedMath.AllocationSize(width, 8, options.Limits);
        double[] sums = new double[width];
        long[] counts = new long[width];
        AccumulateResiduals(storedImage, options, values, rowDeltas, sums, counts, false);
        return Means(sums, counts, "Every stored column requires optical-black samples when column deltas are requested.");
    }

    internal static void AccumulateResiduals(RawStoredImage storedImage, RawNormalizationOptions options, double[] values, double[]? rowDeltas, double[] sums, long[] counts, bool rows)
    {
        int channelCount = storedImage.Info.GetChannelCount();
        int areaIndex = 0;
        while (areaIndex < options.MaskedAreas.Length)
        {
            RawArea area = options.MaskedAreas[areaIndex];
            if (area.Kind == RawAreaKind.OpticalBlack)
            {
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
                            double value = ReadLinearValue(storedImage, options, x, y, channel);
                            value -= values[CellIndex(x, y, channel, channelCount, options)];
                            if (!rows && rowDeltas != null && rowDeltas.Length != 0)
                            {
                                value -= rowDeltas[y];
                            }
                            int index = rows ? y : x;
                            sums[index] += value;
                            counts[index]++;
                            channel++;
                        }
                        x++;
                    }
                    y++;
                }
            }
            areaIndex++;
        }
    }

    internal static double[] Means(double[] sums, long[] counts, string missingMessage)
    {
        double[] result = new double[sums.Length];
        int index = 0;
        while (index < result.Length)
        {
            if (counts[index] == 0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, missingMessage, -1);
            }
            result[index] = sums[index] / counts[index];
            if (!RawModelValidation.IsFinite(result[index]))
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Optical-black means must be finite.", -1);
            }
            index++;
        }
        return result;
    }

    internal static double ReadLinearValue(RawStoredImage storedImage, RawNormalizationOptions options, int x, int y, int channel)
    {
        int index = y * storedImage.RowStrideSamples + x * storedImage.Info.GetChannelCount() + channel;
        double stored = RawNormalizer.ReadValue(storedImage, index);
        double linear = RawNormalizer.Linearize(stored, options);
        if (!RawModelValidation.IsFinite(stored) || !RawModelValidation.IsFinite(linear))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Optical-black samples must be finite.", -1);
        }
        return linear;
    }

    internal static int CellIndex(int x, int y, int channel, int channelCount, RawNormalizationOptions options)
    {
        int repeatX = x % options.OpticalBlackRepeatWidth;
        int repeatY = y % options.OpticalBlackRepeatHeight;
        return (repeatY * options.OpticalBlackRepeatWidth + repeatX) * channelCount + channel;
    }
}
