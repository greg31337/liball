namespace raw;

internal static class RawHistogramProcessor
{
    internal static RawHistogramResult Calculate(RawStoredImage image, RawHistogramOptions options)
    {
        ValidateStored(image, options);
        RawArea area = ResolveArea(options.Area, image.Info.StoredArea, image.Info.Width, image.Info.Height);
        int channelCount = image.Info.GetChannelCount();
        long[] counts = CreateCounts(channelCount);
        double[] minimums = CreateMinimums(channelCount);
        double[] maximums = CreateMaximums(channelCount);
        ScanStoredExtrema(image, options, area, counts, minimums, maximums);
        RequireSamples(counts);
        double[] domainMinimums = new double[channelCount];
        double[] domainMaximums = new double[channelCount];
        SetStoredDomains(image, minimums, maximums, domainMinimums, domainMaximums);
        return BuildStored(image, options, area, counts, minimums, maximums, domainMinimums, domainMaximums);
    }

    internal static RawHistogramResult Calculate(RawSensorImage image, RawHistogramOptions options)
    {
        ValidateSensor(image, options);
        RawArea selected = RawDefectProcessor.SelectedArea(image);
        RawArea area = ResolveArea(options.Area, selected, image.Info.Width, image.Info.Height);
        int channelCount = image.Info.GetChannelCount();
        long[] counts = CreateCounts(channelCount);
        double[] minimums = CreateMinimums(channelCount);
        double[] maximums = CreateMaximums(channelCount);
        ScanSensorExtrema(image, options, area, counts, minimums, maximums);
        RequireSamples(counts);
        double[] domainMinimums = new double[channelCount];
        double[] domainMaximums = new double[channelCount];
        int channel = 0;
        while (channel < channelCount)
        {
            domainMinimums[channel] = 0.0;
            domainMaximums[channel] = 1.0;
            channel++;
        }
        return BuildSensor(image, options, area, counts, minimums, maximums, domainMinimums, domainMaximums);
    }

    internal static RawHistogramResult Calculate(RawRgbImage image, RawHistogramOptions options)
    {
        ValidateRendered(image, options);
        RawArea complete = new RawArea(RawAreaKind.StoredImage, 0, 0, image.Width, image.Height);
        RawArea area = ResolveArea(options.Area, complete, image.Width, image.Height);
        int channelCount = image.ChannelCount;
        long[] counts = CreateCounts(channelCount);
        double[] minimums = CreateMinimums(channelCount);
        double[] maximums = CreateMaximums(channelCount);
        ScanRenderedExtrema(image, options, area, counts, minimums, maximums);
        RequireSamples(counts);
        double[] domainMinimums = new double[channelCount];
        double[] domainMaximums = new double[channelCount];
        SetRenderedDomains(image, minimums, maximums, domainMinimums, domainMaximums);
        return BuildRendered(image, options, area, counts, minimums, maximums, domainMinimums, domainMaximums);
    }

    internal static void ValidateStored(RawStoredImage image, RawHistogramOptions options)
    {
        if (options == null || !options.Validate().IsValid() || image == null || image.Info == null || !image.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid stored image and histogram options are required.", -1);
        }
        (options ?? throw new System.InvalidOperationException("A required value is unavailable.")).CheckCancellation();
    }

    internal static void ValidateSensor(RawSensorImage image, RawHistogramOptions options)
    {
        if (options == null || !options.Validate().IsValid() || image == null || image.Info == null || !image.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid normalized sensor image and histogram options are required.", -1);
        }
        (options ?? throw new System.InvalidOperationException("A required value is unavailable.")).CheckCancellation();
    }

    internal static void ValidateRendered(RawRgbImage image, RawHistogramOptions options)
    {
        if (options == null || !options.Validate().IsValid() || image == null || !image.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid rendered image and histogram options are required.", -1);
        }
        (options ?? throw new System.InvalidOperationException("A required value is unavailable.")).CheckCancellation();
    }

    internal static RawArea ResolveArea(RawArea? requested, RawArea defaultArea, int width, int height)
    {
        RawArea area = requested == null ? defaultArea : requested;
        if (area == null || !area.Validate(width, height).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The histogram area must be contained in the current image.", -1);
        }
        return new RawArea(area.Kind, area.X, area.Y, area.Width, area.Height);
    }

    internal static long[] CreateCounts(int channelCount)
    {
        return new long[channelCount];
    }

    internal static double[] CreateMinimums(int channelCount)
    {
        double[] values = new double[channelCount];
        int channel = 0;
        while (channel < channelCount)
        {
            values[channel] = double.MaxValue;
            channel++;
        }
        return values;
    }

    internal static double[] CreateMaximums(int channelCount)
    {
        double[] values = new double[channelCount];
        int channel = 0;
        while (channel < channelCount)
        {
            values[channel] = -double.MaxValue;
            channel++;
        }
        return values;
    }

    internal static void RequireSamples(long[] counts)
    {
        int channel = 0;
        while (channel < counts.Length)
        {
            if (counts[channel] <= 0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Histogram calculation requires at least one selected sample in every channel.", -1);
            }
            channel++;
        }
    }

    internal static void Observe(double value, int channel, long[] counts, double[] minimums, double[] maximums)
    {
        if (!RawModelValidation.IsFinite(value))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Histogram calculation requires finite sample values.", -1);
        }
        counts[channel]++;
        if (value < minimums[channel])
        {
            minimums[channel] = value;
        }
        if (value > maximums[channel])
        {
            maximums[channel] = value;
        }
    }

    internal static void ScanStoredExtrema(RawStoredImage image, RawHistogramOptions options, RawArea area, long[] counts, double[] minimums, double[] maximums)
    {
        int channelCount = image.Info.GetChannelCount();
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
                    Observe(ReadStored(image, y * image.RowStrideSamples + x * channelCount + channel), channel, counts, minimums, maximums);
                    channel++;
                }
                x++;
            }
            y++;
        }
    }

    internal static void ScanSensorExtrema(RawSensorImage image, RawHistogramOptions options, RawArea area, long[] counts, double[] minimums, double[] maximums)
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
                        Observe(RawFastProcessor.ReadNormalized(image, x, y, channel), channel, counts, minimums, maximums);
                        channel++;
                    }
                }
                x++;
            }
            y++;
        }
    }

    internal static void ScanRenderedExtrema(RawRgbImage image, RawHistogramOptions options, RawArea area, long[] counts, double[] minimums, double[] maximums)
    {
        int y = area.Y;
        while (y < area.Y + area.Height)
        {
            options.CheckCancellation();
            int x = area.X;
            while (x < area.X + area.Width)
            {
                int channel = 0;
                while (channel < image.ChannelCount)
                {
                    Observe(ReadRendered(image, y * image.RowStrideSamples + x * image.ChannelCount + channel), channel, counts, minimums, maximums);
                    channel++;
                }
                x++;
            }
            y++;
        }
    }

    internal static double ReadStored(RawStoredImage image, int index)
    {
        if (image.UnsignedSamples.Length != 0)
        {
            return image.UnsignedSamples[index];
        }
        if (image.SignedSamples.Length != 0)
        {
            return image.SignedSamples[index];
        }
        return image.FloatingSamples[index];
    }

    internal static double ReadRendered(RawRgbImage image, int index)
    {
        if (image.ByteSamples.Length != 0)
        {
            return image.ByteSamples[index];
        }
        if (image.UShortSamples.Length != 0)
        {
            return image.UShortSamples[index];
        }
        return image.FloatingSamples[index];
    }

    internal static void SetStoredDomains(RawStoredImage image, double[] minimums, double[] maximums, double[] domainMinimums, double[] domainMaximums)
    {
        double domainMinimum = minimums[0];
        double domainMaximum = maximums[0];
        if (image.Info.SampleKind == RawSampleKind.UnsignedInteger)
        {
            domainMinimum = 0.0;
            domainMaximum = image.Info.BitDepth == 32 ? uint.MaxValue : (1L << image.Info.BitDepth) - 1L;
        }
        else if (image.Info.SampleKind == RawSampleKind.SignedInteger)
        {
            domainMinimum = image.Info.BitDepth == 32 ? int.MinValue : -(1L << image.Info.BitDepth - 1);
            domainMaximum = image.Info.BitDepth == 32 ? int.MaxValue : (1L << image.Info.BitDepth - 1) - 1L;
        }
        int channel = 0;
        while (channel < domainMinimums.Length)
        {
            if (image.Info.SampleKind == RawSampleKind.Half || image.Info.SampleKind == RawSampleKind.Single)
            {
                domainMinimums[channel] = minimums[channel];
                domainMaximums[channel] = maximums[channel];
            }
            else
            {
                domainMinimums[channel] = domainMinimum;
                domainMaximums[channel] = domainMaximum;
            }
            channel++;
        }
    }

    internal static void SetRenderedDomains(RawRgbImage image, double[] minimums, double[] maximums, double[] domainMinimums, double[] domainMaximums)
    {
        int channel = 0;
        while (channel < domainMinimums.Length)
        {
            if (image.ByteSamples.Length != 0)
            {
                domainMinimums[channel] = 0.0;
                domainMaximums[channel] = byte.MaxValue;
            }
            else if (image.UShortSamples.Length != 0)
            {
                domainMinimums[channel] = 0.0;
                domainMaximums[channel] = ushort.MaxValue;
            }
            else
            {
                domainMinimums[channel] = minimums[channel];
                domainMaximums[channel] = maximums[channel];
            }
            channel++;
        }
    }

    internal static RawHistogramResult BuildStored(RawStoredImage image, RawHistogramOptions options, RawArea area, long[] counts, double[] minimums, double[] maximums, double[] domainMinimums, double[] domainMaximums)
    {
        long[][] histograms = CreateHistograms(counts.Length, options);
        double[][] binMinimums = CreateBinExtrema(counts.Length, options, true);
        double[][] binMaximums = CreateBinExtrema(counts.Length, options, false);
        int channelCount = image.Info.GetChannelCount();
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
                    AddBin(ReadStored(image, y * image.RowStrideSamples + x * channelCount + channel), channel, domainMinimums, domainMaximums, histograms, binMinimums, binMaximums);
                    channel++;
                }
                x++;
            }
            y++;
        }
        return CreateResult(area, options, image.Info.Channels, counts, minimums, maximums, domainMinimums, domainMaximums, histograms, binMinimums, binMaximums);
    }

    internal static RawHistogramResult BuildSensor(RawSensorImage image, RawHistogramOptions options, RawArea area, long[] counts, double[] minimums, double[] maximums, double[] domainMinimums, double[] domainMaximums)
    {
        long[][] histograms = CreateHistograms(counts.Length, options);
        double[][] binMinimums = CreateBinExtrema(counts.Length, options, true);
        double[][] binMaximums = CreateBinExtrema(counts.Length, options, false);
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
                        AddBin(RawFastProcessor.ReadNormalized(image, x, y, channel), channel, domainMinimums, domainMaximums, histograms, binMinimums, binMaximums);
                        channel++;
                    }
                }
                x++;
            }
            y++;
        }
        return CreateResult(area, options, image.Info.Channels, counts, minimums, maximums, domainMinimums, domainMaximums, histograms, binMinimums, binMaximums);
    }

    internal static RawHistogramResult BuildRendered(RawRgbImage image, RawHistogramOptions options, RawArea area, long[] counts, double[] minimums, double[] maximums, double[] domainMinimums, double[] domainMaximums)
    {
        long[][] histograms = CreateHistograms(counts.Length, options);
        double[][] binMinimums = CreateBinExtrema(counts.Length, options, true);
        double[][] binMaximums = CreateBinExtrema(counts.Length, options, false);
        int y = area.Y;
        while (y < area.Y + area.Height)
        {
            options.CheckCancellation();
            int x = area.X;
            while (x < area.X + area.Width)
            {
                int channel = 0;
                while (channel < image.ChannelCount)
                {
                    AddBin(ReadRendered(image, y * image.RowStrideSamples + x * image.ChannelCount + channel), channel, domainMinimums, domainMaximums, histograms, binMinimums, binMaximums);
                    channel++;
                }
                x++;
            }
            y++;
        }
        return CreateResult(area, options, image.Channels, counts, minimums, maximums, domainMinimums, domainMaximums, histograms, binMinimums, binMaximums);
    }

    internal static long[][] CreateHistograms(int channelCount, RawHistogramOptions options)
    {
        long histogramEntries = CheckedMath.Multiply(channelCount, options.HistogramBinCount);
        long percentileEntries = CheckedMath.Multiply(channelCount, options.Percentiles.Length);
        long histogramBytes = CheckedMath.Multiply(histogramEntries, 24);
        long percentileBytes = CheckedMath.Multiply(percentileEntries, 8);
        long supportBytes = CheckedMath.Multiply(channelCount, 96);
        CheckedMath.DecodedSize(CheckedMath.Add(CheckedMath.Add(histogramBytes, percentileBytes), supportBytes), 1, options.Limits);
        CheckedMath.AllocationSize(options.HistogramBinCount, 8, options.Limits);
        CheckedMath.AllocationSize(options.Percentiles.Length, 8, options.Limits);
        long[][] histograms = new long[channelCount][];
        int channel = 0;
        while (channel < channelCount)
        {
            histograms[channel] = new long[options.HistogramBinCount];
            channel++;
        }
        return histograms;
    }

    internal static double[][] CreateBinExtrema(int channelCount, RawHistogramOptions options, bool minimum)
    {
        double[][] values = new double[channelCount][];
        int channel = 0;
        while (channel < channelCount)
        {
            values[channel] = new double[options.HistogramBinCount];
            int bin = 0;
            while (bin < options.HistogramBinCount)
            {
                values[channel][bin] = minimum ? double.MaxValue : -double.MaxValue;
                bin++;
            }
            channel++;
        }
        return values;
    }

    internal static void AddBin(double value, int channel, double[] domainMinimums, double[] domainMaximums, long[][] histograms, double[][] binMinimums, double[][] binMaximums)
    {
        int bin = Bin(value, domainMinimums[channel], domainMaximums[channel], histograms[channel].Length);
        histograms[channel][bin]++;
        if (value < binMinimums[channel][bin])
        {
            binMinimums[channel][bin] = value;
        }
        if (value > binMaximums[channel][bin])
        {
            binMaximums[channel][bin] = value;
        }
    }

    internal static int Bin(double value, double minimum, double maximum, int binCount)
    {
        if (maximum == minimum)
        {
            return 0;
        }
        double normalized = (value - minimum) / (maximum - minimum);
        int bin = (int)System.Math.Floor(normalized * binCount);
        if (bin < 0)
        {
            return 0;
        }
        if (bin >= binCount)
        {
            return binCount - 1;
        }
        return bin;
    }

    internal static RawHistogramResult CreateResult(RawArea area, RawHistogramOptions options, RawChannelDescription[] descriptions, long[] counts, double[] minimums, double[] maximums, double[] domainMinimums, double[] domainMaximums, long[][] histograms, double[][] binMinimums, double[][] binMaximums)
    {
        double[] requested = new double[options.Percentiles.Length];
        System.Array.Copy(options.Percentiles, requested, requested.Length);
        RawHistogramChannel[] channels = new RawHistogramChannel[counts.Length];
        int channel = 0;
        while (channel < channels.Length)
        {
            double[] values = PercentileValues(requested, counts[channel], minimums[channel], maximums[channel], histograms[channel], binMinimums[channel], binMaximums[channel]);
            RawChannelDescription description = descriptions[channel];
            channels[channel] = new RawHistogramChannel(channel, description.Name, description.Kind, counts[channel], minimums[channel], maximums[channel], domainMinimums[channel], domainMaximums[channel], histograms[channel], values);
            channel++;
        }
        return new RawHistogramResult(new RawArea(area.Kind, area.X, area.Y, area.Width, area.Height), options.HistogramBinCount, requested, channels);
    }

    internal static double[] PercentileValues(double[] requested, long sampleCount, double minimum, double maximum, long[] histogram, double[] binMinimums, double[] binMaximums)
    {
        double[] values = new double[requested.Length];
        int percentile = 0;
        while (percentile < requested.Length)
        {
            if (requested[percentile] <= 0.0 || minimum == maximum)
            {
                values[percentile] = minimum;
            }
            else if (requested[percentile] >= 1.0)
            {
                values[percentile] = maximum;
            }
            else
            {
                long rank = (long)System.Math.Floor(requested[percentile] * (sampleCount - 1));
                long cumulative = 0;
                int bin = 0;
                while (bin < histogram.Length)
                {
                    long next = cumulative + histogram[bin];
                    if (next > rank)
                    {
                        long count = histogram[bin];
                        if (count <= 1 || binMinimums[bin] == binMaximums[bin])
                        {
                            values[percentile] = binMinimums[bin];
                        }
                        else
                        {
                            double position = (double)(rank - cumulative) / (count - 1);
                            values[percentile] = binMinimums[bin] + (binMaximums[bin] - binMinimums[bin]) * position;
                        }
                        break;
                    }
                    cumulative = next;
                    bin++;
                }
            }
            percentile++;
        }
        return values;
    }
}
