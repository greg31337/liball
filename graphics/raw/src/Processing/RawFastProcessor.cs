namespace raw;

internal class RawNearestValues
{
    internal double Sum;
    internal int Count;

    internal RawNearestValues()
    {
        Sum = 0.0;
        Count = 0;
    }
}

internal static class RawFastProcessor
{
    internal static RawRgbImage CreateHalfSizePreview(RawSensorImage image, RawPreviewOptions options)
    {
        ValidatePreview(image, options);
        RawArea area = RawDefectProcessor.SelectedArea(image);
        int outputWidth = (area.Width + 1) / 2;
        int outputHeight = (area.Height + 1) / 2;
        long pixelCount = CheckedMath.PixelCount(outputWidth, outputHeight, options.Limits);
        long sampleCountLong = CheckedMath.Multiply(pixelCount, 3);
        CheckedMath.DecodedSize(sampleCountLong, 4, options.Limits);
        CheckedMath.AllocationSize(sampleCountLong, 4, options.Limits);
        int sampleCount = CheckedMath.ToInt32(sampleCountLong);
        float[] output = new float[sampleCount];
        int outputY = 0;
        while (outputY < outputHeight)
        {
            options.CheckCancellation();
            int outputX = 0;
            while (outputX < outputWidth)
            {
                int sourceX = area.X + outputX * 2;
                int sourceY = area.Y + outputY * 2;
                double red = 0.0;
                double green = 0.0;
                double blue = 0.0;
                int redCount = 0;
                int greenCount = 0;
                int blueCount = 0;
                int y = sourceY;
                while (y < sourceY + 2 && y < area.Y + area.Height)
                {
                    int x = sourceX;
                    while (x < sourceX + 2 && x < area.X + area.Width)
                    {
                        if (!RawDefectProcessor.Masked(image.MaskedAreas, x, y))
                        {
                            double value = ReadNormalized(image, x, y, 0);
                            RawCfaColor color = (image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(x, y);
                            if (color == RawCfaColor.Red)
                            {
                                red += value;
                                redCount++;
                            }
                            else if (color == RawCfaColor.Blue)
                            {
                                blue += value;
                                blueCount++;
                            }
                            else
                            {
                                green += value;
                                greenCount++;
                            }
                        }
                        x++;
                    }
                    y++;
                }
                red = redCount == 0 ? NearestColor(image, sourceX, sourceY, RawCfaColor.Red, options) : red / redCount;
                green = greenCount == 0 ? NearestColor(image, sourceX, sourceY, RawCfaColor.Green, options) : green / greenCount;
                blue = blueCount == 0 ? NearestColor(image, sourceX, sourceY, RawCfaColor.Blue, options) : blue / blueCount;
                int destination = (outputY * outputWidth + outputX) * 3;
                output[destination] = (float)red;
                output[destination + 1] = (float)green;
                output[destination + 2] = (float)blue;
                outputX++;
            }
            outputY++;
        }
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue)
        };
        return new RawRgbImage(outputWidth, outputHeight, 3, outputWidth * 3, null, null, output, channels, RawColorSpace.RawCamera, RawTransferFunction.Linear, image.Info.Orientation, false, true, image.Diagnostics);
    }

    internal static void ValidatePreview(RawSensorImage image, RawPreviewOptions options)
    {
        if (options == null || !options.Validate().IsValid() || image == null || image.Info == null || !image.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid normalized CFA image and preview options are required.", -1);
        }
        RawCfaPattern? cfa = image.Info.Cfa;
        if (cfa == null || cfa.RepeatWidth != 2 || cfa.RepeatHeight != 2 || !BayerColors(cfa))
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Half-size preview reconstruction requires a two-by-two Bayer CFA.", -1);
        }
        (options ?? throw new System.InvalidOperationException("A required value is unavailable.")).CheckCancellation();
    }

    internal static bool BayerColors(RawCfaPattern cfa)
    {
        int red = 0;
        int green = 0;
        int blue = 0;
        int index = 0;
        while (index < cfa.Colors.Length)
        {
            RawCfaColor color = cfa.Colors[index];
            if (color == RawCfaColor.Red)
            {
                red++;
            }
            else if (color == RawCfaColor.Blue)
            {
                blue++;
            }
            else if (color == RawCfaColor.Green || color == RawCfaColor.Green2)
            {
                green++;
            }
            index++;
        }
        return red == 1 && green == 2 && blue == 1;
    }

    internal static double NearestColor(RawSensorImage image, int centerX, int centerY, RawCfaColor wanted, RawPreviewOptions options)
    {
        RawArea visible = image.Info.ActiveArea == null ? image.Info.StoredArea : image.Info.ActiveArea;
        int maximumRadius = visible.Width > visible.Height ? visible.Width : visible.Height;
        int radius = 0;
        while (radius <= maximumRadius)
        {
            options.CheckCancellation();
            RawNearestValues values = new RawNearestValues();
            int left = centerX - radius;
            int right = centerX + radius;
            int top = centerY - radius;
            int bottom = centerY + radius;
            int x = left;
            while (x <= right)
            {
                AddNearest(image, visible, x, top, wanted, values);
                if (bottom != top)
                {
                    AddNearest(image, visible, x, bottom, wanted, values);
                }
                x++;
            }
            int y = top + 1;
            while (y < bottom)
            {
                AddNearest(image, visible, left, y, wanted, values);
                if (right != left)
                {
                    AddNearest(image, visible, right, y, wanted, values);
                }
                y++;
            }
            if (values.Count != 0)
            {
                return values.Sum / values.Count;
            }
            radius++;
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Half-size preview reconstruction requires unmasked red, green, and blue samples.", -1);
    }

    internal static void AddNearest(RawSensorImage image, RawArea visible, int x, int y, RawCfaColor wanted, RawNearestValues values)
    {
        if (!visible.Contains(x, y) || RawDefectProcessor.Masked(image.MaskedAreas, x, y))
        {
            return;
        }
        RawCfaColor color = (image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColorUnchecked(x, y);
        bool matching = wanted == RawCfaColor.Green ? color == RawCfaColor.Green || color == RawCfaColor.Green2 : color == wanted;
        if (matching)
        {
            values.Sum += ReadNormalized(image, x, y, 0);
            values.Count++;
        }
    }

    internal static RawAnalysisResult Analyze(RawSensorImage image, RawAnalysisOptions options)
    {
        ValidateAnalysis(image, options);
        RawArea selected = RawDefectProcessor.SelectedArea(image);
        int channelCount = image.Info.GetChannelCount();
        int groupCount = image.Info.Cfa == null ? channelCount : image.Info.Cfa.RepeatWidth * image.Info.Cfa.RepeatHeight;
        long histogramEntries = CheckedMath.Multiply(groupCount, options.HistogramBinCount);
        int requestedPlaneCount = DiagnosticPlaneCount(options, channelCount);
        long selectedPixels = CheckedMath.Multiply(selected.Width, selected.Height);
        long histogramBytes = CheckedMath.Multiply(histogramEntries, 8);
        long accumulatorBytes = CheckedMath.Multiply(groupCount, 64);
        long planeBytes = CheckedMath.Multiply(CheckedMath.Multiply(selectedPixels, requestedPlaneCount), 4);
        long analysisBytes = CheckedMath.Add(CheckedMath.Add(histogramBytes, accumulatorBytes), planeBytes);
        CheckedMath.DecodedSize(analysisBytes, 1, options.Limits);
        CheckedMath.AllocationSize(options.HistogramBinCount, 8, options.Limits);
        long[] counts = new long[groupCount];
        double[] minimums = new double[groupCount];
        double[] maximums = new double[groupCount];
        double[] means = new double[groupCount];
        double[] moments = new double[groupCount];
        long[] blackCounts = new long[groupCount];
        long[] whiteCounts = new long[groupCount];
        long[] repairedCounts = new long[groupCount];
        long[][] histograms = new long[groupCount][];
        int group = 0;
        while (group < groupCount)
        {
            minimums[group] = 1.0;
            maximums[group] = 0.0;
            histograms[group] = new long[options.HistogramBinCount];
            group++;
        }
        Accumulate(image, options, selected, counts, minimums, maximums, means, moments, blackCounts, whiteCounts, repairedCounts, histograms);
        System.Collections.Generic.List<RawSampleStatistics> statistics = new System.Collections.Generic.List<RawSampleStatistics>();
        group = 0;
        while (group < groupCount)
        {
            if (counts[group] != 0)
            {
                int channel = image.Info.Cfa == null ? group : 0;
                int phase = image.Info.Cfa == null ? -1 : group;
                RawCfaColor color = image.Info.Cfa == null ? RawCfaColor.Unknown : image.Info.Cfa.Colors[group];
                double deviation = System.Math.Sqrt(moments[group] / counts[group]);
                statistics.Add(new RawSampleStatistics(channel, phase, color, counts[group], minimums[group], maximums[group], means[group], deviation, blackCounts[group], whiteCounts[group], repairedCounts[group], histograms[group]));
            }
            group++;
        }
        if (statistics.Count == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Raw analysis requires at least one unmasked selected sample.", -1);
        }
        RawDiagnosticPlane[] planes = CreateDiagnosticPlanes(image, options, selected);
        return new RawAnalysisResult(RawCropModel.Copy(selected), options.HistogramBinCount, statistics.ToArray(), planes);
    }

    internal static void ValidateAnalysis(RawSensorImage image, RawAnalysisOptions options)
    {
        if (options == null || !options.Validate().IsValid() || image == null || image.Info == null || !image.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid normalized sensor image and analysis options are required.", -1);
        }
        if ((options ?? throw new System.InvalidOperationException("A required value is unavailable.")).IncludeClippingPlanes && image.ClippingMask == null)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Clipping diagnostic planes require an available clipping mask.", -1);
        }
        if (options.IncludeDefectPlane && image.DefectMap == null)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Defect diagnostic planes require an available defect map.", -1);
        }
        if (options.IncludeCfaPhasePlane && image.Info.Cfa == null)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "A CFA phase diagnostic plane requires CFA metadata.", -1);
        }
        options.CheckCancellation();
    }

    internal static void Accumulate(RawSensorImage image, RawAnalysisOptions options, RawArea area, long[] counts, double[] minimums, double[] maximums, double[] means, double[] moments, long[] blackCounts, long[] whiteCounts, long[] repairedCounts, long[][] histograms)
    {
        int channelCount = image.Info.GetChannelCount();
        int y = area.Y;
        while (y < area.Y + area.Height)
        {
            options.CheckCancellation();
            int x = area.X;
            while (x < area.X + area.Width)
            {
                if (!RawDefectProcessor.Masked(image.MaskedAreas, x, y))
                {
                    int channel = 0;
                    while (channel < channelCount)
                    {
                        int group = image.Info.Cfa == null ? channel : y % image.Info.Cfa.RepeatHeight * image.Info.Cfa.RepeatWidth + x % image.Info.Cfa.RepeatWidth;
                        double value = ReadNormalized(image, x, y, channel);
                        counts[group]++;
                        double delta = value - means[group];
                        means[group] += delta / counts[group];
                        moments[group] += delta * (value - means[group]);
                        if (value < minimums[group])
                        {
                            minimums[group] = value;
                        }
                        if (value > maximums[group])
                        {
                            maximums[group] = value;
                        }
                        int bin = (int)(value * options.HistogramBinCount);
                        if (bin == options.HistogramBinCount)
                        {
                            bin--;
                        }
                        histograms[group][bin]++;
                        int sampleIndex = y * image.RowStrideSamples + x * channelCount + channel;
                        if (image.ClippingMask != null)
                        {
                            blackCounts[group] += image.ClippingMask.BlackClipped[sampleIndex];
                            whiteCounts[group] += image.ClippingMask.WhiteClipped[sampleIndex];
                        }
                        if (image.DefectMap != null)
                        {
                            repairedCounts[group] += image.DefectMap.RepairedSamples[sampleIndex];
                        }
                        channel++;
                    }
                }
                x++;
            }
            y++;
        }
    }

    internal static RawDiagnosticPlane[] CreateDiagnosticPlanes(RawSensorImage image, RawAnalysisOptions options, RawArea area)
    {
        int channelCount = image.Info.GetChannelCount();
        int planeCount = DiagnosticPlaneCount(options, channelCount);
        if (planeCount == 0)
        {
            return System.Array.Empty<RawDiagnosticPlane>();
        }
        long planeSamplesLong = CheckedMath.Multiply(area.Width, area.Height);
        long totalSamples = CheckedMath.Multiply(planeSamplesLong, planeCount);
        CheckedMath.DecodedSize(totalSamples, 4, options.Limits);
        CheckedMath.AllocationSize(planeSamplesLong, 4, options.Limits);
        int planeSamples = CheckedMath.ToInt32(planeSamplesLong);
        System.Collections.Generic.List<RawDiagnosticPlane> planes = new System.Collections.Generic.List<RawDiagnosticPlane>();
        int channel = 0;
        while (options.IncludeSamplePlanes && channel < channelCount)
        {
            planes.Add(CreatePlane(image, options, area, RawDiagnosticPlaneKind.NormalizedSample, channel, planeSamples));
            channel++;
        }
        channel = 0;
        while (options.IncludeClippingPlanes && channel < channelCount)
        {
            planes.Add(CreatePlane(image, options, area, RawDiagnosticPlaneKind.BlackClipped, channel, planeSamples));
            planes.Add(CreatePlane(image, options, area, RawDiagnosticPlaneKind.WhiteClipped, channel, planeSamples));
            channel++;
        }
        channel = 0;
        while (options.IncludeDefectPlane && channel < channelCount)
        {
            planes.Add(CreatePlane(image, options, area, RawDiagnosticPlaneKind.Repaired, channel, planeSamples));
            channel++;
        }
        if (options.IncludeCfaPhasePlane)
        {
            planes.Add(CreatePlane(image, options, area, RawDiagnosticPlaneKind.CfaPhase, -1, planeSamples));
        }
        return planes.ToArray();
    }

    internal static int DiagnosticPlaneCount(RawAnalysisOptions options, int channelCount)
    {
        return (options.IncludeSamplePlanes ? channelCount : 0) + (options.IncludeClippingPlanes ? channelCount * 2 : 0) + (options.IncludeDefectPlane ? channelCount : 0) + (options.IncludeCfaPhasePlane ? 1 : 0);
    }

    internal static RawDiagnosticPlane CreatePlane(RawSensorImage image, RawAnalysisOptions options, RawArea area, RawDiagnosticPlaneKind kind, int channel, int sampleCount)
    {
        float[] samples = new float[sampleCount];
        int channelCount = image.Info.GetChannelCount();
        int outputY = 0;
        while (outputY < area.Height)
        {
            options.CheckCancellation();
            int outputX = 0;
            while (outputX < area.Width)
            {
                int x = area.X + outputX;
                int y = area.Y + outputY;
                int destination = outputY * area.Width + outputX;
                if (kind == RawDiagnosticPlaneKind.NormalizedSample)
                {
                    samples[destination] = (float)ReadNormalized(image, x, y, channel);
                }
                else if (kind == RawDiagnosticPlaneKind.BlackClipped)
                {
                    samples[destination] = (image.ClippingMask ?? throw new System.InvalidOperationException("A required value is unavailable.")).BlackClipped[y * image.RowStrideSamples + x * channelCount + channel];
                }
                else if (kind == RawDiagnosticPlaneKind.WhiteClipped)
                {
                    samples[destination] = (image.ClippingMask ?? throw new System.InvalidOperationException("A required value is unavailable.")).WhiteClipped[y * image.RowStrideSamples + x * channelCount + channel];
                }
                else if (kind == RawDiagnosticPlaneKind.Repaired)
                {
                    samples[destination] = (image.DefectMap ?? throw new System.InvalidOperationException("A required value is unavailable.")).RepairedSamples[y * image.RowStrideSamples + x * channelCount + channel];
                }
                else
                {
                    samples[destination] = y % (image.Info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).RepeatHeight * image.Info.Cfa.RepeatWidth + x % image.Info.Cfa.RepeatWidth;
                }
                outputX++;
            }
            outputY++;
        }
        return new RawDiagnosticPlane(kind, channel, area.X, area.Y, area.Width, area.Height, area.Width, samples);
    }

    internal static double ReadNormalized(RawSensorImage image, int x, int y, int channel)
    {
        int index = y * image.RowStrideSamples + x * image.Info.GetChannelCount() + channel;
        double value = image.FloatingSamples.Length == 0 ? image.IntegerSamples[index] / 65535.0 : image.FloatingSamples[index];
        if (!RawModelValidation.IsFinite(value) || value < 0.0 || value > 1.0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Fast raw processing requires finite normalized samples between zero and one.", -1);
        }
        return value;
    }
}
