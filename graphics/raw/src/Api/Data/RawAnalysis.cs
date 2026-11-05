namespace raw;

/// <summary>Contains normalized statistics and a histogram for one logical channel or CFA phase.</summary>
public class RawSampleStatistics
{
    /// <summary>The zero-based logical channel.</summary>
    public int Channel;
    /// <summary>The zero-based CFA phase, or <c>-1</c> for non-CFA samples.</summary>
    public int CfaPhase;
    /// <summary>The CFA color, or <see cref="RawCfaColor.Unknown"/> for non-CFA samples.</summary>
    public RawCfaColor CfaColor;
    /// <summary>The number of unmasked selected samples.</summary>
    public long SampleCount;
    /// <summary>The minimum normalized sample value.</summary>
    public double Minimum;
    /// <summary>The maximum normalized sample value.</summary>
    public double Maximum;
    /// <summary>The arithmetic mean normalized sample value.</summary>
    public double Mean;
    /// <summary>The population standard deviation.</summary>
    public double StandardDeviation;
    /// <summary>The number of black-clipped samples when a clipping mask is available.</summary>
    public long BlackClippedCount;
    /// <summary>The number of white-clipped samples when a clipping mask is available.</summary>
    public long WhiteClippedCount;
    /// <summary>The number of repaired samples when a defect map is available.</summary>
    public long RepairedCount;
    /// <summary>Histogram counts spanning normalized values from zero through one.</summary>
    public long[] Histogram;

    /// <summary>Creates one raw statistics record.</summary>
    /// <param name="channel">The logical channel.</param>
    /// <param name="cfaPhase">The CFA phase, or <c>-1</c>.</param>
    /// <param name="cfaColor">The CFA color, or unknown.</param>
    /// <param name="sampleCount">The selected sample count.</param>
    /// <param name="minimum">The minimum value.</param>
    /// <param name="maximum">The maximum value.</param>
    /// <param name="mean">The mean value.</param>
    /// <param name="standardDeviation">The population standard deviation.</param>
    /// <param name="blackClippedCount">The black-clipped count.</param>
    /// <param name="whiteClippedCount">The white-clipped count.</param>
    /// <param name="repairedCount">The repaired count.</param>
    /// <param name="histogram">The histogram counts.</param>
    public RawSampleStatistics(int channel, int cfaPhase, RawCfaColor cfaColor, long sampleCount, double minimum, double maximum, double mean, double standardDeviation, long blackClippedCount, long whiteClippedCount, long repairedCount, long[] histogram)
    {
        Channel = channel;
        CfaPhase = cfaPhase;
        CfaColor = cfaColor;
        SampleCount = sampleCount;
        Minimum = minimum;
        Maximum = maximum;
        Mean = mean;
        StandardDeviation = standardDeviation;
        BlackClippedCount = blackClippedCount;
        WhiteClippedCount = whiteClippedCount;
        RepairedCount = repairedCount;
        Histogram = histogram == null ? System.Array.Empty<long>() : histogram;
    }

    /// <summary>Validates normalized moments, counts, and histogram totals.</summary>
    /// <returns>All statistics validation findings.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (Channel < 0 || CfaPhase < -1 || !RawCfaPattern.DefinedColor(CfaColor))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "Statistics.Identity", "Statistics require a valid channel, phase, and CFA color.");
        }
        if (SampleCount <= 0 || !RawModelValidation.IsFinite(Minimum) || !RawModelValidation.IsFinite(Maximum) || !RawModelValidation.IsFinite(Mean) || !RawModelValidation.IsFinite(StandardDeviation) || Minimum < 0.0 || Maximum > 1.0 || Minimum > Maximum || Mean < Minimum || Mean > Maximum || StandardDeviation < 0.0)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "Statistics.Values", "Statistics require finite normalized moments and a positive sample count.");
        }
        if (BlackClippedCount < 0 || WhiteClippedCount < 0 || RepairedCount < 0 || BlackClippedCount > SampleCount || WhiteClippedCount > SampleCount || RepairedCount > SampleCount)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "Statistics.Counts", "Diagnostic counts must lie within the selected sample count.");
        }
        if (Histogram == null || Histogram.Length < 2)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "Statistics.Histogram", "A statistics histogram requires at least two bins.");
        }
        else
        {
            long total = 0;
            int index = 0;
            while (index < Histogram.Length)
            {
                if (Histogram[index] < 0 || total > long.MaxValue - Histogram[index])
                {
                    RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "Statistics.Histogram", "Histogram counts must be nonnegative and cannot overflow.");
                    total = -1;
                    break;
                }
                total += Histogram[index];
                index++;
            }
            if (total >= 0 && total != SampleCount)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "Statistics.Histogram", "Histogram counts must sum to the selected sample count.");
            }
        }
        return new RawValidationReport(true, messages.ToArray());
    }
}

/// <summary>Contains one selected-area floating diagnostic plane.</summary>
public class RawDiagnosticPlane
{
    /// <summary>The values represented by the plane.</summary>
    public RawDiagnosticPlaneKind Kind;
    /// <summary>The logical channel, or <c>-1</c> for a CFA phase plane.</summary>
    public int Channel;
    /// <summary>The left stored coordinate represented by the plane.</summary>
    public int X;
    /// <summary>The top stored coordinate represented by the plane.</summary>
    public int Y;
    /// <summary>The plane width.</summary>
    public int Width;
    /// <summary>The plane height.</summary>
    public int Height;
    /// <summary>The distance between plane row starts in samples.</summary>
    public int RowStrideSamples;
    /// <summary>The floating diagnostic values.</summary>
    public float[] Samples;

    /// <summary>Creates one diagnostic plane.</summary>
    /// <param name="kind">The diagnostic value kind.</param>
    /// <param name="channel">The logical channel, or <c>-1</c>.</param>
    /// <param name="x">The left stored coordinate.</param>
    /// <param name="y">The top stored coordinate.</param>
    /// <param name="width">The plane width.</param>
    /// <param name="height">The plane height.</param>
    /// <param name="rowStrideSamples">The plane row stride.</param>
    /// <param name="samples">The plane samples.</param>
    public RawDiagnosticPlane(RawDiagnosticPlaneKind kind, int channel, int x, int y, int width, int height, int rowStrideSamples, float[] samples)
    {
        Kind = kind;
        Channel = channel;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        RowStrideSamples = rowStrideSamples;
        Samples = samples == null ? System.Array.Empty<float>() : samples;
    }

    /// <summary>Validates plane identity, geometry, storage, and values.</summary>
    /// <returns>All diagnostic-plane validation findings.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        long required = RawImageBufferValidation.RequiredSamples(Width, Height, 1, RowStrideSamples);
        if (Kind <= RawDiagnosticPlaneKind.Unknown || Kind > RawDiagnosticPlaneKind.CfaPhase || Kind == RawDiagnosticPlaneKind.CfaPhase && Channel != -1 || Kind != RawDiagnosticPlaneKind.CfaPhase && Channel < 0 || X < 0 || Y < 0)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "DiagnosticPlane.Identity", "A diagnostic plane requires a defined kind, channel, and stored origin.");
        }
        if (required < 0 || Samples == null || Samples.LongLength < required)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "DiagnosticPlane.Storage", "Diagnostic plane storage must match its geometry.");
        }
        else
        {
            int row = 0;
            while (row < Height)
            {
                int index = row * RowStrideSamples;
                int end = index + Width;
                while (index < end)
                {
                    double value = Samples[index];
                    bool mask = Kind == RawDiagnosticPlaneKind.BlackClipped || Kind == RawDiagnosticPlaneKind.WhiteClipped || Kind == RawDiagnosticPlaneKind.Repaired;
                    bool invalidMask = mask && value != 0.0 && value != 1.0;
                    bool invalidSample = Kind == RawDiagnosticPlaneKind.NormalizedSample && (value < 0.0 || value > 1.0);
                    bool invalidPhase = Kind == RawDiagnosticPlaneKind.CfaPhase && (value < 0.0 || value != (int)value);
                    if (!RawModelValidation.IsFinite(value) || invalidMask || invalidSample || invalidPhase)
                    {
                        RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "DiagnosticPlane.Samples", "Diagnostic samples must be finite and normalized except for CFA phase identifiers.");
                        return new RawValidationReport(true, messages.ToArray());
                    }
                    index++;
                }
                row++;
            }
        }
        return new RawValidationReport(true, messages.ToArray());
    }
}

/// <summary>Contains selected-area raw statistics and requested diagnostic planes.</summary>
public class RawAnalysisResult
{
    /// <summary>The analyzed stored-coordinate area.</summary>
    public RawArea? Area;
    /// <summary>The histogram bin count shared by every statistics record.</summary>
    public int HistogramBinCount;
    /// <summary>Statistics in logical-channel or row-major CFA-phase order.</summary>
    public RawSampleStatistics[] Statistics;
    /// <summary>The requested diagnostic planes.</summary>
    public RawDiagnosticPlane[] DiagnosticPlanes;

    /// <summary>Creates one raw analysis result.</summary>
    /// <param name="area">The analyzed area.</param>
    /// <param name="histogramBinCount">The histogram bin count.</param>
    /// <param name="statistics">The statistics records.</param>
    /// <param name="diagnosticPlanes">The diagnostic planes.</param>
    public RawAnalysisResult(RawArea? area, int histogramBinCount, RawSampleStatistics[] statistics, RawDiagnosticPlane[] diagnosticPlanes)
    {
        Area = area;
        HistogramBinCount = histogramBinCount;
        Statistics = statistics == null ? System.Array.Empty<RawSampleStatistics>() : statistics;
        DiagnosticPlanes = diagnosticPlanes == null ? System.Array.Empty<RawDiagnosticPlane>() : diagnosticPlanes;
    }

    /// <summary>Validates analysis geometry, records, and diagnostic planes.</summary>
    /// <returns>All analysis validation findings.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (Area == null || Area.X < 0 || Area.Y < 0 || Area.Width <= 0 || Area.Height <= 0 || HistogramBinCount < 2)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "Analysis.Geometry", "Analysis requires a nonempty area and at least two histogram bins.");
        }
        if (Statistics == null || Statistics.Length == 0 || DiagnosticPlanes == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "Analysis.Collections", "Analysis requires statistics and a diagnostic-plane collection.");
            return new RawValidationReport(true, messages.ToArray());
        }
        int index = 0;
        while (index < Statistics.Length)
        {
            if (Statistics[index] == null || Statistics[index].Histogram.Length != HistogramBinCount)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "Analysis.Statistics", "Every statistics record must be populated and use the shared histogram geometry.");
            }
            else
            {
                RawModelValidation.AddReport(messages, Statistics[index].Validate());
            }
            index++;
        }
        index = 0;
        while (index < DiagnosticPlanes.Length)
        {
            RawDiagnosticPlane plane = DiagnosticPlanes[index];
            if (plane == null || Area == null || plane.X != Area.X || plane.Y != Area.Y || plane.Width != Area.Width || plane.Height != Area.Height)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "Analysis.DiagnosticPlanes", "Every diagnostic plane must be populated and match the analyzed area.");
            }
            else
            {
                RawModelValidation.AddReport(messages, plane.Validate());
            }
            index++;
        }
        return new RawValidationReport(true, messages.ToArray());
    }
}

/// <summary>Contains one bounded histogram and requested percentile estimates for a logical image channel.</summary>
public class RawHistogramChannel
{
    /// <summary>The zero-based logical channel.</summary>
    public int Channel;
    /// <summary>The stable channel name.</summary>
    public string Name;
    /// <summary>The semantic channel role.</summary>
    public RawChannelKind Kind;
    /// <summary>The number of selected samples represented by the histogram.</summary>
    public long SampleCount;
    /// <summary>The minimum selected sample in its native numeric domain.</summary>
    public double Minimum;
    /// <summary>The maximum selected sample in its native numeric domain.</summary>
    public double Maximum;
    /// <summary>The inclusive lower bound of the numeric domain divided into bins.</summary>
    public double HistogramMinimum;
    /// <summary>The inclusive upper bound of the numeric domain divided into bins.</summary>
    public double HistogramMaximum;
    /// <summary>Counts in fixed, equal-width bins spanning the histogram domain.</summary>
    public long[] Histogram;
    /// <summary>Estimates for the percentile fractions stored on the containing result.</summary>
    public double[] PercentileValues;

    /// <summary>Creates one per-channel histogram result.</summary>
    /// <param name="channel">The zero-based logical channel.</param>
    /// <param name="name">The stable channel name.</param>
    /// <param name="kind">The semantic channel role.</param>
    /// <param name="sampleCount">The selected sample count.</param>
    /// <param name="minimum">The selected sample minimum.</param>
    /// <param name="maximum">The selected sample maximum.</param>
    /// <param name="histogramMinimum">The histogram domain lower bound.</param>
    /// <param name="histogramMaximum">The histogram domain upper bound.</param>
    /// <param name="histogram">The fixed-bin counts.</param>
    /// <param name="percentileValues">The percentile estimates.</param>
    public RawHistogramChannel(int channel, string name, RawChannelKind kind, long sampleCount, double minimum, double maximum, double histogramMinimum, double histogramMaximum, long[] histogram, double[] percentileValues)
    {
        Channel = channel;
        Name = name == null ? string.Empty : name;
        Kind = kind;
        SampleCount = sampleCount;
        Minimum = minimum;
        Maximum = maximum;
        HistogramMinimum = histogramMinimum;
        HistogramMaximum = histogramMaximum;
        Histogram = histogram == null ? System.Array.Empty<long>() : histogram;
        PercentileValues = percentileValues == null ? System.Array.Empty<double>() : percentileValues;
    }

    /// <summary>Validates channel identity, numeric bounds, histogram counts, and percentile estimates.</summary>
    /// <returns>All per-channel histogram validation findings.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (Channel < 0 || Name == null || Name.Length == 0 || Kind < RawChannelKind.Unknown || Kind > RawChannelKind.Custom)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "HistogramChannel.Identity", "A histogram channel requires a nonnegative index, name, and defined channel role.");
        }
        if (SampleCount <= 0 || !RawModelValidation.IsFinite(Minimum) || !RawModelValidation.IsFinite(Maximum) || !RawModelValidation.IsFinite(HistogramMinimum) || !RawModelValidation.IsFinite(HistogramMaximum) || Minimum > Maximum || HistogramMinimum > Minimum || HistogramMaximum < Maximum)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "HistogramChannel.Bounds", "Histogram bounds and selected sample extrema must be finite, ordered, and contain a positive sample count.");
        }
        if (Histogram == null || Histogram.Length < 2)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "HistogramChannel.Histogram", "A channel histogram requires at least two bins.");
        }
        else
        {
            long total = 0;
            int index = 0;
            while (index < Histogram.Length)
            {
                if (Histogram[index] < 0 || total > long.MaxValue - Histogram[index])
                {
                    RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "HistogramChannel.Histogram", "Histogram counts must be nonnegative and cannot overflow.");
                    total = -1;
                    break;
                }
                total += Histogram[index];
                index++;
            }
            if (total >= 0 && total != SampleCount)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "HistogramChannel.Histogram", "Histogram counts must sum to the selected sample count.");
            }
        }
        if (PercentileValues == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "HistogramChannel.Percentiles", "The percentile-value collection cannot be null.");
        }
        else
        {
            int index = 0;
            while (index < PercentileValues.Length)
            {
                double value = PercentileValues[index];
                if (!RawModelValidation.IsFinite(value) || value < Minimum || value > Maximum)
                {
                    RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "HistogramChannel.Percentiles", "Every percentile estimate must be finite and lie within the selected sample range.");
                    break;
                }
                index++;
            }
        }
        return new RawValidationReport(true, messages.ToArray());
    }
}

/// <summary>Contains bounded per-channel histograms and percentile estimates for one selected image region.</summary>
public class RawHistogramResult
{
    /// <summary>The selected region in current image coordinates.</summary>
    public RawArea Area;
    /// <summary>The fixed histogram bin count shared by every channel.</summary>
    public int HistogramBinCount;
    /// <summary>The requested percentile fractions in caller-specified order.</summary>
    public double[] Percentiles;
    /// <summary>Histogram results in pixel-interleaved logical channel order.</summary>
    public RawHistogramChannel[] Channels;

    /// <summary>Creates one multi-channel histogram result.</summary>
    /// <param name="area">The selected image region.</param>
    /// <param name="histogramBinCount">The shared fixed bin count.</param>
    /// <param name="percentiles">The requested percentile fractions.</param>
    /// <param name="channels">The per-channel results.</param>
    public RawHistogramResult(RawArea area, int histogramBinCount, double[] percentiles, RawHistogramChannel[] channels)
    {
        Area = area;
        HistogramBinCount = histogramBinCount;
        Percentiles = percentiles == null ? System.Array.Empty<double>() : percentiles;
        Channels = channels == null ? System.Array.Empty<RawHistogramChannel>() : channels;
    }

    /// <summary>Validates selected geometry, percentile fractions, and consistent channel results.</summary>
    /// <returns>All histogram-result validation findings.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (Area == null || Area.Kind <= RawAreaKind.Unknown || Area.Kind > RawAreaKind.OpticalBlack || Area.X < 0 || Area.Y < 0 || Area.Width <= 0 || Area.Height <= 0 || HistogramBinCount < 2)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "Histogram.Geometry", "A histogram result requires a nonempty defined area and at least two bins.");
        }
        if (Percentiles == null || Percentiles.Length > 256)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "Histogram.Percentiles", "The percentile collection cannot be null or contain more than 256 entries.");
        }
        else
        {
            int index = 0;
            while (index < Percentiles.Length)
            {
                if (!RawModelValidation.IsFinite(Percentiles[index]) || Percentiles[index] < 0.0 || Percentiles[index] > 1.0)
                {
                    RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "Histogram.Percentiles", "Every percentile fraction must be finite and between zero and one.");
                    break;
                }
                index++;
            }
        }
        if (Channels == null || Channels.Length == 0 || Channels.Length > 64)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "Histogram.Channels", "A histogram result requires one through sixty-four channels.");
        }
        else
        {
            int index = 0;
            while (index < Channels.Length)
            {
                RawHistogramChannel channel = Channels[index];
                if (channel == null || channel.Channel != index || channel.Histogram == null || channel.Histogram.Length != HistogramBinCount || channel.PercentileValues == null || Percentiles == null || channel.PercentileValues.Length != Percentiles.Length)
                {
                    RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "Histogram.Channels", "Every channel must be populated in order and match the shared bin and percentile geometry.");
                }
                else
                {
                    RawModelValidation.AddReport(messages, channel.Validate());
                }
                index++;
            }
        }
        return new RawValidationReport(true, messages.ToArray());
    }
}
